using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IWalletService
    {
        Task<WalletResponseDto> GetByUserAsync(int userId);
        Task<WalletResponseDto> CreditAsync(int userId, decimal amount, string description, int? referenceId = null);
        Task<WalletResponseDto> DebitAsync(int userId, decimal amount, string description, int? referenceId = null);
    }
}
