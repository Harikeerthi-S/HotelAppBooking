using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IUserService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<UserResponseDto> GetByIdAsync(int userId);
        Task<IEnumerable<UserResponseDto>> GetAllAsync();
        Task<PagedResponseDto<UserResponseDto>> GetPagedAsync(PagedRequestDto request);
        Task<bool> DeleteAsync(int userId);
    }
}
