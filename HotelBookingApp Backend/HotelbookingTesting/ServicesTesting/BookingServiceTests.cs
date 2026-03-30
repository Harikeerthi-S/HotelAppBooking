using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class BookingServiceTests
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

    private static Room ActiveRoom(int id = 1, int hotelId = 1) =>
        new() { RoomId = id, HotelId = hotelId, RoomType = "Standard", PricePerNight = 2000, IsAvailable = true };

    private static CreateBookingDto ValidDto(int userId = 1) => new()
    {
        UserId        = userId,
        HotelId       = 1,
        RoomId        = 1,
        NumberOfRooms = 1,
        CheckIn       = DateTime.Today.AddDays(1),
        CheckOut      = DateTime.Today.AddDays(3)
    };

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidBooking_ReturnsBookingResponseDto()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveRoom());
        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(new List<Booking>());
        _bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>()))
                    .ReturnsAsync((Booking b) => { b.BookingId = 10; return b; });
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CreateAsync(ValidDto());

        Assert.NotNull(result);
        Assert.Equal(10, result.BookingId);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateAsync_InvalidDates_ThrowsBadRequestException()
    {
        var dto = ValidDto();
        dto.CheckIn  = DateTime.Today.AddDays(3);
        dto.CheckOut = DateTime.Today.AddDays(1); // check-out before check-in

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_HotelNotFound_ThrowsNotFoundException()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(ValidDto()));
    }

    [Fact]
    public async Task CreateAsync_RoomUnavailable_ThrowsBadRequestException()
    {
        var room = ActiveRoom();
        room.IsAvailable = false;
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(ValidDto()));
    }

    [Fact]
    public async Task CreateAsync_RoomAlreadyBookedByOther_ThrowsBadRequestException()
    {
        var overlap = new Booking
        {
            BookingId = 5, UserId = 99, RoomId = 1,
            CheckIn   = DateTime.Today.AddDays(1),
            CheckOut  = DateTime.Today.AddDays(3),
            Status    = "Confirmed"
        };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveRoom());
        _bookingRepo.SetupSequence(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(new List<Booking>())          // same-user check → empty
                    .ReturnsAsync(new List<Booking> { overlap }); // other-user check → overlap

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(ValidDto()));
    }

    // ── CancelAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_PendingBooking_ReturnsTrue()
    {
        var booking = new Booking { BookingId = 1, Status = "Pending", UserId = 1 };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CancelAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task CancelAsync_CompletedBooking_ThrowsBadRequestException()
    {
        var booking = new Booking { BookingId = 1, Status = "Completed" };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CancelAsync(1));
    }

    // ── ConfirmAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_PendingBooking_ReturnsConfirmed()
    {
        var booking = new Booking { BookingId = 1, Status = "Pending", HotelId = 1, UserId = 1 };
        _bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        _bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().ConfirmAsync(1);

        Assert.Equal("Confirmed", result.Status);
    }
}
