using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>Manages user registration, retrieval and deletion.</summary>
    public class UserService : IUserService
    {
        private readonly IRepository<int, User> _userRepo;
        private readonly IPasswordService       _passwordService;
        private readonly ILogger<UserService>   _logger;

        public UserService(
            IRepository<int, User> userRepo,
            IPasswordService       passwordService,
            ILogger<UserService>   logger)
        {
            _userRepo        = userRepo;
            _passwordService = passwordService;
            _logger          = logger;
        }

        // ── REGISTER ─────────────────────────────────────────────────────
        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            _logger.LogInformation("Registering new user: {Email}", request.Email);

            // Validate role — only allow user and hotelmanager from public registration
            var allowedRoles = new[] { "user", "hotelmanager","admin" };
            var role = request.Role.ToLower().Trim();
            if (!allowedRoles.Contains(role))
                throw new BadRequestException($"Invalid role '{role}'. Allowed: user, hotelmanager.");

            // Check email uniqueness
            var emailExists = await _userRepo.ExistsAsync(
                u => u.Email.ToLower() == request.Email.ToLower().Trim()
            );
            if (emailExists)
                throw new AlreadyExistsException($"A user with email '{request.Email}' already exists.");

            var user = new User
            {
                UserName     = request.UserName.Trim(),
                Email        = request.Email.ToLower().Trim(),
                Phone        = request.Phone?.Trim(),
                Role         = role,
                PasswordHash = _passwordService.HashPassword(request.Password)
            };

            var created = await _userRepo.AddAsync(user);
            _logger.LogInformation("User registered: {UserId}", created.UserId);

            return MapToRegisterResponse(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<UserResponseDto> GetByIdAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId)
                       ?? throw new NotFoundException("User", userId);
            return MapToUserResponse(user);
        }

        // ── GET ALL ───────────────────────────────────────────────────────
        public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
        {
            var users = await _userRepo.GetAllAsync();
            return users.Select(MapToUserResponse).ToList();
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int userId)
        {
            _logger.LogInformation("Deleting user {UserId}", userId);
            var deleted = await _userRepo.DeleteAsync(userId);
            if (deleted is null) throw new NotFoundException("User", userId);
            return true;
        }

        // ── MAPPERS ───────────────────────────────────────────────────────
        private static UserResponseDto MapToUserResponse(User u) => new()
        {
            UserId   = u.UserId,
            UserName = u.UserName,
            Email    = u.Email,
            Phone    = u.Phone,
            Role     = u.Role
        };

        private static RegisterResponseDto MapToRegisterResponse(User u) => new()
        {
            UserId   = u.UserId,
            UserName = u.UserName,
            Email    = u.Email,
            Role     = u.Role
        };
    }
}
