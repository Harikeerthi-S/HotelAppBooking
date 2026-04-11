using HotelBookingApp.Context;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Repositories;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBookingApp.Tests.Services
{
    /// <summary>
    /// WalletService uses IServiceScopeFactory internally — each operation
    /// creates its own scope. We use a NAMED InMemory database so all scopes
    /// share the same data store within a single test.
    /// </summary>
    public class WalletServiceTests : IDisposable
    {
        private readonly string          _dbName;
        private readonly ServiceProvider _provider;
        private readonly WalletService   _sut;

        public WalletServiceTests()
        {
            _dbName = Guid.NewGuid().ToString(); // unique per test

            var services = new ServiceCollection();
            services.AddDbContext<HotelBookingContext>(o =>
                o.UseInMemoryDatabase(_dbName));
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            services.AddLogging();

            _provider = services.BuildServiceProvider();

            _sut = new WalletService(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                MockLogger.Create<WalletService>());
        }

        public void Dispose() => _provider.Dispose();

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>Seeds a user into the shared InMemory DB.</summary>
        private async Task<User> SeedUser(int id = 1, string name = "Alice")
        {
            using var scope = _provider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();
            var user = new User
            {
                UserId       = id,
                UserName     = name,
                Email        = $"{name.ToLower()}{id}@test.com",
                Role         = "user",
                PasswordHash = new byte[] { 1 }
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user;
        }

        /// <summary>Seeds a wallet with a given balance.</summary>
        private async Task SeedWallet(int userId, decimal balance)
        {
            using var scope = _provider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();
            ctx.Wallets.Add(new Wallet { UserId = userId, Balance = balance, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_NoWallet_AutoCreatesWithZeroBalance()
        {
            await SeedUser(1);

            var result = await _sut.GetByUserAsync(1);

            Assert.Equal(1, result.UserId);
            Assert.Equal(0m, result.Balance);
            Assert.Empty(result.Transactions);
        }

        [Fact]
        public async Task GetByUserAsync_ExistingWallet_ReturnsBalance()
        {
            await SeedUser(1);
            await SeedWallet(1, 5000m);

            var result = await _sut.GetByUserAsync(1);

            Assert.Equal(5000m, result.Balance);
        }

        [Fact]
        public async Task GetByUserAsync_UserNotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByUserAsync(99));
        }

        [Fact]
        public async Task GetByUserAsync_ReturnsTransactionsNewestFirst()
        {
            await SeedUser(1);
            // Seed wallet + 2 transactions
            using var scope = _provider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();
            var wallet = new Wallet { UserId = 1, Balance = 300m, UpdatedAt = DateTime.UtcNow };
            ctx.Wallets.Add(wallet);
            await ctx.SaveChangesAsync();
            ctx.WalletTransactions.AddRange(
                new WalletTransaction { WalletId = wallet.WalletId, Type = "Credit", Amount = 200m, Description = "Old",  CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new WalletTransaction { WalletId = wallet.WalletId, Type = "Credit", Amount = 100m, Description = "New",  CreatedAt = DateTime.UtcNow }
            );
            await ctx.SaveChangesAsync();

            var result = await _sut.GetByUserAsync(1);

            Assert.Equal(2, result.Transactions.Count);
            Assert.Equal("New", result.Transactions[0].Description); // newest first
        }

        // ── CreditAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreditAsync_NewUser_CreatesWalletAndCredits()
        {
            await SeedUser(1);

            var result = await _sut.CreditAsync(1, 1000m, "Photo review bonus", referenceId: 5);

            Assert.Equal(1000m, result.Balance);
            Assert.Single(result.Transactions);
            Assert.Equal("Credit", result.Transactions[0].Type);
            Assert.Equal(1000m, result.Transactions[0].Amount);
            Assert.Equal("Photo review bonus", result.Transactions[0].Description);
            Assert.Equal(5, result.Transactions[0].ReferenceId);
        }

        [Fact]
        public async Task CreditAsync_ExistingWallet_AddsToBalance()
        {
            await SeedUser(1);
            await SeedWallet(1, 2000m);

            var result = await _sut.CreditAsync(1, 500m, "Refund");

            Assert.Equal(2500m, result.Balance);
        }

        [Fact]
        public async Task CreditAsync_MultipleCalls_AccumulatesBalance()
        {
            await SeedUser(1);

            await _sut.CreditAsync(1, 100m, "First");
            await _sut.CreditAsync(1, 200m, "Second");
            var result = await _sut.CreditAsync(1, 300m, "Third");

            Assert.Equal(600m, result.Balance);
            Assert.Equal(3, result.Transactions.Count);
        }

        [Fact]
        public async Task CreditAsync_ZeroAmount_ThrowsBadRequest()
        {
            await SeedUser(1);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreditAsync(1, 0m, "X"));
        }

        [Fact]
        public async Task CreditAsync_NegativeAmount_ThrowsBadRequest()
        {
            await SeedUser(1);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreditAsync(1, -50m, "X"));
        }

        [Fact]
        public async Task CreditAsync_UserNotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreditAsync(99, 100m, "X"));
        }

        [Fact]
        public async Task CreditAsync_NullReferenceId_Works()
        {
            await SeedUser(1);

            var result = await _sut.CreditAsync(1, 100m, "No ref", referenceId: null);

            Assert.Equal(100m, result.Balance);
            Assert.Null(result.Transactions[0].ReferenceId);
        }

        [Fact]
        public async Task CreditAsync_TransactionDto_HasCorrectFields()
        {
            await SeedUser(1);

            var result = await _sut.CreditAsync(1, 250m, "Test desc", referenceId: 42);

            var tx = result.Transactions[0];
            Assert.Equal("Credit", tx.Type);
            Assert.Equal(250m, tx.Amount);
            Assert.Equal("Test desc", tx.Description);
            Assert.Equal(42, tx.ReferenceId);
            Assert.True(tx.TransactionId > 0);
            Assert.True(tx.CreatedAt > DateTime.MinValue);
        }

        // ── DebitAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task DebitAsync_SufficientBalance_DeductsAmount()
        {
            await SeedUser(1);
            await SeedWallet(1, 5000m);

            var result = await _sut.DebitAsync(1, 2000m, "Purchase");

            Assert.Equal(3000m, result.Balance);
            Assert.Single(result.Transactions);
            Assert.Equal("Debit", result.Transactions[0].Type);
            Assert.Equal(2000m, result.Transactions[0].Amount);
        }

        [Fact]
        public async Task DebitAsync_ExactBalance_DeductsToZero()
        {
            await SeedUser(1);
            await SeedWallet(1, 1000m);

            var result = await _sut.DebitAsync(1, 1000m, "Full spend");

            Assert.Equal(0m, result.Balance);
        }

        [Fact]
        public async Task DebitAsync_InsufficientBalance_ThrowsBadRequest()
        {
            await SeedUser(1);
            await SeedWallet(1, 500m);

            var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.DebitAsync(1, 1000m, "Too much"));

            Assert.Contains("Insufficient", ex.Message);
            Assert.Contains("500", ex.Message);
        }

        [Fact]
        public async Task DebitAsync_ZeroAmount_ThrowsBadRequest()
        {
            await SeedUser(1);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.DebitAsync(1, 0m, "X"));
        }

        [Fact]
        public async Task DebitAsync_NegativeAmount_ThrowsBadRequest()
        {
            await SeedUser(1);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.DebitAsync(1, -100m, "X"));
        }

        [Fact]
        public async Task DebitAsync_UserNotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DebitAsync(99, 100m, "X"));
        }

        [Fact]
        public async Task DebitAsync_WithReferenceId_StoresIt()
        {
            await SeedUser(1);
            await SeedWallet(1, 1000m);

            var result = await _sut.DebitAsync(1, 100m, "Debit with ref", referenceId: 7);

            Assert.Equal(7, result.Transactions[0].ReferenceId);
        }

        // ── Combined scenarios ────────────────────────────────────────────

        [Fact]
        public async Task CreditThenDebit_BalanceIsCorrect()
        {
            await SeedUser(1);

            await _sut.CreditAsync(1, 5000m, "Refund");
            await _sut.CreditAsync(1, 100m,  "Photo bonus");
            var result = await _sut.DebitAsync(1, 2000m, "Redemption");

            Assert.Equal(3100m, result.Balance);
            Assert.Equal(3, result.Transactions.Count);
        }

        [Fact]
        public async Task MultipleUsers_WalletsAreIsolated()
        {
            await SeedUser(1, "Alice");
            await SeedUser(2, "Bob");

            await _sut.CreditAsync(1, 1000m, "Alice credit");
            await _sut.CreditAsync(2, 500m,  "Bob credit");

            var alice = await _sut.GetByUserAsync(1);
            var bob   = await _sut.GetByUserAsync(2);

            Assert.Equal(1000m, alice.Balance);
            Assert.Equal(500m,  bob.Balance);
        }

        [Fact]
        public async Task GetByUserAsync_CalledTwice_DoesNotDuplicateWallet()
        {
            await SeedUser(1);

            await _sut.GetByUserAsync(1); // creates wallet
            var result = await _sut.GetByUserAsync(1); // should reuse

            Assert.Equal(0m, result.Balance);
            Assert.Empty(result.Transactions);
        }

        [Fact]
        public async Task CreditAsync_LargeAmount_Works()
        {
            await SeedUser(1);

            var result = await _sut.CreditAsync(1, 9_999_999m, "Max refund");

            Assert.Equal(9_999_999m, result.Balance);
        }

        [Fact]
        public async Task DebitAsync_AfterMultipleCredits_BalanceCorrect()
        {
            await SeedUser(1);

            await _sut.CreditAsync(1, 1000m, "C1");
            await _sut.CreditAsync(1, 1000m, "C2");
            await _sut.CreditAsync(1, 1000m, "C3");
            var result = await _sut.DebitAsync(1, 500m, "D1");

            Assert.Equal(2500m, result.Balance);
            Assert.Equal(4, result.Transactions.Count);
        }
    }
}
