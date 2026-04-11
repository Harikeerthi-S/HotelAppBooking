using HotelBookingApp.Context;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class CancellationServiceTests : IDisposable
    {
        private readonly HotelBookingContext _context;
        private readonly Mock<IWalletService> _walletMock;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CancellationService _sut;

        public CancellationServiceTests()
        {
            var options = new DbContextOptionsBuilder<HotelBookingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context    = new HotelBookingContext(options);
            _walletMock = new Mock<IWalletService>();

            // Build a real service scope factory with an in-memory audit service
            var services = new ServiceCollection();
            services.AddDbContext<HotelBookingContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddScoped(typeof(Interfaces.IRepositories.IRepository<,>), typeof(Repositories.Repository<,>));
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddLogging();
            _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int?>()))
                       .ReturnsAsync(new WalletResponseDto());

            _sut = new CancellationService(
                _context,
                _walletMock.Object,
                _scopeFactory,
                MockLogger.Create<CancellationService>());
        }

        public void Dispose() => _context.Dispose();

        // ── Helpers ───────────────────────────────────────────────────────

        private async Task<Booking> SeedBooking(double hoursUntilCheckIn = 48, string status = "Confirmed")
        {
            var booking = new Booking
            {
                UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
                TotalAmount = 5000, Status = status,
                CheckIn  = DateTime.UtcNow.AddHours(hoursUntilCheckIn),
                CheckOut = DateTime.UtcNow.AddHours(hoursUntilCheckIn + 24)
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        private async Task<Cancellation> SeedCancellation(int bookingId, string status = "Pending", decimal refund = 1250)
        {
            var c = new Cancellation
            {
                BookingId = bookingId, Reason = "Test", RefundAmount = refund,
                Status = status, CancellationDate = DateTime.UtcNow
            };
            _context.Cancellations.Add(c);
            await _context.SaveChangesAsync();
            return c;
        }

        // ── CreateAsync — refund tiers ────────────────────────────────────

        [Fact]
        public async Task CreateAsync_5DaysPlus_Returns100PercentRefund()
        {
            await SeedBooking(130); // ≥120h → 100%
            var result = await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Early cancel" });
            Assert.Equal(5000m, result.RefundAmount);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task CreateAsync_3To5Days_Returns50PercentRefund()
        {
            await SeedBooking(90); // 72-120h → 50%
            var result = await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Change of plans" });
            Assert.Equal(2500m, result.RefundAmount);
        }

        [Fact]
        public async Task CreateAsync_1To3Days_Returns25PercentRefund()
        {
            await SeedBooking(48); // 24-72h → 25%
            var result = await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Plans changed" });
            Assert.Equal(1250m, result.RefundAmount);
        }

        [Fact]
        public async Task CreateAsync_LessThan1Day_ReturnsZeroRefund()
        {
            await SeedBooking(12); // <24h → 0%
            var result = await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Emergency" });
            Assert.Equal(0m, result.RefundAmount);
        }

        [Fact]
        public async Task CreateAsync_SetsBookingStatusToCancelled()
        {
            await SeedBooking(48);
            await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Test" });
            var booking = await _context.Bookings.FindAsync(1);
            Assert.Equal("Cancelled", booking!.Status);
        }

        [Fact]
        public async Task CreateAsync_PendingBooking_CanBeCancelled()
        {
            await SeedBooking(48, "Pending");
            var result = await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Plans changed" });
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task CreateAsync_BookingNotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateCancellationDto { BookingId = 99, Reason = "X" }));
        }

        [Fact]
        public async Task CreateAsync_CompletedBooking_ThrowsBadRequest()
        {
            await SeedBooking(48, "Completed");
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "X" }));
        }

        [Fact]
        public async Task CreateAsync_CancelledBooking_ThrowsBadRequest()
        {
            await SeedBooking(48, "Cancelled");
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "X" }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            var b = await SeedBooking();
            var c = await SeedCancellation(b.BookingId);
            var result = await _sut.GetByIdAsync(c.CancellationId);
            Assert.Equal(c.CancellationId, result!.CancellationId);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsPaged()
        {
            var b = await SeedBooking();
            for (int i = 0; i < 5; i++) await SeedCancellation(b.BookingId);
            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(5, result.TotalRecords);
        }

        [Fact]
        public async Task GetAllAsync_PageSizeClamped()
        {
            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 0, PageSize = 999 });
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsPaged()
        {
            var b = await SeedBooking();
            await SeedCancellation(b.BookingId);
            var result = await _sut.GetByUserAsync(1, new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(1, result.TotalRecords);
        }

        // ── UpdateStatusAsync ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatusAsync_Approved_CreditsWallet()
        {
            var b = await SeedBooking();
            var c = await SeedCancellation(b.BookingId, "Pending", 2500);

            var result = await _sut.UpdateStatusAsync(c.CancellationId, "Approved");

            Assert.Equal("Approved", result.Status);
            Assert.True(result.WalletCredited);
            _walletMock.Verify(w => w.CreditAsync(1, 2500m, It.IsAny<string>(), c.CancellationId), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_Rejected_DoesNotCreditWallet()
        {
            var b = await SeedBooking();
            var c = await SeedCancellation(b.BookingId, "Pending", 0);

            var result = await _sut.UpdateStatusAsync(c.CancellationId, "Rejected");

            Assert.Equal("Rejected", result.Status);
            Assert.False(result.WalletCredited);
            _walletMock.Verify(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithRefundAmount_OverridesRefund()
        {
            var b = await SeedBooking();
            var c = await SeedCancellation(b.BookingId, "Pending", 1000);

            var result = await _sut.UpdateStatusAsync(c.CancellationId, "Approved", refundAmount: 3000);

            Assert.Equal(3000, result.RefundAmount);
        }

        [Fact]
        public async Task UpdateStatusAsync_Refunded_SetsBookingToRefunded()
        {
            var b = await SeedBooking();
            var c = await SeedCancellation(b.BookingId, "Approved", 5000);

            var result = await _sut.UpdateStatusAsync(c.CancellationId, "Refunded");

            Assert.Equal("Refunded", result.Status);
            var booking = await _context.Bookings.FindAsync(b.BookingId);
            Assert.Equal("Refunded", booking!.Status);
        }

        [Fact]
        public async Task UpdateStatusAsync_WalletCreditFails_StillReturnsSuccess()
        {
            var b = await SeedBooking();
            var c = await SeedCancellation(b.BookingId, "Pending", 2500);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int?>()))
                       .ThrowsAsync(new Exception("Wallet error"));

            var result = await _sut.UpdateStatusAsync(c.CancellationId, "Approved");

            Assert.Equal("Approved", result.Status);
            Assert.False(result.WalletCredited);
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateStatusAsync(1, "Unknown"));
        }

        [Fact]
        public async Task UpdateStatusAsync_NotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateStatusAsync(99, "Approved"));
        }
    }
}
