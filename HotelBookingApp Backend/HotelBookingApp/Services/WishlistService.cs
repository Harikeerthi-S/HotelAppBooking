using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>Manages a user's saved hotels (wishlist / favourites).</summary>
    public class WishlistService : IWishlistService
    {
        private readonly IRepository<int, Wishlist> _wishlistRepo;
        private readonly IRepository<int, User>     _userRepo;
        private readonly IRepository<int, Hotel>    _hotelRepo;
        private readonly ILogger<WishlistService>   _logger;

        public WishlistService(
            IRepository<int, Wishlist> wishlistRepo,
            IRepository<int, User>     userRepo,
            IRepository<int, Hotel>    hotelRepo,
            ILogger<WishlistService>   logger)
        {
            _wishlistRepo = wishlistRepo ?? throw new ArgumentNullException(nameof(wishlistRepo));
            _userRepo     = userRepo     ?? throw new ArgumentNullException(nameof(userRepo));
            _hotelRepo    = hotelRepo    ?? throw new ArgumentNullException(nameof(hotelRepo));
            _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── ADD ───────────────────────────────────────────────────────────
        public async Task<WishlistResponseDto> AddAsync(CreateWishlistDto dto)
        {
            _logger.LogInformation("Adding Hotel:{HotelId} to wishlist of User:{UserId}", dto.HotelId, dto.UserId);

            var user = await _userRepo.GetByIdAsync(dto.UserId)
                       ?? throw new NotFoundException("User", dto.UserId);

            var hotel = await _hotelRepo.GetByIdAsync(dto.HotelId);
            if (hotel is null || !hotel.IsActive)
                throw new NotFoundException("Hotel", dto.HotelId);

            var alreadySaved = await _wishlistRepo.ExistsAsync(
                w => w.UserId == dto.UserId && w.HotelId == dto.HotelId
            );
            if (alreadySaved)
                throw new AlreadyExistsException($"Hotel '{hotel.HotelName}' is already in your wishlist.");

            var wishlist = new Wishlist
            {
                UserId  = dto.UserId,
                HotelId = dto.HotelId,
                SavedAt = DateTime.UtcNow
            };

            var created = await _wishlistRepo.AddAsync(wishlist);
            _logger.LogInformation("Wishlist entry created: {WishlistId}", created.WishlistId);
            return MapToDto(created);
        }

        // ── GET BY USER ───────────────────────────────────────────────────
        public async Task<IEnumerable<WishlistResponseDto>> GetByUserAsync(int userId)
        {
            var items = await _wishlistRepo.FindAllAsync(w => w.UserId == userId);
            return items.OrderByDescending(w => w.SavedAt).Select(MapToDto).ToList();
        }

        // ── REMOVE BY WISHLIST ID ─────────────────────────────────────────
        public async Task<bool> RemoveAsync(int wishlistId)
        {
            _logger.LogInformation("Removing wishlist entry {WishlistId}", wishlistId);
            var deleted = await _wishlistRepo.DeleteAsync(wishlistId);
            if (deleted is null) throw new NotFoundException("Wishlist entry", wishlistId);
            return true;
        }

        // ── REMOVE BY USER + HOTEL ────────────────────────────────────────
        public async Task<bool> RemoveByUserAndHotelAsync(int userId, int hotelId)
        {
            _logger.LogInformation("Removing Hotel:{HotelId} from wishlist of User:{UserId}", hotelId, userId);

            var item = await _wishlistRepo.FindAsync(
                w => w.UserId == userId && w.HotelId == hotelId
            );
            if (item is null)
                throw new NotFoundException($"Hotel {hotelId} not found in wishlist of user {userId}.");

            await _wishlistRepo.DeleteAsync(item.WishlistId);
            return true;
        }

        // ── MAPPER ────────────────────────────────────────────────────────
        private static WishlistResponseDto MapToDto(Wishlist w) => new()
        {
            WishlistId = w.WishlistId,
            UserId     = w.UserId,
            HotelId    = w.HotelId
        };
    }
}
