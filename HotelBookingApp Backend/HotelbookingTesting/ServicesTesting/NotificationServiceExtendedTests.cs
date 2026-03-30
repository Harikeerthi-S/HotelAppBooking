using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class NotificationServiceExtendedTests
{
    private readonly Mock<IRepository<int, Notification>> _notifRepo = new();
    private readonly Mock<IRepository<int, User>>         _userRepo  = new();
    private readonly Mock<ILogger<NotificationService>>   _logger    = new();

    private NotificationService CreateService() =>
        new(_notifRepo.Object, _userRepo.Object, _logger.Object);

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllNotifications()
    {
        var notifications = new List<Notification>
        {
            new() { NotificationId = 1, UserId = 1, Message = "Booking confirmed", IsRead = false, CreatedAt = DateTime.UtcNow },
            new() { NotificationId = 2, UserId = 2, Message = "Payment received",  IsRead = true,  CreatedAt = DateTime.UtcNow }
        };
        _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(notifications);

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedNotifications()
    {
        var notifications = Enumerable.Range(1, 25).Select(i => new Notification
        {
            NotificationId = i, UserId = 1, Message = $"Notification {i}",
            IsRead = i % 2 == 0, CreatedAt = DateTime.UtcNow
        }).ToList();

        _notifRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(notifications);

        var result = await CreateService().GetPagedAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(25, result.TotalRecords);
        Assert.Equal(3, result.TotalPages);
    }

    // ── GetPagedByUserAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByUserAsync_ReturnsUserNotifications()
    {
        var notifications = new List<Notification>
        {
            new() { NotificationId = 1, UserId = 3, Message = "Msg1", IsRead = false, CreatedAt = DateTime.UtcNow },
            new() { NotificationId = 2, UserId = 3, Message = "Msg2", IsRead = true,  CreatedAt = DateTime.UtcNow }
        };
        _notifRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                  .ReturnsAsync(notifications);

        var result = await CreateService().GetPagedByUserAsync(3, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalRecords);
    }

    // ── GetUnreadCountForUserAsync ────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCountForUserAsync_ReturnsCorrectCount()
    {
        var unread = new List<Notification>
        {
            new() { NotificationId = 1, UserId = 1, Message = "Unread1", IsRead = false },
            new() { NotificationId = 2, UserId = 1, Message = "Unread2", IsRead = false }
        };
        _notifRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                  .ReturnsAsync(unread);

        var count = await CreateService().GetUnreadCountForUserAsync(1);

        Assert.Equal(2, count);
    }

    // ── MarkAsReadAsync — already read ────────────────────────────────────

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_ReturnsTrue()
    {
        var notif = new Notification { NotificationId = 1, UserId = 1, Message = "Test", IsRead = true };
        _notifRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);

        var result = await CreateService().MarkAsReadAsync(1);

        Assert.True(result);
        // UpdateAsync should NOT be called since already read
        _notifRepo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Notification>()), Times.Never);
    }

    // ── DeleteAsync — not found ───────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _notifRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);
        _notifRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99));
    }
}
