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

                _logger.LogInformation("CreateBooking: User={UserId}, Hotel={HotelId}", dto.UserId, dto.HotelId);

                var booking = await _bookingService.CreateAsync(dto);

                return CreatedAtAction(nameof(GetById), new { bookingId = booking.BookingId }, booking);
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("CreateBooking BadRequest: {Message}", ex.Message);

                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("CreateBooking NotFound: {Message}", ex.Message);

                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating the booking.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?? GET BY ID ??????????????????????????
        [HttpGet("{bookingId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetById(int bookingId)
        {
            try
            {
                _logger.LogInformation("GetBookingById: {BookingId}", bookingId);

                var booking = await _bookingService.GetByIdAsync(bookingId);

                return Ok(booking);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Booking not found: {BookingId}", bookingId);

                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking {BookingId}", bookingId);

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving the booking.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?? GET ALL (PAGED) ? FIXED ???????????
        [HttpPost("all/paged")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll([FromBody] PagedRequestDto request)
        {
            try
            {
                _logger.LogInformation("GetAllBookings");

                var result = await _bookingService.GetAllAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all bookings");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving bookings.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?? GET BY USER (PAGED) ????????????????
        [HttpPost("user/{userId:int}/paged")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetByUser(int userId, [FromBody] PagedRequestDto request)
        {
            try
            {
                _logger.LogInformation("GetBookingsByUser: UserId={UserId}", userId);

                var result = await _bookingService.GetByUserAsync(userId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings for user {UserId}", userId);

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving bookings.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?? GET BY HOTEL (PAGED) ???????????????
        [HttpPost("hotel/{hotelId:int}/paged")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetByHotel(int hotelId, [FromBody] PagedRequestDto request)
        {
            try
            {
                _logger.LogInformation("GetBookingsByHotel: HotelId={HotelId}", hotelId);

                var result = await _bookingService.GetByHotelAsync(hotelId, request);

                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings for hotel {HotelId}", hotelId);

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving bookings.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?? CONFIRM ????????????????????????????
        [HttpPut("{bookingId:int}/confirm")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Confirm(int bookingId)
        {
            try
            {
                _logger.LogInformation("ConfirmBooking: {BookingId}", bookingId);

                var booking = await _bookingService.ConfirmAsync(bookingId);

                return Ok(booking);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming booking {BookingId}", bookingId);

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while confirming the booking.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?? COMPLETE ???????????????????????????
        [HttpPut("{bookingId:int}/complete")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Complete(int bookingId)
        {
            try
            {
                _logger.LogInformation("CompleteBooking: {BookingId}", bookingId);

                var booking = await _bookingService.CompleteAsync(bookingId);

                return Ok(booking);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing booking {BookingId}", bookingId);

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while completing the booking.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?? CANCEL ?????????????????????????????
        [HttpPut("{bookingId:int}/cancel")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            try
            {
                _logger.LogInformation("CancelBooking: {BookingId}", bookingId);

                await _bookingService.CancelAsync(bookingId);

                return Ok(new { message = "Booking cancelled successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while cancelling the booking.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}