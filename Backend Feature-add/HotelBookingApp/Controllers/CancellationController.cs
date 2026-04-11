using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/cancellation")]
    [Authorize]
    public class CancellationController : ControllerBase
    {
        private readonly ICancellationService            _cancellationService;
        private readonly ILogger<CancellationController> _logger;

        public CancellationController(ICancellationService cancellationService, ILogger<CancellationController> logger)
        {
            _cancellationService = cancellationService;
            _logger              = logger;
        }

        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create(CreateCancellationDto dto)
        {
            try
            {
                var result = await _cancellationService.CreateAsync(dto);
                return Ok(result);
            }
            catch (NotFoundException ex)      { return NotFound(Err(404, ex.Message)); }
            catch (BadRequestException ex)    { return BadRequest(Err(400, ex.Message)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create cancellation error");
                return StatusCode(500, Err(500, "An error occurred while creating the cancellation."));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _cancellationService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (NotFoundException ex) { return NotFound(Err(404, ex.Message)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get cancellation error");
                return StatusCode(500, Err(500, "Error retrieving cancellation."));
            }
        }

        [HttpPost("paged")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll(PagedRequestDto request)
        {
            try
            {
                var result = await _cancellationService.GetAllAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get all cancellations error");
                return StatusCode(500, Err(500, "Error retrieving cancellations."));
            }
        }

        [HttpPost("user/{userId}/paged")]
        public async Task<IActionResult> GetByUser(int userId, PagedRequestDto request)
        {
            try
            {
                var result = await _cancellationService.GetByUserAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get user cancellations error");
                return StatusCode(500, Err(500, "Error retrieving user cancellations."));
            }
        }

        /// <summary>
        /// Approve / Reject / Refund a cancellation.
        /// On Approved or Refunded with refundAmount > 0, the amount is auto-credited to the user's wallet.
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status, [FromQuery] decimal refundAmount = 0)
        {
            try
            {
                _logger.LogInformation("UpdateCancellationStatus: id={Id} status={Status} refund={Refund}", id, status, refundAmount);
                var result = await _cancellationService.UpdateStatusAsync(id, status, refundAmount);
                return Ok(result);
            }
            catch (NotFoundException ex)   { return NotFound(Err(404, ex.Message)); }
            catch (BadRequestException ex) { return BadRequest(Err(400, ex.Message)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update cancellation status error — id={Id} status={Status} innerMessage={Inner}",
                    id, status, ex.InnerException?.Message ?? ex.Message);
                return StatusCode(500, Err(500, $"Error updating cancellation: {ex.InnerException?.Message ?? ex.Message}"));
            }
        }

        private static ErrorResponseDto Err(int code, string msg) =>
            new() { StatusCode = code, Message = msg, Timestamp = DateTime.UtcNow };
    }
}
