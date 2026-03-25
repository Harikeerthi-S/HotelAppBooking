using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IUserService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<UserResponseDto> GetByIdAsync(int userId);
        Task<IEnumerable<UserResponseDto>> GetAllAsync();
        Task<bool> DeleteAsync(int userId);
    }
}
