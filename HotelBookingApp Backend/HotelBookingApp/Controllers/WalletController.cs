using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
            => _walletService = walletService;

        /// <summary>Get (or create) wallet balance for a user.</summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetWallet(int userId)
            => Ok(await _walletService.GetOrCreateWalletAsync(userId));

        /// <summary>Paged transaction history for a user.</summary>
        [HttpPost("{userId}/transactions")]
        public async Task<IActionResult> GetTransactions(int userId, PagedRequestDto request)
            => Ok(await _walletService.GetTransactionsAsync(userId, request));

        /// <summary>Admin: manually debit wallet.</summary>
        [HttpPost("{userId}/debit")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Debit(int userId, [FromQuery] decimal amount, [FromQuery] string description)
            => Ok(await _walletService.DebitAsync(userId, amount, description));

        /// <summary>Process any pending cancellation refunds and credit them to the wallet.</summary>
        [HttpPost("{userId}/process-pending-refunds")]
        public async Task<IActionResult> ProcessPendingRefunds(int userId)
        {
            var count = await _walletService.ProcessPendingRefundsAsync(userId);
            return Ok(new { processed = count, message = $"{count} pending refund(s) credited to wallet." });
        }
    }
}
