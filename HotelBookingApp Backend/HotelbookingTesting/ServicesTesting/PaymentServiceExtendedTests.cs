using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class PaymentServiceExtendedTests
{
    private readonly Mock<IRepository<int, Payment>> _paymentRepo = new();
    private readonly Mock<IRepository<int, Booking>> _bookingRepo = new();
    private readonly Mock<IAuditLogService>          _audit       = new();
    private readonly Mock<ILogger<PaymentService>>   _logger      = new();

    private PaymentService CreateService() =>
        new(_paymentRepo.Object, _bookingRepo.Object, _audit.Object, _logger.Object);

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllPayments()
    {
        var payments = new List<Payment>
        {
            new() { PaymentId = 1, BookingId = 1, Amount = 3000, PaymentMethod = "UPI",    PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow },
            new() { PaymentId = 2, BookingId = 2, Amount = 5000, PaymentMethod = "Card",   PaymentStatus = "Pending",   CreatedAt = DateTime.UtcNow },
            new() { PaymentId = 3, BookingId = 3, Amount = 2000, PaymentMethod = "Wallet", PaymentStatus = "Refunded",  CreatedAt = DateTime.UtcNow }
        };
        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedPayments()
    {
        var payments = Enumerable.Range(1, 25).Select(i => new Payment
        {
            PaymentId = i, BookingId = i, Amount = 1000 * i,
            PaymentMethod = "UPI", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow
        }).ToList();

        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);

        var result = await CreateService().GetPagedAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(25, result.TotalRecords);
        Assert.Equal(3, result.TotalPages);
    }

    // ── GetPagedByUserAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByUserAsync_ReturnsUserPayments()
    {
        var bookings = new List<Booking>
        {
            new() { BookingId = 1, UserId = 3 },
            new() { BookingId = 2, UserId = 3 },
            new() { BookingId = 3, UserId = 9 }  // different user
        };
        var payments = new List<Payment>
        {
            new() { PaymentId = 1, BookingId = 1, Amount = 2000, PaymentMethod = "UPI",  PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow },
            new() { PaymentId = 2, BookingId = 2, Amount = 3000, PaymentMethod = "Card", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow },
            new() { PaymentId = 3, BookingId = 3, Amount = 1000, PaymentMethod = "UPI",  PaymentStatus = "Pending",   CreatedAt = DateTime.UtcNow }
        };

        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);
        _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);

        var result = await CreateService().GetPagedByUserAsync(3, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalRecords);
    }

    // ── UpdateStatusAsync edge cases ──────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_InvalidStatus_ThrowsBadRequestException()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService().UpdateStatusAsync(1, "InvalidStatus"));
    }

    [Fact]
    public async Task UpdateStatusAsync_NotFound_ThrowsNotFoundException()
    {
        _paymentRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Payment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().UpdateStatusAsync(99, "Completed"));
    }

    // ── MakePaymentAsync edge cases ───────────────────────────────────────

    [Fact]
    public async Task MakePaymentAsync_ZeroAmount_ThrowsBadRequestException()
    {
        var booking = new Booking { BookingId = 1, TotalAmount = 1000, Status = "Pending" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService().MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 0, PaymentMethod = "UPI" }));
    }
}
