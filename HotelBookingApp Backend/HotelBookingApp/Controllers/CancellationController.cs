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
        private readonly ICancellationService _cancellationService;
        private readonly ILogger<CancellationController> _logger;

        public CancellationController(ICancellationService cancellationService, ILogger<CancellationController> logger)
        {
            _cancellationService = cancellationService;
            _logger = logger;
        }

        // ?? CREATE ?????????????????????????????
        [HttpPost]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Create(CreateCancellationDto dto)
        {
            try
            {
                var result = await _cancellationService.CreateAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create cancellation error");
                return StatusCode(500, ex.Message);
            }
        }

        // ?? GET BY ID ??????????????????????????
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _cancellationService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get cancellation error");
                return StatusCode(500, ex.Message);
            }
        }

        // ?? GET ALL (PAGED) ? FIXED ???????????
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
                return StatusCode(500, ex.Message);
            }
        }

        // ?? GET BY USER ????????????????????????
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
                return StatusCode(500, ex.Message);
            }
        }

        // ?? UPDATE STATUS ??????????????????????
        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> UpdateStatus(int id, string status, decimal refundAmount = 0)
        {
            try
            {
                var result = await _cancellationService.UpdateStatusAsync(id, status, refundAmount);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update cancellation error");
                return StatusCode(500, ex.Message);
            }
        }
    }
}