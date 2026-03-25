using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface ICancellationService
    {
        // CREATE
        Task<CancellationResponseDto> CreateAsync(CreateCancellationDto dto);

        // GET BY ID
        Task<CancellationResponseDto?> GetByIdAsync(int cancellationId);

        // GET ALL (PAGED) ✅ NEW
        Task<PagedResponseDto<CancellationResponseDto>> GetAllAsync(PagedRequestDto request);

        // GET BY USER
        Task<PagedResponseDto<CancellationResponseDto>> GetByUserAsync(int userId, PagedRequestDto request);

        // UPDATE STATUS
        Task<CancellationResponseDto> UpdateStatusAsync(int cancellationId, string status, decimal refundAmount = 0);
    }
}