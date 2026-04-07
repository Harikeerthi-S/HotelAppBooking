using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    /// <summary>
    /// Covers the ?? throw new ArgumentNullException branches in every service constructor.
    /// Each test passes null for one dependency and verifies ArgumentNullException is thrown.
    /// </summary>
    public class ConstructorNullGuardTests
    {
        // ── AmenityService ────────────────────────────────────────────────

        [Fact]
        public void AmenityService_NullRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AmenityService(null!, MockLogger.Create<AmenityService>()));
        }

        [Fact]
        public void AmenityService_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AmenityService(new Mock<IRepository<int, Amenity>>().Object, null!));
        }

        // ── AuthService ───────────────────────────────────────────────────

        [Fact]
        public void AuthService_NullRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AuthService(null!, new Mock<IPasswordService>().Object, MockLogger.Create<AuthService>()));
        }

        [Fact]
        public void AuthService_NullPasswordService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AuthService(new Mock<IRepository<int, User>>().Object, null!, MockLogger.Create<AuthService>()));
        }

        [Fact]
        public void AuthService_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AuthService(new Mock<IRepository<int, User>>().Object, new Mock<IPasswordService>().Object, null!));
        }

        // ── HotelService ──────────────────────────────────────────────────

        [Fact]
        public void HotelService_NullRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HotelService(null!, new Mock<IAuditLogService>().Object, MockLogger.Create<HotelService>()));
        }

        [Fact]
        public void HotelService_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HotelService(new Mock<IRepository<int, Hotel>>().Object, new Mock<IAuditLogService>().Object, null!));
        }

        // ── HotelAmenityService ───────────────────────────────────────────

        [Fact]
        public void HotelAmenityService_NullHaRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HotelAmenityService(null!,
                    new Mock<IRepository<int, Hotel>>().Object,
                    new Mock<IRepository<int, Amenity>>().Object,
                    MockLogger.Create<HotelAmenityService>()));
        }

        [Fact]
        public void HotelAmenityService_NullHotelRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HotelAmenityService(
                    new Mock<IRepository<int, HotelAmenity>>().Object,
                    null!,
                    new Mock<IRepository<int, Amenity>>().Object,
                    MockLogger.Create<HotelAmenityService>()));
        }

        [Fact]
        public void HotelAmenityService_NullAmenityRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HotelAmenityService(
                    new Mock<IRepository<int, HotelAmenity>>().Object,
                    new Mock<IRepository<int, Hotel>>().Object,
                    null!,
                    MockLogger.Create<HotelAmenityService>()));
        }

        [Fact]
        public void HotelAmenityService_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HotelAmenityService(
                    new Mock<IRepository<int, HotelAmenity>>().Object,
                    new Mock<IRepository<int, Hotel>>().Object,
                    new Mock<IRepository<int, Amenity>>().Object,
                    null!));
        }

        // ── NotificationService ───────────────────────────────────────────

        [Fact]
        public void NotificationService_NullNotifRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NotificationService(null!, new Mock<IRepository<int, User>>().Object, MockLogger.Create<NotificationService>()));
        }

        [Fact]
        public void NotificationService_NullUserRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NotificationService(new Mock<IRepository<int, Notification>>().Object, null!, MockLogger.Create<NotificationService>()));
        }

        [Fact]
        public void NotificationService_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new NotificationService(new Mock<IRepository<int, Notification>>().Object, new Mock<IRepository<int, User>>().Object, null!));
        }

        // ── WishlistService ───────────────────────────────────────────────

        [Fact]
        public void WishlistService_NullWishlistRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WishlistService(null!,
                    new Mock<IRepository<int, User>>().Object,
                    new Mock<IRepository<int, Hotel>>().Object,
                    MockLogger.Create<WishlistService>()));
        }

        [Fact]
        public void WishlistService_NullUserRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WishlistService(
                    new Mock<IRepository<int, Wishlist>>().Object,
                    null!,
                    new Mock<IRepository<int, Hotel>>().Object,
                    MockLogger.Create<WishlistService>()));
        }

        [Fact]
        public void WishlistService_NullHotelRepo_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WishlistService(
                    new Mock<IRepository<int, Wishlist>>().Object,
                    new Mock<IRepository<int, User>>().Object,
                    null!,
                    MockLogger.Create<WishlistService>()));
        }

        [Fact]
        public void WishlistService_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WishlistService(
                    new Mock<IRepository<int, Wishlist>>().Object,
                    new Mock<IRepository<int, User>>().Object,
                    new Mock<IRepository<int, Hotel>>().Object,
                    null!));
        }
    }
}
