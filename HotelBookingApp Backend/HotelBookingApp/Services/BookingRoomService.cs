using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>Manages room line-items inside a booking — create, read, update, delete.</summary>
    public class BookingRoomService : IBookingRoomService
    {
        private readonly IRepository<int, BookingRoom> _bookingRoomRepo;
        private readonly IRepository<int, Booking>     _bookingRepo;
        private readonly IRepository<int, Room>        _roomRepo;
        private readonly ILogger<BookingRoomService>   _logger;

        public BookingRoomService(
            IRepository<int, BookingRoom> bookingRoomRepo,
            IRepository<int, Booking>     bookingRepo,
            IRepository<int, Room>        roomRepo,
            ILogger<BookingRoomService>   logger)
        {
            _bookingRoomRepo = bookingRoomRepo;
            _bookingRepo     = bookingRepo;
            _roomRepo        = roomRepo;
            _logger          = logger;
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<BookingRoomResponseDto> CreateAsync(CreateBookingRoomDto dto)
        {
            _logger.LogInformation("Creating BookingRoom — Booking:{BookingId} Room:{RoomId}", dto.BookingId, dto.RoomId);

            var booking = await _bookingRepo.GetByIdAsync(dto.BookingId)
                          ?? throw new NotFoundException("Booking", dto.BookingId);

            var room = await _roomRepo.GetByIdAsync(dto.RoomId)
                       ?? throw new NotFoundException("Room", dto.RoomId);

            if (!room.IsAvailable)
                throw new BadRequestException($"Room #{room.RoomNumber} is currently unavailable.");

            var entity = new BookingRoom
            {
                BookingId     = dto.BookingId,
                RoomId        = dto.RoomId,
                PricePerNight = dto.PricePerNight,
                NumberOfRooms = dto.NumberOfRooms
            };

            var created = await _bookingRoomRepo.AddAsync(entity);
            _logger.LogInformation("BookingRoom created: {BookingRoomId}", created.BookingRoomId);
            return MapToDto(created);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────
        public async Task<BookingRoomResponseDto?> GetByIdAsync(int bookingRoomId)
        {
            var br = await _bookingRoomRepo.GetByIdAsync(bookingRoomId)
                     ?? throw new NotFoundException("BookingRoom", bookingRoomId);
            return MapToDto(br);
        }

        // ── GET BY BOOKING ────────────────────────────────────────────────
        public async Task<IEnumerable<BookingRoomResponseDto>> GetByBookingAsync(int bookingId)
        {
            var list = await _bookingRoomRepo.FindAllAsync(br => br.BookingId == bookingId);
            return list.Select(MapToDto).ToList();
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        public async Task<BookingRoomResponseDto?> UpdateAsync(int bookingRoomId, CreateBookingRoomDto dto)
        {
            _logger.LogInformation("Updating BookingRoom {BookingRoomId}", bookingRoomId);

            var br = await _bookingRoomRepo.GetByIdAsync(bookingRoomId)
                     ?? throw new NotFoundException("BookingRoom", bookingRoomId);

            var room = await _roomRepo.GetByIdAsync(dto.RoomId)
                       ?? throw new NotFoundException("Room", dto.RoomId);

            if (!room.IsAvailable)
                throw new BadRequestException($"Room #{room.RoomNumber} is currently unavailable.");

            br.RoomId        = dto.RoomId;
            br.PricePerNight = dto.PricePerNight;
            br.NumberOfRooms = dto.NumberOfRooms;

            var updated = await _bookingRoomRepo.UpdateAsync(bookingRoomId, br);
            return updated is null ? null : MapToDto(updated);
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int bookingRoomId)
        {
            _logger.LogInformation("Deleting BookingRoom {BookingRoomId}", bookingRoomId);

            var deleted = await _bookingRoomRepo.DeleteAsync(bookingRoomId);
            if (deleted is null) throw new NotFoundException("BookingRoom", bookingRoomId);
            return true;
        }

        // ── MAPPER ────────────────────────────────────────────────────────
        private static BookingRoomResponseDto MapToDto(BookingRoom br) => new()
        {
            BookingRoomId  = br.BookingRoomId,
            BookingId      = br.BookingId,
            RoomId         = br.RoomId,
            PricePerNight  = br.PricePerNight,
            NumberOfRooms  = br.NumberOfRooms
        };
    }
}
