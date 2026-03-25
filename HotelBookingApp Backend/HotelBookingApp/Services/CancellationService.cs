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
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly ILogger<CancellationService> _logger;

        private readonly RefundCalculatorDelegate _refundCalculator =
            AppDelegateFactory.StandardRefundPolicy;

        private static readonly string[] ValidStatuses =
            { "Pending", "Approved", "Rejected", "Refunded" };

        public CancellationService(
            IRepository<int, Cancellation> cancellationRepo,
            IRepository<int, Booking> bookingRepo,
            ILogger<CancellationService> logger)
        {
            _cancellationRepo = cancellationRepo;
            _bookingRepo = bookingRepo;
            _logger = logger;
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
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

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
                TotalRecords = total
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
                TotalRecords = total
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