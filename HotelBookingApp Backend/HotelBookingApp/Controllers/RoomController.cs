using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/room")]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        // CREATE
        [HttpPost]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            var room = await _roomService.CreateAsync(dto);
            return Ok(room);
        }

        // GET BY ID
        [HttpGet("{roomId:int}")]
        public async Task<IActionResult> GetById(int roomId)
        {
            var room = await _roomService.GetByIdAsync(roomId);
            return Ok(room);
        }

        // GET ALL WITH PAGINATION
        [HttpPost("all/paged")]
        public async Task<IActionResult> GetAll(
            [FromBody] PagedRequestDto request,
            [FromQuery] int? hotelId)
        {
            var result = await _roomService.GetAllAsync(request, hotelId);
            return Ok(result);
        }

        // UPDATE
        [HttpPut("{roomId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Update(int roomId, [FromBody] CreateRoomDto dto)
        {
            var room = await _roomService.UpdateAsync(roomId, dto);
            return Ok(room);
        }

        // DELETE (Deactivate)
        [HttpDelete("{roomId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Deactivate(int roomId)
        {
            await _roomService.DeactivateAsync(roomId);
            return NoContent();
        }

        // FILTER
        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] RoomFilterDto filter)
        {
            var rooms = await _roomService.FilterAsync(filter);
            return Ok(rooms);
        }

        // CHECK DATE-RANGE AVAILABILITY
        [HttpGet("{roomId:int}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability(
            int roomId,
            [FromQuery] string checkIn,
            [FromQuery] string checkOut)
        {
            try
            {
                if (!DateTime.TryParse(checkIn, out var ciDate) ||
                    !DateTime.TryParse(checkOut, out var coDate))
                    return BadRequest(new { message = "Invalid date format. Use YYYY-MM-DD." });

                if (ciDate >= coDate)
                    return BadRequest(new { message = "Check-out must be after check-in." });

                var available = await _roomService.IsAvailableForDatesAsync(roomId, ciDate, coDate);
                return Ok(new { roomId, checkIn = ciDate, checkOut = coDate, isAvailable = available });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}