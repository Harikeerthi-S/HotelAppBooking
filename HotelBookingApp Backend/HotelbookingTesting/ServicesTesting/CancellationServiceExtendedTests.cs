using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class CancellationServiceExtendedTests
{
    private readonly Mock<IRepository<int, Cancellation>> _cancelRepo  = new();
    private readonly Mock<IRepository<int, Booking>>      _bookingRepo = new();
    private readonly Mock<IAuditLogService>               _audit       = new();
    private readonly Mock<ILogger<CancellationService>>   _logger      = new();

    private CancellationService CreateService() =>
        new(_cancelRepo.Object, _bookingRepo.Object, _audit.Object, _logger.Object);

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedCancellations()
    {
        var cancellations = Enumerable.Range(1, 15).Select(i => new Cancellation
        {
            CancellationId = i, BookingId = i, Reason = $"Reason {i}",
            Status = "Pending", RefundAmount = 0, CancellationDate = DateTime.UtcNow
        }).ToList();

        _cancelRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(cancellations);

        var result = await CreateService().GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(15, result.TotalRecords);
        Assert.Equal(2, result.TotalPages);
    }

    // ── GetByUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_ReturnsUserCancellations()
    {
        var bookings = new List<Booking>
        {
            new() { BookingId = 1, UserId = 5 },
            new() { BookingId = 2, UserId = 5 },
            new() { BookingId = 3, UserId = 9 }
        };
        var cancellations = new List<Cancellation>
        {
            new() { CancellationId = 1, BookingId = 1, Reason = "Test", Status = "Pending", CancellationDate = DateTime.UtcNow },
            new() { CancellationId = 2, BookingId = 2, Reason = "Test", Status = "Approved", CancellationDate = DateTime.UtcNow }
        };

        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(bookings.Where(b => b.UserId == 5).ToList());
        _cancelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cancellation, bool>>>()))
                   .ReturnsAsync(cancellations);

        var result = await CreateService().GetByUserAsync(5, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(2, result.Data.Count);
    }

    // ── UpdateStatusAsync edge cases ──────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_RejectCancellation_ReturnsRejected()
    {
        var cancellation = new Cancellation { CancellationId = 1, BookingId = 1, Status = "Pending", RefundAmount = 0 };
        var booking      = new Booking { BookingId = 1, Status = "Confirmed" };

        _cancelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cancellation);
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _cancelRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Cancellation>())).ReturnsAsync(cancellation);
        _bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().UpdateStatusAsync(1, "Rejected", 0);

        Assert.NotNull(result);
        Assert.Equal("Rejected", result.Status);
    }

    // ── CreateAsync — pending booking can be cancelled ───────────────────

    [Fact]
    public async Task CreateAsync_PendingBooking_ReturnsCancellationDto()
    {
        var booking = new Booking { BookingId = 1, UserId = 1, Status = "Pending" };
        var dto     = new CreateCancellationDto { BookingId = 1, Reason = "Changed my mind" };

        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _cancelRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cancellation, bool>>>()))
                   .ReturnsAsync(false);
        _cancelRepo.Setup(r => r.AddAsync(It.IsAny<Cancellation>()))
                   .ReturnsAsync((Cancellation c) => { c.CancellationId = 5; return c; });
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
    }

    // ── CreateAsync — completed booking ──────────────────────────────────

    [Fact]
    public async Task CreateAsync_CompletedBooking_ThrowsBadRequestException()
    {
        var booking = new Booking { BookingId = 1, UserId = 1, Status = "Completed" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

        var dto = new CreateCancellationDto { BookingId = 1, Reason = "Want to cancel" };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto));
    }
}
