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
        private readonly IReviewService            _reviewService;
        private readonly IWebHostEnvironment       _env;
        private readonly ILogger<ReviewController> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public ReviewController(
            IReviewService            reviewService,
            IWebHostEnvironment       env,
            ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _env           = env;
            _logger        = logger;
        }

        /// <summary>Create a hotel review. Optionally include a PhotoUrl for +100 wallet credits.</summary>
        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var review = await _reviewService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { reviewId = review.ReviewId }, review);
            }
            catch (BadRequestException ex)    { return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow }); }
            catch (NotFoundException ex)      { return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow }); }
            catch (AlreadyExistsException ex) { return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error creating review.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>
        /// Upload a photo for an existing review.
        /// Awards 100 wallet credits to the reviewer. One photo per review.
        /// </summary>
        [HttpPost("{reviewId:int}/photo")]
        [Authorize(Roles = "user")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto(int reviewId, IFormFile photo)
        {
            try
            {
                if (photo is null || photo.Length == 0)
                    return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = "No file uploaded.", Timestamp = DateTime.UtcNow });

                if (photo.Length > MaxFileSizeBytes)
                    return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = "File size must be ≤ 5 MB.", Timestamp = DateTime.UtcNow });

                var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                    return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = "Only .jpg, .jpeg, .png, .webp files are allowed.", Timestamp = DateTime.UtcNow });

                // Save to wwwroot/uploads/reviews/
                var webRoot     = _env.WebRootPath
                                  ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var uploadsDir  = Path.Combine(webRoot, "uploads", "reviews");
                Directory.CreateDirectory(uploadsDir);

                var fileName  = $"review_{reviewId}_{Guid.NewGuid():N}{ext}";
                var filePath  = Path.Combine(uploadsDir, fileName);

                await using (var stream = System.IO.File.Create(filePath))
                    await photo.CopyToAsync(stream);

                var photoUrl = $"/uploads/reviews/{fileName}";

                var result = await _reviewService.UploadPhotoAsync(reviewId, photoUrl);
                return Ok(result);
            }
            catch (BadRequestException ex) { return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow }); }
            catch (NotFoundException ex)   { return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for review {ReviewId}", reviewId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error uploading photo.", Timestamp = DateTime.UtcNow });
            }
        }

        [HttpGet("{reviewId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetById(int reviewId)
        {
            try
            {
                var review = await _reviewService.GetByIdAsync(reviewId);
                return Ok(review);
            }
            catch (NotFoundException ex) { return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching review {ReviewId}", reviewId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error retrieving review.", Timestamp = DateTime.UtcNow });
            }
        }

        [HttpPost("paged")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetPaged([FromBody] ReviewFilterDto filter, [FromQuery] PagedRequestDto request)
        {
            try
            {
                var result = await _reviewService.GetPagedAsync(filter, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paged reviews");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error retrieving reviews.", Timestamp = DateTime.UtcNow });
            }
        }

        [HttpDelete("{reviewId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Delete(int reviewId)
        {
            try
            {
                await _reviewService.DeleteAsync(reviewId);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error deleting review.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
