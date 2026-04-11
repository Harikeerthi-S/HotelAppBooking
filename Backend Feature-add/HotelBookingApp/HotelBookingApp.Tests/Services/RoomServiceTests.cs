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
    public class RoomServiceTests
    {
        private readonly Mock<IRepository<int, Room>>    _roomRepoMock;
        private readonly Mock<IRepository<int, Hotel>>   _hotelRepoMock;
        private readonly Mock<IRepository<int, Booking>> _bookingRepoMock;
        private readonly Mock<IAuditLogService>          _auditMock;
        private readonly RoomService                     _sut;

        public RoomServiceTests()
        {
            _roomRepoMock    = new Mock<IRepository<int, Room>>();
            _hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            _bookingRepoMock = new Mock<IRepository<int, Booking>>();
            _auditMock       = new Mock<IAuditLogService>();
            _auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                      .ReturnsAsync(new AuditLogResponseDto());
            _sut = new RoomService(_roomRepoMock.Object, _hotelRepoMock.Object,
                _bookingRepoMock.Object, _auditMock.Object, MockLogger.Create<RoomService>());
        }

        private static Hotel ActiveHotel()   => new() { HotelId = 1, HotelName = "H", Location = "L", IsActive = true };
        private static Hotel InactiveHotel() => new() { HotelId = 1, HotelName = "H", Location = "L", IsActive = false };
        private static Room SampleRoom(int id = 1) => new()
        {
            RoomId = id, HotelId = 1, RoomNumber = 101, RoomType = "Deluxe",
            PricePerNight = 2000, Capacity = 2, IsAvailable = true
        };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidRoom_ReturnsDto()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _roomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                         .ReturnsAsync(false);
            _roomRepoMock.Setup(r => r.AddAsync(It.IsAny<Room>()))
                         .ReturnsAsync((Room r) => { r.RoomId = 1; return r; });

            var dto = new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 2000, Capacity = 2 };
            var result = await _sut.CreateAsync(dto);
            Assert.Equal(101, result.RoomNumber);
        }

        [Fact]
        public async Task CreateAsync_HotelNull_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateRoomDto { HotelId = 99, RoomNumber = 1, RoomType = "X", PricePerNight = 100, Capacity = 1 }));
        }

        [Fact]
        public async Task CreateAsync_HotelInactive_ThrowsNotFoundException()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(InactiveHotel());
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateRoomDto { HotelId = 1, RoomNumber = 1, RoomType = "X", PricePerNight = 100, Capacity = 1 }));
        }

        [Fact]
        public async Task CreateAsync_DuplicateRoomNumber_ThrowsAlreadyExists()
        {
            _hotelRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ActiveHotel());
            _roomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                         .ReturnsAsync(true);
            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.CreateAsync(new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "X", PricePerNight = 100, Capacity = 1 }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleRoom());
            var result = await _sut.GetByIdAsync(1);
            Assert.Equal(101, result!.RoomNumber);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_WithHotelId_UsesFilter()
        {
            _roomRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                         .ReturnsAsync(new List<Room> { SampleRoom(1), SampleRoom(2) });

            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 }, hotelId: 1);
            Assert.Equal(2, result.TotalRecords);
        }

        [Fact]
        public async Task GetAllAsync_WithoutHotelId_GetsAll()
        {
            _roomRepoMock.Setup(r => r.GetAllAsync())
                         .ReturnsAsync(new List<Room> { SampleRoom(1), SampleRoom(2), SampleRoom(3) });

            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(3, result.TotalRecords);
        }

        [Fact]
        public async Task GetAllAsync_PageSizeClamped()
        {
            _roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>());
            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 0, PageSize = 999 });
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        // ── UpdateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ValidRoom_ReturnsUpdatedDto()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleRoom());
            _roomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                         .ReturnsAsync(false);
            _roomRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Room>()))
                         .ReturnsAsync((int _, Room r) => r);

            var dto = new CreateRoomDto { HotelId = 1, RoomNumber = 102, RoomType = "Suite", PricePerNight = 5000, Capacity = 4 };
            var result = await _sut.UpdateAsync(1, dto);
            Assert.Equal("Suite", result!.RoomType);
        }

        [Fact]
        public async Task UpdateAsync_UpdateReturnsNull_ReturnsNull()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleRoom());
            _roomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                         .ReturnsAsync(false);
            _roomRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Room>())).ReturnsAsync((Room?)null);

            var result = await _sut.UpdateAsync(1, new CreateRoomDto { HotelId = 1, RoomNumber = 102, RoomType = "Suite", PricePerNight = 5000, Capacity = 4 });
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateAsync(99, new CreateRoomDto { HotelId = 1, RoomNumber = 1, RoomType = "X", PricePerNight = 100, Capacity = 1 }));
        }

        [Fact]
        public async Task UpdateAsync_DuplicateRoomNumber_ThrowsAlreadyExists()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleRoom());
            _roomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                         .ReturnsAsync(true);
            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.UpdateAsync(1, new CreateRoomDto { HotelId = 1, RoomNumber = 102, RoomType = "X", PricePerNight = 100, Capacity = 1 }));
        }

        // ── DeactivateAsync ───────────────────────────────────────────────

        [Fact]
        public async Task DeactivateAsync_AvailableRoom_ReturnsTrue()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleRoom());
            _roomRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Room>())).ReturnsAsync((int _, Room r) => r);
            Assert.True(await _sut.DeactivateAsync(1));
        }

        [Fact]
        public async Task DeactivateAsync_NotFound_ThrowsNotFoundException()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeactivateAsync(99));
        }

        [Fact]
        public async Task DeactivateAsync_AlreadyInactive_ThrowsBadRequest()
        {
            var room = SampleRoom(); room.IsAvailable = false;
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
            await Assert.ThrowsAsync<BadRequestException>(() => _sut.DeactivateAsync(1));
        }

        // ── FilterAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task FilterAsync_AllFilters_ReturnsFiltered()
        {
            var rooms = new List<Room>
            {
                new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 2000, Capacity = 2, IsAvailable = true },
                new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Suite",  PricePerNight = 5000, Capacity = 4, IsAvailable = true },
                new() { RoomId = 3, HotelId = 2, RoomNumber = 201, RoomType = "Deluxe", PricePerNight = 1500, Capacity = 2, IsAvailable = false }
            };
            _roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

            var filter = new RoomFilterDto
            {
                OnlyAvailable = true, HotelId = 1, RoomType = "Deluxe",
                MinPrice = 1000, MaxPrice = 3000, MinCapacity = 1, MaxCapacity = 3
            };
            var result = (await _sut.FilterAsync(filter)).ToList();
            Assert.Single(result);
            Assert.Equal(101, result[0].RoomNumber);
        }

        [Fact]
        public async Task FilterAsync_NoFilters_ReturnsAll()
        {
            var rooms = new List<Room>
            {
                new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 2000, Capacity = 2, IsAvailable = true },
                new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Suite",  PricePerNight = 5000, Capacity = 4, IsAvailable = false }
            };
            _roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

            var filter = new RoomFilterDto { OnlyAvailable = false };
            var result = (await _sut.FilterAsync(filter)).ToList();
            Assert.Equal(2, result.Count);
        }

        // ── IsAvailableForDatesAsync ──────────────────────────────────────

        [Fact]
        public async Task IsAvailableForDatesAsync_NoOverlap_ReturnsTrue()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleRoom());
            _bookingRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                            .ReturnsAsync(new List<Booking>());

            Assert.True(await _sut.IsAvailableForDatesAsync(1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8)));
        }

        [Fact]
        public async Task IsAvailableForDatesAsync_Overlap_ReturnsFalse()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleRoom());
            _bookingRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                            .ReturnsAsync(new List<Booking>
                            {
                                new() { BookingId = 1, RoomId = 1, Status = "Confirmed",
                                    CheckIn = DateTime.Today.AddDays(4), CheckOut = DateTime.Today.AddDays(7) }
                            });

            Assert.False(await _sut.IsAvailableForDatesAsync(1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8)));
        }

        [Fact]
        public async Task IsAvailableForDatesAsync_InactiveRoom_ReturnsFalse()
        {
            var room = SampleRoom(); room.IsAvailable = false;
            _roomRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

            Assert.False(await _sut.IsAvailableForDatesAsync(1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
        }

        [Fact]
        public async Task IsAvailableForDatesAsync_NotFound_ThrowsNotFoundException()
        {
            _roomRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.IsAvailableForDatesAsync(99, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
        }
    }
}
