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
        private readonly IRepository<int, Hotel>   _hotelRepo;
        private readonly IRepository<int, Room>    _roomRepo;
        private readonly IAuditLogService          _audit;
        private readonly ILogger<BookingService>   _logger;

        private readonly DateRangeValidatorDelegate _dateValidator =
            AppDelegateFactory.StrictDateRangeValidator;

        public BookingService(
            IRepository<int, Booking> bookingRepo,
            IRepository<int, Hotel>   hotelRepo,
            IRepository<int, Room>    roomRepo,
            IAuditLogService          audit,
            ILogger<BookingService>   logger)
        {
            _bookingRepo = bookingRepo;
            _hotelRepo   = hotelRepo;
            _roomRepo    = roomRepo;
            _audit       = audit;
            _logger      = logger;
        }

        private void Log(string action, string entity, int? entityId, int? userId, string? changes = null)
            => _ = _audit.CreateAsync(new CreateAuditLogDto
            {
                UserId     = userId,
                Action     = action,
                EntityName = entity,
                EntityId   = entityId,
                Changes    = changes
            });

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

            // ── ADMIN DEACTIVATION CHECK ───────────────────────────────────
            if (!room.IsAvailable)
                throw new BadRequestException(
                    "This room is currently unavailable. Please choose a different room.");

            // ── SAME-USER DATE-OVERLAP CHECK ───────────────────────────────
            // Block if this user already has a Pending/Confirmed booking for this
            // room whose dates overlap the requested period.
            // Non-overlapping dates on the same room are allowed.
            var existingUserBookings = await _bookingRepo.FindAllAsync(
                b => b.UserId == dto.UserId &&
                     b.RoomId == dto.RoomId &&
                     (b.Status == "Pending" || b.Status == "Confirmed") &&
                     dto.CheckIn  < b.CheckOut &&   // new check-in is before existing check-out
                     dto.CheckOut > b.CheckIn        // new check-out is after existing check-in
            );
            if (existingUserBookings.Any())
            {
                var existing = existingUserBookings.First();
                throw new AlreadyExistsException(
                    $"You already have an active booking (#{existing.BookingId}) for this room " +
                    $"from {existing.CheckIn:dd MMM yyyy} to {existing.CheckOut:dd MMM yyyy}. " +
                    "Please choose non-overlapping dates or cancel the existing booking.");
            }

            // ── OTHER-USER DATE-OVERLAP CHECK ──────────────────────────────
            // Block if any other user has a Pending/Confirmed booking for this
            // room whose dates overlap the requested period.
            var otherOverlap = await _bookingRepo.FindAllAsync(
                b => b.RoomId != 0 &&               // always true — keeps EF happy
                     b.RoomId == dto.RoomId &&
                     b.UserId != dto.UserId &&
                     (b.Status == "Pending" || b.Status == "Confirmed") &&
                     dto.CheckIn  < b.CheckOut &&
                     dto.CheckOut > b.CheckIn
            );
            if (otherOverlap.Any())
                throw new BadRequestException(
                    "This room is already booked for the selected dates by another guest. " +
                    "Please choose different dates or a different room.");

            var nights = (decimal)(dto.CheckOut - dto.CheckIn).TotalDays;
            var totalAmount = Math.Round(nights * room.PricePerNight * dto.NumberOfRooms, 2);

            var booking = new Booking
            {
                UserId        = dto.UserId,
                HotelId       = dto.HotelId,
                RoomId        = dto.RoomId,
                NumberOfRooms = dto.NumberOfRooms,
                CheckIn       = dto.CheckIn,
                CheckOut      = dto.CheckOut,
                TotalAmount   = totalAmount,
                Status        = "Pending"
            };

            var created = await _bookingRepo.AddAsync(booking);
            _logger.LogInformation("Booking created: {BookingId}", created.BookingId);
            Log("BookingCreated", "Booking", created.BookingId, dto.UserId,
                $"Hotel:{dto.HotelId} Room:{dto.RoomId} {dto.CheckIn:dd-MMM-yyyy}→{dto.CheckOut:dd-MMM-yyyy} ₹{totalAmount}");
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
            request.PageSize = Math.Clamp(request.PageSize, 1, 10);

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
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
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
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
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
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
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

            var bookingCopy = new Booking
            {
                BookingId     = booking.BookingId,
                UserId        = booking.UserId,
                HotelId       = booking.HotelId,
                RoomId        = booking.RoomId,
                NumberOfRooms = booking.NumberOfRooms,
                CheckIn       = booking.CheckIn,
                CheckOut      = booking.CheckOut,
                TotalAmount   = booking.TotalAmount,
                Status        = "Cancelled"
            };

            await _bookingRepo.UpdateAsync(bookingId, bookingCopy);
            Log("BookingCancelled", "Booking", bookingId, booking.UserId, $"Status→Cancelled");
            return true;
        }

        // ── PRIVATE ────────────────────────────
        private async Task<BookingResponseDto> ChangeStatusAsync(int bookingId, string newStatus, string[] allowed)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId)
                ?? throw new NotFoundException("Booking", bookingId);

            if (!allowed.Contains(booking.Status))
                throw new BadRequestException($"Invalid status change {booking.Status} → {newStatus}");

            // Use a clean copy to avoid EF Core tracking conflicts
            var bookingCopy = new Booking
            {
                BookingId     = booking.BookingId,
                UserId        = booking.UserId,
                HotelId       = booking.HotelId,
                RoomId        = booking.RoomId,
                NumberOfRooms = booking.NumberOfRooms,
                CheckIn       = booking.CheckIn,
                CheckOut      = booking.CheckOut,
                TotalAmount   = booking.TotalAmount,
                Status        = newStatus
            };

            await _bookingRepo.UpdateAsync(bookingId, bookingCopy);
            Log($"Booking{newStatus}", "Booking", bookingId, booking.UserId, $"Status→{newStatus}");
            var hotel = await _hotelRepo.GetByIdAsync(booking.HotelId);

            return MapToDto(bookingCopy, hotel?.HotelName ?? string.Empty);
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