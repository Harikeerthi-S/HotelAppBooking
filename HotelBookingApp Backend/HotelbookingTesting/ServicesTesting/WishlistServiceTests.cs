using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class WishlistServiceTests
{
    private readonly Mock<IRepository<int, Wishlist>> _wishlistRepo = new();
    private readonly Mock<IRepository<int, User>>     _userRepo     = new();
    private readonly Mock<IRepository<int, Hotel>>    _hotelRepo    = new();
    private readonly Mock<ILogger<WishlistService>>   _logger       = new();

    private WishlistService CreateService() =>
        new(_wishlistRepo.Object, _userRepo.Object, _hotelRepo.Object, _logger.Object);

    // ── AddAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidEntry_ReturnsWishlistDto()
    {
        var user  = new User  { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };
        var hotel = new Hotel { HotelId = 1, HotelName = "Sea View", Location = "Goa", IsActive = true };
        var dto   = new CreateWishlistDto { UserId = 1, HotelId = 1 };

        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _wishlistRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                     .ReturnsAsync(false);
        _wishlistRepo.Setup(r => r.AddAsync(It.IsAny<Wishlist>()))
                     .ReturnsAsync((Wishlist w) => { w.WishlistId = 1; return w; });

        var result = await CreateService().AddAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(1, result.HotelId);
    }

    [Fact]
    public async Task AddAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
        var dto = new CreateWishlistDto { UserId = 99, HotelId = 1 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_HotelNotFound_ThrowsNotFoundException()
    {
        var user = new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        var dto = new CreateWishlistDto { UserId = 1, HotelId = 99 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_AlreadySaved_ThrowsAlreadyExistsException()
    {
        var user  = new User  { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };
        var hotel = new Hotel { HotelId = 1, HotelName = "Sea View", Location = "Goa", IsActive = true };

        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _wishlistRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                     .ReturnsAsync(true);

        var dto = new CreateWishlistDto { UserId = 1, HotelId = 1 };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().AddAsync(dto));
    }

    // ── GetByUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_ReturnsUserWishlist()
    {
        var items = new List<Wishlist>
        {
            new() { WishlistId = 1, UserId = 1, HotelId = 1, SavedAt = DateTime.UtcNow },
            new() { WishlistId = 2, UserId = 1, HotelId = 2, SavedAt = DateTime.UtcNow }
        };

        _wishlistRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                     .ReturnsAsync(items);

        var result = (await CreateService().GetByUserAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(1, r.UserId));
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_ExistingEntry_ReturnsTrue()
    {
        var wishlist = new Wishlist { WishlistId = 1, UserId = 1, HotelId = 1 };
        _wishlistRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(wishlist);

        var result = await CreateService().RemoveAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveAsync_NotFound_ThrowsNotFoundException()
    {
        _wishlistRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Wishlist?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RemoveAsync(99));
    }

    // ── RemoveByUserAndHotelAsync ─────────────────────────────────────────

    [Fact]
    public async Task RemoveByUserAndHotelAsync_ExistingEntry_ReturnsTrue()
    {
        var wishlist = new Wishlist { WishlistId = 1, UserId = 1, HotelId = 1 };
        _wishlistRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                     .ReturnsAsync(wishlist);
        _wishlistRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(wishlist);

        var result = await CreateService().RemoveByUserAndHotelAsync(1, 1);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveByUserAndHotelAsync_NotFound_ThrowsNotFoundException()
    {
        _wishlistRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                     .ReturnsAsync((Wishlist?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RemoveByUserAndHotelAsync(1, 99));
    }
}
