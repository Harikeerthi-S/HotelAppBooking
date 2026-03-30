using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class HotelServiceTests
{
    private readonly Mock<IRepository<int, Hotel>> _hotelRepo = new();
    private readonly Mock<IAuditLogService>        _audit     = new();
    private readonly Mock<ILogger<HotelService>>   _logger    = new();

    private HotelService CreateService() =>
        new(_hotelRepo.Object, _audit.Object, _logger.Object);

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsHotelResponseDto()
    {
        var dto = new CreateHotelDto
        {
            HotelName = "Grand Palace", Location = "Mumbai",
            StarRating = 5, TotalRooms = 50
        };

        _hotelRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                  .ReturnsAsync(false);

        _hotelRepo.Setup(r => r.AddAsync(It.IsAny<Hotel>()))
                  .ReturnsAsync((Hotel h) => { h.HotelId = 1; return h; });

        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
              .ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Grand Palace", result.HotelName);
        Assert.Equal("Mumbai", result.Location);
    }

    [Fact]
    public async Task CreateAsync_DuplicateHotel_ThrowsAlreadyExistsException()
    {
        var dto = new CreateHotelDto { HotelName = "Existing", Location = "Delhi", StarRating = 3, TotalRooms = 10 };

        _hotelRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                  .ReturnsAsync(true);

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().CreateAsync(dto));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingHotel_ReturnsDto()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Sea View", Location = "Goa", StarRating = 4, IsActive = true };
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);

        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Sea View", result!.HotelName);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByIdAsync_InactiveHotel_ThrowsNotFoundException()
    {
        var hotel = new Hotel { HotelId = 2, HotelName = "Old Hotel", Location = "Pune", IsActive = false };
        _hotelRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(hotel);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(2));
    }

    // ── SearchAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_EmptyLocation_ThrowsBadRequestException()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().SearchAsync(""));
    }

    [Fact]
    public async Task SearchAsync_ValidLocation_ReturnsMatchingHotels()
    {
        var hotels = new List<Hotel>
        {
            new() { HotelId = 1, HotelName = "Beach Resort", Location = "Goa", IsActive = true }
        };
        _hotelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                  .ReturnsAsync(hotels);

        var result = await CreateService().SearchAsync("Goa");

        Assert.Single(result);
    }

    // ── DeactivateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ActiveHotel_ReturnsTrue()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _hotelRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync(hotel);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().DeactivateAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeactivateAsync_AlreadyInactive_ThrowsBadRequestException()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = false };
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().DeactivateAsync(1));
    }
}
