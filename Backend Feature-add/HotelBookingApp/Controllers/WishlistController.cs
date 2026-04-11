using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize(Roles = "user,hotelmanager,admin")]
    [Produces("application/json")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService           _wishlistService;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(IWishlistService wishlistService, ILogger<WishlistController> logger)
        {
            _wishlistService = wishlistService;
            _logger          = logger;
        }

        /// <summary>Add a hotel to the user's wishlist. User only.</summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateWishlistDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation("AddToWishlist: User={UserId} Hotel={HotelId}", dto.UserId, dto.HotelId);
                var result = await _wishlistService.AddAsync(dto);
                _logger.LogInformation("Wishlist entry created: {WishlistId}", result.WishlistId);
                return CreatedAtAction(nameof(GetByUser), new { userId = result.UserId }, result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("AddToWishlist not found: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (AlreadyExistsException ex)
            {
                _logger.LogWarning("AddToWishlist conflict: {Message}", ex.Message);
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding to wishlist");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while adding to wishlist.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get all wishlist entries for a user. User only.</summary>
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                _logger.LogInformation("GetWishlistByUser: {UserId}", userId);
                var list = await _wishlistService.GetByUserAsync(userId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching wishlist for user {UserId}", userId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the wishlist.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Remove a wishlist entry by wishlist ID. User only.</summary>
        [HttpDelete("{wishlistId:int}")]
        public async Task<IActionResult> Remove(int wishlistId)
        {
            try
            {
                _logger.LogInformation("RemoveWishlistEntry: {WishlistId}", wishlistId);
                await _wishlistService.RemoveAsync(wishlistId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Wishlist entry not found: {WishlistId}", wishlistId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing wishlist entry {WishlistId}", wishlistId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while removing the wishlist entry.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Remove a hotel from a user's wishlist by userId + hotelId. User only.</summary>
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveByUserAndHotel([FromQuery] int userId, [FromQuery] int hotelId)
        {
            try
            {
                _logger.LogInformation("RemoveFromWishlist: User={UserId} Hotel={HotelId}", userId, hotelId);
                await _wishlistService.RemoveByUserAndHotelAsync(userId, hotelId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Wishlist entry not found: User={UserId} Hotel={HotelId}", userId, hotelId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing hotel {HotelId} from wishlist of user {UserId}", hotelId, userId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while removing from wishlist.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
