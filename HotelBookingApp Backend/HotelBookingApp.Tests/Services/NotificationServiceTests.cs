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
        private readonly Mock<IRepository<int, User>> _userRepoMock;
        private readonly NotificationService _sut;

        public NotificationServiceTests()
        {
            _notifRepoMock = new Mock<IRepository<int, Notification>>();
            _userRepoMock = new Mock<IRepository<int, User>>();
            _sut = new NotificationService(_notifRepoMock.Object, _userRepoMock.Object, MockLogger.Create<NotificationService>());
        }

        private static User SampleUser() => new() { UserId = 1, UserName = "Alice", Email = "a@a.com", Role = "user" };

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
        public async Task CreateAsync_EmptyMessage_ThrowsBadRequestException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.CreateAsync(new CreateNotificationDto { UserId = 1, Message = "  " }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsDto()
        {
            _notifRepoMock.Setup(r => r.GetByIdAsync(1))
                          .ReturnsAsync(new Notification { NotificationId = 1, UserId = 1, Message = "Hello", IsRead = false });

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
        public async Task GetByUserAsync_ReturnsUserNotifications()
        {
            _notifRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                          .ReturnsAsync(new List<Notification>
                          {
                              new() { NotificationId = 1, UserId = 1, Message = "A", CreatedAt = DateTime.UtcNow },
                              new() { NotificationId = 2, UserId = 1, Message = "B", CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
                          });

            var result = (await _sut.GetByUserAsync(1)).ToList();

            Assert.Equal(2, result.Count);
        }

        // ── GetUnreadCountForUserAsync ────────────────────────────────────

        [Fact]
        public async Task GetUnreadCountForUserAsync_ReturnsCount()
        {
            _notifRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                          .ReturnsAsync(new List<Notification>
                          {
                              new() { NotificationId = 1, UserId = 1, IsRead = false },
                              new() { NotificationId = 2, UserId = 1, IsRead = false }
                          });

            var count = await _sut.GetUnreadCountForUserAsync(1);

            Assert.Equal(2, count);
        }

        // ── MarkAsReadAsync ───────────────────────────────────────────────

        [Fact]
        public async Task MarkAsReadAsync_Unread_ReturnsTrue()
        {
            var notif = new Notification { NotificationId = 1, UserId = 1, Message = "X", IsRead = false };
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);
            _notifRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Notification>()))
                          .ReturnsAsync((int _, Notification n) => n);

            Assert.True(await _sut.MarkAsReadAsync(1));
        }

        [Fact]
        public async Task MarkAsReadAsync_AlreadyRead_ReturnsTrue()
        {
            var notif = new Notification { NotificationId = 1, UserId = 1, Message = "X", IsRead = true };
            _notifRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);

            Assert.True(await _sut.MarkAsReadAsync(1));
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
            _notifRepoMock.Setup(r => r.DeleteAsync(1))
                          .ReturnsAsync(new Notification { NotificationId = 1, UserId = 1, Message = "X" });

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
