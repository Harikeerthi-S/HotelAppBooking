using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class CancellationServiceTests
{
    private readonly Mock<IRepository<int, Cancellation>> _cancelRepo  = new();
    private readonly Mock<IRepository<int, Booking>>      _bookingRepo = new();
    private readonly Mock<IAuditLogService>               _audit       = new();
    private readonly Mock<ILogger<CancellationService>>   _logger      = new();

    private CancellationService CreateService() =>
        new(_cancelRepo.Object, _bookingRepo.Object, _audit.Object, _logger.Object);

    // CreateCancellationDto has: BookingId, Reason (no UserId)
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCancellationDto()
    {
        var booking = new Booking { BookingId = 1, UserId = 1, Status = "Confirmed" };
        var dto     = new CreateCancellationDto { BookingId = 1, Reason = "Change of plans" };

        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _cancelRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cancellation, bool>>>()))
                   .ReturnsAsync(false);
        _cancelRepo.Setup(r => r.AddAsync(It.IsAny<Cancellation>()))
                   .ReturnsAsync((Cancellation c) => { c.CancellationId = 1; return c; });
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateAsync_BookingNotFound_ThrowsNotFoundException()
    {
        _bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);
        var dto = new CreateCancellationDto { BookingId = 99, Reason = "Test" };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_AlreadyCancelled_ThrowsBadRequestException()
    {
        var booking = new Booking { BookingId = 1, UserId = 1, Status = "Cancelled" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

        var dto = new CreateCancellationDto { BookingId = 1, Reason = "Test" };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateStatusAsync_ApproveCancellation_ReturnsUpdatedDto()
    {
        var cancellation = new Cancellation { CancellationId = 1, BookingId = 1, Status = "Pending", RefundAmount = 0 };
        var booking      = new Booking { BookingId = 1, Status = "Confirmed" };

        _cancelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cancellation);
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _cancelRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Cancellation>())).ReturnsAsync(cancellation);
        _bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().UpdateStatusAsync(1, "Approved", 2000);

        Assert.NotNull(result);
        Assert.Equal("Approved", result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NotFound_ThrowsNotFoundException()
    {
        _cancelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cancellation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().UpdateStatusAsync(99, "Approved", 0));
    }
}
