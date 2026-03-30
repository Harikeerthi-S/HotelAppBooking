using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class BookingServiceExtendedTests
{
    private readonly Mock<IRepository<int, Booking>> _bookingRepo = new();
    private readonly Mock<IRepository<int, Hotel>>   _hotelRepo   = new();
    private readonly Mock<IRepository<int, Room>>    _roomRepo    = new();
    private readonly Mock<IAuditLogService>          _audit       = new();
    private readonly Mock<ILogger<BookingService>>   _logger      = new();

    private BookingService CreateService() =>
        new(_bookingRepo.Object, _hotelRepo.Object, _roomRepo.Object, _audit.Object, _logger.Object);

    private static Hotel ActiveHotel(int id = 1) =>
        new() { HotelId = id, HotelName = "Test Hotel", Location = "Mumbai", IsActive = true };

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedBookings()
    {
        var bookings = Enumerable.Range(1, 12).Select(i => new Booking
        {
            BookingId = i, UserId = 1, HotelId = 1, RoomId = 1,
            CheckIn = DateTime.Today.AddDays(i), CheckOut = DateTime.Today.AddDays(i + 2),
            TotalAmount = 2000, Status = "Confirmed"
        }).ToList();

        _bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());

        var result = await CreateService().GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(12, result.TotalRecords);
        Assert.Equal(2, result.TotalPages);
    }

    // ── GetByUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_ReturnsUserBookings()
    {
        var bookings = new List<Booking>
        {
            new() { BookingId = 1, UserId = 5, HotelId = 1, RoomId = 1, CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 3000, Status = "Confirmed" },
            new() { BookingId = 2, UserId = 5, HotelId = 1, RoomId = 2, CheckIn = DateTime.Today.AddDays(5), CheckOut = DateTime.Today.AddDays(7), TotalAmount = 4000, Status = "Pending" }
        };

        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(bookings);
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());

        var result = await CreateService().GetByUserAsync(5, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, b => Assert.Equal(5, b.UserId));
    }

    // ── GetByHotelAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByHotelAsync_ReturnsHotelBookings()
    {
        var hotel = ActiveHotel(2);
        var bookings = new List<Booking>
        {
            new() { BookingId = 1, UserId = 1, HotelId = 2, RoomId = 1, CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 2000, Status = "Confirmed" }
        };

        _hotelRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(hotel);
        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(bookings);

        var result = await CreateService().GetByHotelAsync(2, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Single(result.Data);
        Assert.Equal(2, result.Data[0].HotelId);
    }

    [Fact]
    public async Task GetByHotelAsync_HotelNotFound_ThrowsNotFoundException()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().GetByHotelAsync(99, new PagedRequestDto { PageNumber = 1, PageSize = 10 }));
    }

    // ── CompleteAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_ConfirmedBooking_ReturnsCompleted()
    {
        var booking = new Booking { BookingId = 1, Status = "Confirmed", HotelId = 1, UserId = 1 };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CompleteAsync(1);

        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task CompleteAsync_PendingBooking_ThrowsBadRequestException()
    {
        var booking = new Booking { BookingId = 1, Status = "Pending" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CompleteAsync(1));
    }

    // ── GetPendingByHotelAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetPendingByHotelAsync_ReturnsPendingBookings()
    {
        var hotel = ActiveHotel(1);
        var bookings = new List<Booking>
        {
            new() { BookingId = 1, UserId = 1, HotelId = 1, Status = "Pending", CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 2000 }
        };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(bookings);

        var result = await CreateService().GetPendingByHotelAsync(1);

        Assert.Single(result);
        Assert.Equal("Pending", result[0].Status);
    }

    // ── CancelAsync edge cases ────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ThrowsBadRequestException()
    {
        var booking = new Booking { BookingId = 1, Status = "Cancelled" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CancelAsync(1));
    }

    [Fact]
    public async Task CancelAsync_NotFound_ThrowsNotFoundException()
    {
        _bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CancelAsync(99));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingBooking_ReturnsDto()
    {
        var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 2000, Status = "Confirmed" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());

        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.BookingId);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(99));
    }
}
