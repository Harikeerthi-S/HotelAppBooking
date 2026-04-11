using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IHotelService
    {
        Task<HotelResponseDto> CreateAsync(CreateHotelDto dto);
        Task<HotelResponseDto?> GetByIdAsync(int hotelId);
        Task<PagedResponseDto<HotelResponseDto>> GetPagedAsync(PagedRequestDto request);
        Task<PagedResponseDto<HotelResponseDto>> FilterPagedAsync(HotelFilterDto filter, PagedRequestDto request);
        Task<IEnumerable<HotelResponseDto>> SearchAsync(string location);
        Task<HotelResponseDto?> UpdateAsync(int hotelId, CreateHotelDto dto);
        Task<bool> DeactivateAsync(int hotelId);
    }

}
