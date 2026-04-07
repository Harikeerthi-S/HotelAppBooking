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
    public class BookingServiceTests
    {
        private readonly Mock<IRepository<int, Booking>> _bookingRepoMock;
        private readonly Mock<IRepository<int, Hotel>>   _hotelRepoMock;
        private readonly Mock<IRepository<int, Room>>    _roomRepoMock;
        private readonly Mock<IAuditLogService>          _auditMock;
        private readonly BookingService                  _sut;

        public BookingServiceTests()
        {
            _bookingRepoMock = new Mock<IRepository<int, Booking>>();
            _hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            _roomRepoMock    = new Mock<IRepository<int, Room>>();
            _auditMock       = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new BookingService(_bookingRepoMock.Object, _hotelRepoMock.Object,
                _roomRepoMock.Object, _auditMock.Object, MockLogger.Create<BookingService>());
        }

        private static Hotel ActiveHotel() => new() { HotelId = 1, HotelName = "Grand", Location = "Mumbai", IsActive = true };
        private static Hotel InactiveHotel() => new() { HotelId = 1, HotelName = "Grand", Location = "Mumbai", IsActive = false };
        private static Room AvailableRoom() => new() { RoomId = 1, HotelId = 1, RoomNumber = 101, PricePerNight = 1000, IsAvailable = true };

        private CreateBookingDto ValidDto() => new()
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn  = DateTime.Today.AddDays(2),
            CheckOut = DateTime.Today.AddDays(4)
        };

        private void SetupNoOverlap()
        {
            _bookingRepoMock
                .Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>());
        }

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidBooking_ReturnsDto()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(AvailableRoom());
            SetupNoOverlap();
            _bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>()))
                            .ReturnsAsync((Booking b) => { b.BookingId = 10; return b; });

            var result = await _sut.CreateAsync(ValidDto());

            Assert.Equal(10, result.BookingId);
            Assert.Equal("Pending", result.Status);
            Assert.Equal(2000m, result.TotalAmount);
        }

        [Fact]
        public async Task CreateAsync_CheckOutBeforeCheckIn_ThrowsBadRequest()
        {
            var dto = ValidDto(); dto.CheckIn = DateTime.Today.AddDays(5); dto.CheckOut = DateTime.Today.AddDays(3);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_EqualCheckInCheckOut_ThrowsBadRequest()
        {
            var dto = ValidDto(); dto.CheckIn = dto.CheckOut = DateTime.Today.AddDays(3);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_PastCheckIn_ThrowsBadRequest()
        {
            var dto = ValidDto(); dto.CheckIn = DateTime.Today.AddDays(-1); dto.CheckOut = DateTime.Today.AddDays(2);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_HotelNull_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(ValidDto()));
        }

        [Fact]
        public async Task CreateAsync_HotelInactive_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(InactiveHotel());
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(ValidDto()));
        }

        [Fact]
        public async Task CreateAsync_RoomNull_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Room?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(ValidDto()));
        }

        [Fact]
        public async Task CreateAsync_RoomWrongHotel_ThrowsBadRequest()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            var room = AvailableRoom(); room.HotelId = 99;
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(ValidDto()));
        }

        [Fact]
        public async Task CreateAsync_RoomUnavailable_ThrowsBadRequest()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            var room = AvailableRoom(); room.IsAvailable = false;
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(ValidDto()));
        }

        [Fact]
        public async Task CreateAsync_SameUserOverlap_ThrowsAlreadyExists()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(AvailableRoom());
            _bookingRepoMock.SetupSequence(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>
                {
                    new() { BookingId = 5, UserId = 1, RoomId = 1, Status = "Confirmed",
                        CheckIn = DateTime.Today.AddDays(1), CheckOut = DateTime.Today.AddDays(5) }
                });
            await Assert.ThrowsAsync<AlreadyExistsException>(() => _sut.CreateAsync(ValidDto()));
        }

        [Fact]
        public async Task CreateAsync_OtherUserOverlap_ThrowsBadRequest()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(AvailableRoom());
            _bookingRepoMock.SetupSequence(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>())   // same-user: no overlap
                .ReturnsAsync(new List<Booking>
                {
                    new() { BookingId = 6, UserId = 2, RoomId = 1, Status = "Confirmed",
                        CheckIn = DateTime.Today.AddDays(1), CheckOut = DateTime.Today.AddDays(5) }
                });
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(ValidDto()));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());

            var result = await _sut.GetByIdAsync(1);
            Assert.Equal(1, result!.BookingId);
            Assert.Equal("Grand", result.HotelName);
        }

        [Fact]
        public async Task GetByIdAsync_HotelNull_ReturnsEmptyHotelName()
        {
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 99, RoomId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

            var result = await _sut.GetByIdAsync(1);
            Assert.Equal(string.Empty, result!.HotelName);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsPaged()
        {
            var bookings = Enumerable.Range(1, 5).Select(i => new Booking
            {
                BookingId = i, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(i), CheckOut = DateTime.Today.AddDays(i + 2)
            }).ToList();
            _bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);
            _hotelRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(ActiveHotel());

            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(5, result.TotalRecords);
        }

        [Fact]
        public async Task GetAllAsync_PageSizeClamped_MaxTen()
        {
            _bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 0, PageSize = 999 });
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsPaged()
        {
            var bookings = Enumerable.Range(1, 3).Select(i => new Booking
            {
                BookingId = i, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(i), CheckOut = DateTime.Today.AddDays(i + 2)
            }).ToList();
            _bookingRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                            .ReturnsAsync(bookings);
            _hotelRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(ActiveHotel());

            var result = await _sut.GetByUserAsync(1, new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(3, result.TotalRecords);
        }

        // ── GetByHotelAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetByHotelAsync_ReturnsPaged()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _bookingRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                            .ReturnsAsync(new List<Booking>
                            {
                                new() { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
                                    CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) }
                            });

            var result = await _sut.GetByHotelAsync(1, new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(1, result.TotalRecords);
        }

        [Fact]
        public async Task GetByHotelAsync_HotelNotFound_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.GetByHotelAsync(99, new PagedRequestDto { PageNumber = 1, PageSize = 10 }));
        }

        // ── GetPendingByHotelAsync ────────────────────────────────────────

        [Fact]
        public async Task GetPendingByHotelAsync_ReturnsPendingList()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _bookingRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                            .ReturnsAsync(new List<Booking>
                            {
                                new() { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
                                    CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) }
                            });

            var result = await _sut.GetPendingByHotelAsync(1);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetPendingByHotelAsync_HotelNotFound_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetPendingByHotelAsync(99));
        }

        // ── ConfirmAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task ConfirmAsync_PendingBooking_ReturnsConfirmed()
        {
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync((int _, Booking b) => b);
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());

            var result = await _sut.ConfirmAsync(1);
            Assert.Equal("Confirmed", result.Status);
        }

        [Fact]
        public async Task ConfirmAsync_NotFound_ThrowsNotFoundException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.ConfirmAsync(99));
        }

        [Fact]
        public async Task ConfirmAsync_AlreadyConfirmed_ThrowsBadRequest()
        {
            var booking = new Booking { BookingId = 1, Status = "Confirmed",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.ConfirmAsync(1));
        }

        [Fact]
        public async Task ConfirmAsync_HotelNullAfterUpdate_ReturnsEmptyHotelName()
        {
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 99, RoomId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync((int _, Booking b) => b);
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

            var result = await _sut.ConfirmAsync(1);
            Assert.Equal(string.Empty, result.HotelName);
        }

        // ── CompleteAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task CompleteAsync_ConfirmedBooking_ReturnsCompleted()
        {
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Confirmed",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync((int _, Booking b) => b);
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());

            var result = await _sut.CompleteAsync(1);
            Assert.Equal("Completed", result.Status);
        }

        [Fact]
        public async Task CompleteAsync_NotConfirmed_ThrowsBadRequest()
        {
            var booking = new Booking { BookingId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CompleteAsync(1));
        }

        // ── CancelAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CancelAsync_PendingBooking_ReturnsTrue()
        {
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync((int _, Booking b) => b);

            Assert.True(await _sut.CancelAsync(1));
        }

        [Fact]
        public async Task CancelAsync_NotFound_ThrowsNotFoundException()
        {
            _bookingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.CancelAsync(99));
        }

        [Fact]
        public async Task CancelAsync_CompletedBooking_ThrowsBadRequest()
        {
            var booking = new Booking { BookingId = 1, Status = "Completed",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CancelAsync(1));
        }

        [Fact]
        public async Task CancelAsync_AlreadyCancelled_ThrowsBadRequest()
        {
            var booking = new Booking { BookingId = 1, Status = "Cancelled",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            _bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.CancelAsync(1));
        }
    }
}
