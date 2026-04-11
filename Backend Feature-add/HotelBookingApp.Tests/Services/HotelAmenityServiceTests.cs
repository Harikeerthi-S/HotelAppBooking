using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class HotelAmenityServiceTests
    {
        private readonly Mock<IRepository<int, HotelAmenity>> _haRepoMock;
        private readonly Mock<IRepository<int, Hotel>> _hotelRepoMock;
        private readonly Mock<IRepository<int, Amenity>> _amenityRepoMock;
        private readonly HotelAmenityService _sut;

        public HotelAmenityServiceTests()
        {
            _haRepoMock = new Mock<IRepository<int, HotelAmenity>>();
            _hotelRepoMock = new Mock<IRepository<int, Hotel>>();
            _amenityRepoMock = new Mock<IRepository<int, Amenity>>();
            _sut = new HotelAmenityService(_haRepoMock.Object, _hotelRepoMock.Object,
                _amenityRepoMock.Object, MockLogger.Create<HotelAmenityService>());
        }

        private static Hotel ActiveHotel() => new() { HotelId = 1, HotelName = "Grand", Location = "Mumbai", IsActive = true };
        private static Amenity SampleAmenity() => new() { AmenityId = 1, Name = "WiFi" };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidAssignment_ReturnsDto()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _amenityRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleAmenity());
            _haRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, bool>>>()))
                       .ReturnsAsync(false);
            _haRepoMock.Setup(r => r.AddAsync(It.IsAny<HotelAmenity>()))
                       .ReturnsAsync((HotelAmenity ha) => { ha.HotelAmenityId = 1; return ha; });

            var result = await _sut.CreateAsync(new CreateHotelAmenityDto { HotelId = 1, AmenityId = 1 });

            Assert.Equal(1, result.HotelAmenityId);
            Assert.Equal(1, result.HotelId);
        }

        [Fact]
        public async Task CreateAsync_HotelNotFound_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateHotelAmenityDto { HotelId = 99, AmenityId = 1 }));
        }

        [Fact]
        public async Task CreateAsync_AmenityNotFound_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _amenityRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateHotelAmenityDto { HotelId = 1, AmenityId = 99 }));
        }

        [Fact]
        public async Task CreateAsync_AlreadyAssigned_ThrowsAlreadyExistsException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _amenityRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleAmenity());
            _haRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, bool>>>()))
                       .ReturnsAsync(true);

            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.CreateAsync(new CreateHotelAmenityDto { HotelId = 1, AmenityId = 1 }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _haRepoMock.Setup(r => r.GetByIdAsync(1))
                       .ReturnsAsync(new HotelAmenity { HotelAmenityId = 1, HotelId = 1, AmenityId = 1 });

            var result = await _sut.GetByIdAsync(1);

            Assert.Equal(1, result!.HotelAmenityId);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _haRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HotelAmenity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsAll()
        {
            _haRepoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, object>>[]>()))
                       .ReturnsAsync(new List<HotelAmenity>
                       {
                           new() { HotelAmenityId = 1, HotelId = 1, AmenityId = 1 },
                           new() { HotelAmenityId = 2, HotelId = 1, AmenityId = 2 }
                       });

            var result = (await _sut.GetAllAsync()).ToList();

            Assert.Equal(2, result.Count);
        }

        // ── GetByHotelAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetByHotelAsync_ReturnsHotelAmenities()
        {
            _haRepoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, object>>[]>()))
                       .ReturnsAsync(new List<HotelAmenity>
                       {
                           new() { HotelAmenityId = 1, HotelId = 1, AmenityId = 1 },
                           new() { HotelAmenityId = 2, HotelId = 2, AmenityId = 1 }
                       });

            var result = (await _sut.GetByHotelAsync(1)).ToList();

            Assert.Single(result);
        }

        // ── DeleteAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_Exists_ReturnsTrue()
        {
            _haRepoMock.Setup(r => r.DeleteAsync(1))
                       .ReturnsAsync(new HotelAmenity { HotelAmenityId = 1, HotelId = 1, AmenityId = 1 });

            Assert.True(await _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _haRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((HotelAmenity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
        }
    }
}
