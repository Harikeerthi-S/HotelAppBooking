using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>CRUD operations for amenities (WiFi, Pool, Gym, etc.).</summary>
    public class AmenityService : IAmenityService
    {
        private readonly IRepository<int, Amenity> _amenityRepo;
        private readonly ILogger<AmenityService>   _logger;

        public AmenityService(
            IRepository<int, Amenity> amenityRepo,
            ILogger<AmenityService>   logger)
        {
            _amenityRepo = amenityRepo ?? throw new ArgumentNullException(nameof(amenityRepo));
            _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<AmenityResponseDto> CreateAsync(CreateAmenityDto dto)
        {
            _logger.LogInformation("Creating amenity: {Name}", dto.Name);

            var name = dto.Name.Trim();

            // Uniqueness check
            var exists = await _amenityRepo.ExistsAsync(
                a => a.Name.ToLower() == name.ToLower()
            );
            if (exists)
                throw new AlreadyExistsException($"Amenity '{name}' already exists.");

            var amenity = new Amenity
            {
                Name        = name,
                Description = dto.Description?.Trim(),
                Icon        = dto.Icon?.Trim()
            };

            var created = await _amenityRepo.AddAsync(amenity);
            _logger.LogInformation("Amenity created: {AmenityId}", created.AmenityId);
            return MapToDto(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<AmenityResponseDto?> GetByIdAsync(int amenityId)
        {
            var amenity = await _amenityRepo.GetByIdAsync(amenityId)
                          ?? throw new NotFoundException("Amenity", amenityId);
            return MapToDto(amenity);
        }

        // ── GET ALL ───────────────────────────────────────────────────────
        public async Task<IEnumerable<AmenityResponseDto>> GetAllAsync()
        {
            var amenities = await _amenityRepo.GetAllAsync();
            return amenities.OrderBy(a => a.Name).Select(MapToDto).ToList();
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        public async Task<bool> UpdateAsync(int amenityId, CreateAmenityDto dto)
        {
            _logger.LogInformation("Updating amenity {AmenityId}", amenityId);

            var amenity = await _amenityRepo.GetByIdAsync(amenityId)
                          ?? throw new NotFoundException("Amenity", amenityId);

            var name = dto.Name.Trim();

            // Check another amenity doesn't already use this name
            var duplicate = await _amenityRepo.ExistsAsync(
                a => a.AmenityId != amenityId && a.Name.ToLower() == name.ToLower()
            );
            if (duplicate)
                throw new AlreadyExistsException($"Another amenity named '{name}' already exists.");

            amenity.Name        = name;
            amenity.Description = dto.Description?.Trim();
            amenity.Icon        = dto.Icon?.Trim();

            await _amenityRepo.UpdateAsync(amenityId, amenity);
            return true;
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int amenityId)
        {
            _logger.LogInformation("Deleting amenity {AmenityId}", amenityId);
            var deleted = await _amenityRepo.DeleteAsync(amenityId);
            if (deleted is null) throw new NotFoundException("Amenity", amenityId);
            return true;
        }

        // ── MAPPER ────────────────────────────────────────────────────────
        private static AmenityResponseDto MapToDto(Amenity a) => new()
        {
            AmenityId   = a.AmenityId,
            Name        = a.Name,
            Description = a.Description,
            Icon        = a.Icon
        };
    }
}
