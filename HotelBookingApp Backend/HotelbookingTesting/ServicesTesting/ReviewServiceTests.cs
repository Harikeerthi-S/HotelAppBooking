using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class ReviewServiceTests
{
    private readonly Mock<IRepository<int, Review>>  _reviewRepo  = new();
    private readonly Mock<IRepository<int, Hotel>>   _hotelRepo   = new();
    private readonly Mock<IRepository<int, User>>    _userRepo    = new();
    private readonly Mock<IAuditLogService>          _audit       = new();
    private readonly Mock<ILogger<ReviewService>>    _logger      = new();

    // ReviewService(reviewRepo, hotelRepo, userRepo, audit, logger)
    private ReviewService CreateService() =>
        new(_reviewRepo.Object, _hotelRepo.Object, _userRepo.Object, _audit.Object, _logger.Object);

    [Fact]
    public async Task CreateAsync_ValidReview_ReturnsReviewResponseDto()
    {
        var hotel   = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        var user    = new User  { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };
        var dto     = new CreateReviewDto { UserId = 1, HotelId = 1, Rating = 5, Comment = "Excellent!" };

        _hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _reviewRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
                   .ReturnsAsync(false); // no duplicate
        _reviewRepo.Setup(r => r.AddAsync(It.IsAny<Review>()))
                   .ReturnsAsync((Review rv) => { rv.ReviewId = 1; return rv; });
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Excellent!", result.Comment);
    }

    [Fact]
    public async Task CreateAsync_InvalidRating_ThrowsBadRequestException()
    {
        var dto = new CreateReviewDto { UserId = 1, HotelId = 1, Rating = 6, Comment = "Bad" };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_HotelNotFound_ThrowsNotFoundException()
    {
        _hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
        var dto = new CreateReviewDto { UserId = 1, HotelId = 99, Rating = 4, Comment = "Good" };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task DeleteAsync_ExistingReview_ReturnsTrue()
    {
        var review = new Review { ReviewId = 1, UserId = 1, HotelId = 1, Rating = 4, Comment = "Nice" };
        _reviewRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(review);
        _reviewRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(review);
        _audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var result = await CreateService().DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _reviewRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Review?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99));
    }
}
