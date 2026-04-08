using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    public class WalletService : IWalletService
    {
        private readonly IRepository<int, Wallet>            _walletRepo;
        private readonly IRepository<int, WalletTransaction> _txRepo;
        private readonly IRepository<int, User>              _userRepo;
        private readonly IRepository<int, Cancellation>      _cancellationRepo;
        private readonly IRepository<int, Booking>           _bookingRepo;
        private readonly ILogger<WalletService>              _logger;

        public WalletService(
            IRepository<int, Wallet>            walletRepo,
            IRepository<int, WalletTransaction> txRepo,
            IRepository<int, User>              userRepo,
            IRepository<int, Cancellation>      cancellationRepo,
            IRepository<int, Booking>           bookingRepo,
            ILogger<WalletService>              logger)
        {
            _walletRepo       = walletRepo;
            _txRepo           = txRepo;
            _userRepo         = userRepo;
            _cancellationRepo = cancellationRepo;
            _bookingRepo      = bookingRepo;
            _logger           = logger;
        }

        // ── GET OR CREATE ──────────────────────────────────────────────────
        public async Task<WalletResponseDto> GetOrCreateWalletAsync(int userId)
        {
            _ = await _userRepo.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            var existing = (await _walletRepo.FindAllAsync(w => w.UserId == userId)).FirstOrDefault();
            if (existing != null) return MapWallet(existing);

            var wallet = await _walletRepo.AddAsync(new Wallet { UserId = userId });
            _logger.LogInformation("Wallet created for User:{UserId}", userId);
            return MapWallet(wallet);
        }

        // ── CREDIT ─────────────────────────────────────────────────────────
        public async Task<WalletResponseDto> CreditAsync(int userId, decimal amount, string description, int? referenceId = null)
        {
            if (amount <= 0) throw new BadRequestException("Credit amount must be positive.");

            var wallet = await GetOrCreateInternalAsync(userId);
            wallet.Balance   += amount;
            wallet.UpdatedAt  = DateTime.UtcNow;
            await _walletRepo.UpdateAsync(wallet.WalletId, wallet);

            await _txRepo.AddAsync(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                Type        = "Credit",
                Amount      = amount,
                Description = description,
                ReferenceId = referenceId
            });

            _logger.LogInformation("Wallet credited ₹{Amount} for User:{UserId}", amount, userId);
            return MapWallet(wallet);
        }

        // ── DEBIT ──────────────────────────────────────────────────────────
        public async Task<WalletResponseDto> DebitAsync(int userId, decimal amount, string description, int? referenceId = null)
        {
            if (amount <= 0) throw new BadRequestException("Debit amount must be positive.");

            var wallet = await GetOrCreateInternalAsync(userId);
            if (wallet.Balance < amount)
                throw new BadRequestException($"Insufficient wallet balance. Available: ₹{wallet.Balance:N2}");

            wallet.Balance   -= amount;
            wallet.UpdatedAt  = DateTime.UtcNow;
            await _walletRepo.UpdateAsync(wallet.WalletId, wallet);

            await _txRepo.AddAsync(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                Type        = "Debit",
                Amount      = amount,
                Description = description,
                ReferenceId = referenceId
            });

            _logger.LogInformation("Wallet debited ₹{Amount} for User:{UserId}", amount, userId);
            return MapWallet(wallet);
        }

        // ── TRANSACTIONS (PAGED) ───────────────────────────────────────────
        public async Task<PagedResponseDto<WalletTransactionDto>> GetTransactionsAsync(int userId, PagedRequestDto request)
        {
            request.PageNumber = Math.Max(1, request.PageNumber);
            request.PageSize   = Math.Clamp(request.PageSize, 1, 50);

            var wallet = await GetOrCreateInternalAsync(userId);
            var all    = (await _txRepo.FindAllAsync(t => t.WalletId == wallet.WalletId))
                            .OrderByDescending(t => t.CreatedAt)
                            .ToList();

            var total = all.Count;
            var data  = all
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(MapTx)
                .ToList();

            return new PagedResponseDto<WalletTransactionDto>
            {
                Data         = data,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling((double)total / request.PageSize)
            };
        }

        // ── PROCESS PENDING REFUNDS ────────────────────────────────────────
        public async Task<int> ProcessPendingRefundsAsync(int userId)
        {
            // Get all bookings for this user
            var bookings = await _bookingRepo.FindAllAsync(b => b.UserId == userId);
            var bookingIds = bookings.ToDictionary(b => b.BookingId);

            // Find cancellations that have a refund amount but haven't been credited yet
            var allCancellations = await _cancellationRepo.FindAllAsync(
                c => bookingIds.ContainsKey(c.BookingId)
                     && c.RefundAmount > 0
                     && c.Status != "Refunded");

            int count = 0;
            foreach (var cancellation in allCancellations)
            {
                try
                {
                    await CreditAsync(
                        userId,
                        cancellation.RefundAmount,
                        $"Refund for Booking #{cancellation.BookingId} cancellation",
                        cancellation.CancellationId);

                    cancellation.Status = "Refunded";
                    await _cancellationRepo.UpdateAsync(cancellation.CancellationId, cancellation);

                    if (bookingIds.TryGetValue(cancellation.BookingId, out var booking))
                    {
                        booking.Status = "Refunded";
                        await _bookingRepo.UpdateAsync(booking.BookingId, booking);
                    }

                    count++;
                    _logger.LogInformation("Retroactive refund ₹{Amount} credited for Cancellation:{Id}", cancellation.RefundAmount, cancellation.CancellationId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process refund for Cancellation:{Id}", cancellation.CancellationId);
                }
            }

            return count;
        }

        // ── INTERNAL HELPERS ───────────────────────────────────────────────
        private async Task<Wallet> GetOrCreateInternalAsync(int userId)
        {
            var existing = (await _walletRepo.FindAllAsync(w => w.UserId == userId)).FirstOrDefault();
            if (existing != null) return existing;
            return await _walletRepo.AddAsync(new Wallet { UserId = userId });
        }

        private static WalletResponseDto MapWallet(Wallet w) => new()
        {
            WalletId  = w.WalletId,
            UserId    = w.UserId,
            Balance   = w.Balance,
            UpdatedAt = w.UpdatedAt
        };

        private static WalletTransactionDto MapTx(WalletTransaction t) => new()
        {
            TransactionId = t.TransactionId,
            Type          = t.Type,
            Amount        = t.Amount,
            Description   = t.Description,
            ReferenceId   = t.ReferenceId,
            CreatedAt     = t.CreatedAt
        };
    }
}
