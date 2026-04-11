using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>Handles user authentication — validates credentials and returns user info for JWT generation.</summary>
    public class AuthService : IAuthService
    {
        private readonly IRepository<int, User> _userRepo;
        private readonly IPasswordService       _passwordService;
        private readonly ILogger<AuthService>   _logger;

        public AuthService(
            IRepository<int, User> userRepo,
            IPasswordService       passwordService,
            ILogger<AuthService>   logger)
        {
            _userRepo        = userRepo        ?? throw new ArgumentNullException(nameof(userRepo));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _logger          = logger          ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Validates email + password and returns user details for JWT generation.</summary>
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            // Find user by email using predicate
            var user = await _userRepo.FindAsync(
                u => u.Email.ToLower() == request.Email.ToLower().Trim()
            );

            if (user is null)
            {
                _logger.LogWarning("Login failed — email not found: {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password.");
            }

            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed — wrong password for: {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password.");
            }

            _logger.LogInformation("Login successful for user {UserId} ({Role})", user.UserId, user.Role);

            return new LoginResponseDto
            {
                UserId   = user.UserId,
                UserName = user.UserName,
                Email    = user.Email,
                Role     = user.Role
            };
        }
    }
}
