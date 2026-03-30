using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IUserAmenityPreferenceService
    {
        Task<UserAmenityPreferenceResponseDto> AddAsync(CreateUserAmenityPreferenceDto dto);
        Task<IEnumerable<UserAmenityPreferenceResponseDto>> GetByUserAsync(int userId);
        Task<IEnumerable<UserAmenityPreferenceResponseDto>> GetAllAsync();
        Task<UserAmenityPreferenceResponseDto> ApproveAsync(int preferenceId);
        Task<UserAmenityPreferenceResponseDto> RejectAsync(int preferenceId);
        Task<bool> RemoveAsync(int preferenceId);
        Task<bool> RemoveByUserAndAmenityAsync(int userId, int amenityId);
    }
}
