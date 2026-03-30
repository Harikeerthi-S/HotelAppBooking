using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class ReviewServiceExtendedTests
{
    private readonly Mock<IRepository<int, Review>>  _reviewRepo  = new();
    private readonly Mock<IRepository<int, Hotel>>   _hotelRepo   = new();
    private readonly Mock<IRepository<int, User>>    _userRepo    = new();
    private readonly Mock<IAuditLogService>          _audit       = new();
    private readonly Mock<ILogger<ReviewService>>    _logger      = new();

    private ReviewService CreateService() =>
        new(_reviewRepo.Object, _hotelRepo.Object, _userRepo.Object, _audit.Object, _logger.Object);

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_NoFilter_ReturnsPaginatedReviews()
    {
        var reviews = Enumerable.Range(1, 12).Select(i => new Review
        {
            ReviewId = i, HotelId = 1, UserId = i, Rating = (i % 5) + 1,
            Comment = $"Review {i}", CreatedAt = DateTime.UtcNow
        }).ToList();

        _reviewRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

        var result = await CreateService().GetPagedAsync(
            new ReviewFilterDto(),
            new PagedRequestDto { PageNumber = 1, PageSize = 10 }
        );

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(12, result.TotalRecords);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByHotelId_ReturnsFilteredReviews()
    {
        var reviews = new List<Review>
        {
            new() { ReviewId = 1, HotelId = 2, UserId = 1, Rating = 5, Comment = "Excellent!", CreatedAt = DateTime.UtcNow },
            new() { ReviewId = 2, HotelId = 2, UserId = 2, Rating = 4, Comment = "Good",       CreatedAt = DateTime.UtcNow },
            new() { ReviewId = 3, HotelId = 3, UserId = 1, Rating = 3, Comment = "Average",    CreatedAt = DateTime.UtcNow }
        };

        _reviewRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

        var result = await CreateService().GetPagedAsync(
            new ReviewFilterDto { HotelId = 2 },
            new PagedRequestDto { PageNumber = 1, PageSize = 10 }
        );

        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, r => Assert.Equal(2, r.HotelId));
    }

    [Fact]
    public async Task GetPagedAsync_FilterByRating_ReturnsExactRatingMatch()
    {
        var reviews = new List<Review>
        {
            new() { ReviewId = 1, HotelId = 1, UserId = 1, Rating = 5, Comment = "Excellent!", CreatedAt = DateTime.UtcNow },
            new() { ReviewId = 2, HotelId = 1, UserId = 2, Rating = 2, Comment = "Poor",       CreatedAt = DateTime.UtcNow },
            new() { ReviewId = 3, HotelId = 2, UserId = 3, Rating = 4, Comment = "Good",       CreatedAt = DateTime.UtcNow }
        };

        _reviewRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

        var result = await CreateService().GetPagedAsync(
            new ReviewFilterDto { Rating = 4 },
            new PagedRequestDto { PageNumber = 1, PageSize = 10 }
        );

        Assert.Single(result.Data); // exact match: only rating == 4
        Assert.Equal(4, result.Data[0].Rating);
    }

    // ── CreateAsync — duplicate review ────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DuplicateReview_ThrowsAlreadyExistsException()
    {
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        var user  = new User  { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _reviewRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
                   .ReturnsAsync(true);

        var dto = new CreateReviewDto { UserId = 1, HotelId = 1, Rating = 4, Comment = "Good" };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().CreateAsync(dto));
    }

    // ── CreateAsync — rating boundary ─────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task CreateAsync_InvalidRatingBoundary_ThrowsBadRequestException(int rating)
    {
        var dto = new CreateReviewDto { UserId = 1, HotelId = 1, Rating = rating, Comment = "Test" };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingReview_ReturnsDto()
    {
        var review = new Review { ReviewId = 1, HotelId = 1, UserId = 1, Rating = 5, Comment = "Great!", CreatedAt = DateTime.UtcNow };
        _reviewRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);

        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Rating);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _reviewRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Review?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(99));
    }
}
