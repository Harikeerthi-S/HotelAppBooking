using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/amenities")]
    [Produces("application/json")]
    public class AmenitiesController : ControllerBase
    {
        private readonly IAmenityService           _amenityService;
        private readonly ILogger<AmenitiesController> _logger;

        public AmenitiesController(IAmenityService amenityService, ILogger<AmenitiesController> logger)
        {
            _amenityService = amenityService;
            _logger         = logger;
        }

        /// <summary>Get all amenities. Public.</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("GetAllAmenities called");
                var amenities = await _amenityService.GetAllAsync();
                return Ok(amenities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all amenities");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving amenities.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get amenity by ID. Public.</summary>
        [HttpGet("{amenityId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int amenityId)
        {
            try
            {
                _logger.LogInformation("GetAmenityById: {AmenityId}", amenityId);
                var amenity = await _amenityService.GetByIdAsync(amenityId);
                return Ok(amenity);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Amenity not found: {AmenityId}", amenityId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenity {AmenityId}", amenityId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the amenity.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Create a new amenity. Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CreateAmenityDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("CreateAmenity: {Name}", dto.Name);
                var amenity = await _amenityService.CreateAsync(dto);
                _logger.LogInformation("Amenity created: {AmenityId}", amenity.AmenityId);
                return CreatedAtAction(nameof(GetById), new { amenityId = amenity.AmenityId }, amenity);
            }
            catch (AlreadyExistsException ex)
            {
                _logger.LogWarning("Create amenity conflict: {Message}", ex.Message);
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("Create amenity bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating amenity");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while creating the amenity.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Update an amenity. Admin only.</summary>
        [HttpPut("{amenityId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int amenityId, [FromBody] CreateAmenityDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("UpdateAmenity: {AmenityId}", amenityId);
                await _amenityService.UpdateAsync(amenityId, dto);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Amenity not found for update: {AmenityId}", amenityId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (AlreadyExistsException ex)
            {
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating amenity {AmenityId}", amenityId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while updating the amenity.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Delete an amenity. Admin only.</summary>
        [HttpDelete("{amenityId:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int amenityId)
        {
            try
            {
                _logger.LogInformation("DeleteAmenity: {AmenityId}", amenityId);
                await _amenityService.DeleteAsync(amenityId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Amenity not found for delete: {AmenityId}", amenityId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting amenity {AmenityId}", amenityId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while deleting the amenity.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
