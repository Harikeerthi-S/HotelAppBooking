using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class HotelServiceExtendedTests
{
    private readonly Mock<IRepository<int, Hotel>> _hotelRepo = new();
    private readonly Mock<IAuditLogService>        _audit     = new();
    private readonly Mock<ILogger<HotelService>>   _logger    = new();

    private HotelService CreateService() =>
        new(_hotelRepo.Object, _audit.Object, _logger.Object);

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResults()
    {
        var hotels = Enumerable.Range(1, 15).Select(i => new Hotel
        {
            HotelId = i, HotelName = $"Hotel{i}", Location = "Mumbai",
            StarRating = 3, IsActive = true
        }).ToList();

        _hotelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                  .ReturnsAsync(hotels);

        var result = await CreateService().GetPagedAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(15, result.TotalRecords);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_ReturnsCorrectSlice()
    {
        var hotels = Enumerable.Range(1, 15).Select(i => new Hotel
        {
            HotelId = i, HotelName = $"Hotel{i}", Location = "Mumbai",
            StarRating = 3, IsActive = true
        }).ToList();

        _hotelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                  .ReturnsAsync(hotels);

        var result = await CreateService().GetPagedAsync(new PagedRequestDto { PageNumber = 2, PageSize = 10 });

        Assert.Equal(5, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
    }

    // ── FilterPagedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task FilterPagedAsync_ByLocation_ReturnsFilteredResults()
    {
        var hotels = new List<Hotel>
        {
            new() { HotelId = 1, HotelName = "Goa Beach", Location = "Goa", StarRating = 4, IsActive = true },
            new() { HotelId = 2, HotelName = "Mumbai Grand", Location = "Mumbai", StarRating = 5, IsActive = true }
        };

        _hotelRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(hotels);

        var result = await CreateService().FilterPagedAsync(
            new HotelFilterDto { Location = "Goa" },
            new PagedRequestDto { PageNumber = 1, PageSize = 10 }
        );

        Assert.Single(result.Data);
        Assert.Equal("Goa Beach", result.Data[0].HotelName);
    }

    [Fact]
    public async Task FilterPagedAsync_ByMinRating_ReturnsFilteredResults()
    {
        var hotels = new List<Hotel>
        {
            new() { HotelId = 1, HotelName = "Budget Inn", Location = "Delhi", StarRating = 2, IsActive = true },
            new() { HotelId = 2, HotelName = "Luxury Palace", Location = "Delhi", StarRating = 5, IsActive = true }
        };

        _hotelRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(hotels);

        var result = await CreateService().FilterPagedAsync(
            new HotelFilterDto { MinRating = 4 },
            new PagedRequestDto { PageNumber = 1, PageSize = 10 }
        );

        Assert.Single(result.Data);
        Assert.Equal("Luxury Palace", result.Data[0].HotelName);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingHotel_ReturnsUpdatedDto()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Old Name", Location = "Mumbai", IsActive = true };
        var dto   = new CreateHotelDto { HotelName = "New Name", Location = "Goa", StarRating = 5, TotalRooms = 20 };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _hotelRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync(hotel);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().UpdateAsync(1, dto);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
        var dto = new CreateHotelDto { HotelName = "Test", Location = "Mumbai", StarRating = 3, TotalRooms = 10 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().UpdateAsync(99, dto));
    }

    // ── SearchAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_NoResults_ReturnsEmptyList()
    {
        _hotelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                  .ReturnsAsync(new List<Hotel>());

        var result = await CreateService().SearchAsync("NonExistentCity");

        Assert.Empty(result);
    }
}
