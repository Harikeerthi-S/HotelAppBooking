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
    public class ReviewServiceTests
    {
        private readonly Mock<IRepository<int, Review>> _reviewRepoMock;
        private readonly Mock<IRepository<int, Hotel>>  _hotelRepoMock;
        private readonly Mock<IRepository<int, User>>   _userRepoMock;
        private readonly Mock<IAuditLogService>         _auditMock;
        private readonly ReviewService                  _sut;

        public ReviewServiceTests()
        {
            _reviewRepoMock = new Mock<IRepository<int, Review>>();
            _hotelRepoMock  = new Mock<IRepository<int, Hotel>>();
            _userRepoMock   = new Mock<IRepository<int, User>>();
            _auditMock      = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new ReviewService(_reviewRepoMock.Object, _hotelRepoMock.Object,
                _userRepoMock.Object, _auditMock.Object, MockLogger.Create<ReviewService>());
        }

        private static Hotel ActiveHotel()   => new() { HotelId = 1, HotelName = "H", Location = "L", IsActive = true };
        private static Hotel InactiveHotel() => new() { HotelId = 1, HotelName = "H", Location = "L", IsActive = false };
        private static User SampleUser()     => new() { UserId = 1, UserName = "Alice", Email = "a@a.com", Role = "user" };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidReview_ReturnsDto()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            _reviewRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
                           .ReturnsAsync(false);
            _reviewRepoMock.Setup(r => r.AddAsync(It.IsAny<Review>()))
                           .ReturnsAsync((Review r) => { r.ReviewId = 1; return r; });

            var result = await _sut.CreateAsync(new CreateReviewDto { HotelId = 1, UserId = 1, Rating = 4, Comment = "Great!" });
            Assert.Equal(1, result.ReviewId);
            Assert.Equal(4, result.Rating);
        }

        [Fact]
        public async Task CreateAsync_RatingZero_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.CreateAsync(new CreateReviewDto { HotelId = 1, UserId = 1, Rating = 0, Comment = "X" }));
        }

        [Fact]
        public async Task CreateAsync_RatingSix_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.CreateAsync(new CreateReviewDto { HotelId = 1, UserId = 1, Rating = 6, Comment = "X" }));
        }

        [Fact]
        public async Task CreateAsync_HotelNull_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateReviewDto { HotelId = 99, UserId = 1, Rating = 4, Comment = "X" }));
        }

        [Fact]
        public async Task CreateAsync_HotelInactive_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(InactiveHotel());
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateReviewDto { HotelId = 1, UserId = 1, Rating = 4, Comment = "X" }));
        }

        [Fact]
        public async Task CreateAsync_UserNotFound_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateReviewDto { HotelId = 1, UserId = 99, Rating = 4, Comment = "X" }));
        }

        [Fact]
        public async Task CreateAsync_DuplicateReview_ThrowsAlreadyExists()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            _reviewRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
                           .ReturnsAsync(true);
            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.CreateAsync(new CreateReviewDto { HotelId = 1, UserId = 1, Rating = 3, Comment = "Again" }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _reviewRepoMock.Setup(r => r.GetByIdAsync(1))
                           .ReturnsAsync(new Review { ReviewId = 1, HotelId = 1, UserId = 1, Rating = 5, Comment = "Excellent" });
            var result = await _sut.GetByIdAsync(1);
            Assert.Equal(5, result!.Rating);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _reviewRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Review?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetPagedAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_WithHotelFilter_ReturnsPaged()
        {
            var reviews = Enumerable.Range(1, 8).Select(i => new Review
            {
                ReviewId = i, HotelId = 1, UserId = i, Rating = 4, Comment = $"R{i}", CreatedAt = DateTime.UtcNow
            }).ToList();
            _reviewRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

            var result = await _sut.GetPagedAsync(
                new ReviewFilterDto { HotelId = 1 },
                new PagedRequestDto { PageNumber = 1, PageSize = 5 });
            Assert.Equal(5, result.Data.Count);
            Assert.Equal(8, result.TotalRecords);
        }

        [Fact]
        public async Task GetPagedAsync_WithUserFilter_ReturnsFiltered()
        {
            var reviews = new List<Review>
            {
                new() { ReviewId = 1, HotelId = 1, UserId = 1, Rating = 4, Comment = "A", CreatedAt = DateTime.UtcNow },
                new() { ReviewId = 2, HotelId = 1, UserId = 2, Rating = 3, Comment = "B", CreatedAt = DateTime.UtcNow }
            };
            _reviewRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

            var result = await _sut.GetPagedAsync(
                new ReviewFilterDto { UserId = 1 },
                new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(1, result.TotalRecords);
        }

        [Fact]
        public async Task GetPagedAsync_WithRatingFilter_ReturnsFiltered()
        {
            var reviews = new List<Review>
            {
                new() { ReviewId = 1, HotelId = 1, UserId = 1, Rating = 5, Comment = "A", CreatedAt = DateTime.UtcNow },
                new() { ReviewId = 2, HotelId = 1, UserId = 2, Rating = 3, Comment = "B", CreatedAt = DateTime.UtcNow }
            };
            _reviewRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

            var result = await _sut.GetPagedAsync(
                new ReviewFilterDto { Rating = 5 },
                new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(1, result.TotalRecords);
        }

        [Fact]
        public async Task GetPagedAsync_NoFilters_ReturnsAll()
        {
            var reviews = Enumerable.Range(1, 3).Select(i => new Review
            {
                ReviewId = i, HotelId = i, UserId = i, Rating = 4, Comment = "X", CreatedAt = DateTime.UtcNow
            }).ToList();
            _reviewRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

            var result = await _sut.GetPagedAsync(new ReviewFilterDto(), new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(3, result.TotalRecords);
        }

        // ── DeleteAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_Exists_ReturnsTrue()
        {
            _reviewRepoMock.Setup(r => r.DeleteAsync(1))
                           .ReturnsAsync(new Review { ReviewId = 1, HotelId = 1, UserId = 1, Rating = 4, Comment = "X" });
            Assert.True(await _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _reviewRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Review?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
        }
    }
}
