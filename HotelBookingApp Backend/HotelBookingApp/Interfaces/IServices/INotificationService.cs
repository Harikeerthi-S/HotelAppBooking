using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface INotificationService
    {
        Task<NotificationResponseDto> CreateAsync(CreateNotificationDto dto);
        Task<NotificationResponseDto?> GetByIdAsync(int notificationId);
        Task<IEnumerable<NotificationResponseDto>> GetByUserAsync(int userId);
        Task<IEnumerable<NotificationResponseDto>> GetAllAsync();
        Task<PagedResponseDto<NotificationResponseDto>> GetPagedAsync(PagedRequestDto request);
        Task<PagedResponseDto<NotificationResponseDto>> GetPagedByUserAsync(int userId, PagedRequestDto request);
        Task<int> GetUnreadCountForUserAsync(int userId);
        Task<int> GetUnreadCountAllAsync();
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> DeleteAsync(int notificationId);
    }
}
