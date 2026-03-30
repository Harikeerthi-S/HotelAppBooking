using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/booking")]
    [Authorize]
    [Produces("application/json")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        // ?? CREATE ?????????????????????????????
        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var booking = await _bookingService.CreateAsync(dto);

                return CreatedAtAction(nameof(GetById),
                    new { bookingId = booking.BookingId }, booking);
            }
            catch (AlreadyExistsException ex)
            {
                return Conflict(Error(ex.Message, 409));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(Error(ex.Message, 400));
            }
            catch (NotFoundException ex)
            {
                return NotFound(Error(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? GET BY ID ??????????????????????????
        [HttpGet("{bookingId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetById(int bookingId)
        {
            try
            {
                var booking = await _bookingService.GetByIdAsync(bookingId);
                return Ok(booking);
            }
            catch (NotFoundException ex)
            {
                return NotFound(Error(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking {BookingId}", bookingId);
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? GET ALL (PAGED) ????????????????????
        [HttpPost("all/paged")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll([FromBody] PagedRequestDto request)
        {
            try
            {
                var result = await _bookingService.GetAllAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all bookings");
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? GET BY USER ????????????????????????
        [HttpPost("user/{userId:int}/paged")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetByUser(int userId, [FromBody] PagedRequestDto request)
        {
            try
            {
                var result = await _bookingService.GetByUserAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings for user {UserId}", userId);
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? GET BY HOTEL ???????????????????????
        [HttpPost("hotel/{hotelId:int}/paged")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetByHotel(int hotelId, [FromBody] PagedRequestDto request)
        {
            try
            {
                var result = await _bookingService.GetByHotelAsync(hotelId, request);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(Error(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings for hotel {HotelId}", hotelId);
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? GET PENDING BOOKINGS ???????????????
        [HttpGet("hotel/{hotelId:int}/pending")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetPending(int hotelId)
        {
            try
            {
                var result = await _bookingService.GetPendingByHotelAsync(hotelId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(Error(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending bookings");
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? CONFIRM ????????????????????????????
        [HttpPut("{bookingId:int}/confirm")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Confirm(int bookingId)
        {
            try
            {
                var booking = await _bookingService.ConfirmAsync(bookingId);
                return Ok(booking);
            }
            catch (NotFoundException ex)
            {
                return NotFound(Error(ex.Message, 404));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(Error(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming booking");
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? COMPLETE ???????????????????????????
        [HttpPut("{bookingId:int}/complete")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Complete(int bookingId)
        {
            try
            {
                var booking = await _bookingService.CompleteAsync(bookingId);
                return Ok(booking);
            }
            catch (NotFoundException ex)
            {
                return NotFound(Error(ex.Message, 404));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(Error(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing booking");
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? CANCEL ?????????????????????????????
        [HttpPut("{bookingId:int}/cancel")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            try
            {
                await _bookingService.CancelAsync(bookingId);
                return Ok(new { message = "Booking cancelled successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(Error(ex.Message, 404));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(Error(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking");
                return StatusCode(500, Error("Internal server error", 500));
            }
        }

        // ?? HELPER ?????????????????????????????
        private static ErrorResponseDto Error(string message, int code) => new()
        {
            StatusCode = code,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }
}