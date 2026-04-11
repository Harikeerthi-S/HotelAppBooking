using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Produces("application/json")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService          _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger      = logger;
        }

        /// <summary>Send a message and get a bot reply. Open to all (guests + users).</summary>
        [HttpPost("message")]
        [AllowAnonymous]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var response = await _chatService.ReplyAsync(dto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { message = "Chat service unavailable. Please try again." });
            }
        }

        /// <summary>Get conversation history for a session.</summary>
        [HttpGet("history/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHistory(string sessionId)
        {
            try
            {
                var history = await _chatService.GetHistoryAsync(sessionId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chat history");
                return StatusCode(500, new { message = "Error retrieving chat history." });
            }
        }

        /// <summary>Get all chat messages for a user. User/Admin only.</summary>
        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "user,admin")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            try
            {
                var history = await _chatService.GetByUserAsync(userId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chat history for user {UserId}", userId);
                return StatusCode(500, new { message = "Error retrieving chat history." });
            }
        }

        /// <summary>Clear a chat session. Admin only.</summary>
        [HttpDelete("session/{sessionId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ClearSession(string sessionId)
        {
            try
            {
                await _chatService.ClearSessionAsync(sessionId);
                return Ok(new { message = "Session cleared." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing chat session");
                return StatusCode(500, new { message = "Error clearing session." });
            }
        }
    }
}
