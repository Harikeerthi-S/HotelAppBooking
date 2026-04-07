using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock;
        private readonly Mock<IRepository<int, Booking>> _bookingRepoMock;
        private readonly Mock<IAuditLogService> _auditMock;
        private readonly PaymentService _sut;

        public PaymentServiceTests()
        {
            _paymentRepoMock = new Mock<IRepository<int, Payment>>();
            _bookingRepoMock = new Mock<IRepository<int, Booking>>();
            _auditMock = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new PaymentService(_paymentRepoMock.Object, _bookingRepoMock.Object,
                _auditMock.Object, MockLogger.Create<PaymentService>());
        }

        private static Booking SampleBooking() => new()
        {
            BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1,
            NumberOfRooms = 1, TotalAmount = 2000,
            CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4),
            Status = "Pending"
        };

        // ── MakePaymentAsync ──────────────────────────────────────────────

        [Fact]
        public async Task MakePaymentAsync_ValidPayment_ReturnsCompleted()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var dto = new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard" };
            var result = await _sut.MakePaymentAsync(dto);

            Assert.Equal("Completed", result.PaymentStatus);
            Assert.Equal(1, result.PaymentId);
        }

        [Fact]
        public async Task MakePaymentAsync_AmountLessThanBooking_ReturnsFailed()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => { p.PaymentId = 2; return p; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var dto = new PaymentDto { BookingId = 1, Amount = 500, PaymentMethod = "CreditCard" };
            var result = await _sut.MakePaymentAsync(dto);

            Assert.Equal("Failed", result.PaymentStatus);
        }

        [Fact]
        public async Task MakePaymentAsync_BookingNotFound_ThrowsNotFoundException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.MakePaymentAsync(new PaymentDto { BookingId = 99, Amount = 1000, PaymentMethod = "CreditCard" }));
        }

        [Fact]
        public async Task MakePaymentAsync_DuplicatePayment_ThrowsBadRequestException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment>
                            {
                                new() { PaymentId = 1, BookingId = 1, PaymentStatus = "Completed" }
                            });

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard" }));
        }

        [Fact]
        public async Task MakePaymentAsync_InvalidMethod_ThrowsBadRequestException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "Bitcoin" }));
        }

        [Fact]
        public async Task MakePaymentAsync_ZeroAmount_ThrowsBadRequestException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 0, PaymentMethod = "CreditCard" }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1))
                            .ReturnsAsync(new Payment { PaymentId = 1, BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard", PaymentStatus = "Completed" });

            var result = await _sut.GetByIdAsync(1);

            Assert.Equal(1, result!.PaymentId);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _paymentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Payment?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetByBookingIdAsync ───────────────────────────────────────────

        [Fact]
        public async Task GetByBookingIdAsync_Exists_ReturnsLatest()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment>
                            {
                                new() { PaymentId = 1, BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                                new() { PaymentId = 2, BookingId = 1, Amount = 2000, PaymentMethod = "DebitCard", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow }
                            });

            var result = await _sut.GetByBookingIdAsync(1);

            Assert.Equal(2, result!.PaymentId);
        }

        [Fact]
        public async Task GetByBookingIdAsync_NotFound_ThrowsNotFoundException()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByBookingIdAsync(99));
        }

        // ── UpdateStatusAsync ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatusAsync_ValidStatus_ReturnsUpdated()
        {
            var payment = new Payment { PaymentId = 1, BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard", PaymentStatus = "Pending" };
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>()))
                            .ReturnsAsync((int _, Payment p) => p);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending", CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.UpdateStatusAsync(1, "Completed");

            Assert.Equal("Completed", result!.PaymentStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_ThrowsBadRequestException()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateStatusAsync(1, "Unknown"));
        }

        // ── GetRevenueAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetRevenueAsync_ReturnsCorrectTotals()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment>
                            {
                                new() { PaymentId = 1, BookingId = 1, Amount = 3000, PaymentMethod = "CreditCard", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow },
                                new() { PaymentId = 2, BookingId = 2, Amount = 1000, PaymentMethod = "DebitCard", PaymentStatus = "Refunded", CreatedAt = DateTime.UtcNow }
                            });

            var result = await _sut.GetRevenueAsync();

            Assert.Equal(3000, result.TotalRevenue);
            Assert.Equal(1000, result.TotalRefunds);
            Assert.Equal(2000, result.NetRevenue);
        }
    }
}
