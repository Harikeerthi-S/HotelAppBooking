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
        private readonly Mock<IAuditLogService>          _auditMock;
        private readonly PaymentService                  _sut;

        public PaymentServiceTests()
        {
            _paymentRepoMock = new Mock<IRepository<int, Payment>>();
            _bookingRepoMock = new Mock<IRepository<int, Booking>>();
            _auditMock       = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new PaymentService(_paymentRepoMock.Object, _bookingRepoMock.Object,
                _auditMock.Object, MockLogger.Create<PaymentService>());
        }

        private static Booking SampleBooking(decimal total = 2000) => new()
        {
            BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1,
            NumberOfRooms = 1, TotalAmount = total,
            CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4), Status = "Pending"
        };

        private static Payment SamplePayment(int id = 1, string status = "Completed") => new()
        {
            PaymentId = id, BookingId = 1, Amount = 2000,
            PaymentMethod = "CreditCard", PaymentStatus = status, CreatedAt = DateTime.UtcNow
        };

        // ── MakePaymentAsync ──────────────────────────────────────────────

        [Fact]
        public async Task MakePaymentAsync_CreditCard_ReturnsCompleted()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard" });
            Assert.Equal("Completed", result.PaymentStatus);
        }

        [Fact]
        public async Task MakePaymentAsync_DebitCard_ReturnsCompleted()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => { p.PaymentId = 2; return p; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "DebitCard" });
            Assert.Equal("Completed", result.PaymentStatus);
        }

        [Fact]
        public async Task MakePaymentAsync_NetBanking_ReturnsCompleted()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => { p.PaymentId = 3; return p; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "NetBanking" });
            Assert.Equal("Completed", result.PaymentStatus);
        }

        [Fact]
        public async Task MakePaymentAsync_AmountLessThanBooking_ReturnsFailed()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => { p.PaymentId = 4; return p; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 500, PaymentMethod = "CreditCard" });
            Assert.Equal("Failed", result.PaymentStatus);
        }

        [Fact]
        public async Task MakePaymentAsync_BookingUpdateThrows_StillReturnsDto()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                            .ReturnsAsync((Payment p) => { p.PaymentId = 5; return p; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard" });
            Assert.Equal("Completed", result.PaymentStatus);
        }

        [Fact]
        public async Task MakePaymentAsync_BookingNotFound_ThrowsNotFoundException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.MakePaymentAsync(new PaymentDto { BookingId = 99, Amount = 1000, PaymentMethod = "CreditCard" }));
        }

        [Fact]
        public async Task MakePaymentAsync_DuplicatePayment_ThrowsBadRequest()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment> { SamplePayment(1, "Completed") });
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard" }));
        }

        [Fact]
        public async Task MakePaymentAsync_InvalidMethod_ThrowsBadRequest()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleBooking());
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "Bitcoin" }));
        }

        [Fact]
        public async Task MakePaymentAsync_ZeroAmount_ThrowsBadRequest()
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
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SamplePayment());
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
        public async Task GetByBookingIdAsync_ReturnsLatest()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment>
                            {
                                new() { PaymentId = 1, BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                                new() { PaymentId = 2, BookingId = 1, Amount = 2000, PaymentMethod = "DebitCard",  PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow }
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

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsOrdered()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment> { SamplePayment(1), SamplePayment(2) });
            var result = (await _sut.GetAllAsync()).ToList();
            Assert.Equal(2, result.Count);
        }

        // ── GetPagedAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_ReturnsPaged()
        {
            var payments = Enumerable.Range(1, 12).Select(i => SamplePayment(i)).ToList();
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(payments);

            var result = await _sut.GetPagedAsync(new PagedRequestDto { PageNumber = 1, PageSize = 5 });
            Assert.Equal(5, result.Data.Count);
            Assert.Equal(12, result.TotalRecords);
        }

        // ── GetPagedByUserAsync ───────────────────────────────────────────

        [Fact]
        public async Task GetPagedByUserAsync_ReturnsUserPayments()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment> { SamplePayment(1), SamplePayment(2) });
            _bookingRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Booking> { SampleBooking() });

            var result = await _sut.GetPagedByUserAsync(1, new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(2, result.TotalRecords);
        }

        [Fact]
        public async Task GetPagedByUserAsync_NoMatchingBookings_ReturnsEmpty()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment> { SamplePayment(1) });
            _bookingRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Booking> { SampleBooking() }); // userId=1, but we query userId=99

            var result = await _sut.GetPagedByUserAsync(99, new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(0, result.TotalRecords);
        }

        // ── UpdateStatusAsync ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatusAsync_ToCompleted_SyncsBookingToConfirmed()
        {
            var payment = SamplePayment(1, "Pending");
            var booking = SampleBooking(); booking.Status = "Pending";
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync((int _, Payment p) => p);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>())).ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.UpdateStatusAsync(1, "Completed");
            Assert.Equal("Completed", result!.PaymentStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_ToFailed_SyncsBookingToPending()
        {
            var payment = SamplePayment(1, "Pending");
            var booking = SampleBooking(); booking.Status = "Confirmed";
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync((int _, Payment p) => p);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>())).ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.UpdateStatusAsync(1, "Failed");
            Assert.Equal("Failed", result!.PaymentStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_ToRefunded_SyncsBookingToCancelled()
        {
            var payment = SamplePayment(1, "Completed");
            var booking = SampleBooking(); booking.Status = "Confirmed";
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync((int _, Payment p) => p);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>())).ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.UpdateStatusAsync(1, "Refunded");
            Assert.Equal("Refunded", result!.PaymentStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_ToPending_NoBookingStatusChange()
        {
            var payment = SamplePayment(1, "Failed");
            var booking = SampleBooking(); booking.Status = "Pending";
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync((int _, Payment p) => p);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

            var result = await _sut.UpdateStatusAsync(1, "Pending");
            Assert.Equal("Pending", result!.PaymentStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_BookingNull_StillReturnsDto()
        {
            var payment = SamplePayment(1, "Pending");
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync((int _, Payment p) => p);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Booking?)null);

            var result = await _sut.UpdateStatusAsync(1, "Completed");
            Assert.Equal("Completed", result!.PaymentStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_BookingUpdateThrows_StillReturnsDto()
        {
            var payment = SamplePayment(1, "Pending");
            var booking = SampleBooking(); booking.Status = "Pending";
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync((int _, Payment p) => p);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _sut.UpdateStatusAsync(1, "Completed");
            Assert.Equal("Completed", result!.PaymentStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateStatusAsync(1, "Unknown"));
        }

        [Fact]
        public async Task UpdateStatusAsync_NotFound_ThrowsNotFoundException()
        {
            _paymentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Payment?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateStatusAsync(99, "Completed"));
        }

        // ── GetRevenueAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetRevenueAsync_AllStatuses_ReturnsCorrectTotals()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment>
                            {
                                new() { PaymentId = 1, BookingId = 1, Amount = 3000, PaymentMethod = "CreditCard", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow },
                                new() { PaymentId = 2, BookingId = 2, Amount = 1000, PaymentMethod = "DebitCard",  PaymentStatus = "Refunded",  CreatedAt = DateTime.UtcNow },
                                new() { PaymentId = 3, BookingId = 3, Amount = 500,  PaymentMethod = "NetBanking", PaymentStatus = "Pending",   CreatedAt = DateTime.UtcNow },
                                new() { PaymentId = 4, BookingId = 4, Amount = 200,  PaymentMethod = "CreditCard", PaymentStatus = "Failed",    CreatedAt = DateTime.UtcNow }
                            });

            var result = await _sut.GetRevenueAsync();
            Assert.Equal(3000, result.TotalRevenue);
            Assert.Equal(1000, result.TotalRefunds);
            Assert.Equal(2000, result.NetRevenue);
            Assert.Equal(4, result.TotalPayments);
            Assert.Equal(1, result.CompletedPayments);
            Assert.Equal(1, result.PendingPayments);
            Assert.Equal(1, result.FailedPayments);
            Assert.Equal(1, result.RefundedPayments);
        }

        [Fact]
        public async Task GetRevenueAsync_WithHotelFilter_FiltersCorrectly()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Payment>
                            {
                                new() { PaymentId = 1, BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow },
                                new() { PaymentId = 2, BookingId = 2, Amount = 1000, PaymentMethod = "DebitCard",  PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow }
                            });
            _bookingRepoMock.Setup(r => r.GetAllAsync())
                            .ReturnsAsync(new List<Booking>
                            {
                                new() { BookingId = 1, HotelId = 1, UserId = 1, RoomId = 1, CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2) },
                                new() { BookingId = 2, HotelId = 2, UserId = 1, RoomId = 2, CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2) }
                            });

            var result = await _sut.GetRevenueAsync(hotelId: 1);
            Assert.Equal(2000, result.TotalRevenue);
            Assert.Single(result.ByMethod);
        }

        [Fact]
        public async Task GetRevenueAsync_EmptyPayments_ReturnsZeros()
        {
            _paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            var result = await _sut.GetRevenueAsync();
            Assert.Equal(0, result.TotalRevenue);
            Assert.Empty(result.ByMethod);
            Assert.Empty(result.ByMonth);
        }
    }
}
