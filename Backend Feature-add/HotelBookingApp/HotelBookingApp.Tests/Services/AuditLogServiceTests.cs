using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IRepository<int, AuditLog>> _repoMock;
        private readonly AuditLogService                  _sut;

        public AuditLogServiceTests()
        {
            _repoMock = new Mock<IRepository<int, AuditLog>>();
            _sut = new AuditLogService(_repoMock.Object, MockLogger.Create<AuditLogService>());
        }

        private static AuditLog SampleLog(int id = 1, int? userId = 1) => new()
        {
            AuditLogId = id, UserId = userId, Action = "BookingCreated",
            EntityName = "Booking", EntityId = 10, CreatedAt = DateTime.UtcNow
        };

        private static AuditLog LogWithUser(int id = 1) => new()
        {
            AuditLogId = id, UserId = 1, Action = "BookingCreated",
            EntityName = "Booking", EntityId = 10, CreatedAt = DateTime.UtcNow,
            User = new User { UserId = 1, UserName = "Alice", Email = "a@a.com", Role = "user" }
        };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsDto()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .ReturnsAsync((AuditLog a) => { a.AuditLogId = 1; return a; });

            var result = await _sut.CreateAsync(new CreateAuditLogDto { Action = "BookingCreated", EntityName = "Booking", EntityId = 10 });
            Assert.Equal("BookingCreated", result.Action);
        }

        [Fact]
        public async Task CreateAsync_WithChanges_PreservesChanges()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .ReturnsAsync((AuditLog a) => { a.AuditLogId = 2; return a; });

            var result = await _sut.CreateAsync(new CreateAuditLogDto
            {
                Action = "RoomUpdated", EntityName = "Room", EntityId = 5, Changes = "Price changed"
            });
            Assert.Equal("Price changed", result.Changes);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_WithUser_ReturnsUserName()
        {
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(new List<AuditLog> { LogWithUser() });

            var result = await _sut.GetByIdAsync(1);
            Assert.Equal("Alice", result!.UserName);
        }

        [Fact]
        public async Task GetByIdAsync_WithoutUser_ReturnsEmptyUserName()
        {
            var log = SampleLog(); // no User nav property
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(new List<AuditLog> { log });

            var result = await _sut.GetByIdAsync(1);
            Assert.Equal(string.Empty, result!.UserName);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(new List<AuditLog>());
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_WithUsers_ReturnsPaged()
        {
            var logs = Enumerable.Range(1, 8).Select(i => LogWithUser(i)).ToList();
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(logs);

            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 5 });
            Assert.Equal(5, result.Data.Count);
            Assert.Equal(8, result.TotalRecords);
        }

        [Fact]
        public async Task GetAllAsync_PageSizeClamped()
        {
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(new List<AuditLog>());
            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 0, PageSize = 999 });
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        // ── FilterAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task FilterAsync_ByAction_ReturnsFiltered()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<AuditLog>
                     {
                         new() { AuditLogId = 1, Action = "BookingCreated", EntityName = "Booking", CreatedAt = DateTime.UtcNow },
                         new() { AuditLogId = 2, Action = "HotelUpdated",   EntityName = "Hotel",   CreatedAt = DateTime.UtcNow }
                     });

            var result = await _sut.FilterAsync(new AuditLogFilterDto { Action = "Booking" });
            Assert.Single(result);
        }

        [Fact]
        public async Task FilterAsync_AllFilters_ReturnsFiltered()
        {
            var now = DateTime.UtcNow;
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<AuditLog>
                     {
                         new() { AuditLogId = 1, UserId = 1, Action = "BookingCreated", EntityName = "Booking", EntityId = 5, CreatedAt = now },
                         new() { AuditLogId = 2, UserId = 2, Action = "HotelUpdated",   EntityName = "Hotel",   EntityId = 1, CreatedAt = now.AddDays(-5) }
                     });

            var result = await _sut.FilterAsync(new AuditLogFilterDto
            {
                UserId = 1, Action = "Booking", EntityName = "Booking", EntityId = 5,
                FromDate = now.AddDays(-1), ToDate = now.AddDays(1)
            });
            Assert.Single(result);
        }

        [Fact]
        public async Task FilterAsync_NoFilters_ReturnsAll()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<AuditLog>
                     {
                         new() { AuditLogId = 1, Action = "A", EntityName = "E", CreatedAt = DateTime.UtcNow },
                         new() { AuditLogId = 2, Action = "B", EntityName = "E", CreatedAt = DateTime.UtcNow }
                     });

            var result = await _sut.FilterAsync(new AuditLogFilterDto());
            Assert.Equal(2, result.Count);
        }

        // ── FilterPagedAsync ──────────────────────────────────────────────

        [Fact]
        public async Task FilterPagedAsync_WithEntityName_ReturnsFiltered()
        {
            var logs = new List<AuditLog>
            {
                LogWithUser(1),
                new() { AuditLogId = 2, Action = "HotelUpdated", EntityName = "Hotel", CreatedAt = DateTime.UtcNow }
            };
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(logs);

            var result = await _sut.FilterPagedAsync(
                new AuditLogFilterDto { EntityName = "Booking" },
                new PagedRequestDto { PageNumber = 1, PageSize = 10 });

            Assert.Equal(1, result.TotalRecords);
        }

        // ── GetByEntityAsync ──────────────────────────────────────────────

        [Fact]
        public async Task GetByEntityAsync_ReturnsMatchingLogs()
        {
            _repoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>()))
                     .ReturnsAsync(new List<AuditLog> { SampleLog() });

            var result = await _sut.GetByEntityAsync("Booking", 10);
            Assert.Single(result);
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsUserLogs()
        {
            _repoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>()))
                     .ReturnsAsync(new List<AuditLog> { SampleLog(1), SampleLog(2) });

            var result = await _sut.GetByUserAsync(1);
            Assert.Equal(2, result.Count);
        }

        // ── DeleteAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_Exists_ReturnsTrue()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleLog());
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(SampleLog());
            Assert.True(await _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((AuditLog?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
        }
    }
}
