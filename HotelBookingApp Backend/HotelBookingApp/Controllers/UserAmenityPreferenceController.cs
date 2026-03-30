using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/user-amenity-preference")]
    [Authorize]
    [Produces("application/json")]
    public class UserAmenityPreferenceController : ControllerBase
    {
        private readonly IUserAmenityPreferenceService _service;
        private readonly ILogger<UserAmenityPreferenceController> _logger;

        public UserAmenityPreferenceController(
            IUserAmenityPreferenceService service,
            ILogger<UserAmenityPreferenceController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>User selects a preferred amenity.</summary>
        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Add([FromBody] CreateUserAmenityPreferenceDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _service.AddAsync(dto);
                return CreatedAtAction(nameof(GetByUser), new { userId = dto.UserId }, result);
            }
            catch (AlreadyExistsException ex) { return Conflict(new { message = ex.Message }); }
            catch (NotFoundException ex)      { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding preference");
                // Likely table doesn't exist — tell frontend clearly
                return StatusCode(503, new { message = "Preference service unavailable. Please run database migrations." });
            }
        }

        /// <summary>Get all preferences for a user.</summary>
        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                var result = await _service.GetByUserAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preferences for user {UserId}", userId);
                // Return empty list instead of 500 — table may not exist yet
                return Ok(Array.Empty<object>());
            }
        }

        /// <summary>Get all user preferences. Admin and HotelManager only.</summary>
        [HttpGet("all")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _service.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all preferences");
                return Ok(Array.Empty<object>());
            }
        }

        /// <summary>Approve a user amenity preference. Admin and HotelManager only. (POST — broad client/proxy support)</summary>
        [HttpPost("{preferenceId:int}/approve")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Approve(int preferenceId)
        {
            try
            {
                var result = await _service.ApproveAsync(preferenceId);
                return Ok(result);
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving preference {Id}", preferenceId);
                return StatusCode(500, new { message = "Error updating preference." });
            }
        }

        /// <summary>Reject a user amenity preference. Admin and HotelManager only.</summary>
        [HttpPost("{preferenceId:int}/reject")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Reject(int preferenceId)
        {
            try
            {
                var result = await _service.RejectAsync(preferenceId);
                return Ok(result);
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting preference {Id}", preferenceId);
                return StatusCode(500, new { message = "Error updating preference." });
            }
        }

        /// <summary>Remove a preference by ID.</summary>
        [HttpDelete("{preferenceId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> Remove(int preferenceId)
        {
            try
            {
                await _service.RemoveAsync(preferenceId);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing preference {Id}", preferenceId);
                return StatusCode(500, new { message = "Error removing preference." });
            }
        }

        /// <summary>Remove by user + amenity combo.</summary>
        [HttpDelete("user/{userId:int}/amenity/{amenityId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> RemoveByUserAmenity(int userId, int amenityId)
        {
            try
            {
                await _service.RemoveByUserAndAmenityAsync(userId, amenityId);
                return NoContent();
            }
            catch (NotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing preference");
                return StatusCode(500, new { message = "Error removing preference." });
            }
        }
    }
}
