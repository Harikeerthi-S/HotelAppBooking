using HotelBookingApp.Exceptions;
using HotelBookingApp.Helpers;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService           _authService;
        private readonly JwtTokenHelper         _jwtHelper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService           authService,
            JwtTokenHelper         jwtHelper,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _jwtHelper   = jwtHelper;
            _logger      = logger;
        }

        /// <summary>Authenticate with email and password. Returns a JWT token.</summary>
        /// <response code="200">Returns JWT token</response>
        /// <response code="400">Validation errors</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                var user  = await _authService.LoginAsync(request);
                var token = _jwtHelper.GenerateToken(user.UserId, user.UserName, user.Role);

                _logger.LogInformation("Login successful — User:{UserId} Role:{Role}", user.UserId, user.Role);

                return Ok(new TokenResponseDto { Token = token });
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}", request.Email, ex.Message);
                return Unauthorized(new ErrorResponseDto
                {
                    StatusCode = 401,
                    Message    = ex.Message,
                    Timestamp  = DateTime.UtcNow
                });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning("Login bad request: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = 400,
                    Message    = ex.Message,
                    Timestamp  = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Login for {Email}", request.Email);
                return StatusCode(500, new ErrorResponseDto
                {
                    StatusCode = 500,
                    Message    = "An unexpected error occurred during login.",
                    Timestamp  = DateTime.UtcNow
                });
            }
        }
    }
}
