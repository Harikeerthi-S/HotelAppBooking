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
        private readonly AuditLogService _sut;

        public AuditLogServiceTests()
        {
            _repoMock = new Mock<IRepository<int, AuditLog>>();
            _sut = new AuditLogService(_repoMock.Object, MockLogger.Create<AuditLogService>());
        }

        private static AuditLog SampleLog(int id = 1) => new()
        {
            AuditLogId = id, UserId = 1, Action = "BookingCreated",
            EntityName = "Booking", EntityId = 10, CreatedAt = DateTime.UtcNow
        };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsDto()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .ReturnsAsync((AuditLog a) => { a.AuditLogId = 1; return a; });

            var dto = new CreateAuditLogDto { Action = "BookingCreated", EntityName = "Booking", EntityId = 10 };
            var result = await _sut.CreateAsync(dto);

            Assert.Equal("BookingCreated", result.Action);
            Assert.Equal(1, result.AuditLogId);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(new List<AuditLog> { SampleLog() });

            var result = await _sut.GetByIdAsync(1);

            Assert.Equal("BookingCreated", result!.Action);
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
        public async Task GetAllAsync_ReturnsPaged()
        {
            var logs = Enumerable.Range(1, 8).Select(i => SampleLog(i)).ToList();
            _repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                     .ReturnsAsync(logs);

            var result = await _sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 5 });

            Assert.Equal(5, result.Data.Count);
            Assert.Equal(8, result.TotalRecords);
        }

        // ── FilterAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task FilterAsync_ByAction_ReturnsFiltered()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<AuditLog>
                     {
                         new() { AuditLogId = 1, Action = "BookingCreated", EntityName = "Booking", CreatedAt = DateTime.UtcNow },
                         new() { AuditLogId = 2, Action = "HotelUpdated", EntityName = "Hotel", CreatedAt = DateTime.UtcNow }
                     });

            var result = await _sut.FilterAsync(new AuditLogFilterDto { Action = "Booking" });

            Assert.Single(result);
            Assert.Equal("BookingCreated", result[0].Action);
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
                     .ReturnsAsync(new List<AuditLog> { SampleLog(), SampleLog(2) });

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
