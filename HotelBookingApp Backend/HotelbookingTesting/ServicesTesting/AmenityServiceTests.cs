using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class AmenityServiceTests
{
    private readonly Mock<IRepository<int, Amenity>> _amenityRepo = new();
    private readonly Mock<ILogger<AmenityService>>   _logger      = new();

    private AmenityService CreateService() =>
        new(_amenityRepo.Object, _logger.Object);

    [Fact]
    public async Task CreateAsync_NewAmenity_ReturnsAmenityResponseDto()
    {
        var dto = new CreateAmenityDto { Name = "Pool", Description = "Swimming pool", Icon = "🏊" };

        _amenityRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                    .ReturnsAsync(false);
        _amenityRepo.Setup(r => r.AddAsync(It.IsAny<Amenity>()))
                    .ReturnsAsync((Amenity a) => { a.AmenityId = 1; return a; });

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Pool", result.Name);
        Assert.Equal("🏊", result.Icon);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsAlreadyExistsException()
    {
        _amenityRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                    .ReturnsAsync(true);

        var dto = new CreateAmenityDto { Name = "Pool", Description = "", Icon = "" };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAmenities()
    {
        var amenities = new List<Amenity>
        {
            new() { AmenityId = 1, Name = "Pool", Icon = "🏊" },
            new() { AmenityId = 2, Name = "Gym",  Icon = "🏋️" }
        };
        _amenityRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(amenities);

        var result = await CreateService().GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DeleteAsync_ExistingAmenity_ReturnsTrue()
    {
        var amenity = new Amenity { AmenityId = 1, Name = "Pool" };
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amenity);
        _amenityRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(amenity);

        var result = await CreateService().DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _amenityRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99));
    }
}
