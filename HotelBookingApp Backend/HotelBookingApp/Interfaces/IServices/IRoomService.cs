using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IRoomService
    {
        Task<RoomResponseDto> CreateAsync(CreateRoomDto dto);
        Task<RoomResponseDto?> GetByIdAsync(int roomId);
        Task<IEnumerable<RoomResponseDto>> GetAllAsync(int? hotelId = null);
        Task<RoomResponseDto?> UpdateAsync(int roomId, CreateRoomDto dto);
        Task<bool> DeactivateAsync(int roomId);
        Task<IEnumerable<RoomResponseDto>> FilterAsync(RoomFilterDto filter);
        Task<PagedResponseDto<RoomResponseDto>> FilterPagedAsync(RoomFilterDto filter, PagedRequestDto request);
    }
}
