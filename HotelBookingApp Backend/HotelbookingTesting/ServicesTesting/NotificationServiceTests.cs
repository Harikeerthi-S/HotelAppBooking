using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class NotificationServiceTests
{
    private readonly Mock<IRepository<int, Notification>> _notifRepo = new();
    private readonly Mock<IRepository<int, User>>         _userRepo  = new();
    private readonly Mock<ILogger<NotificationService>>   _logger    = new();

    // NotificationService(notifRepo, userRepo, logger)
    private NotificationService CreateService() =>
        new(_notifRepo.Object, _userRepo.Object, _logger.Object);

    [Fact]
    public async Task CreateAsync_ValidNotification_ReturnsDto()
    {
        var user = new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };
        var dto  = new CreateNotificationDto { UserId = 1, Message = "Your booking is confirmed!" };

        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                  .ReturnsAsync((Notification n) => { n.NotificationId = 1; return n; });

        var result = await CreateService().CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Your booking is confirmed!", result.Message);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
        var dto = new CreateNotificationDto { UserId = 99, Message = "Test" };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_EmptyMessage_ThrowsBadRequestException()
    {
        var user = new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var dto = new CreateNotificationDto { UserId = 1, Message = "" };

        await Assert.ThrowsAsync<HotelBookingApp.Exceptions.BadRequestException>(() => CreateService().CreateAsync(dto));
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsUserNotifications()
    {
        var notifications = new List<Notification>
        {
            new() { NotificationId = 1, UserId = 1, Message = "Booking confirmed", IsRead = false },
            new() { NotificationId = 2, UserId = 1, Message = "Payment received",  IsRead = true  }
        };
        _notifRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                  .ReturnsAsync(notifications);

        var result = await CreateService().GetByUserAsync(1);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task MarkAsReadAsync_ExistingNotification_ReturnsTrue()
    {
        var notif = new Notification { NotificationId = 1, UserId = 1, Message = "Test", IsRead = false };
        _notifRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);
        _notifRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Notification>())).ReturnsAsync(notif);

        var result = await CreateService().MarkAsReadAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task MarkAsReadAsync_NotFound_ThrowsNotFoundException()
    {
        _notifRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().MarkAsReadAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_ExistingNotification_ReturnsTrue()
    {
        var notif = new Notification { NotificationId = 1, UserId = 1, Message = "Test" };
        _notifRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);
        _notifRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(notif);

        var result = await CreateService().DeleteAsync(1);

        Assert.True(result);
    }
}
