using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>Assigns and removes amenities from hotels (many-to-many junction).</summary>
    public class HotelAmenityService : IHotelAmenityService
    {
        private readonly IRepository<int, HotelAmenity> _hotelAmenityRepo;
        private readonly IRepository<int, Hotel>        _hotelRepo;
        private readonly IRepository<int, Amenity>      _amenityRepo;
        private readonly ILogger<HotelAmenityService>   _logger;

        public HotelAmenityService(
            IRepository<int, HotelAmenity> hotelAmenityRepo,
            IRepository<int, Hotel>        hotelRepo,
            IRepository<int, Amenity>      amenityRepo,
            ILogger<HotelAmenityService>   logger)
        {
            _hotelAmenityRepo = hotelAmenityRepo ?? throw new ArgumentNullException(nameof(hotelAmenityRepo));
            _hotelRepo        = hotelRepo        ?? throw new ArgumentNullException(nameof(hotelRepo));
            _amenityRepo      = amenityRepo      ?? throw new ArgumentNullException(nameof(amenityRepo));
            _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<HotelAmenityResponseDto> CreateAsync(CreateHotelAmenityDto dto)
        {
            _logger.LogInformation("Assigning Amenity:{AmenityId} to Hotel:{HotelId}", dto.AmenityId, dto.HotelId);

            var hotel = await _hotelRepo.GetByIdAsync(dto.HotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", dto.HotelId);

            var amenity = await _amenityRepo.GetByIdAsync(dto.AmenityId)
                          ?? throw new NotFoundException("Amenity", dto.AmenityId);

            // Prevent duplicate assignment
            var alreadyAssigned = await _hotelAmenityRepo.ExistsAsync(
                ha => ha.HotelId == dto.HotelId && ha.AmenityId == dto.AmenityId
            );
            if (alreadyAssigned)
                throw new AlreadyExistsException(
                    $"Amenity '{amenity.Name}' is already assigned to hotel '{hotel.HotelName}'.");

            var entity = new HotelAmenity
            {
                HotelId   = dto.HotelId,
                AmenityId = dto.AmenityId
            };

            var created = await _hotelAmenityRepo.AddAsync(entity);
            _logger.LogInformation("HotelAmenity created: {HotelAmenityId}", created.HotelAmenityId);
            return MapToDto(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<HotelAmenityResponseDto?> GetByIdAsync(int id)
        {
            var ha = await _hotelAmenityRepo.GetByIdAsync(id)
                     ?? throw new NotFoundException("HotelAmenity", id);
            return MapToDto(ha);
        }

        // ── GET ALL ───────────────────────────────────────────────────────
        public async Task<IEnumerable<HotelAmenityResponseDto>> GetAllAsync()
        {
            var all = await _hotelAmenityRepo.GetAllIncludingAsync(ha => ha.Amenity!);
            return all.Select(MapToDto).ToList();
        }

        // ── GET BY HOTEL ──────────────────────────────────────────────────
        public async Task<IEnumerable<HotelAmenityResponseDto>> GetByHotelAsync(int hotelId)
        {
            var all  = await _hotelAmenityRepo.GetAllIncludingAsync(ha => ha.Amenity!);
            return all.Where(ha => ha.HotelId == hotelId).Select(MapToDto).ToList();
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Removing HotelAmenity {Id}", id);
            var deleted = await _hotelAmenityRepo.DeleteAsync(id);
            if (deleted is null) throw new NotFoundException("HotelAmenity", id);
            return true;
        }

        // ── MAPPER ────────────────────────────────────────────────────────
        private static HotelAmenityResponseDto MapToDto(HotelAmenity ha) => new()
        {
            HotelAmenityId       = ha.HotelAmenityId,
            HotelId              = ha.HotelId,
            AmenityId            = ha.AmenityId,
            AmenityName          = ha.Amenity?.Name        ?? string.Empty,
            AmenityIcon          = ha.Amenity?.Icon,
            AmenityDescription   = ha.Amenity?.Description
        };
    }
}
