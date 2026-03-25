using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IRoomService
    {
        // ── CREATE ─────────────────────────────
        Task<RoomResponseDto> CreateAsync(CreateRoomDto dto);

        // ── GET BY ID ──────────────────────────
        Task<RoomResponseDto?> GetByIdAsync(int roomId);

        // ── GET ALL WITH PAGINATION ────────────
        Task<PagedResponseDto<RoomResponseDto>> GetAllAsync(
            PagedRequestDto request,
            int? hotelId = null
        );

        // ── UPDATE ─────────────────────────────
        Task<RoomResponseDto?> UpdateAsync(int roomId, CreateRoomDto dto);

        // ── DEACTIVATE ─────────────────────────
        Task<bool> DeactivateAsync(int roomId);

        // ── FILTER (NON-PAGED) ─────────────────
        Task<IEnumerable<RoomResponseDto>> FilterAsync(RoomFilterDto filter);
    }
}