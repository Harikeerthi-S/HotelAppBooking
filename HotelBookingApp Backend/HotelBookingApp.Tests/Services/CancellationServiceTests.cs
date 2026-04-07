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
    public class CancellationServiceTests
    {
        private readonly Mock<IRepository<int, Cancellation>> _cancellationRepoMock;
        private readonly Mock<IRepository<int, Booking>> _bookingRepoMock;
        private readonly Mock<IAuditLogService> _auditMock;
        private readonly CancellationService _sut;

        public CancellationServiceTests()
        {
            _cancellationRepoMock = new Mock<IRepository<int, Cancellation>>();
            _bookingRepoMock = new Mock<IRepository<int, Booking>>();
            _auditMock = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new CancellationService(_cancellationRepoMock.Object, _bookingRepoMock.Object,
                _auditMock.Object, MockLogger.Create<CancellationService>());
        }

        private static Booking ConfirmedBooking(double hoursUntilCheckIn = 48) => new()
        {
            BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1,
            TotalAmount = 5000, Status = "Confirmed",
            CheckIn = DateTime.UtcNow.AddHours(hoursUntilCheckIn),
            CheckOut = DateTime.UtcNow.AddHours(hoursUntilCheckIn + 24)
        };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ConfirmedBooking_Returns80PercentRefund()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ConfirmedBooking(48));
            _cancellationRepoMock.Setup(r => r.AddAsync(It.IsAny<Cancellation>()))
                                 .ReturnsAsync((Cancellation c) => { c.CancellationId = 1; return c; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Change of plans" });

            Assert.Equal(4000m, result.RefundAmount); // 80% of 5000
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task CreateAsync_LessThan24Hours_ReturnsZeroRefund()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ConfirmedBooking(12));
            _cancellationRepoMock.Setup(r => r.AddAsync(It.IsAny<Cancellation>()))
                                 .ReturnsAsync((Cancellation c) => { c.CancellationId = 1; return c; });
            _bookingRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "Emergency" });

            Assert.Equal(0m, result.RefundAmount);
        }

        [Fact]
        public async Task CreateAsync_BookingNotFound_ThrowsNotFoundException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateCancellationDto { BookingId = 99, Reason = "X" }));
        }

        [Fact]
        public async Task CreateAsync_CompletedBooking_ThrowsBadRequestException()
        {
            var booking = ConfirmedBooking(); booking.Status = "Completed";
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.CreateAsync(new CreateCancellationDto { BookingId = 1, Reason = "X" }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _cancellationRepoMock.Setup(r => r.GetByIdAsync(1))
                                 .ReturnsAsync(new Cancellation { CancellationId = 1, BookingId = 1, Reason = "X", Status = "Pending" });

            var result = await _sut.GetByIdAsync(1);

            Assert.Equal(1, result!.CancellationId);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _cancellationRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cancellation?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsPaged()
        {
            var cancellations = Enumerable.Range(1, 5).Select(i => new Cancellation
            {
                CancellationId = i, BookingId = i, Reason = "R", Status = "Pending",
                CancellationDate = DateTime.UtcNow.AddMinutes(-i)
            }).ToList();
            _cancellationRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(cancellations);

            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

            Assert.Equal(5, result.TotalRecords);
        }

        // ── UpdateStatusAsync ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateStatusAsync_ValidStatus_ReturnsUpdated()
        {
            var cancellation = new Cancellation { CancellationId = 1, BookingId = 1, Status = "Pending", RefundAmount = 4000 };
            _cancellationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cancellation);
            _cancellationRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Cancellation>()))
                                 .ReturnsAsync((int _, Cancellation c) => c);

            var result = await _sut.UpdateStatusAsync(1, "Approved");

            Assert.Equal("Approved", result.Status);
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_ThrowsBadRequestException()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateStatusAsync(1, "Unknown"));
        }

        [Fact]
        public async Task UpdateStatusAsync_Refunded_UpdatesBookingStatus()
        {
            var cancellation = new Cancellation { CancellationId = 1, BookingId = 1, Status = "Approved", RefundAmount = 4000 };
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Cancelled", CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };

            _cancellationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cancellation);
            _cancellationRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Cancellation>()))
                                 .ReturnsAsync((int _, Cancellation c) => c);
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>()))
                            .ReturnsAsync((int _, Booking b) => b);

            var result = await _sut.UpdateStatusAsync(1, "Refunded");

            Assert.Equal("Refunded", result.Status);
        }
    }
}
