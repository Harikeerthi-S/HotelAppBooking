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
        private readonly Mock<IAuditLogService>        _auditMock;
        private readonly HotelService                  _sut;

        public HotelServiceTests()
        {
            _repoMock  = new Mock<IRepository<int, Hotel>>();
            _auditMock = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new HotelService(_repoMock.Object, _auditMock.Object, MockLogger.Create<HotelService>());
        }

        private static Hotel ActiveHotel(int id = 1) => new()
        {
            HotelId = id, HotelName = "Grand Hotel", Location = "Mumbai",
            StarRating = 4, TotalRooms = 50, IsActive = true
        };

        private static CreateHotelDto ValidDto() => new()
        {
            HotelName = "Grand Hotel", Location = "Mumbai", StarRating = 4, TotalRooms = 50
        };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_NewHotel_ReturnsDto()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(false);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Hotel>()))
                     .ReturnsAsync((Hotel h) => { h.HotelId = 1; return h; });

            var result = await _sut.CreateAsync(ValidDto());
            Assert.Equal("Grand Hotel", result.HotelName);
        }

        [Fact]
        public async Task CreateAsync_Duplicate_ThrowsAlreadyExists()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(true);
            await Assert.ThrowsAsync<AlreadyExistsException>(() => _sut.CreateAsync(ValidDto()));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ActiveHotel_ReturnsDto()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            var result = await _sut.GetByIdAsync(1);
            Assert.Equal("Grand Hotel", result!.HotelName);
        }

        [Fact]
        public async Task GetByIdAsync_Null_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        [Fact]
        public async Task GetByIdAsync_InactiveHotel_ThrowsNotFoundException()
        {
            var hotel = ActiveHotel(); hotel.IsActive = false;
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

        [Fact]
        public async Task GetPagedAsync_PageSizeClamped()
        {
            _repoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(new List<Hotel>());
            var result = await _sut.GetPagedAsync(new PagedRequestDto { PageNumber = 0, PageSize = 999 });
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        // ── FilterPagedAsync ──────────────────────────────────────────────

        [Fact]
        public async Task FilterPagedAsync_AllFilters_ReturnsFiltered()
        {
            var hotels = new List<Hotel>
            {
                new() { HotelId = 1, HotelName = "A", Location = "Mumbai", StarRating = 4, TotalRooms = 10, IsActive = true },
                new() { HotelId = 2, HotelName = "B", Location = "Delhi",  StarRating = 3, TotalRooms = 10, IsActive = true },
                new() { HotelId = 3, HotelName = "C", Location = "Mumbai", StarRating = 2, TotalRooms = 10, IsActive = true }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(hotels);

            var result = await _sut.FilterPagedAsync(
                new HotelFilterDto { HotelId = 1, Location = "Mumbai", MinRating = 3 },
                new PagedRequestDto { PageNumber = 1, PageSize = 10 });

            Assert.Equal(1, result.TotalRecords);
        }

        [Fact]
        public async Task FilterPagedAsync_NoFilters_ReturnsAllActive()
        {
            var hotels = new List<Hotel>
            {
                new() { HotelId = 1, HotelName = "A", Location = "Mumbai", StarRating = 4, TotalRooms = 10, IsActive = true },
                new() { HotelId = 2, HotelName = "B", Location = "Delhi",  StarRating = 3, TotalRooms = 10, IsActive = false }
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(hotels);

            var result = await _sut.FilterPagedAsync(new HotelFilterDto(), new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(1, result.TotalRecords);
        }

        // ── SearchAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task SearchAsync_ValidLocation_ReturnsMatches()
        {
            _repoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                     .ReturnsAsync(new List<Hotel> { ActiveHotel() });
            var result = (await _sut.SearchAsync("Mumbai")).ToList();
            Assert.Single(result);
        }

        [Fact]
        public async Task SearchAsync_EmptyLocation_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.SearchAsync("  "));
        }

        // ── UpdateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ValidHotel_ReturnsUpdatedDto()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync((int _, Hotel h) => h);

            var result = await _sut.UpdateAsync(1, new CreateHotelDto { HotelName = "Updated", Location = "Delhi", StarRating = 5, TotalRooms = 100 });
            Assert.Equal("Updated", result!.HotelName);
        }

        [Fact]
        public async Task UpdateAsync_UpdateReturnsNull_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync((Hotel?)null);

            var result = await _sut.UpdateAsync(1, ValidDto());
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_HotelNull_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(99, ValidDto()));
        }

        [Fact]
        public async Task UpdateAsync_HotelInactive_ThrowsNotFoundException()
        {
            var hotel = ActiveHotel(); hotel.IsActive = false;
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(1, ValidDto()));
        }

        // ── DeactivateAsync ───────────────────────────────────────────────

        [Fact]
        public async Task DeactivateAsync_ActiveHotel_ReturnsTrue()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync((int _, Hotel h) => h);
            Assert.True(await _sut.DeactivateAsync(1));
        }

        [Fact]
        public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeactivateAsync(99));
        }

        [Fact]
        public async Task DeactivateAsync_AlreadyInactive_ThrowsBadRequest()
        {
            var hotel = ActiveHotel(); hotel.IsActive = false;
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.DeactivateAsync(1));
        }
    }
}
