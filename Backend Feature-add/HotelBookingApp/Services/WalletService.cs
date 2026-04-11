using HotelBookingApp.Context;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBookingApp.Services
{
    public class WalletService : IWalletService
    {
        private readonly IServiceScopeFactory  _scopeFactory;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            IServiceScopeFactory   scopeFactory,
            ILogger<WalletService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        // ── Each wallet operation gets its OWN DbContext scope ────────────
        // This prevents EF tracking conflicts when called from other services
        // that already have tracked entities in their own scoped DbContext.

        private async Task<Wallet> GetOrCreateWalletAsync(HotelBookingContext ctx, int userId)
        {
            var wallet = await ctx.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet != null) return wallet;

            // Verify user exists (use the same context)
            var user = await ctx.Users.FindAsync(userId)
                       ?? throw new NotFoundException("User", userId);

            wallet = new Wallet { UserId = userId, Balance = 0m, UpdatedAt = DateTime.UtcNow };
            ctx.Wallets.Add(wallet);
            await ctx.SaveChangesAsync();
            return wallet;
        }

        public async Task<WalletResponseDto> GetByUserAsync(int userId)
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();

            var wallet = await GetOrCreateWalletAsync(ctx, userId);
            var txs    = await ctx.WalletTransactions
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return MapToDto(wallet, txs);
        }

        public async Task<WalletResponseDto> CreditAsync(int userId, decimal amount, string description, int? referenceId = null)
        {
            if (amount <= 0) throw new BadRequestException("Credit amount must be greater than zero.");

            // Fresh scope = fresh DbContext = no tracking conflicts
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();

            var wallet = await GetOrCreateWalletAsync(ctx, userId);
            wallet.Balance  += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            ctx.WalletTransactions.Add(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                Type        = "Credit",
                Amount      = amount,
                Description = description,
                ReferenceId = referenceId,
                CreatedAt   = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Wallet credited: User={UserId} ₹{Amount} Ref={Ref}", userId, amount, referenceId);

            var txs = await ctx.WalletTransactions
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return MapToDto(wallet, txs);
        }

        public async Task<WalletResponseDto> DebitAsync(int userId, decimal amount, string description, int? referenceId = null)
        {
            if (amount <= 0) throw new BadRequestException("Debit amount must be greater than zero.");

            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();

            var wallet = await GetOrCreateWalletAsync(ctx, userId);
            if (wallet.Balance < amount)
                throw new BadRequestException($"Insufficient wallet balance. Available: ₹{wallet.Balance:N2}");

            wallet.Balance  -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            ctx.WalletTransactions.Add(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                Type        = "Debit",
                Amount      = amount,
                Description = description,
                ReferenceId = referenceId,
                CreatedAt   = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Wallet debited: User={UserId} ₹{Amount}", userId, amount);

            var txs = await ctx.WalletTransactions
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return MapToDto(wallet, txs);
        }

        private static WalletResponseDto MapToDto(Wallet w, IEnumerable<WalletTransaction> txs) => new()
        {
            WalletId  = w.WalletId,
            UserId    = w.UserId,
            Balance   = w.Balance,
            UpdatedAt = w.UpdatedAt,
            Transactions = txs.Select(t => new WalletTransactionDto
            {
                TransactionId = t.TransactionId,
                Type          = t.Type,
                Amount        = t.Amount,
                Description   = t.Description,
                ReferenceId   = t.ReferenceId,
                CreatedAt     = t.CreatedAt
            }).ToList()
        };
    }
}
