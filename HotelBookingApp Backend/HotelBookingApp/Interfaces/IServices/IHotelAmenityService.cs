using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IHotelAmenityService
    {
        Task<HotelAmenityResponseDto> CreateAsync(CreateHotelAmenityDto dto);
        Task<HotelAmenityResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<HotelAmenityResponseDto>> GetAllAsync();
        Task<bool> DeleteAsync(int id);
    }
}
