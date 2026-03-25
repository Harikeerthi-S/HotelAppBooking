using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>Full room lifecycle — create, list, filter, update, and soft-deactivate.</summary>
    public class RoomService : IRoomService
    {
        private readonly IRepository<int, Room>  _roomRepo;
        private readonly IRepository<int, Hotel> _hotelRepo;
        private readonly ILogger<RoomService>    _logger;

        public RoomService(
            IRepository<int, Room>  roomRepo,
            IRepository<int, Hotel> hotelRepo,
            ILogger<RoomService>    logger)
        {
            _roomRepo  = roomRepo;
            _hotelRepo = hotelRepo;
            _logger    = logger;
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<RoomResponseDto> CreateAsync(CreateRoomDto dto)
        {
            _logger.LogInformation("Creating room #{RoomNumber} for hotel {HotelId}", dto.RoomNumber, dto.HotelId);

            var hotel = await _hotelRepo.GetByIdAsync(dto.HotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", dto.HotelId);

            var roomExists = await _roomRepo.ExistsAsync(
                r => r.HotelId == dto.HotelId && r.RoomNumber == dto.RoomNumber
            );
            if (roomExists)
                throw new AlreadyExistsException($"Room #{dto.RoomNumber} already exists in hotel {dto.HotelId}.");

            var room = new Room
            {
                HotelId       = dto.HotelId,
                RoomNumber    = dto.RoomNumber,
                RoomType      = dto.RoomType.Trim(),
                PricePerNight = dto.PricePerNight,
                ImageUrl = dto.ImageUrl?.Trim(),
                Capacity      = dto.Capacity,
                IsAvailable   = true
            };

            var created = await _roomRepo.AddAsync(room);
            _logger.LogInformation("Room created: {RoomId}", created.RoomId);
            return MapToDto(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<RoomResponseDto?> GetByIdAsync(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId)
                       ?? throw new NotFoundException("Room", roomId);
            return MapToDto(room);
        }

        // ── GET ALL (optionally filtered by hotel) ────────────────────────
        public async Task<IEnumerable<RoomResponseDto>> GetAllAsync(int? hotelId = null)
        {
            var rooms = hotelId.HasValue
                ? await _roomRepo.FindAllAsync(r => r.HotelId == hotelId.Value)
                : await _roomRepo.GetAllAsync();

            return rooms.OrderBy(r => r.RoomNumber).Select(MapToDto).ToList();
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        public async Task<RoomResponseDto?> UpdateAsync(int roomId, CreateRoomDto dto)
        {
            _logger.LogInformation("Updating room {RoomId}", roomId);

            var room = await _roomRepo.GetByIdAsync(roomId)
                       ?? throw new NotFoundException("Room", roomId);

            // Check duplicate room number in the same hotel (excluding self)
            var duplicate = await _roomRepo.ExistsAsync(
                r => r.RoomId != roomId
                  && r.HotelId == dto.HotelId
                  && r.RoomNumber == dto.RoomNumber
            );
            if (duplicate)
                throw new AlreadyExistsException($"Room #{dto.RoomNumber} already exists in hotel {dto.HotelId}.");

            room.HotelId       = dto.HotelId;
            room.RoomNumber    = dto.RoomNumber;
            room.RoomType      = dto.RoomType.Trim();
            room.PricePerNight = dto.PricePerNight;
            room.Capacity      = dto.Capacity;
            room.ImageUrl = dto.ImageUrl?.Trim();

            var updated = await _roomRepo.UpdateAsync(roomId, room);
            return updated is null ? null : MapToDto(updated);
        }

        // ── SOFT DEACTIVATE ───────────────────────────────────────────────
        public async Task<bool> DeactivateAsync(int roomId)
        {
            _logger.LogInformation("Deactivating room {RoomId}", roomId);

            var room = await _roomRepo.GetByIdAsync(roomId)
                       ?? throw new NotFoundException("Room", roomId);

            if (!room.IsAvailable)
                throw new BadRequestException("Room is already deactivated.");

            room.IsAvailable = false;
            await _roomRepo.UpdateAsync(roomId, room);
            return true;
        }

        // ── FILTER ────────────────────────────────────────────────────────
        public async Task<IEnumerable<RoomResponseDto>> FilterAsync(RoomFilterDto filter)
        {
            var rooms = await _roomRepo.GetAllAsync();
            return ApplyFilter(rooms.AsQueryable(), filter)
                .OrderBy(r => r.RoomNumber)
                .Select(MapToDto)
                .ToList();
        }

        // ── FILTER PAGED ──────────────────────────────────────────────────
        public async Task<PagedResponseDto<RoomResponseDto>> FilterPagedAsync(
            RoomFilterDto filter, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 100);

            var rooms  = await _roomRepo.GetAllAsync();
            var query  = ApplyFilter(rooms.AsQueryable(), filter).OrderBy(r => r.RoomNumber);
            var total  = query.Count();
            var data   = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<RoomResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total
            };
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────
        private static IQueryable<Room> ApplyFilter(IQueryable<Room> query, RoomFilterDto filter)
        {
            if (filter.OnlyAvailable)
                query = query.Where(r => r.IsAvailable);
            if (filter.HotelId.HasValue)
                query = query.Where(r => r.HotelId == filter.HotelId.Value);
            if (!string.IsNullOrWhiteSpace(filter.RoomType))
                query = query.Where(r => r.RoomType.ToLower() == filter.RoomType.ToLower().Trim());
            if (filter.MinPrice.HasValue)
                query = query.Where(r => r.PricePerNight >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(r => r.PricePerNight <= filter.MaxPrice.Value);
            if (filter.MinCapacity.HasValue)
                query = query.Where(r => r.Capacity >= filter.MinCapacity.Value);
            if (filter.MaxCapacity.HasValue)
                query = query.Where(r => r.Capacity <= filter.MaxCapacity.Value);
            return query;
        }

        private static RoomResponseDto MapToDto(Room r) => new()
        {
            RoomId        = r.RoomId,
            HotelId       = r.HotelId,
            RoomNumber    = r.RoomNumber,
            RoomType      = r.RoomType,
            PricePerNight = r.PricePerNight,
            Capacity      = r.Capacity,
            ImageUrl=     r.ImageUrl,
            IsAvailable   = r.IsAvailable
        };
    }
}
