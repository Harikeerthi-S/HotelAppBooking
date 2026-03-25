using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IBookingRoomService
    {
        Task<BookingRoomResponseDto> CreateAsync(CreateBookingRoomDto dto);
        Task<BookingRoomResponseDto?> GetByIdAsync(int bookingRoomId);
        Task<IEnumerable<BookingRoomResponseDto>> GetByBookingAsync(int bookingId);
        Task<BookingRoomResponseDto?> UpdateAsync(int bookingRoomId, CreateBookingRoomDto dto);
        Task<bool> DeleteAsync(int bookingRoomId);
    }
}
