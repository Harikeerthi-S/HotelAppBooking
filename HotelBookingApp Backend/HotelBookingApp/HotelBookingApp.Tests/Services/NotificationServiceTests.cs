using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<IRepository<int, Notification>> _notifRepoMock;
        private readonly Mock<IRepository<int, User>>         _userRepoMock;
        private readonly NotificationService                  _sut;

        public NotificationServiceTests()
        {
            _notifRepoMock = new Mock<IRepository<int, Notification>>();
            _userRepoMock  = new Mock<IRepository<int, User>>();
            _sut = new NotificationService(_notifRepoMock.Object, _userRepoMock.Object, MockLogger.Create<NotificationService>());
        }

        private static User SampleUser() => new() { UserId = 1, UserName = "Alice", Email = "a@a.com", Role = "user" };
        private static Notification SampleNotif(int id = 1, bool isRead = false) =>
            new() { NotificationId = id, UserId = 1, Message = "Hello", IsRead = isRead, CreatedAt = DateTime.UtcNow };

        // ── CreateAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidNotification_ReturnsDto()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            _notifRepoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                          .ReturnsAsync((Notification n) => { n.NotificationId = 1; return n; });

            var result = await _sut.CreateAsync(new CreateNotificationDto { UserId = 1, Message = "Booking confirmed!" });
            Assert.Equal(1, result.NotificationId);
            Assert.False(result.IsRead);
        }

        [Fact]
        public async Task CreateAsync_UserNotFound_ThrowsNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateAsync(new CreateNotificationDto { UserId = 99, Message = "Hello" }));
        }

        [Fact]
        public async Task CreateAsync_EmptyMessage_ThrowsBadRequest()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.CreateAsync(new CreateNotificationDto { UserId = 1, Message = "  " }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleNotif());
            var result = await _sut.GetByIdAsync(1);
            Assert.Equal("Hello", result!.Message);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _notifRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsOrdered()
        {
            _notifRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                          .ReturnsAsync(new List<Notification>
                          {
                              new() { NotificationId = 1, UserId = 1, Message = "A", CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
                              new() { NotificationId = 2, UserId = 1, Message = "B", CreatedAt = DateTime.UtcNow }
                          });

            var result = (await _sut.GetByUserAsync(1)).ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal("B", result[0].Message); // newest first
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsAll()
        {
            _notifRepoMock.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<Notification> { SampleNotif(1), SampleNotif(2) });

            var result = (await _sut.GetAllAsync()).ToList();
            Assert.Equal(2, result.Count);
        }

        // ── GetPagedAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_ReturnsPaged()
        {
            var notifs = Enumerable.Range(1, 12).Select(i => SampleNotif(i)).ToList();
            _notifRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(notifs);

            var result = await _sut.GetPagedAsync(new PagedRequestDto { PageNumber = 1, PageSize = 5 });
            Assert.Equal(5, result.Data.Count);
            Assert.Equal(12, result.TotalRecords);
        }

        [Fact]
        public async Task GetPagedAsync_PageSizeClamped()
        {
            _notifRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Notification>());
            var result = await _sut.GetPagedAsync(new PagedRequestDto { PageNumber = 0, PageSize = 999 });
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        // ── GetPagedByUserAsync ───────────────────────────────────────────

        [Fact]
        public async Task GetPagedByUserAsync_ReturnsPaged()
        {
            _notifRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                          .ReturnsAsync(new List<Notification> { SampleNotif(1), SampleNotif(2) });

            var result = await _sut.GetPagedByUserAsync(1, new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(2, result.TotalRecords);
        }

        // ── GetUnreadCountForUserAsync ────────────────────────────────────

        [Fact]
        public async Task GetUnreadCountForUserAsync_ReturnsCount()
        {
            _notifRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                          .ReturnsAsync(new List<Notification> { SampleNotif(1, false), SampleNotif(2, false) });

            Assert.Equal(2, await _sut.GetUnreadCountForUserAsync(1));
        }

        // ── GetUnreadCountAllAsync ────────────────────────────────────────

        [Fact]
        public async Task GetUnreadCountAllAsync_ReturnsCount()
        {
            _notifRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                          .ReturnsAsync(new List<Notification> { SampleNotif(1, false), SampleNotif(2, false), SampleNotif(3, false) });

            Assert.Equal(3, await _sut.GetUnreadCountAllAsync());
        }

        // ── MarkAsReadAsync ───────────────────────────────────────────────

        [Fact]
        public async Task MarkAsReadAsync_Unread_SetsReadAndReturnsTrue()
        {
            var notif = SampleNotif(1, false);
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);
            _notifRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Notification>())).ReturnsAsync((int _, Notification n) => n);

            Assert.True(await _sut.MarkAsReadAsync(1));
        }

        [Fact]
        public async Task MarkAsReadAsync_AlreadyRead_ReturnsTrue()
        {
            var notif = SampleNotif(1, true);
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);

            Assert.True(await _sut.MarkAsReadAsync(1));
            _notifRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsReadAsync_NotFound_ThrowsNotFoundException()
        {
            _notifRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.MarkAsReadAsync(99));
        }

        // ── DeleteAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_Exists_ReturnsTrue()
        {
            _notifRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(SampleNotif());
            Assert.True(await _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _notifRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Notification?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
        }
    }
}
