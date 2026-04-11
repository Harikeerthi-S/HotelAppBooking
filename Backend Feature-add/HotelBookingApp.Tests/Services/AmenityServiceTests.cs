using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class AmenityServiceTests
    {
        private readonly Mock<IRepository<int, Amenity>> _repoMock;
        private readonly AmenityService _sut;

        public AmenityServiceTests()
        {
            _repoMock = new Mock<IRepository<int, Amenity>>();
            _sut = new AmenityService(_repoMock.Object, MockLogger.Create<AmenityService>());
        }

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_NewAmenity_ReturnsDto()
        {
            var dto = new CreateAmenityDto { Name = "WiFi", Description = "Free WiFi", Icon = "wifi" };
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                     .ReturnsAsync(false);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Amenity>()))
                     .ReturnsAsync((Amenity a) => { a.AmenityId = 1; return a; });

            var result = await _sut.CreateAsync(dto);

            Assert.Equal("WiFi", result.Name);
            Assert.Equal(1, result.AmenityId);
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_ThrowsAlreadyExistsException()
        {
            var dto = new CreateAmenityDto { Name = "WiFi" };
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                     .ReturnsAsync(true);

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _sut.CreateAsync(dto));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync(new Amenity { AmenityId = 1, Name = "Pool" });

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Pool", result!.Name);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsOrderedList()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<Amenity>
                     {
                         new() { AmenityId = 2, Name = "Spa" },
                         new() { AmenityId = 1, Name = "Gym" }
                     });

            var result = (await _sut.GetAllAsync()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Gym", result[0].Name); // ordered by name
        }

        // ── UpdateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ValidUpdate_ReturnsTrue()
        {
            var dto = new CreateAmenityDto { Name = "Gym Updated" };
            _repoMock.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync(new Amenity { AmenityId = 1, Name = "Gym" });
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                     .ReturnsAsync(false);
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Amenity>()))
                     .ReturnsAsync(new Amenity { AmenityId = 1, Name = "Gym Updated" });

            var result = await _sut.UpdateAsync(1, dto);

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateAsync(99, new CreateAmenityDto { Name = "X" }));
        }

        [Fact]
        public async Task UpdateAsync_DuplicateName_ThrowsAlreadyExistsException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync(new Amenity { AmenityId = 1, Name = "Gym" });
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                     .ReturnsAsync(true);

            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.UpdateAsync(1, new CreateAmenityDto { Name = "Pool" }));
        }

        // ── DeleteAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_Exists_ReturnsTrue()
        {
            _repoMock.Setup(r => r.DeleteAsync(1))
                     .ReturnsAsync(new Amenity { AmenityId = 1, Name = "WiFi" });

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Amenity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
        }
    }
}
