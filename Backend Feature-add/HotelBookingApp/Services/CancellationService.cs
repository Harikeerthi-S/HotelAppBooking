using HotelBookingApp.Context;
using HotelBookingApp.Delegates;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBookingApp.Services
{
    public class CancellationService : ICancellationService
    {
        private readonly HotelBookingContext            _context;
        private readonly IWalletService                 _walletService;
        private readonly IServiceScopeFactory           _scopeFactory;
        private readonly ILogger<CancellationService>   _logger;

        private readonly RefundCalculatorDelegate _refundCalculator = AppDelegateFactory.StandardRefundPolicy;

        private static readonly string[] ValidStatuses =
            { "Pending", "Approved", "Rejected", "Refunded" };

        public CancellationService(
            HotelBookingContext          context,
            IWalletService              walletService,
            IServiceScopeFactory        scopeFactory,
            ILogger<CancellationService> logger)
        {
            _context       = context;
            _walletService = walletService;
            _scopeFactory  = scopeFactory;
            _logger        = logger;
        }

        // ── Audit log via isolated scope — never touches the main DbContext ──
        private void Log(string action, int? entityId, int? userId = null, string? changes = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var audit = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                    await audit.CreateAsync(new CreateAuditLogDto
                    {
                        UserId = userId, Action = action, EntityName = "Cancellation",
                        EntityId = entityId, Changes = changes
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Audit log failed for action {Action}", action);
                }
            });
        }

        // ── CREATE ─────────────────────────────
        public async Task<CancellationResponseDto> CreateAsync(CreateCancellationDto dto)
        {
            var booking = await _context.Bookings.FindAsync(dto.BookingId)
                ?? throw new NotFoundException("Booking", dto.BookingId);

            if (booking.Status != "Confirmed" && booking.Status != "Pending")
                throw new BadRequestException("Only Pending/Confirmed bookings can be cancelled.");

            var hoursUntilCheckIn = (booking.CheckIn - DateTime.UtcNow).TotalHours;
            var refundAmount      = _refundCalculator(booking.TotalAmount, hoursUntilCheckIn);
            var refundPolicy      = AppDelegateFactory.RefundTierLabel(hoursUntilCheckIn);
            var refundPercent     = AppDelegateFactory.RefundPercent(hoursUntilCheckIn);

            var cancellation = new Cancellation
            {
                BookingId        = dto.BookingId,
                Reason           = dto.Reason.Trim(),
                RefundAmount     = refundAmount,
                Status           = "Pending",
                CancellationDate = DateTime.UtcNow
            };

            _context.Cancellations.Add(cancellation);
            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            Log("CancellationRequested", cancellation.CancellationId, booking.UserId,
                $"Booking:{dto.BookingId} Policy:{refundPolicy} Refund:₹{refundAmount}");

            return MapToDto(cancellation, refundPolicy, refundPercent, walletCredited: false);
        }

        // ── GET BY ID ──────────────────────────
        public async Task<CancellationResponseDto?> GetByIdAsync(int cancellationId)
        {
            var c = await _context.Cancellations.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.CancellationId == cancellationId)
                    ?? throw new NotFoundException("Cancellation", cancellationId);
            return MapToDto(c);
        }

        // ── GET ALL (PAGED) ────────────────────
        public async Task<PagedResponseDto<CancellationResponseDto>> GetAllAsync(PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 10);

            var all = await _context.Cancellations.AsNoTracking()
                          .OrderByDescending(c => c.CancellationDate)
                          .ToListAsync();

            var total = all.Count;
            var paged = all
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => MapToDto(c))
                .ToList();

            return new PagedResponseDto<CancellationResponseDto>
            {
                Data         = paged,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── GET BY USER ────────────────────────
        public async Task<PagedResponseDto<CancellationResponseDto>> GetByUserAsync(int userId, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 100);

            var bookingIds = await _context.Bookings.AsNoTracking()
                .Where(b => b.UserId == userId)
                .Select(b => b.BookingId)
                .ToHashSetAsync();

            var all = await _context.Cancellations.AsNoTracking()
                .Where(c => bookingIds.Contains(c.BookingId))
                .OrderByDescending(c => c.CancellationDate)
                .ToListAsync();

            var total = all.Count;
            var data  = all
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => MapToDto(c))
                .ToList();

            return new PagedResponseDto<CancellationResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── UPDATE STATUS ──────────────────────
        public async Task<CancellationResponseDto> UpdateStatusAsync(int cancellationId, string status, decimal refundAmount = 0)
        {
            if (!ValidStatuses.Contains(status))
                throw new BadRequestException("Invalid status");

            // All DB work in one sequential flow — no concurrent operations
            var cancellation = await _context.Cancellations.FindAsync(cancellationId)
                ?? throw new NotFoundException("Cancellation", cancellationId);

            cancellation.Status = status;
            if (refundAmount > 0)
                cancellation.RefundAmount = refundAmount;

            // Sync booking to Refunded if needed
            if (status == "Refunded")
            {
                var booking = await _context.Bookings.FindAsync(cancellation.BookingId);
                if (booking != null)
                    booking.Status = "Refunded";
            }

            // Single SaveChangesAsync — no concurrent operations on this context
            await _context.SaveChangesAsync();

            // Audit log via isolated scope (Task.Run) — never touches _context
            Log("CancellationStatusUpdated", cancellationId, null,
                $"Status→{status} Refund:₹{cancellation.RefundAmount}");

            bool walletCredited = false;

            // Wallet credit via WalletService (uses its own isolated scope)
            if ((status == "Approved" || status == "Refunded") && cancellation.RefundAmount > 0)
            {
                // Read booking with AsNoTracking — no tracking conflict
                var booking = await _context.Bookings.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BookingId == cancellation.BookingId);

                if (booking != null)
                {
                    try
                    {
                        await _walletService.CreditAsync(
                            userId:      booking.UserId,
                            amount:      cancellation.RefundAmount,
                            description: $"Refund for Booking #{booking.BookingId} — Cancellation #{cancellationId}",
                            referenceId: cancellationId
                        );
                        walletCredited = true;
                        _logger.LogInformation(
                            "Wallet credited ₹{Amount} for User={UserId} Cancellation={CancellationId}",
                            cancellation.RefundAmount, booking.UserId, cancellationId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Wallet credit failed for Cancellation={CancellationId} — status still updated",
                            cancellationId);
                    }
                }
            }

            return MapToDto(cancellation, walletCredited: walletCredited);
        }

        // ── MAPPER ─────────────────────────────
        private static CancellationResponseDto MapToDto(
            Cancellation c,
            string? refundPolicy   = null,
            int     refundPercent  = 0,
            bool    walletCredited = false) => new()
        {
            CancellationId   = c.CancellationId,
            BookingId        = c.BookingId,
            Reason           = c.Reason,
            RefundAmount     = c.RefundAmount,
            Status           = c.Status,
            CancellationDate = c.CancellationDate,
            RefundPolicy     = refundPolicy  ?? string.Empty,
            RefundPercent    = refundPercent,
            WalletCredited   = walletCredited
        };
    }
}
