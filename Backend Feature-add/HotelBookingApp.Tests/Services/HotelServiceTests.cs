using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class HotelServiceTests
    {
        private readonly Mock<IRepository<int, Hotel>> _repoMock;
        private readonly Mock<IAuditLogService> _auditMock;
        private readonly HotelService _sut;

        public HotelServiceTests()
        {
            _repoMock = new Mock<IRepository<int, Hotel>>();
            _auditMock = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new HotelService(_repoMock.Object, _auditMock.Object, MockLogger.Create<HotelService>());
        }

        private static Hotel SampleHotel(int id = 1) => new()
        {
            HotelId = id, HotelName = "Grand Hotel", Location = "Mumbai",
            StarRating = 4, TotalRooms = 50, IsActive = true
        };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_NewHotel_ReturnsDto()
        {
            var dto = new CreateHotelDto { HotelName = "Grand Hotel", Location = "Mumbai", StarRating = 4, TotalRooms = 50 };
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(false);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Hotel>()))
                     .ReturnsAsync((Hotel h) => { h.HotelId = 1; return h; });

            var result = await _sut.CreateAsync(dto);

            Assert.Equal("Grand Hotel", result.HotelName);
            Assert.Equal(1, result.HotelId);
        }

        [Fact]
        public async Task CreateAsync_Duplicate_ThrowsAlreadyExistsException()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(true);

            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.CreateAsync(new CreateHotelDto { HotelName = "Grand Hotel", Location = "Mumbai", StarRating = 4, TotalRooms = 50 }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ActiveHotel_ReturnsDto()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleHotel());

            var result = await _sut.GetByIdAsync(1);

            Assert.Equal("Grand Hotel", result!.HotelName);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        [Fact]
        public async Task GetByIdAsync_InactiveHotel_ThrowsNotFoundException()
        {
            var hotel = SampleHotel(); hotel.IsActive = false;
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
        }

        // ── GetPagedAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_ReturnsCorrectPage()
        {
            var hotels = Enumerable.Range(1, 12)
                .Select(i => new Hotel { HotelId = i, HotelName = $"H{i}", Location = "City", StarRating = 3, TotalRooms = 10, IsActive = true })
                .ToList();
            _repoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(hotels);

            var result = await _sut.GetPagedAsync(new PagedRequestDto { PageNumber = 1, PageSize = 5 });

            Assert.Equal(5, result.Data.Count);
            Assert.Equal(12, result.TotalRecords);
        }

        // ── SearchAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task SearchAsync_ValidLocation_ReturnsMatches()
        {
            _repoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(new List<Hotel> { SampleHotel() });

            var result = (await _sut.SearchAsync("Mumbai")).ToList();

            Assert.Single(result);
        }

        [Fact]
        public async Task SearchAsync_EmptyLocation_ThrowsBadRequestException()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.SearchAsync("  "));
        }

        // ── UpdateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ValidHotel_ReturnsUpdatedDto()
        {
            var hotel = SampleHotel();
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>()))
                     .ReturnsAsync((int _, Hotel h) => h);

            var dto = new CreateHotelDto { HotelName = "Updated", Location = "Delhi", StarRating = 5, TotalRooms = 100 };
            var result = await _sut.UpdateAsync(1, dto);

            Assert.Equal("Updated", result!.HotelName);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateAsync(99, new CreateHotelDto { HotelName = "X", Location = "Y", StarRating = 3, TotalRooms = 10 }));
        }

        // ── DeactivateAsync ───────────────────────────────────────────────

        [Fact]
        public async Task DeactivateAsync_ActiveHotel_ReturnsTrue()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleHotel());
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>()))
                     .ReturnsAsync((int _, Hotel h) => h);

            Assert.True(await _sut.DeactivateAsync(1));
        }

        [Fact]
        public async Task DeactivateAsync_AlreadyInactive_ThrowsBadRequestException()
        {
            var hotel = SampleHotel(); hotel.IsActive = false;
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);

            await Assert.ThrowsAsync<BadRequestException>(() => _sut.DeactivateAsync(1));
        }

        [Fact]
        public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeactivateAsync(99));
        }
    }
}
