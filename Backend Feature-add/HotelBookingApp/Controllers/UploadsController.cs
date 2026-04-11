using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    /// <summary>
    /// Fallback controller to serve uploaded files (review photos etc.)
    /// when static files middleware doesn't pick them up.
    /// GET /uploads/reviews/{filename}
    /// </summary>
    [ApiController]
    [Route("uploads")]
    public class UploadsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UploadsController> _logger;

        public UploadsController(IWebHostEnvironment env, ILogger<UploadsController> logger)
        {
            _env    = env;
            _logger = logger;
        }

        [HttpGet("reviews/{fileName}")]
        public IActionResult GetReviewPhoto(string fileName)
        {
            // Sanitise — no path traversal
            var safe = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safe))
                return BadRequest("Invalid file name.");

            var uploadsDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "reviews");
            var filePath   = Path.Combine(uploadsDir, safe);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Review photo not found: {Path}", filePath);
                return NotFound();
            }

            var ext         = Path.GetExtension(safe).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".webp"           => "image/webp",
                _                 => "application/octet-stream"
            };

            // Cache for 7 days
            Response.Headers["Cache-Control"] = "public, max-age=604800";
            return PhysicalFile(filePath, contentType);
        }
    }
}
