using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/payment")]
    [Authorize]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        // ?????????????????????????????????????????????
        // ? MAKE PAYMENT
        // ?????????????????????????????????????????????
        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> MakePayment([FromBody] PaymentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("MakePayment: Booking={BookingId}, Method={Method}",
                    dto.BookingId, dto.PaymentMethod);

                var result = await _paymentService.MakePaymentAsync(dto);

                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Booking not found: {Message}", ex.Message);

                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("Bad request: {Message}", ex.Message);

                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during payment");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while processing payment.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?????????????????????????????????????????????
        // ? GET ALL PAYMENTS (ADMIN / MANAGER)
        // ?????????????????????????????????????????????
        [HttpGet]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("Fetching all payments");

                var payments = await _paymentService.GetAllAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payments");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error retrieving payments",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?????????????????????????????????????????????
        // ? GET BY PAYMENT ID
        // ?????????????????????????????????????????????
        [HttpGet("{paymentId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetById(int paymentId)
        {
            try
            {
                _logger.LogInformation("Fetching payment by ID: {PaymentId}", paymentId);

                var payment = await _paymentService.GetByIdAsync(paymentId);

                return Ok(payment);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Payment not found: {PaymentId}", paymentId);

                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error retrieving payment",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?????????????????????????????????????????????
        // ?? GET BY BOOKING ID (FIXED API)
        // ?????????????????????????????????????????????
        [HttpGet("booking/{bookingId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetByBookingId(int bookingId)
        {
            try
            {
                _logger.LogInformation("Fetching payment by BookingId: {BookingId}", bookingId);

                var payment = await _paymentService.GetByBookingIdAsync(bookingId);

                return Ok(payment);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Payment not found for BookingId: {BookingId}", bookingId);

                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment by bookingId");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error retrieving payment",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ?????????????????????????????????????????????
        // ? UPDATE PAYMENT STATUS
        // ?????????????????????????????????????????????
        [HttpPut("{paymentId:int}/status")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> UpdateStatus(int paymentId, [FromQuery] string status)
        {
            try
            {
                _logger.LogInformation("Updating payment {PaymentId} ? {Status}", paymentId, status);

                var result = await _paymentService.UpdateStatusAsync(paymentId, status);

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
                _logger.LogError(ex, "Error updating payment");

                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "Error updating payment",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}