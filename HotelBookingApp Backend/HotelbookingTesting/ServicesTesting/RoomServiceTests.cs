using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class RoomServiceTests
{
    private readonly Mock<IRepository<int, Room>>    _roomRepo  = new();
    private readonly Mock<IRepository<int, Hotel>>   _hotelRepo = new();
    private readonly Mock<IRepository<int, Booking>> _bookingRepo = new();
    private readonly Mock<IAuditLogService>          _audit     = new();
    private readonly Mock<ILogger<RoomService>>      _logger    = new();

    private RoomService CreateService() =>
        new(_roomRepo.Object, _hotelRepo.Object, _bookingRepo.Object, _audit.Object, _logger.Object);

    [Fact]
    public async Task CreateAsync_ValidRoom_ReturnsRoomResponseDto()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        var dto   = new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1500, Capacity = 2 };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _roomRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                 .ReturnsAsync(false);
        _roomRepo.Setup(r => r.AddAsync(It.IsAny<Room>()))
                 .ReturnsAsync((Room r) => { r.RoomId = 5; return r; });
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(101, result.RoomNumber);
        Assert.Equal("Standard", result.RoomType);
    }

    [Fact]
    public async Task CreateAsync_HotelNotFound_ThrowsNotFoundException()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
        var dto = new CreateRoomDto { HotelId = 99, RoomNumber = 1, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_DuplicateRoomNumber_ThrowsAlreadyExistsException()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _roomRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                 .ReturnsAsync(true);

        var dto = new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRoom_ReturnsDto()
    {
        var room = new Room { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 3000, IsAvailable = true };
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Deluxe", result!.RoomType);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _roomRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(99));
    }

    [Fact]
    public async Task DeactivateAsync_ActiveRoom_ReturnsTrue()
    {
        var room = new Room { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, IsAvailable = true };
        _roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _roomRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Room>())).ReturnsAsync(room);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().DeactivateAsync(1);

        Assert.True(result);
    }
}
