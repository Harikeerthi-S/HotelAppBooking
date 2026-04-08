using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Interfaces.IServices
{
    public interface IWalletService
    {
        /// <summary>Returns (or creates) the wallet for a user.</summary>
        Task<WalletResponseDto> GetOrCreateWalletAsync(int userId);

        /// <summary>Credits the wallet — called automatically on cancellation refund.</summary>
        Task<WalletResponseDto> CreditAsync(int userId, decimal amount, string description, int? referenceId = null);

        /// <summary>Debits the wallet — e.g. when user pays with wallet balance.</summary>
        Task<WalletResponseDto> DebitAsync(int userId, decimal amount, string description, int? referenceId = null);

        /// <summary>Paged transaction history for a user.</summary>
        Task<PagedResponseDto<WalletTransactionDto>> GetTransactionsAsync(int userId, PagedRequestDto request);

        /// <summary>
        /// Finds all cancellations for a user that have a refund amount but are not yet
        /// credited (Status != "Refunded"), credits the wallet, and marks them Refunded.
        /// Call this to fix existing data or as a safety net.
        /// </summary>
        Task<int> ProcessPendingRefundsAsync(int userId);
    }
}
