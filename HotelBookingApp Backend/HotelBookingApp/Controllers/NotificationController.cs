using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/notification")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // ----------------------------------
        // ✅ CREATE (Admin)
        // ----------------------------------
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("CreateNotification: UserId={UserId}", dto.UserId);

                var result = await _notificationService.CreateAsync(dto);

                _logger.LogInformation("Notification created: {NotificationId}", result.NotificationId);

                return CreatedAtAction(nameof(GetById),
                    new { notificationId = result.NotificationId },
                    result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("CreateNotification not found: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("CreateNotification bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating notification");
                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while creating the notification.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ----------------------------------
        // ✅ GET MY NOTIFICATIONS (🔥 FIXED)
        // ----------------------------------
        [HttpGet("my")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> GetMy()
        {
            try
            {
                // 🔥 FIX: support BOTH claim types
                var userIdClaim =
                    User.FindFirst("id")?.Value ??
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("UserId not found in token.");
                    return Unauthorized(new ErrorResponseDto
                    {
                        StatusCode = 401,
                        Message = "Invalid token — user ID missing.",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("GetMyNotifications: UserId={UserId}", userId);

                var list = await _notificationService.GetByUserAsync(userId);

                _logger.LogInformation("Notifications fetched: Count={Count}", list.Count());

                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications for current user");
                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving notifications.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ----------------------------------
        // ✅ GET BY ID
        // ----------------------------------
        [HttpGet("{notificationId:int}")]
        [Authorize(Roles = "admin,user")]
        public async Task<IActionResult> GetById(int notificationId)
        {
            try
            {
                _logger.LogInformation("GetNotificationById: {NotificationId}", notificationId);

                var notification = await _notificationService.GetByIdAsync(notificationId);

                // 🔥 USER CAN ONLY ACCESS OWN NOTIFICATION
                var role = User.FindFirstValue(ClaimTypes.Role);

                if (role == "user")
                {
                    var userIdClaim =
                        User.FindFirst("id")?.Value ??
                        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (int.TryParse(userIdClaim, out var userId) &&
                        notification!.UserId != userId)
                    {
                        _logger.LogWarning("User {UserId} tried to access another user's notification", userId);
                        return Forbid();
                    }
                }

                return Ok(notification);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("Notification not found: {NotificationId}", notificationId);
                return NotFound(new ErrorResponseDto
                {
                    StatusCode = 404,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notification {NotificationId}", notificationId);
                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while retrieving the notification.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ----------------------------------
        // ✅ MARK AS READ
        // ----------------------------------
        [HttpPut("{notificationId:int}/read")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                _logger.LogInformation("MarkNotificationAsRead: {NotificationId}", notificationId);

                await _notificationService.MarkAsReadAsync(notificationId);

                return Ok(new { message = "Notification marked as read." });
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
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while updating the notification.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        // ----------------------------------
        // ✅ DELETE
        // ----------------------------------
        [HttpDelete("{notificationId:int}")]
        [Authorize(Roles = "admin,user")]
        public async Task<IActionResult> Delete(int notificationId)
        {
            try
            {
                _logger.LogInformation("DeleteNotification: {NotificationId}", notificationId);

                await _notificationService.DeleteAsync(notificationId);

                return NoContent();
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
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message = "An error occurred while deleting the notification.",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}