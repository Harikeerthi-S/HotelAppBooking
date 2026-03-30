using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class RoomServiceExtendedTests
{
    private readonly Mock<IRepository<int, Room>>    _roomRepo    = new();
    private readonly Mock<IRepository<int, Hotel>>   _hotelRepo   = new();
    private readonly Mock<IRepository<int, Booking>> _bookingRepo = new();
    private readonly Mock<IAuditLogService>          _audit       = new();
    private readonly Mock<ILogger<RoomService>>      _logger      = new();

    private RoomService CreateService() =>
        new(_roomRepo.Object, _hotelRepo.Object, _bookingRepo.Object, _audit.Object, _logger.Object);

    private static Room ActiveRoom(int id = 1, int hotelId = 1) =>
        new() { RoomId = id, HotelId = hotelId, RoomNumber = id * 100, RoomType = "Standard", PricePerNight = 2000, Capacity = 2, IsAvailable = true };

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsPaginatedRooms()
    {
        var rooms = Enumerable.Range(1, 15).Select(i => ActiveRoom(i)).ToList();
        _roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = await CreateService().GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(15, result.TotalRecords);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_WithHotelId_ReturnsHotelRooms()
    {
        var rooms = new List<Room>
        {
            ActiveRoom(1, hotelId: 2),
            ActiveRoom(2, hotelId: 2),
            ActiveRoom(3, hotelId: 5)
        };
        _roomRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                 .ReturnsAsync(rooms.Where(r => r.HotelId == 2).ToList());

        var result = await CreateService().GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 }, hotelId: 2);

        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, r => Assert.Equal(2, r.HotelId));
    }

    [Fact]
    public async Task GetAllAsync_Page2_ReturnsCorrectSlice()
    {
        var rooms = Enumerable.Range(1, 15).Select(i => ActiveRoom(i)).ToList();
        _roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = await CreateService().GetAllAsync(new PagedRequestDto { PageNumber = 2, PageSize = 10 });

        Assert.Equal(5, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRoom_ReturnsUpdatedDto()
    {
        var room = ActiveRoom(1);
        var dto  = new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 3500, Capacity = 3 };

        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _roomRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                 .ReturnsAsync(false);
        _roomRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Room>())).ReturnsAsync(room);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().UpdateAsync(1, dto);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        _roomRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);
        var dto = new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().UpdateAsync(99, dto));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateRoomNumber_ThrowsAlreadyExistsException()
    {
        var room = ActiveRoom(1);
        var dto  = new CreateRoomDto { HotelId = 1, RoomNumber = 102, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 };

        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _roomRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                 .ReturnsAsync(true); // duplicate

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().UpdateAsync(1, dto));
    }

    // ── FilterAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_ByRoomType_ReturnsMatchingRooms()
    {
        var rooms = new List<Room>
        {
            new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Deluxe",   PricePerNight = 3000, Capacity = 2, IsAvailable = true },
            new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Standard", PricePerNight = 1500, Capacity = 2, IsAvailable = true },
            new() { RoomId = 3, HotelId = 1, RoomNumber = 103, RoomType = "Suite",    PricePerNight = 6000, Capacity = 4, IsAvailable = true }
        };
        _roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = (await CreateService().FilterAsync(new RoomFilterDto { RoomType = "Deluxe" })).ToList();

        Assert.Single(result);
        Assert.Equal("Deluxe", result[0].RoomType);
    }

    [Fact]
    public async Task FilterAsync_ByPriceRange_ReturnsMatchingRooms()
    {
        var rooms = new List<Room>
        {
            new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2, IsAvailable = true },
            new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Deluxe",   PricePerNight = 3000, Capacity = 2, IsAvailable = true },
            new() { RoomId = 3, HotelId = 1, RoomNumber = 103, RoomType = "Suite",    PricePerNight = 8000, Capacity = 4, IsAvailable = true }
        };
        _roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = (await CreateService().FilterAsync(new RoomFilterDto { MinPrice = 2000, MaxPrice = 5000 })).ToList();

        Assert.Single(result);
        Assert.Equal(3000, result[0].PricePerNight);
    }

    [Fact]
    public async Task FilterAsync_OnlyAvailable_ExcludesInactiveRooms()
    {
        var rooms = new List<Room>
        {
            new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2, IsAvailable = true },
            new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Deluxe",   PricePerNight = 3000, Capacity = 2, IsAvailable = false }
        };
        _roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = (await CreateService().FilterAsync(new RoomFilterDto { OnlyAvailable = true })).ToList();

        Assert.Single(result);
        Assert.True(result[0].IsAvailable);
    }

    [Fact]
    public async Task FilterAsync_ByCapacity_ReturnsMatchingRooms()
    {
        var rooms = new List<Room>
        {
            new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Single", PricePerNight = 800,  Capacity = 1, IsAvailable = true },
            new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Double", PricePerNight = 1500, Capacity = 2, IsAvailable = true },
            new() { RoomId = 3, HotelId = 1, RoomNumber = 103, RoomType = "Suite",  PricePerNight = 5000, Capacity = 4, IsAvailable = true }
        };
        _roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = (await CreateService().FilterAsync(new RoomFilterDto { MinCapacity = 2 })).ToList();

        Assert.Equal(2, result.Count);
    }

    // ── IsAvailableForDatesAsync ──────────────────────────────────────────

    [Fact]
    public async Task IsAvailableForDatesAsync_NoOverlap_ReturnsTrue()
    {
        var room = ActiveRoom(1);
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(new List<Booking>()); // no overlapping bookings

        var result = await CreateService().IsAvailableForDatesAsync(
            1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8));

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableForDatesAsync_WithOverlap_ReturnsFalse()
    {
        var room = ActiveRoom(1);
        var overlap = new Booking
        {
            BookingId = 1, RoomId = 1, Status = "Confirmed",
            CheckIn   = DateTime.Today.AddDays(4),
            CheckOut  = DateTime.Today.AddDays(7)
        };

        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(new List<Booking> { overlap });

        var result = await CreateService().IsAvailableForDatesAsync(
            1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8));

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableForDatesAsync_InactiveRoom_ReturnsFalse()
    {
        var room = new Room { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, IsAvailable = false };
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        var result = await CreateService().IsAvailableForDatesAsync(
            1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableForDatesAsync_RoomNotFound_ThrowsNotFoundException()
    {
        _roomRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().IsAvailableForDatesAsync(99, DateTime.Today, DateTime.Today.AddDays(2)));
    }

    // ── DeactivateAsync — already inactive ───────────────────────────────

    [Fact]
    public async Task DeactivateAsync_AlreadyInactive_ThrowsBadRequestException()
    {
        var room = new Room { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, IsAvailable = false };
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().DeactivateAsync(1));
    }
}
