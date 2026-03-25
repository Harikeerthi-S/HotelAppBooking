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
        [HttpGet("{roomId}")]
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
        [HttpPut("{roomId}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Update(int roomId, [FromBody] CreateRoomDto dto)
        {
            var room = await _roomService.UpdateAsync(roomId, dto);
            return Ok(room);
        }

        // DELETE (Deactivate)
        [HttpDelete("{roomId}")]
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
    }
}