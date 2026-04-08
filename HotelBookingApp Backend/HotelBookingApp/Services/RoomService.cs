using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRepository<int, Room>    _roomRepo;
        private readonly IRepository<int, Hotel>   _hotelRepo;
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IAuditLogService          _audit;
        private readonly ILogger<RoomService>      _logger;

        public RoomService(
            IRepository<int, Room>    roomRepo,
            IRepository<int, Hotel>   hotelRepo,
            IRepository<int, Booking> bookingRepo,
            IAuditLogService          audit,
            ILogger<RoomService>      logger)
        {
            _roomRepo    = roomRepo;
            _hotelRepo   = hotelRepo;
            _bookingRepo = bookingRepo;
            _audit       = audit;
            _logger      = logger;
        }

        private async Task LogAsync(string action, int? entityId, string? changes = null)
        {
            try { await _audit.CreateAsync(new CreateAuditLogDto { Action = action, EntityName = "Room", EntityId = entityId, Changes = changes }); }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed: {Action}", action); }
        }

        // ── CREATE ─────────────────────────────
        public async Task<RoomResponseDto> CreateAsync(CreateRoomDto dto)
        {
            var hotel = await _hotelRepo.GetByIdAsync(dto.HotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", dto.HotelId);

            var exists = await _roomRepo.ExistsAsync(
                r => r.HotelId == dto.HotelId && r.RoomNumber == dto.RoomNumber
            );
            if (exists)
                throw new AlreadyExistsException($"Room #{dto.RoomNumber} already exists.");

            var room = new Room
            {
                HotelId       = dto.HotelId,
                RoomNumber    = dto.RoomNumber,
                RoomType      = dto.RoomType.Trim(),
                PricePerNight = dto.PricePerNight,
                Capacity      = dto.Capacity,
                ImageUrl      = dto.ImageUrl?.Trim(),
                IsAvailable   = true
            };

            var created = await _roomRepo.AddAsync(room);
            await LogAsync("RoomCreated", created.RoomId, $"Hotel:{dto.HotelId} #{dto.RoomNumber} {dto.RoomType} ₹{dto.PricePerNight}");
            return MapToDto(created);
        }

        // ── GET BY ID ──────────────────────────
        public async Task<RoomResponseDto?> GetByIdAsync(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId)
                ?? throw new NotFoundException("Room", roomId);
            return MapToDto(room);
        }

        // ── GET ALL WITH PAGINATION ────────────
        public async Task<PagedResponseDto<RoomResponseDto>> GetAllAsync(
            PagedRequestDto request, int? hotelId = null)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 10);

            var rooms = hotelId.HasValue
                ? await _roomRepo.FindAllAsync(r => r.HotelId == hotelId.Value)
                : await _roomRepo.GetAllAsync();

            var ordered = rooms.OrderBy(r => r.RoomNumber);
            var total   = ordered.Count();

            var data = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedResponseDto<RoomResponseDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── UPDATE ─────────────────────────────
        public async Task<RoomResponseDto?> UpdateAsync(int roomId, CreateRoomDto dto)
        {
            var room = await _roomRepo.GetByIdAsync(roomId)
                ?? throw new NotFoundException("Room", roomId);

            var duplicate = await _roomRepo.ExistsAsync(
                r => r.RoomId != roomId &&
                     r.HotelId == dto.HotelId &&
                     r.RoomNumber == dto.RoomNumber
            );
            if (duplicate)
                throw new AlreadyExistsException($"Room #{dto.RoomNumber} already exists.");

            room.HotelId       = dto.HotelId;
            room.RoomNumber    = dto.RoomNumber;
            room.RoomType      = dto.RoomType.Trim();
            room.PricePerNight = dto.PricePerNight;
            room.Capacity      = dto.Capacity;
            room.ImageUrl      = dto.ImageUrl?.Trim();

            var updated = await _roomRepo.UpdateAsync(roomId, room);
            await LogAsync("RoomUpdated", roomId, $"#{dto.RoomNumber} {dto.RoomType} ₹{dto.PricePerNight}");
            return updated is null ? null : MapToDto(updated);
        }

        // ── DEACTIVATE ─────────────────────────
        public async Task<bool> DeactivateAsync(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId)
                ?? throw new NotFoundException("Room", roomId);

            if (!room.IsAvailable)
                throw new BadRequestException("Room already inactive.");

            room.IsAvailable = false;
            await _roomRepo.UpdateAsync(roomId, room);
            await LogAsync("RoomDeactivated", roomId);
            return true;
        }

        // ── FILTER ─────────────────────────────
        public async Task<IEnumerable<RoomResponseDto>> FilterAsync(RoomFilterDto filter)
        {
            var rooms = await _roomRepo.GetAllAsync();
            return ApplyFilter(rooms.AsQueryable(), filter)
                .OrderBy(r => r.RoomNumber)
                .Select(MapToDto)
                .ToList();
        }

        // ── DATE-RANGE AVAILABILITY ────────────
        public async Task<bool> IsAvailableForDatesAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var room = await _roomRepo.GetByIdAsync(roomId)
                ?? throw new NotFoundException("Room", roomId);

            // Admin-deactivated rooms are never available
            if (!room.IsAvailable) return false;

            // Check for any active booking that overlaps the requested dates
            var overlapping = await _bookingRepo.FindAllAsync(
                b => b.RoomId  == roomId &&
                     (b.Status == "Pending" || b.Status == "Confirmed") &&
                     checkIn  < b.CheckOut &&
                     checkOut > b.CheckIn
            );

            return !overlapping.Any();
        }

        // ── PRIVATE ────────────────────────────
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
            ImageUrl      = r.ImageUrl,
            IsAvailable   = r.IsAvailable
        };
    }
}
