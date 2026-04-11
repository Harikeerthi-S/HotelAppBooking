using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletService walletService, ILogger<WalletController> logger)
        {
            _walletService = walletService;
            _logger = logger;
        }

        /// <summary>Get wallet balance + transaction history for a user.</summary>
        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                // Users can only see their own wallet
                if (User.IsInRole("user"))
                {
                    var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(claim, out var uid) || uid != userId)
                        return Forbid();
                }

                var result = await _walletService.GetByUserAsync(userId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching wallet for user {UserId}", userId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error retrieving wallet.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Admin: manually credit a user's wallet.</summary>
        [HttpPost("user/{userId:int}/credit")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Credit(int userId, [FromBody] WalletCreditDto dto)
        {
            try
            {
                var result = await _walletService.CreditAsync(userId, dto.Amount, dto.Description);
                return Ok(result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crediting wallet for user {UserId}", userId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "Error crediting wallet.", Timestamp = DateTime.UtcNow });
            }
        }
    }

    public class WalletCreditDto
    {
        public decimal Amount      { get; set; }
        public string  Description { get; set; } = string.Empty;
    }
}
