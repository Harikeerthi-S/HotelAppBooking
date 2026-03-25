using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IBookingService
    {
        // ── CREATE ─────────────────────────────
        Task<BookingResponseDto> CreateAsync(CreateBookingDto dto);

        // ── GET ────────────────────────────────
        Task<BookingResponseDto?> GetByIdAsync(int bookingId);

        Task<PagedResponseDto<BookingResponseDto>> GetAllAsync(PagedRequestDto request);

        Task<PagedResponseDto<BookingResponseDto>> GetByUserAsync(
            int userId,
            PagedRequestDto request
        );

        Task<PagedResponseDto<BookingResponseDto>> GetByHotelAsync(
            int hotelId,
            PagedRequestDto request
        );

        Task<List<BookingResponseDto>> GetPendingByHotelAsync(int hotelId);

        // ── STATUS MANAGEMENT ──────────────────
        Task<BookingResponseDto> ConfirmAsync(int bookingId);

        Task<BookingResponseDto> CompleteAsync(int bookingId);

        Task<bool> CancelAsync(int bookingId);
    }
}