using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>CRUD and search operations for hotels with soft-delete support.</summary>
    public class HotelService : IHotelService
    {
        private readonly IRepository<int, Hotel> _hotelRepo;
        private readonly ILogger<HotelService>   _logger;

        public HotelService(
            IRepository<int, Hotel> hotelRepo,
            ILogger<HotelService>   logger)
        {
            _hotelRepo = hotelRepo ?? throw new ArgumentNullException(nameof(hotelRepo));
            _logger    = logger    ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<HotelResponseDto> CreateAsync(CreateHotelDto dto)
        {
            _logger.LogInformation("Creating hotel: {Name}", dto.HotelName);

            // Check for duplicate name in same location
            var duplicate = await _hotelRepo.ExistsAsync(
                h => h.HotelName.ToLower() == dto.HotelName.ToLower().Trim()
                  && h.Location.ToLower()  == dto.Location.ToLower().Trim()
                  && h.IsActive
            );
            if (duplicate)
                throw new AlreadyExistsException($"Hotel '{dto.HotelName}' already exists in '{dto.Location}'.");

            var hotel = new Hotel
            {
                HotelName     = dto.HotelName.Trim(),
                Location      = dto.Location.Trim(),
                Address       = dto.Address?.Trim(),
                StarRating    = dto.StarRating,
                TotalRooms    = dto.TotalRooms,
                ContactNumber = dto.ContactNumber?.Trim(),
                ImagePath     = dto.ImagePath?.Trim(),
                IsActive      = true
            };

            var created = await _hotelRepo.AddAsync(hotel);
            _logger.LogInformation("Hotel created: {HotelId}", created.HotelId);
            return MapToDto(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<HotelResponseDto?> GetByIdAsync(int hotelId)
        {
            var hotel = await _hotelRepo.GetByIdAsync(hotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", hotelId);
            return MapToDto(hotel);
        }

        // ── GET PAGED ─────────────────────────────────────────────────────
        public async Task<PagedResponseDto<HotelResponseDto>> GetPagedAsync(PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 100);

            var all     = await _hotelRepo.FindAllAsync(h => h.IsActive);
            var total   = all.Count();
            var data    = all
                .OrderByDescending(h => h.StarRating)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<HotelResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total
            };
        }

        // ── FILTER PAGED ──────────────────────────────────────────────────
        public async Task<PagedResponseDto<HotelResponseDto>> FilterPagedAsync(
            HotelFilterDto filter,
            PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 100);

            var all = await _hotelRepo.GetAllAsync();
            var query = all.Where(h => h.IsActive).AsQueryable();

            if (filter.HotelId.HasValue)
                query = query.Where(h => h.HotelId == filter.HotelId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Location))
                query = query.Where(h => h.Location.ToLower().Contains(filter.Location.ToLower().Trim()));

            if (filter.MinRating.HasValue)
                query = query.Where(h => h.StarRating >= filter.MinRating.Value);

            var total = query.Count();
            var data  = query
                .OrderByDescending(h => h.StarRating)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<HotelResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total
            };
        }

        // ── SEARCH ────────────────────────────────────────────────────────
        public async Task<IEnumerable<HotelResponseDto>> SearchAsync(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new BadRequestException("Location search term is required.");

            var hotels = await _hotelRepo.FindAllAsync(
                h => h.IsActive && h.Location.ToLower().Contains(location.ToLower().Trim())
            );
            return hotels.Select(MapToDto).ToList();
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        public async Task<HotelResponseDto?> UpdateAsync(int hotelId, CreateHotelDto dto)
        {
            _logger.LogInformation("Updating hotel {HotelId}", hotelId);

            var hotel = await _hotelRepo.GetByIdAsync(hotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", hotelId);

            hotel.HotelName     = dto.HotelName.Trim();
            hotel.Location      = dto.Location.Trim();
            hotel.Address       = dto.Address?.Trim();
            hotel.StarRating    = dto.StarRating;
            hotel.TotalRooms    = dto.TotalRooms;
            hotel.ContactNumber = dto.ContactNumber?.Trim();
            hotel.ImagePath     = dto.ImagePath?.Trim();

            var updated = await _hotelRepo.UpdateAsync(hotelId, hotel);
            return updated is null ? null : MapToDto(updated);
        }

        // ── SOFT DELETE ───────────────────────────────────────────────────
        public async Task<bool> DeactivateAsync(int hotelId)
        {
            _logger.LogInformation("Deactivating hotel {HotelId}", hotelId);

            var hotel = await _hotelRepo.GetByIdAsync(hotelId);
            if (hotel is null) throw new NotFoundException("Hotel", hotelId);
            if (!hotel.IsActive) throw new BadRequestException("Hotel is already deactivated.");

            hotel.IsActive = false;
            await _hotelRepo.UpdateAsync(hotelId, hotel);
            return true;
        }

        // ── MAPPER ────────────────────────────────────────────────────────
        private static HotelResponseDto MapToDto(Hotel h) => new()
        {
            HotelId       = h.HotelId,
            HotelName     = h.HotelName,
            Location      = h.Location,
            Address       = h.Address,
            StarRating    = h.StarRating,
            TotalRooms    = h.TotalRooms,
            ContactNumber = h.ContactNumber,
            ImagePath     = h.ImagePath
        };
    }
}
