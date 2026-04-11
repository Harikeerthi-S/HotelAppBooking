using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/hotelamenity")]
    [Authorize]
    [Produces("application/json")]
    public class HotelAmenityController : ControllerBase
    {
        private readonly IHotelAmenityService           _service;
        private readonly ILogger<HotelAmenityController> _logger;

        public HotelAmenityController(IHotelAmenityService service, ILogger<HotelAmenityController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Get all hotel-amenity assignments. Admin and HotelManager.</summary>
        [HttpGet]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("GetAllHotelAmenities called");
                var list = await _service.GetAllAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all hotel amenities");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving hotel amenities.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get amenities assigned to a specific hotel. Public.</summary>
        [HttpGet("hotel/{hotelId:int}", Order = 0)]
        [AllowAnonymous]
        public async Task<IActionResult> GetByHotel(int hotelId)
        {
            try
            {
                var list = await _service.GetByHotelAsync(hotelId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenities for hotel {HotelId}", hotelId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error retrieving hotel amenities.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get a hotel-amenity assignment by ID. Admin and HotelManager.</summary>
        [HttpGet("{id:int}", Order = 1)]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation("GetHotelAmenityById: {Id}", id);
                var result = await _service.GetByIdAsync(id);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("HotelAmenity not found: {Id}", id);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hotel amenity {Id}", id);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the hotel amenity.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Assign an amenity to a hotel. Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Create([FromBody] CreateHotelAmenityDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("AssignAmenity: Hotel={HotelId} Amenity={AmenityId}", dto.HotelId, dto.AmenityId);
                var result = await _service.CreateAsync(dto);
                _logger.LogInformation("HotelAmenity created: {HotelAmenityId}", result.HotelAmenityId);
                return CreatedAtAction(nameof(GetById), new { id = result.HotelAmenityId }, result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("AssignAmenity not found: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (AlreadyExistsException ex)
            {
                _logger.LogWarning("AssignAmenity conflict: {Message}", ex.Message);
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error assigning amenity");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while assigning the amenity.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Remove an amenity from a hotel. Admin only.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("DeleteHotelAmenity: {Id}", id);
                await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("HotelAmenity not found for delete: {Id}", id);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting hotel amenity {Id}", id);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while removing the hotel amenity.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
