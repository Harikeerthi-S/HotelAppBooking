using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/hotel")]
    [Produces("application/json")]
    public class HotelController : ControllerBase
    {
        private readonly IHotelService           _hotelService;
        private readonly IHotelAmenityService    _hotelAmenityService;
        private readonly ILogger<HotelController> _logger;

        public HotelController(
            IHotelService hotelService,
            IHotelAmenityService hotelAmenityService,
            ILogger<HotelController> logger)
        {
            _hotelService        = hotelService;
            _hotelAmenityService = hotelAmenityService;
            _logger              = logger;
        }

        /// <summary>Create a new hotel. Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Create([FromBody] CreateHotelDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("Creating hotel: {Name}", dto.HotelName);
                var hotel = await _hotelService.CreateAsync(dto);
                _logger.LogInformation("Hotel created: {HotelId}", hotel.HotelId);
                return CreatedAtAction(nameof(GetById), new { hotelId = hotel.HotelId }, hotel);
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("Create hotel bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (AlreadyExistsException ex)
            {
                _logger.LogWarning("Create hotel conflict: {Message}", ex.Message);
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating hotel");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while creating the hotel.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get paginated hotels. Anonymous access.</summary>
        [HttpPost("paged")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged([FromBody] PagedRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("GetHotelsPaged: Page {Page} Size {Size}", request.PageNumber, request.PageSize);
                var result = await _hotelService.GetPagedAsync(request);
                return Ok(result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paged hotels");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving hotels.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Filter hotels with pagination. Anonymous access.</summary>
        [HttpPost("filter")]
        [AllowAnonymous]
        public async Task<IActionResult> Filter([FromBody] HotelFilterDto filter, [FromQuery] PagedRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("FilterHotels: Location={Location} MinRating={MinRating}", filter.Location, filter.MinRating);
                var result = await _hotelService.FilterPagedAsync(filter, request);
                return Ok(result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering hotels");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while filtering hotels.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get hotel by ID. Anonymous access.</summary>
        [HttpGet("{hotelId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int hotelId)
        {
            try
            {
                _logger.LogInformation("GetHotelById: {HotelId}", hotelId);
                var hotel = await _hotelService.GetByIdAsync(hotelId);
                return Ok(hotel);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Hotel not found: {HotelId}", hotelId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching hotel {HotelId}", hotelId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the hotel.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Search hotels by location. Anonymous access.</summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromQuery] string location)
        {
            try
            {
                _logger.LogInformation("SearchHotels: {Location}", location);
                var hotels = await _hotelService.SearchAsync(location);
                return Ok(hotels);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching hotels");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while searching hotels.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get amenities assigned to a hotel. Anonymous access.</summary>
        [HttpGet("{hotelId:int}/amenities")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAmenities(int hotelId)
        {
            try
            {
                var list = await _hotelAmenityService.GetByHotelAsync(hotelId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching amenities for hotel {HotelId}", hotelId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error retrieving hotel amenities.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Update hotel details. Admin only.</summary>
        [HttpPut("{hotelId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Update(int hotelId, [FromBody] CreateHotelDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("UpdateHotel: {HotelId}", hotelId);
                var hotel = await _hotelService.UpdateAsync(hotelId, dto);
                return Ok(hotel);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating hotel {HotelId}", hotelId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while updating the hotel.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Soft-delete a hotel. Admin only.</summary>
        [HttpDelete("{hotelId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Deactivate(int hotelId)
        {
            try
            {
                _logger.LogInformation("DeactivateHotel: {HotelId}", hotelId);
                await _hotelService.DeactivateAsync(hotelId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating hotel {HotelId}", hotelId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while deactivating the hotel.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
