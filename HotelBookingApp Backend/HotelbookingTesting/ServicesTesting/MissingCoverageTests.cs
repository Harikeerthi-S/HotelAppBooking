using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

/// <summary>Covers GetByIdAsync methods not tested in the primary test files.</summary>
public class MissingCoverageTests
{
    // ── AmenityService.GetByIdAsync ───────────────────────────────────────

    [Fact]
    public async Task AmenityService_GetByIdAsync_ExistingAmenity_ReturnsDto()
    {
        var repo    = new Mock<IRepository<int, Amenity>>();
        var logger  = new Mock<ILogger<AmenityService>>();
        var amenity = new Amenity { AmenityId = 1, Name = "Pool", Icon = "🏊", Description = "Swimming pool" };

        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amenity);

        var svc    = new AmenityService(repo.Object, logger.Object);
        var result = await svc.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Pool", result!.Name);
        Assert.Equal("🏊", result.Icon);
    }

    [Fact]
    public async Task AmenityService_GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var repo   = new Mock<IRepository<int, Amenity>>();
        var logger = new Mock<ILogger<AmenityService>>();

        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

        var svc = new AmenityService(repo.Object, logger.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(99));
    }

    // ── HotelAmenityService.GetByIdAsync ──────────────────────────────────

    [Fact]
    public async Task HotelAmenityService_GetByIdAsync_ExistingEntry_ReturnsDto()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();
        var logger      = new Mock<ILogger<HotelAmenityService>>();

        var ha = new HotelAmenity
        {
            HotelAmenityId = 1, HotelId = 1, AmenityId = 2,
            Amenity = new Amenity { AmenityId = 2, Name = "Gym", Icon = "🏋️" }
        };

        haRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ha);

        var svc    = new HotelAmenityService(haRepo.Object, hotelRepo.Object, amenityRepo.Object, logger.Object);
        var result = await svc.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.HotelAmenityId);
        Assert.Equal(2, result.AmenityId);
    }

    [Fact]
    public async Task HotelAmenityService_GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();
        var logger      = new Mock<ILogger<HotelAmenityService>>();

        haRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((HotelAmenity?)null);

        var svc = new HotelAmenityService(haRepo.Object, hotelRepo.Object, amenityRepo.Object, logger.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(99));
    }

    // ── HotelAmenityService.GetAllAsync ───────────────────────────────────

    [Fact]
    public async Task HotelAmenityService_GetAllAsync_ReturnsAllEntries()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();
        var logger      = new Mock<ILogger<HotelAmenityService>>();

        var items = new List<HotelAmenity>
        {
            new() { HotelAmenityId = 1, HotelId = 1, AmenityId = 1, Amenity = new Amenity { Name = "Pool" } },
            new() { HotelAmenityId = 2, HotelId = 1, AmenityId = 2, Amenity = new Amenity { Name = "Gym"  } },
            new() { HotelAmenityId = 3, HotelId = 2, AmenityId = 1, Amenity = new Amenity { Name = "Pool" } }
        };

        haRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, object>>[]>()))
              .ReturnsAsync(items);

        var svc    = new HotelAmenityService(haRepo.Object, hotelRepo.Object, amenityRepo.Object, logger.Object);
        var result = (await svc.GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    // ── NotificationService.GetByIdAsync ─────────────────────────────────

    [Fact]
    public async Task NotificationService_GetByIdAsync_ExistingNotification_ReturnsDto()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();
        var logger    = new Mock<ILogger<NotificationService>>();

        var notif = new Notification { NotificationId = 1, UserId = 1, Message = "Test message", IsRead = false, CreatedAt = DateTime.UtcNow };
        notifRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);

        var svc    = new NotificationService(notifRepo.Object, userRepo.Object, logger.Object);
        var result = await svc.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Test message", result!.Message);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task NotificationService_GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();
        var logger    = new Mock<ILogger<NotificationService>>();

        notifRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);

        var svc = new NotificationService(notifRepo.Object, userRepo.Object, logger.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(99));
    }

    // ── NotificationService.GetUnreadCountAllAsync ────────────────────────

    [Fact]
    public async Task NotificationService_GetUnreadCountAllAsync_ReturnsCorrectCount()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();
        var logger    = new Mock<ILogger<NotificationService>>();

        var unread = new List<Notification>
        {
            new() { NotificationId = 1, UserId = 1, Message = "Msg1", IsRead = false },
            new() { NotificationId = 2, UserId = 2, Message = "Msg2", IsRead = false },
            new() { NotificationId = 3, UserId = 3, Message = "Msg3", IsRead = false }
        };
        notifRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>()))
                 .ReturnsAsync(unread);

        var svc   = new NotificationService(notifRepo.Object, userRepo.Object, logger.Object);
        var count = await svc.GetUnreadCountAllAsync();

        Assert.Equal(3, count);
    }

    // ── ChatService.GetByUserAsync ────────────────────────────────────────

    [Fact]
    public async Task ChatService_GetByUserAsync_ReturnsUserMessages()
    {
        var chatRepo    = new Mock<IRepository<int, ChatMessage>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var logger      = new Mock<ILogger<ChatService>>();

        var msgs = new List<ChatMessage>
        {
            new() { ChatMessageId = 1, UserId = 5, SessionId = "s1", Sender = "user", Message = "Hello", CreatedAt = DateTime.UtcNow },
            new() { ChatMessageId = 2, UserId = 5, SessionId = "s1", Sender = "bot",  Message = "Hi!",   CreatedAt = DateTime.UtcNow.AddSeconds(1) }
        };

        chatRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                .ReturnsAsync(msgs);

        var svc    = new ChatService(chatRepo.Object, hotelRepo.Object, bookingRepo.Object, logger.Object);
        var result = (await svc.GetByUserAsync(5)).ToList();

        Assert.Equal(2, result.Count);
    }
}
