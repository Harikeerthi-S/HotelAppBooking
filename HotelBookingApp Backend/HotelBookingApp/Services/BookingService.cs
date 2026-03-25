using HotelBookingApp.Delegates;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    public class BookingService : IBookingService
    {
        private readonly IRepository<int, Booking> _bookingRepo;
        private readonly IRepository<int, Hotel> _hotelRepo;
        private readonly IRepository<int, Room> _roomRepo;
        private readonly ILogger<BookingService> _logger;

        private readonly DateRangeValidatorDelegate _dateValidator =
            AppDelegateFactory.StrictDateRangeValidator;

        public BookingService(
            IRepository<int, Booking> bookingRepo,
            IRepository<int, Hotel> hotelRepo,
            IRepository<int, Room> roomRepo,
            ILogger<BookingService> logger)
        {
            _bookingRepo = bookingRepo;
            _hotelRepo = hotelRepo;
            _roomRepo = roomRepo;
            _logger = logger;
        }

        // ── CREATE ─────────────────────────────
        public async Task<BookingResponseDto> CreateAsync(CreateBookingDto dto)
        {
            _logger.LogInformation("Creating booking for User:{UserId}", dto.UserId);

            var dateError = _dateValidator(dto.CheckIn, dto.CheckOut);
            if (dateError is not null)
                throw new BadRequestException(dateError);

            var hotel = await _hotelRepo.GetByIdAsync(dto.HotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", dto.HotelId);

            var room = await _roomRepo.GetByIdAsync(dto.RoomId);
            if (room is null)
                throw new NotFoundException("Room", dto.RoomId);

            if (room.HotelId != dto.HotelId)
                throw new BadRequestException("Room does not belong to selected hotel.");

            // ✅ INVENTORY CHECK
            if (!room.IsAvailable || room.AvailableRooms < dto.NumberOfRooms)
                throw new BadRequestException("Not enough rooms available.");

            var nights = (decimal)(dto.CheckOut - dto.CheckIn).TotalDays;
            var totalAmount = Math.Round(nights * room.PricePerNight * dto.NumberOfRooms, 2);

            // ✅ REDUCE INVENTORY
            room.AvailableRooms -= dto.NumberOfRooms;
            room.IsAvailable = room.AvailableRooms > 0;
            await _roomRepo.UpdateAsync(room.RoomId, room);

            var booking = new Booking
            {
                UserId = dto.UserId,
                HotelId = dto.HotelId,
                RoomId = dto.RoomId,
                NumberOfRooms = dto.NumberOfRooms,
                CheckIn = dto.CheckIn,
                CheckOut = dto.CheckOut,
                TotalAmount = totalAmount,
                Status = "Pending"
            };

            var created = await _bookingRepo.AddAsync(booking);

            _logger.LogInformation("Booking created: {BookingId}", created.BookingId);

            return MapToDto(created, hotel.HotelName);
        }

        // ── GET BY ID ──────────────────────────
        public async Task<BookingResponseDto?> GetByIdAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId)
                ?? throw new NotFoundException("Booking", bookingId);

            var hotel = await _hotelRepo.GetByIdAsync(booking.HotelId);

            return MapToDto(booking, hotel?.HotelName ?? string.Empty);
        }

        // ── GET ALL (PAGED) ────────────────────
        public async Task<PagedResponseDto<BookingResponseDto>> GetAllAsync(PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            var all = await _bookingRepo.GetAllAsync();
            var ordered = all.OrderByDescending(b => b.CheckIn).ToList();
            var total = ordered.Count;

            var paged = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var data = new List<BookingResponseDto>();

            foreach (var b in paged)
            {
                var hotel = await _hotelRepo.GetByIdAsync(b.HotelId);
                data.Add(MapToDto(b, hotel?.HotelName ?? string.Empty));
            }

            return new PagedResponseDto<BookingResponseDto>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = total
            };
        }

        // ── GET BY USER ────────────────────────
        public async Task<PagedResponseDto<BookingResponseDto>> GetByUserAsync(int userId, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            var all = await _bookingRepo.FindAllAsync(b => b.UserId == userId);
            var ordered = all.OrderByDescending(b => b.CheckIn).ToList();
            var total = ordered.Count;

            var paged = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var data = new List<BookingResponseDto>();

            foreach (var b in paged)
            {
                var hotel = await _hotelRepo.GetByIdAsync(b.HotelId);
                data.Add(MapToDto(b, hotel?.HotelName ?? string.Empty));
            }

            return new PagedResponseDto<BookingResponseDto>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = total
            };
        }

        // ── GET BY HOTEL (FIXED ERROR) ─────────
        public async Task<PagedResponseDto<BookingResponseDto>> GetByHotelAsync(int hotelId, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);

            var hotel = await _hotelRepo.GetByIdAsync(hotelId)
                ?? throw new NotFoundException("Hotel", hotelId);

            var all = await _bookingRepo.FindAllAsync(b => b.HotelId == hotelId);
            var ordered = all.OrderByDescending(b => b.CheckIn).ToList();
            var total = ordered.Count;

            var data = ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => MapToDto(b, hotel.HotelName))
                .ToList();

            return new PagedResponseDto<BookingResponseDto>
            {
                Data = data,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = total
            };
        }

        // ── GET PENDING BY HOTEL (FIXED ERROR) ─
        public async Task<List<BookingResponseDto>> GetPendingByHotelAsync(int hotelId)
        {
            var hotel = await _hotelRepo.GetByIdAsync(hotelId)
                ?? throw new NotFoundException("Hotel", hotelId);

            var bookings = await _bookingRepo.FindAllAsync(
                b => b.HotelId == hotelId && b.Status == "Pending"
            );

            return bookings
                .Select(b => MapToDto(b, hotel.HotelName))
                .ToList();
        }

        // ── CONFIRM ────────────────────────────
        public async Task<BookingResponseDto> ConfirmAsync(int bookingId)
        {
            return await ChangeStatusAsync(bookingId, "Confirmed", new[] { "Pending" });
        }

        // ── COMPLETE ───────────────────────────
        public async Task<BookingResponseDto> CompleteAsync(int bookingId)
        {
            return await ChangeStatusAsync(bookingId, "Completed", new[] { "Confirmed" });
        }

        // ── CANCEL ─────────────────────────────
        public async Task<bool> CancelAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId)
                ?? throw new NotFoundException("Booking", bookingId);

            if (booking.Status == "Completed")
                throw new BadRequestException("Cannot cancel completed booking.");

            if (booking.Status == "Cancelled")
                throw new BadRequestException("Already cancelled.");

            // ✅ RESTORE INVENTORY
            var room = await _roomRepo.GetByIdAsync(booking.RoomId);
            if (room != null)
            {
                room.AvailableRooms += booking.NumberOfRooms;
                room.IsAvailable = true;

                await _roomRepo.UpdateAsync(room.RoomId, room);
            }

            booking.Status = "Cancelled";
            await _bookingRepo.UpdateAsync(bookingId, booking);

            return true;
        }

        // ── PRIVATE ────────────────────────────
        private async Task<BookingResponseDto> ChangeStatusAsync(int bookingId, string newStatus, string[] allowed)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId)
                ?? throw new NotFoundException("Booking", bookingId);

            if (!allowed.Contains(booking.Status))
                throw new BadRequestException($"Invalid status change {booking.Status} → {newStatus}");

            booking.Status = newStatus;

            await _bookingRepo.UpdateAsync(bookingId, booking);

            var hotel = await _hotelRepo.GetByIdAsync(booking.HotelId);

            return MapToDto(booking, hotel?.HotelName ?? string.Empty);
        }

        private static BookingResponseDto MapToDto(Booking b, string hotelName) => new()
        {
            BookingId = b.BookingId,
            UserId = b.UserId,
            HotelId = b.HotelId,
            HotelName = hotelName,
            RoomId = b.RoomId,
            NumberOfRooms = b.NumberOfRooms,
            CheckIn = b.CheckIn,
            CheckOut = b.CheckOut,
            TotalAmount = b.TotalAmount,
            Status = b.Status
        };
    }
}