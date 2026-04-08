using HotelBookingApp.Delegates;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    public class CancellationService : ICancellationService
    {
        private readonly IRepository<int, Cancellation> _cancellationRepo;
        private readonly IRepository<int, Booking>      _bookingRepo;
        private readonly IRepository<int, User>         _userRepo;
        private readonly INotificationService           _notificationService;
        private readonly IWalletService                 _walletService;
        private readonly IAuditLogService               _audit;
        private readonly ILogger<CancellationService>   _logger;

        private readonly RefundCalculatorDelegate _refundCalculator = AppDelegateFactory.StandardRefundPolicy;

        private static readonly string[] ValidStatuses =
            { "Pending", "Approved", "Rejected", "Refunded" };

        public CancellationService(
            IRepository<int, Cancellation> cancellationRepo,
            IRepository<int, Booking>      bookingRepo,
            IRepository<int, User>         userRepo,
            INotificationService           notificationService,
            IWalletService                 walletService,
            IAuditLogService               audit,
            ILogger<CancellationService>   logger)
        {
            _cancellationRepo    = cancellationRepo;
            _bookingRepo         = bookingRepo;
            _userRepo            = userRepo;
            _notificationService = notificationService;
            _walletService       = walletService;
            _audit               = audit;
            _logger              = logger;
        }

        private async Task LogAsync(string action, int? entityId, int? userId = null, string? changes = null)
        {
            try
            {
                await _audit.CreateAsync(new CreateAuditLogDto
                {
                    UserId = userId, Action = action, EntityName = "Cancellation",
                    EntityId = entityId, Changes = changes
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed for action {Action}", action);
            }
        }

        // ── CREATE ─────────────────────────────
        public async Task<CancellationResponseDto> CreateAsync(CreateCancellationDto dto)
        {
            var booking = await _bookingRepo.GetByIdAsync(dto.BookingId)
                ?? throw new NotFoundException("Booking", dto.BookingId);

            if (booking.Status != "Confirmed" && booking.Status != "Pending")
                throw new BadRequestException("Only Pending/Confirmed bookings can be cancelled.");

            var hoursUntilCheckIn = (booking.CheckIn - DateTime.UtcNow).TotalHours;
            var refundAmount = _refundCalculator(booking.TotalAmount, hoursUntilCheckIn);

            var cancellation = new Cancellation
            {
                BookingId = dto.BookingId,
                Reason = dto.Reason.Trim(),
                RefundAmount = refundAmount,
                Status = "Pending",
                CancellationDate = DateTime.UtcNow
            };

            await _cancellationRepo.AddAsync(cancellation);
            booking.Status = "Cancelled";
            await _bookingRepo.UpdateAsync(booking.BookingId, booking);
            await LogAsync("CancellationRequested", cancellation.CancellationId, booking.UserId,
                $"Booking:{dto.BookingId} Refund:₹{refundAmount}");

            // Auto-credit wallet if refund applies
            if (refundAmount > 0)
            {
                await _walletService.CreditAsync(
                    booking.UserId,
                    refundAmount,
                    $"Refund for Booking #{booking.BookingId} cancellation",
                    cancellation.CancellationId);

                cancellation.Status = "Refunded";
                await _cancellationRepo.UpdateAsync(cancellation.CancellationId, cancellation);
                booking.Status = "Refunded";
                await _bookingRepo.UpdateAsync(booking.BookingId, booking);

                await LogAsync("AutoRefundCredited", cancellation.CancellationId, booking.UserId,
                    $"₹{refundAmount} credited to wallet of User:{booking.UserId}");

                // Notify the user about their refund
                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId  = booking.UserId,
                        Message = $"✅ Refund Processed: ₹{refundAmount:N2} has been credited to your Wallet for cancellation of Booking #{booking.BookingId}. Check your Wallet Balance in the dashboard."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send refund notification to User:{UserId}", booking.UserId);
                }
            }
            else
            {
                // Notify user that no refund applies
                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId  = booking.UserId,
                        Message = $"❌ Booking #{booking.BookingId} has been cancelled. No refund is applicable as per our cancellation policy (cancellation more than 5 days before check-in)."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send cancellation notification to User:{UserId}", booking.UserId);
                }
            }

            // Notify all hotel managers automatically
            await NotifyHotelManagersAsync(booking, cancellation, refundAmount);

            return MapToDto(cancellation);
        }

        // ── GET BY ID ──────────────────────────
        public async Task<CancellationResponseDto?> GetByIdAsync(int cancellationId)
        {
            var c = await _cancellationRepo.GetByIdAsync(cancellationId)
                ?? throw new NotFoundException("Cancellation", cancellationId);

            return MapToDto(c);
        }

        // ── GET ALL (PAGED) ✅ FIX ─────────────
        public async Task<PagedResponseDto<CancellationResponseDto>> GetAllAsync(PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize = Math.Clamp(request.PageSize, 1, 10);

            var all = await _cancellationRepo.GetAllAsync();

            var ordered = all
                .OrderByDescending(c => c.CancellationDate)
                .ToList();

            var total = ordered.Count;

            var paged = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<CancellationResponseDto>
            {
                Data = paged,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── GET BY USER ────────────────────────
        public async Task<PagedResponseDto<CancellationResponseDto>> GetByUserAsync(int userId, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            var bookings = await _bookingRepo.FindAllAsync(b => b.UserId == userId);
            var bookingIds = bookings.Select(b => b.BookingId).ToHashSet();

            var cancellations = await _cancellationRepo.FindAllAsync(
                c => bookingIds.Contains(c.BookingId)
            );

            var ordered = cancellations.OrderByDescending(c => c.CancellationDate).ToList();

            var total = ordered.Count;

            var data = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<CancellationResponseDto>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── UPDATE STATUS ──────────────────────
        public async Task<CancellationResponseDto> UpdateStatusAsync(int cancellationId, string status, decimal refundAmount = 0)
        {
            if (!ValidStatuses.Contains(status))
                throw new BadRequestException("Invalid status");

            var cancellation = await _cancellationRepo.GetByIdAsync(cancellationId)
                ?? throw new NotFoundException("Cancellation", cancellationId);

            cancellation.Status = status;

            if (refundAmount > 0)
                cancellation.RefundAmount = refundAmount;

            await _cancellationRepo.UpdateAsync(cancellationId, cancellation);
            await LogAsync("CancellationStatusUpdated", cancellationId, null, $"Status→{status} Refund:₹{cancellation.RefundAmount}");

            if (status == "Refunded")
            {
                var booking = await _bookingRepo.GetByIdAsync(cancellation.BookingId);
                if (booking != null)
                {
                    booking.Status = "Refunded";
                    await _bookingRepo.UpdateAsync(booking.BookingId, booking);
                }
            }

            return MapToDto(cancellation);
        }

        // ── NOTIFY HOTEL MANAGERS ───────────────
        private async Task NotifyHotelManagersAsync(Booking booking, Cancellation cancellation, decimal refundAmount)
        {
            var managers = await _userRepo.FindAllAsync(u => u.Role == "hotelmanager");
            var refundNote = refundAmount > 0
                ? $"Refund: ₹{refundAmount:N2}"
                : "No refund applicable";

            var message = $"Cancellation Alert: Booking #{booking.BookingId} has been cancelled. " +
                          $"Hotel #{booking.HotelId} | Room #{booking.RoomId} | {refundNote}.";

            foreach (var manager in managers)
            {
                try
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId  = manager.UserId,
                        Message = message
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify hotel manager {ManagerId}", manager.UserId);
                }
            }
        }

        // ── HELPER ─────────────────────────────
        private static CancellationResponseDto MapToDto(Cancellation c) => new()
        {
            CancellationId = c.CancellationId,
            BookingId = c.BookingId,
            Reason = c.Reason,
            RefundAmount = c.RefundAmount,
            Status = c.Status,
            CancellationDate = c.CancellationDate
        };
    }
}
