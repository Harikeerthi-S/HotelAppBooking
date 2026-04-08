using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> CreateAsync(CreateReviewDto dto);
        Task<ReviewResponseDto?> GetByIdAsync(int reviewId);
        Task<PagedResponseDto<ReviewResponseDto>> GetPagedAsync(ReviewFilterDto filter, PagedRequestDto request);
        Task<bool> DeleteAsync(int reviewId);
        Task<ReviewResponseDto> UploadPhotoAsync(int reviewId, IFormFile photo, IWebHostEnvironment env);
    }
}
