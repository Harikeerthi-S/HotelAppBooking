using HotelBookingApp.Delegates;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IAuditLogService          _audit;
        private readonly ILogger<PaymentService>   _logger;

        private readonly PaymentStatusResolverDelegate _statusResolver =
            AppDelegateFactory.DefaultPaymentStatusResolver;

        private static readonly string[] ValidStatuses =
            { "Pending", "Completed", "Failed", "Refunded" };

        private static readonly string[] ValidMethods =
            { "CreditCard", "DebitCard", "NetBanking" };

        public PaymentService(
            IRepository<int, Payment> paymentRepo,
            IRepository<int, Booking> bookingRepo,
            IAuditLogService          audit,
            ILogger<PaymentService>   logger)
        {
            _paymentRepo = paymentRepo;
            _bookingRepo = bookingRepo;
            _audit       = audit;
            _logger      = logger;
        }

        private async Task LogAsync(string action, int? entityId, int? userId = null, string? changes = null)
        {
            try { await _audit.CreateAsync(new CreateAuditLogDto { UserId = userId, Action = action, EntityName = "Payment", EntityId = entityId, Changes = changes }); }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed: {Action}", action); }
        }

        // ─────────────────────────────────────────────
        // ✅ MAKE PAYMENT
        // ─────────────────────────────────────────────
        public async Task<PaymentResponseDto> MakePaymentAsync(PaymentDto dto)
        {
            _logger.LogInformation("Processing payment for BookingId={BookingId}", dto.BookingId);

            var booking = await _bookingRepo.GetByIdAsync(dto.BookingId)
                          ?? throw new NotFoundException("Booking", dto.BookingId);

            // 🔥 Prevent duplicate payment
            var existingPayments = await _paymentRepo.GetAllAsync();
            if (existingPayments.Any(p =>
                p.BookingId == dto.BookingId &&
                p.PaymentStatus == "Completed"))
            {
                throw new BadRequestException("Payment already completed for this booking.");
            }

            if (!ValidMethods.Contains(dto.PaymentMethod))
                throw new BadRequestException($"Invalid payment method: {dto.PaymentMethod}");

            if (dto.Amount <= 0)
                throw new BadRequestException("Amount must be greater than zero.");

            // 🔥 Calculate status using delegate
            var status = _statusResolver(dto.PaymentMethod, dto.Amount, booking.TotalAmount);

            var payment = new Payment
            {
                BookingId = dto.BookingId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = status,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _paymentRepo.AddAsync(payment);

            // Sync booking status — use clean copy to avoid EF tracking conflicts
            try
            {
                var bookingCopy = new Booking
                {
                    BookingId     = booking.BookingId,
                    UserId        = booking.UserId,
                    HotelId       = booking.HotelId,
                    RoomId        = booking.RoomId,
                    NumberOfRooms = booking.NumberOfRooms,
                    CheckIn       = booking.CheckIn,
                    CheckOut      = booking.CheckOut,
                    TotalAmount   = booking.TotalAmount,
                    Status        = status switch
                    {
                        "Completed" => "Confirmed",
                        "Failed"    => "Pending",
                        _           => booking.Status
                    }
                };
                await _bookingRepo.UpdateAsync(booking.BookingId, bookingCopy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not sync booking status after payment {PaymentId}", created.PaymentId);
            }

            _logger.LogInformation("Payment created: {PaymentId} Status={Status}",
                created.PaymentId, created.PaymentStatus);
            await LogAsync("PaymentCreated", created.PaymentId, booking.UserId,
                $"Booking:{dto.BookingId} ₹{dto.Amount} {dto.PaymentMethod} Status:{status}");
            return MapToDto(created);
        }

        // ─────────────────────────────────────────────
        // ✅ GET BY PAYMENT ID
        // ─────────────────────────────────────────────
        public async Task<PaymentResponseDto?> GetByIdAsync(int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                          ?? throw new NotFoundException("Payment", paymentId);

            return MapToDto(payment);
        }

        // ─────────────────────────────────────────────
        // 🔥 GET BY BOOKING ID (FIXED)
        // ─────────────────────────────────────────────
        public async Task<PaymentResponseDto?> GetByBookingIdAsync(int bookingId)
        {
            _logger.LogInformation("Fetching payment for BookingId={BookingId}", bookingId);

            var payments = await _paymentRepo.GetAllAsync();

            var payment = payments
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (payment == null)
                throw new NotFoundException("Payment for Booking", bookingId);

            return MapToDto(payment);
        }

        // ─────────────────────────────────────────────
        // ✅ GET ALL
        // ─────────────────────────────────────────────
        public async Task<IEnumerable<PaymentResponseDto>> GetAllAsync()
        {
            var payments = await _paymentRepo.GetAllAsync();

            return payments
                .OrderByDescending(p => p.CreatedAt)
                .Select(MapToDto)
                .ToList();
        }

        // ─────────────────────────────────────────────
        // ✅ GET PAGED
        // ─────────────────────────────────────────────
        public async Task<PagedResponseDto<PaymentResponseDto>> GetPagedAsync(PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize = Math.Clamp(request.PageSize, 1, 10);

            var all = await _paymentRepo.GetAllAsync();

            var data = all
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<PaymentResponseDto>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = all.Count(),
                TotalPages   = (int)Math.Ceiling((double)all.Count() / request.PageSize)
            };
        }

        // ─────────────────────────────────────────────
        // ✅ GET PAGED BY USER (payments for bookings owned by user)
        // ─────────────────────────────────────────────
        public async Task<PagedResponseDto<PaymentResponseDto>> GetPagedByUserAsync(int userId, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 100);

            var allPayments = await _paymentRepo.GetAllAsync();
            var allBookings = await _bookingRepo.GetAllAsync();
            var userBookingIds = allBookings.Where(b => b.UserId == userId).Select(b => b.BookingId).ToHashSet();

            var filtered = allPayments
                .Where(p => userBookingIds.Contains(p.BookingId))
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var total = filtered.Count;
            var data    = filtered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<PaymentResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ─────────────────────────────────────────────
        // ✅ UPDATE STATUS
        // ─────────────────────────────────────────────
        public async Task<PaymentResponseDto?> UpdateStatusAsync(int paymentId, string status)
        {
            _logger.LogInformation("Updating PaymentId={PaymentId} to {Status}", paymentId, status);

            if (!ValidStatuses.Contains(status))
                throw new BadRequestException($"Invalid status: {status}");

            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                          ?? throw new NotFoundException("Payment", paymentId);

            // Create a clean copy without navigation properties to avoid EF tracking conflicts
            var updated = new Payment
            {
                PaymentId     = payment.PaymentId,
                BookingId     = payment.BookingId,
                Amount        = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = status,
                CreatedAt     = payment.CreatedAt
            };

            await _paymentRepo.UpdateAsync(paymentId, updated);
            await LogAsync("PaymentStatusUpdated", paymentId, null, $"Status→{status}");

            // Sync booking status
            try
            {
                var booking = await _bookingRepo.GetByIdAsync(payment.BookingId);
                if (booking != null)
                {
                    var newBookingStatus = status switch
                    {
                        "Completed" => "Confirmed",
                        "Failed"    => "Pending",
                        "Refunded"  => "Cancelled",
                        _           => booking.Status
                    };

                    if (booking.Status != newBookingStatus)
                    {
                        var bookingCopy = new Booking
                        {
                            BookingId     = booking.BookingId,
                            UserId        = booking.UserId,
                            HotelId       = booking.HotelId,
                            RoomId        = booking.RoomId,
                            NumberOfRooms = booking.NumberOfRooms,
                            CheckIn       = booking.CheckIn,
                            CheckOut      = booking.CheckOut,
                            TotalAmount   = booking.TotalAmount,
                            Status        = newBookingStatus
                        };
                        await _bookingRepo.UpdateAsync(booking.BookingId, bookingCopy);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not sync booking status for payment {PaymentId}", paymentId);
            }

            return MapToDto(updated);
        }

        // ─────────────────────────────────────────────
        // ✅ MAPPER
        // ─────────────────────────────────────────────
        private static PaymentResponseDto MapToDto(Payment p) => new()
        {
            PaymentId = p.PaymentId,
            BookingId = p.BookingId,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentStatus = p.PaymentStatus,
            CreatedAt = p.CreatedAt
        };
    }
}
