using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IAmenityService
    {
        Task<AmenityResponseDto> CreateAsync(CreateAmenityDto dto);
        Task<AmenityResponseDto?> GetByIdAsync(int amenityId);
        Task<IEnumerable<AmenityResponseDto>> GetAllAsync();
        Task<bool> UpdateAsync(int amenityId, CreateAmenityDto dto);
        Task<bool> DeleteAsync(int amenityId);
    }
}
