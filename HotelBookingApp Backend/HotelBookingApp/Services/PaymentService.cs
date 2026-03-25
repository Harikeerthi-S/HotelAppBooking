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
        private readonly ILogger<PaymentService> _logger;

        private readonly PaymentStatusResolverDelegate _statusResolver =
            AppDelegateFactory.DefaultPaymentStatusResolver;

        private static readonly string[] ValidStatuses =
            { "Pending", "Completed", "Failed", "Refunded" };

        private static readonly string[] ValidMethods =
            { "CreditCard", "DebitCard", "UPI", "Wallet", "PayPal", "NetBanking" };

        public PaymentService(
            IRepository<int, Payment> paymentRepo,
            IRepository<int, Booking> bookingRepo,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _bookingRepo = bookingRepo;
            _logger = logger;
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

            // 🔥 Sync booking status
            booking.Status = status switch
            {
                "Completed" => "Confirmed",
                "Failed" => "Payment Failed",
                _ => booking.Status
            };

            await _bookingRepo.UpdateAsync(booking.BookingId, booking);

            _logger.LogInformation("Payment created: {PaymentId} Status={Status}",
                created.PaymentId, created.PaymentStatus);

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
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

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
                TotalRecords = all.Count()
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

            payment.PaymentStatus = status;

            await _paymentRepo.UpdateAsync(paymentId, payment);

            // 🔥 Sync booking
            var booking = await _bookingRepo.GetByIdAsync(payment.BookingId);

            if (booking != null)
            {
                booking.Status = status switch
                {
                    "Completed" => "Confirmed",
                    "Failed" => "Payment Failed",
                    "Refunded" => "Refunded",
                    _ => booking.Status
                };

                await _bookingRepo.UpdateAsync(booking.BookingId, booking);
            }

            return MapToDto(payment);
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