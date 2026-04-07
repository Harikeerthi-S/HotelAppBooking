using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class WishlistServiceTests
    {
        private readonly Mock<IRepository<int, Wishlist>> _wishlistRepoMock;
        private readonly Mock<IRepository<int, User>> _userRepoMock;
        private readonly Mock<IRepository<int, Hotel>> _hotelRepoMock;
        private readonly WishlistService _sut;

        public WishlistServiceTests()
        {
            _wishlistRepoMock = new Mock<IRepository<int, Wishlist>>();
            _userRepoMock = new Mock<IRepository<int, User>>();
            _hotelRepoMock = new Mock<IRepository<int, Hotel>>();
            _sut = new WishlistService(_wishlistRepoMock.Object, _userRepoMock.Object,
                _hotelRepoMock.Object, MockLogger.Create<WishlistService>());
        }

        private static User SampleUser() => new() { UserId = 1, UserName = "Alice", Email = "a@a.com", Role = "user" };
        private static Hotel ActiveHotel() => new() { HotelId = 1, HotelName = "Grand", Location = "Mumbai", IsActive = true };

        // ── AddAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_ValidEntry_ReturnsDto()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _wishlistRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                             .ReturnsAsync(false);
            _wishlistRepoMock.Setup(r => r.AddAsync(It.IsAny<Wishlist>()))
                             .ReturnsAsync((Wishlist w) => { w.WishlistId = 1; return w; });

            var result = await _sut.AddAsync(new CreateWishlistDto { UserId = 1, HotelId = 1 });

            Assert.Equal(1, result.WishlistId);
            Assert.Equal(1, result.UserId);
        }

        [Fact]
        public async Task AddAsync_UserNotFound_ThrowsNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.AddAsync(new CreateWishlistDto { UserId = 99, HotelId = 1 }));
        }

        [Fact]
        public async Task AddAsync_HotelNotFound_ThrowsNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.AddAsync(new CreateWishlistDto { UserId = 1, HotelId = 99 }));
        }

        [Fact]
        public async Task AddAsync_AlreadySaved_ThrowsAlreadyExistsException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _wishlistRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                             .ReturnsAsync(true);

            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.AddAsync(new CreateWishlistDto { UserId = 1, HotelId = 1 }));
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsUserWishlist()
        {
            _wishlistRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                             .ReturnsAsync(new List<Wishlist>
                             {
                                 new() { WishlistId = 1, UserId = 1, HotelId = 1, SavedAt = DateTime.UtcNow },
                                 new() { WishlistId = 2, UserId = 1, HotelId = 2, SavedAt = DateTime.UtcNow.AddMinutes(-1) }
                             });

            var result = (await _sut.GetByUserAsync(1)).ToList();

            Assert.Equal(2, result.Count);
        }

        // ── RemoveAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task RemoveAsync_Exists_ReturnsTrue()
        {
            _wishlistRepoMock.Setup(r => r.DeleteAsync(1))
                             .ReturnsAsync(new Wishlist { WishlistId = 1, UserId = 1, HotelId = 1 });

            Assert.True(await _sut.RemoveAsync(1));
        }

        [Fact]
        public async Task RemoveAsync_NotFound_ThrowsNotFoundException()
        {
            _wishlistRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Wishlist?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveAsync(99));
        }

        // ── RemoveByUserAndHotelAsync ─────────────────────────────────────

        [Fact]
        public async Task RemoveByUserAndHotelAsync_Exists_ReturnsTrue()
        {
            var item = new Wishlist { WishlistId = 1, UserId = 1, HotelId = 1 };
            _wishlistRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                             .ReturnsAsync(item);
            _wishlistRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(item);

            Assert.True(await _sut.RemoveByUserAndHotelAsync(1, 1));
        }

        [Fact]
        public async Task RemoveByUserAndHotelAsync_NotFound_ThrowsNotFoundException()
        {
            _wishlistRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                             .ReturnsAsync((Wishlist?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveByUserAndHotelAsync(1, 99));
        }
    }
}
