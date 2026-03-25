using HotelBookingApp.Models.Dtos;

public interface IPaymentService
{
    Task<PaymentResponseDto> MakePaymentAsync(PaymentDto dto);

    Task<PaymentResponseDto?> GetByIdAsync(int paymentId);

    Task<PaymentResponseDto?> GetByBookingIdAsync(int bookingId); // ✅ ADD THIS

    Task<IEnumerable<PaymentResponseDto>> GetAllAsync();

    Task<PagedResponseDto<PaymentResponseDto>> GetPagedAsync(PagedRequestDto request);

    Task<PaymentResponseDto?> UpdateStatusAsync(int paymentId, string newStatus);
}