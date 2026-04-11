using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IWishlistService
    {
        Task<WishlistResponseDto> AddAsync(CreateWishlistDto dto);
        Task<IEnumerable<WishlistResponseDto>> GetByUserAsync(int userId);
        Task<bool> RemoveAsync(int wishlistId);
        Task<bool> RemoveByUserAndHotelAsync(int userId, int hotelId);
    }
}
