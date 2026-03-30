using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    public class UserAmenityPreferenceService : IUserAmenityPreferenceService
    {
        private readonly IRepository<int, UserAmenityPreference> _prefRepo;
        private readonly IRepository<int, User>                  _userRepo;
        private readonly IRepository<int, Amenity>               _amenityRepo;
        private readonly ILogger<UserAmenityPreferenceService>   _logger;

        public UserAmenityPreferenceService(
            IRepository<int, UserAmenityPreference> prefRepo,
            IRepository<int, User>                  userRepo,
            IRepository<int, Amenity>               amenityRepo,
            ILogger<UserAmenityPreferenceService>   logger)
        {
            _prefRepo    = prefRepo;
            _userRepo    = userRepo;
            _amenityRepo = amenityRepo;
            _logger      = logger;
        }

        public async Task<UserAmenityPreferenceResponseDto> AddAsync(CreateUserAmenityPreferenceDto dto)
        {
            var user    = await _userRepo.GetByIdAsync(dto.UserId)       ?? throw new NotFoundException("User",    dto.UserId);
            var amenity = await _amenityRepo.GetByIdAsync(dto.AmenityId) ?? throw new NotFoundException("Amenity", dto.AmenityId);

            var exists = await _prefRepo.ExistsAsync(
                p => p.UserId == dto.UserId && p.AmenityId == dto.AmenityId);
            if (exists)
                throw new AlreadyExistsException($"You have already selected '{amenity.Name}'.");

            var pref = new UserAmenityPreference
            {
                UserId    = dto.UserId,
                AmenityId = dto.AmenityId,
                CreatedAt = DateTime.UtcNow,
                Status    = "Pending"
            };

            var created = await _prefRepo.AddAsync(pref);
            _logger.LogInformation("UserAmenityPreference created: {Id}", created.PreferenceId);
            return MapToDto(created, user.UserName, amenity.Name, amenity.Icon);
        }

        public async Task<UserAmenityPreferenceResponseDto> ApproveAsync(int preferenceId)
        {
            var pref = await _prefRepo.GetByIdAsync(preferenceId) ?? throw new NotFoundException("Preference", preferenceId);
            pref.Status = "Approved";
            await _prefRepo.UpdateAsync(preferenceId, pref);
            return await MapDtoAfterStatusChange(pref);
        }

        public async Task<UserAmenityPreferenceResponseDto> RejectAsync(int preferenceId)
        {
            var pref = await _prefRepo.GetByIdAsync(preferenceId) ?? throw new NotFoundException("Preference", preferenceId);
            pref.Status = "Rejected";
            await _prefRepo.UpdateAsync(preferenceId, pref);
            return await MapDtoAfterStatusChange(pref);
        }

        private async Task<UserAmenityPreferenceResponseDto> MapDtoAfterStatusChange(UserAmenityPreference pref)
        {
            var user    = await _userRepo.GetByIdAsync(pref.UserId);
            var amenity = await _amenityRepo.GetByIdAsync(pref.AmenityId);
            return MapToDto(pref, user?.UserName ?? string.Empty, amenity?.Name ?? string.Empty, amenity?.Icon);
        }

        public async Task<IEnumerable<UserAmenityPreferenceResponseDto>> GetByUserAsync(int userId)
        {
            // Eager-load User and Amenity in one query
            var all   = await _prefRepo.GetAllIncludingAsync(p => p.User!, p => p.Amenity!);
            var prefs = all.Where(p => p.UserId == userId)
                           .OrderByDescending(p => p.CreatedAt)
                           .ToList();
            return prefs.Select(p => MapToDto(p, p.User?.UserName ?? string.Empty,
                                               p.Amenity?.Name    ?? string.Empty,
                                               p.Amenity?.Icon)).ToList();
        }

        public async Task<IEnumerable<UserAmenityPreferenceResponseDto>> GetAllAsync()
        {
            var prefs = await _prefRepo.GetAllIncludingAsync(p => p.User!, p => p.Amenity!);
            return prefs.OrderByDescending(p => p.CreatedAt)
                        .Select(p => MapToDto(p, p.User?.UserName ?? string.Empty,
                                               p.Amenity?.Name    ?? string.Empty,
                                               p.Amenity?.Icon)).ToList();
        }

        public async Task<bool> RemoveAsync(int preferenceId)
        {
            var deleted = await _prefRepo.DeleteAsync(preferenceId);
            if (deleted is null) throw new NotFoundException("Preference", preferenceId);
            return true;
        }

        public async Task<bool> RemoveByUserAndAmenityAsync(int userId, int amenityId)
        {
            var pref = await _prefRepo.FindAsync(p => p.UserId == userId && p.AmenityId == amenityId);
            if (pref is null) throw new NotFoundException("Preference", $"User:{userId} Amenity:{amenityId}");
            await _prefRepo.DeleteAsync(pref.PreferenceId);
            return true;
        }

        private static UserAmenityPreferenceResponseDto MapToDto(
            UserAmenityPreference p, string userName, string amenityName, string? icon) => new()
        {
            PreferenceId = p.PreferenceId,
            UserId       = p.UserId,
            UserName     = userName,
            AmenityId    = p.AmenityId,
            AmenityName  = amenityName,
            AmenityIcon  = icon,
            CreatedAt    = p.CreatedAt,
            Status       = p.Status
        };
    }
}
