using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService            _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger      = logger;
        }

        /// <summary>Register a new user account. Public.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("Register attempt for email: {Email}", request.Email);

                var result = await _userService.RegisterAsync(request);

                _logger.LogInformation("User registered successfully: {UserId}", result.UserId);

                return CreatedAtAction(nameof(GetById), new { userId = result.UserId }, result);
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("Register bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto { StatusCode = 400, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (AlreadyExistsException ex)
            {
                _logger.LogWarning("Register conflict: {Message}", ex.Message);
                return Conflict(new ErrorResponseDto { StatusCode = 409, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Register for {Email}", request.Email);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An unexpected error occurred during registration.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get user by ID. User can view own profile; Admin can view all.</summary>
        [HttpGet("{userId:int}")]
        [Authorize(Roles = "user,admin,hotelmanager")]
        public async Task<IActionResult> GetById(int userId)
        {
            try
            {
                _logger.LogInformation("GetUserById: {UserId}", userId);
                var user = await _userService.GetByIdAsync(userId);
                return Ok(user);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", userId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving the user.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get all users. Admin only.</summary>
        [HttpGet("GetAllUsers")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("GetAllUsers called");
                var users = await _userService.GetAllAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all users");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving users.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Get users (paged). Admin and HotelManager.</summary>
        [HttpPost("paged")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> GetPaged([FromBody] PagedRequestDto request)
        {
            try
            {
                var result = await _userService.GetPagedAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paged users");
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while retrieving users.", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>Delete a user. Admin only.</summary>
        [HttpDelete("{userId:int}")]
        [Authorize(Roles = "admin,hotelmanager")]
        public async Task<IActionResult> Delete(int userId)
        {
            try
            {
                _logger.LogInformation("DeleteUser: {UserId}", userId);
                await _userService.DeleteAsync(userId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("User not found for delete: {UserId}", userId);
                return NotFound(new ErrorResponseDto { StatusCode = 404, Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new ErrorResponseDto { StatusCode = 500, Message = "An error occurred while deleting the user.", Timestamp = DateTime.UtcNow });
            }
        }
    }
}
