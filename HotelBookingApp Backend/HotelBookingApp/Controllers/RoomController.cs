using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/room")]
    [Produces("application/json")]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService            _roomService;
        private readonly ILogger<RoomController> _logger;

        public RoomController(IRoomService roomService, ILogger<RoomController> logger)
        {
            _roomService = roomService;
            _logger      = logger;
        }

        /// <summary>Create a room. Admin or HotelManager only.</summary>
        [HttpPost]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("Creating room #{RoomNumber} for hotel {HotelId}", dto.RoomNumber, dto.HotelId);
                var room = await _roomService.CreateAsync(dto);
                _logger.LogInformation("Room created: {RoomId}", room.RoomId);
                return CreatedAtAction(nameof(GetById), new { roomId = room.RoomId }, room);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Hotel not found for room creation: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (AlreadyExistsException ex)
            {
                _logger.LogWarning("Room conflict: {Message}", ex.Message);
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("Create room bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating room");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while creating the room.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get room by ID. Anonymous access.</summary>
        [HttpGet("{roomId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int roomId)
        {
            try
            {
                _logger.LogInformation("GetRoomById: {RoomId}", roomId);
                var room = await _roomService.GetByIdAsync(roomId);
                return Ok(room);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Room not found: {RoomId}", roomId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room {RoomId}", roomId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the room.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get all rooms, optionally filtered by hotelId. Anonymous access.</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] int? hotelId)
        {
            try
            {
                _logger.LogInformation("GetAllRooms: hotelId={HotelId}", hotelId);
                var rooms = await _roomService.GetAllAsync(hotelId);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all rooms");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving rooms.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Update room details. Admin or HotelManager only.</summary>
        [HttpPut("{roomId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Update(int roomId, [FromBody] CreateRoomDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("UpdateRoom: {RoomId}", roomId);
                var room = await _roomService.UpdateAsync(roomId, dto);
                return Ok(room);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Room not found for update: {RoomId}", roomId);
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
                _logger.LogError(ex, "Error updating room {RoomId}", roomId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while updating the room.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Deactivate a room. Admin or HotelManager only.</summary>
        [HttpDelete("{roomId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Deactivate(int roomId)
        {
            try
            {
                _logger.LogInformation("DeactivateRoom: {RoomId}", roomId);
                await _roomService.DeactivateAsync(roomId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Room not found for deactivation: {RoomId}", roomId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating room {RoomId}", roomId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while deactivating the room.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Filter rooms by criteria. Anonymous access.</summary>
        [HttpPost("filter")]
        [AllowAnonymous]
        public async Task<IActionResult> Filter([FromBody] RoomFilterDto filter)
        {
            try
            {
                _logger.LogInformation("FilterRooms: HotelId={HotelId} Type={Type}", filter.HotelId, filter.RoomType);
                var rooms = await _roomService.FilterAsync(filter);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering rooms");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while filtering rooms.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Filter rooms with pagination. Anonymous access.</summary>
        [HttpPost("filter/paged")]
        [AllowAnonymous]
        public async Task<IActionResult> FilterPaged([FromBody] RoomFilterDto filter, [FromQuery] PagedRequestDto request)
        {
            try
            {
                _logger.LogInformation("FilterRoomsPaged: Page={Page}", request.PageNumber);
                var result = await _roomService.FilterPagedAsync(filter, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering rooms paged");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while filtering rooms.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
