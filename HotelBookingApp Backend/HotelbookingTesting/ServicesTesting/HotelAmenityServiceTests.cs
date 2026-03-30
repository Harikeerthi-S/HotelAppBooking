using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class HotelAmenityServiceTests
{
    private readonly Mock<IRepository<int, HotelAmenity>> _haRepo      = new();
    private readonly Mock<IRepository<int, Hotel>>        _hotelRepo   = new();
    private readonly Mock<IRepository<int, Amenity>>      _amenityRepo = new();
    private readonly Mock<ILogger<HotelAmenityService>>   _logger      = new();

    private HotelAmenityService CreateService() =>
        new(_haRepo.Object, _hotelRepo.Object, _amenityRepo.Object, _logger.Object);

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidAssignment_ReturnsDto()
    {
        var hotel   = new Hotel   { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        var amenity = new Amenity { AmenityId = 2, Name = "Pool", Icon = "🏊" };
        var dto     = new CreateHotelAmenityDto { HotelId = 1, AmenityId = 2 };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _amenityRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(amenity);
        _haRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, bool>>>()))
               .ReturnsAsync(false);
        _haRepo.Setup(r => r.AddAsync(It.IsAny<HotelAmenity>()))
               .ReturnsAsync((HotelAmenity ha) => { ha.HotelAmenityId = 10; return ha; });

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(1, result.HotelId);
        Assert.Equal(2, result.AmenityId);
    }

    [Fact]
    public async Task CreateAsync_HotelNotFound_ThrowsNotFoundException()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
        var dto = new CreateHotelAmenityDto { HotelId = 99, AmenityId = 1 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_AmenityNotFound_ThrowsNotFoundException()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _amenityRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

        var dto = new CreateHotelAmenityDto { HotelId = 1, AmenityId = 99 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_DuplicateAssignment_ThrowsAlreadyExistsException()
    {
        var hotel   = new Hotel   { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        var amenity = new Amenity { AmenityId = 2, Name = "Pool" };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _amenityRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(amenity);
        _haRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, bool>>>()))
               .ReturnsAsync(true);

        var dto = new CreateHotelAmenityDto { HotelId = 1, AmenityId = 2 };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().CreateAsync(dto));
    }

    // ── GetByHotelAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByHotelAsync_ReturnsHotelAmenities()
    {
        var items = new List<HotelAmenity>
        {
            new() { HotelAmenityId = 1, HotelId = 1, AmenityId = 1, Amenity = new Amenity { Name = "Pool", Icon = "🏊" } },
            new() { HotelAmenityId = 2, HotelId = 1, AmenityId = 2, Amenity = new Amenity { Name = "Gym",  Icon = "🏋️" } },
            new() { HotelAmenityId = 3, HotelId = 2, AmenityId = 1, Amenity = new Amenity { Name = "Pool", Icon = "🏊" } }
        };

        _haRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, object>>[]>()))
               .ReturnsAsync(items);

        var result = (await CreateService().GetByHotelAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(1, r.HotelId));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEntry_ReturnsTrue()
    {
        var ha = new HotelAmenity { HotelAmenityId = 1, HotelId = 1, AmenityId = 1 };
        _haRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(ha);

        var result = await CreateService().DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _haRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync((HotelAmenity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99));
    }
}
