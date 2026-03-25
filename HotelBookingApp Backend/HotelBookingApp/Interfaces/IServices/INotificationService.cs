using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface INotificationService
    {
        Task<NotificationResponseDto> CreateAsync(CreateNotificationDto dto);
        Task<NotificationResponseDto?> GetByIdAsync(int notificationId);
        Task<IEnumerable<NotificationResponseDto>> GetByUserAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> DeleteAsync(int notificationId);
    }
}
