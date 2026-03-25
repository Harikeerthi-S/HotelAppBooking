using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/bookingroom")]
    [Authorize]
    [Produces("application/json")]
    public class BookingRoomController : ControllerBase
    {
        private readonly IBookingRoomService           _service;
        private readonly ILogger<BookingRoomController> _logger;

        public BookingRoomController(IBookingRoomService service, ILogger<BookingRoomController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Add a room line-item to a booking. User or Admin.</summary>
        [HttpPost]
        [Authorize(Roles = "user,admin")]
        public async Task<IActionResult> Create([FromBody] CreateBookingRoomDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("CreateBookingRoom: Booking={BookingId} Room={RoomId}", dto.BookingId, dto.RoomId);
                var result = await _service.CreateAsync(dto);
                _logger.LogInformation("BookingRoom created: {BookingRoomId}", result.BookingRoomId);
                return CreatedAtAction(nameof(GetById), new { bookingRoomId = result.BookingRoomId }, result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("CreateBookingRoom not found: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("CreateBookingRoom bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating BookingRoom");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while creating the booking room.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get a booking room by ID. User, Admin, HotelManager.</summary>
        [HttpGet("{bookingRoomId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetById(int bookingRoomId)
        {
            try
            {
                _logger.LogInformation("GetBookingRoomById: {BookingRoomId}", bookingRoomId);
                var result = await _service.GetByIdAsync(bookingRoomId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("BookingRoom not found: {BookingRoomId}", bookingRoomId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BookingRoom {BookingRoomId}", bookingRoomId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the booking room.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get all room line-items for a booking. User, Admin, HotelManager.</summary>
        [HttpGet("booking/{bookingId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            try
            {
                _logger.LogInformation("GetBookingRoomsByBooking: {BookingId}", bookingId);
                var result = await _service.GetByBookingAsync(bookingId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking rooms for booking {BookingId}", bookingId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving booking rooms.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Update a booking room entry. User or Admin.</summary>
        [HttpPut("{bookingRoomId:int}")]
        [Authorize(Roles = "user,admin")]
        public async Task<IActionResult> Update(int bookingRoomId, [FromBody] CreateBookingRoomDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("UpdateBookingRoom: {BookingRoomId}", bookingRoomId);
                var result = await _service.UpdateAsync(bookingRoomId, dto);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("BookingRoom not found for update: {BookingRoomId}", bookingRoomId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating BookingRoom {BookingRoomId}", bookingRoomId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while updating the booking room.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Delete a booking room entry. User or Admin.</summary>
        [HttpDelete("{bookingRoomId:int}")]
        [Authorize(Roles = "user,admin")]
        public async Task<IActionResult> Delete(int bookingRoomId)
        {
            try
            {
                _logger.LogInformation("DeleteBookingRoom: {BookingRoomId}", bookingRoomId);
                await _service.DeleteAsync(bookingRoomId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("BookingRoom not found for delete: {BookingRoomId}", bookingRoomId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting BookingRoom {BookingRoomId}", bookingRoomId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while deleting the booking room.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
