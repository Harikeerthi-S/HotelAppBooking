using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class PaymentServiceTests
{
    private readonly Mock<IRepository<int, Payment>> _paymentRepo = new();
    private readonly Mock<IRepository<int, Booking>> _bookingRepo = new();
    private readonly Mock<IAuditLogService>          _audit       = new();
    private readonly Mock<ILogger<PaymentService>>   _logger      = new();

    // PaymentService(paymentRepo, bookingRepo, audit, logger)
    private PaymentService CreateService() =>
        new(_paymentRepo.Object, _bookingRepo.Object, _audit.Object, _logger.Object);

    private static PaymentDto ValidDto() => new()
    {
        BookingId     = 1,
        Amount        = 5000,
        PaymentMethod = "UPI"
    };

    [Fact]
    public async Task MakePaymentAsync_ValidPayment_ReturnsPaymentResponseDto()
    {
        var booking = new Booking { BookingId = 1, UserId = 1, TotalAmount = 5000, Status = "Pending" };

        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                    .ReturnsAsync((Payment p) => { p.PaymentId = 10; return p; });
        _bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().MakePaymentAsync(ValidDto());

        Assert.NotNull(result);
        Assert.Equal(10, result.PaymentId);
        Assert.Equal("UPI", result.PaymentMethod);
    }

    [Fact]
    public async Task MakePaymentAsync_BookingNotFound_ThrowsNotFoundException()
    {
        _bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);
        var dto = new PaymentDto { BookingId = 99, Amount = 1000, PaymentMethod = "UPI" };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().MakePaymentAsync(dto));
    }

    [Fact]
    public async Task MakePaymentAsync_AlreadyPaid_ThrowsBadRequestException()
    {
        var booking  = new Booking { BookingId = 1, TotalAmount = 1000, Status = "Confirmed" };
        var existing = new Payment { PaymentId = 1, BookingId = 1, PaymentStatus = "Completed" };

        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment> { existing });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService().MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 1000, PaymentMethod = "UPI" }));
    }

    [Fact]
    public async Task MakePaymentAsync_InvalidMethod_ThrowsBadRequestException()
    {
        var booking = new Booking { BookingId = 1, TotalAmount = 1000, Status = "Pending" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService().MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 1000, PaymentMethod = "Bitcoin" }));
    }

    [Fact]
    public async Task GetByBookingIdAsync_ExistingPayment_ReturnsDto()
    {
        var payments = new List<Payment>
        {
            new() { PaymentId = 1, BookingId = 1, Amount = 3000, PaymentMethod = "UPI", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow }
        };
        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);

        var result = await CreateService().GetByBookingIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Completed", result!.PaymentStatus);
    }

    [Fact]
    public async Task GetByBookingIdAsync_NotFound_ThrowsNotFoundException()
    {
        _paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByBookingIdAsync(99));
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidStatus_ReturnsUpdatedDto()
    {
        var payment = new Payment { PaymentId = 1, BookingId = 1, PaymentStatus = "Pending", Amount = 2000, PaymentMethod = "UPI" };
        var booking = new Booking { BookingId = 1, Status = "Pending" };

        _paymentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        _paymentRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().UpdateStatusAsync(1, "Completed");

        Assert.NotNull(result);
    }
}
