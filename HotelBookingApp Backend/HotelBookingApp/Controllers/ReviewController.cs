using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/review")]
    [Produces("application/json")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService           _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _logger        = logger;
        }

        /// <summary>Create a hotel review. User only. One review per hotel per user.</summary>
        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("CreateReview: Hotel={HotelId} User={UserId}", dto.HotelId, dto.UserId);
                var review = await _reviewService.CreateAsync(dto);
                _logger.LogInformation("Review created: {ReviewId}", review.ReviewId);
                return CreatedAtAction(nameof(GetById), new { reviewId = review.ReviewId }, review);
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("CreateReview bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("CreateReview not found: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (AlreadyExistsException ex)
            {
                _logger.LogWarning("CreateReview conflict: {Message}", ex.Message);
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating review");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while creating the review.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get review by ID. All authenticated users.</summary>
        [HttpGet("{reviewId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetById(int reviewId)
        {
            try
            {
                _logger.LogInformation("GetReviewById: {ReviewId}", reviewId);
                var review = await _reviewService.GetByIdAsync(reviewId);
                return Ok(review);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Review not found: {ReviewId}", reviewId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching review {ReviewId}", reviewId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the review.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get paginated reviews with optional filters. All authenticated users.</summary>
        [HttpPost("paged")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetPaged([FromBody] ReviewFilterDto filter, [FromQuery] PagedRequestDto request)
        {
            try
            {
                _logger.LogInformation("GetReviewsPaged: HotelId={HotelId} Page={Page}", filter.HotelId, request.PageNumber);
                var result = await _reviewService.GetPagedAsync(filter, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paged reviews");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving reviews.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Delete a review. Admin and HotelManager only.</summary>
        [HttpDelete("{reviewId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Delete(int reviewId)
        {
            try
            {
                _logger.LogInformation("DeleteReview: {ReviewId}", reviewId);
                await _reviewService.DeleteAsync(reviewId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Review not found for delete: {ReviewId}", reviewId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while deleting the review.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
