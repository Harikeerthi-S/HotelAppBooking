using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

/// <summary>Covers every remaining uncovered line and branch across all services.</summary>
public class CompleteCoverageTests
{
    // ════════════════════════════════════════════════════════════════════
    // AMENITY SERVICE — UpdateAsync (all branches)
    // ════════════════════════════════════════════════════════════════════

    private static AmenityService MakeAmenitySvc(Mock<IRepository<int, Amenity>> repo) =>
        new(repo.Object, new Mock<ILogger<AmenityService>>().Object);

    [Fact]
    public async Task AmenityService_UpdateAsync_ValidUpdate_ReturnsTrue()
    {
        var repo    = new Mock<IRepository<int, Amenity>>();
        var amenity = new Amenity { AmenityId = 1, Name = "Pool", Icon = "🏊" };
        var dto     = new CreateAmenityDto { Name = "Swimming Pool", Description = "Outdoor pool", Icon = "🏊" };

        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amenity);
        repo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(1, It.IsAny<Amenity>())).ReturnsAsync(amenity);

        var result = await MakeAmenitySvc(repo).UpdateAsync(1, dto);

        Assert.True(result);
    }

    [Fact]
    public async Task AmenityService_UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IRepository<int, Amenity>>();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeAmenitySvc(repo).UpdateAsync(99, new CreateAmenityDto { Name = "Test" }));
    }

    [Fact]
    public async Task AmenityService_UpdateAsync_DuplicateName_ThrowsAlreadyExistsException()
    {
        var repo    = new Mock<IRepository<int, Amenity>>();
        var amenity = new Amenity { AmenityId = 1, Name = "Pool" };
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amenity);
        repo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
            .ReturnsAsync(true); // duplicate

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            MakeAmenitySvc(repo).UpdateAsync(1, new CreateAmenityDto { Name = "Gym" }));
    }

    // ════════════════════════════════════════════════════════════════════
    // WISHLIST SERVICE — GetByUserAsync empty, MapToDto
    // ════════════════════════════════════════════════════════════════════

    private static WishlistService MakeWishlistSvc(
        Mock<IRepository<int, Wishlist>> wishlistRepo,
        Mock<IRepository<int, User>>?    userRepo  = null,
        Mock<IRepository<int, Hotel>>?   hotelRepo = null)
    {
        userRepo  ??= new Mock<IRepository<int, User>>();
        hotelRepo ??= new Mock<IRepository<int, Hotel>>();
        return new WishlistService(wishlistRepo.Object, userRepo.Object, hotelRepo.Object,
                                   new Mock<ILogger<WishlistService>>().Object);
    }

    [Fact]
    public async Task WishlistService_GetByUserAsync_EmptyList_ReturnsEmpty()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        wishlistRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                    .ReturnsAsync(new List<Wishlist>());

        var result = await MakeWishlistSvc(wishlistRepo).GetByUserAsync(99);

        Assert.Empty(result);
    }

    [Fact]
    public async Task WishlistService_GetByUserAsync_MultipleItems_OrderedByDate()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var items = new List<Wishlist>
        {
            new() { WishlistId = 1, UserId = 1, HotelId = 1, SavedAt = DateTime.UtcNow.AddDays(-2) },
            new() { WishlistId = 2, UserId = 1, HotelId = 2, SavedAt = DateTime.UtcNow }
        };
        wishlistRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                    .ReturnsAsync(items);

        var result = (await MakeWishlistSvc(wishlistRepo).GetByUserAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].HotelId); // most recent first
    }

    // ════════════════════════════════════════════════════════════════════
    // REVIEW SERVICE — inactive hotel branch, DeleteAsync null branch
    // ════════════════════════════════════════════════════════════════════

    private static ReviewService MakeReviewSvc(
        Mock<IRepository<int, Review>>  reviewRepo,
        Mock<IRepository<int, Hotel>>   hotelRepo,
        Mock<IRepository<int, User>>    userRepo,
        Mock<IAuditLogService>?         audit = null)
    {
        audit ??= new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new ReviewService(reviewRepo.Object, hotelRepo.Object, userRepo.Object,
                                 audit.Object, new Mock<ILogger<ReviewService>>().Object);
    }

    [Fact]
    public async Task ReviewService_CreateAsync_InactiveHotel_ThrowsNotFoundException()
    {
        var reviewRepo = new Mock<IRepository<int, Review>>();
        var hotelRepo  = new Mock<IRepository<int, Hotel>>();
        var userRepo   = new Mock<IRepository<int, User>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = false });

        var dto = new CreateReviewDto { UserId = 1, HotelId = 1, Rating = 4, Comment = "Good" };

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeReviewSvc(reviewRepo, hotelRepo, userRepo).CreateAsync(dto));
    }

    [Fact]
    public async Task ReviewService_DeleteAsync_DeleteReturnsNull_ThrowsNotFoundException()
    {
        var reviewRepo = new Mock<IRepository<int, Review>>();
        var hotelRepo  = new Mock<IRepository<int, Hotel>>();
        var userRepo   = new Mock<IRepository<int, User>>();

        reviewRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Review { ReviewId = 1, HotelId = 1, UserId = 1, Rating = 4, Comment = "Good" });
        reviewRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync((Review?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeReviewSvc(reviewRepo, hotelRepo, userRepo).DeleteAsync(1));
    }

    [Fact]
    public async Task ReviewService_GetPagedAsync_Page2_ReturnsCorrectSlice()
    {
        var reviewRepo = new Mock<IRepository<int, Review>>();
        var hotelRepo  = new Mock<IRepository<int, Hotel>>();
        var userRepo   = new Mock<IRepository<int, User>>();

        var reviews = Enumerable.Range(1, 15).Select(i => new Review
        {
            ReviewId = i, HotelId = 1, UserId = i, Rating = 4, Comment = $"Review {i}", CreatedAt = DateTime.UtcNow
        }).ToList();
        reviewRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

        var result = await MakeReviewSvc(reviewRepo, hotelRepo, userRepo).GetPagedAsync(
            new ReviewFilterDto(),
            new PagedRequestDto { PageNumber = 2, PageSize = 10 });

        Assert.Equal(5, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
    }

    // ════════════════════════════════════════════════════════════════════
    // HOTEL AMENITY SERVICE — inactive hotel branch
    // ════════════════════════════════════════════════════════════════════

    private static HotelAmenityService MakeHotelAmenitySvc(
        Mock<IRepository<int, HotelAmenity>> haRepo,
        Mock<IRepository<int, Hotel>>        hotelRepo,
        Mock<IRepository<int, Amenity>>      amenityRepo) =>
        new(haRepo.Object, hotelRepo.Object, amenityRepo.Object,
            new Mock<ILogger<HotelAmenityService>>().Object);

    [Fact]
    public async Task HotelAmenityService_CreateAsync_InactiveHotel_ThrowsNotFoundException()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = false });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo)
                .CreateAsync(new CreateHotelAmenityDto { HotelId = 1, AmenityId = 1 }));
    }

    [Fact]
    public async Task HotelAmenityService_CreateAsync_NullHotel_ThrowsNotFoundException()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();

        hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo)
                .CreateAsync(new CreateHotelAmenityDto { HotelId = 99, AmenityId = 1 }));
    }

    // ════════════════════════════════════════════════════════════════════
    // BOOKING SERVICE — GetByHotelAsync pagination, GetByUserAsync empty
    // ════════════════════════════════════════════════════════════════════

    private static BookingService MakeBookingSvc(
        Mock<IRepository<int, Booking>> bookingRepo,
        Mock<IRepository<int, Hotel>>   hotelRepo,
        Mock<IRepository<int, Room>>    roomRepo)
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new BookingService(bookingRepo.Object, hotelRepo.Object, roomRepo.Object,
                                  audit.Object, new Mock<ILogger<BookingService>>().Object);
    }

    [Fact]
    public async Task BookingService_GetByUserAsync_EmptyList_ReturnsEmpty()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                   .ReturnsAsync(new List<Booking>());

        var result = await MakeBookingSvc(bookingRepo, new Mock<IRepository<int, Hotel>>(), new Mock<IRepository<int, Room>>())
            .GetByUserAsync(99, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task BookingService_GetByHotelAsync_Page2_ReturnsCorrectSlice()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });

        var bookings = Enumerable.Range(1, 15).Select(i => new Booking
        {
            BookingId = i, UserId = 1, HotelId = 1, RoomId = 1,
            CheckIn = DateTime.Today.AddDays(i), CheckOut = DateTime.Today.AddDays(i + 2),
            TotalAmount = 2000, Status = "Confirmed"
        }).ToList();

        bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                   .ReturnsAsync(bookings);

        var result = await MakeBookingSvc(bookingRepo, hotelRepo, new Mock<IRepository<int, Room>>())
            .GetByHotelAsync(1, new PagedRequestDto { PageNumber = 2, PageSize = 10 });

        Assert.Equal(5, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
    }

    // ════════════════════════════════════════════════════════════════════
    // CANCELLATION SERVICE — GetByUserAsync empty bookings
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancellationService_GetByUserAsync_NoBookings_ReturnsEmpty()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var audit       = new Mock<IAuditLogService>();

        bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                   .ReturnsAsync(new List<Booking>());
        cancelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Cancellation, bool>>>()))
                  .ReturnsAsync(new List<Cancellation>());

        var svc    = new CancellationService(cancelRepo.Object, bookingRepo.Object,
                                             audit.Object, new Mock<ILogger<CancellationService>>().Object);
        var result = await svc.GetByUserAsync(99, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Empty(result.Data);
    }

    // ════════════════════════════════════════════════════════════════════
    // HOTEL SERVICE — FilterPagedAsync inactive hotels excluded
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HotelService_FilterPagedAsync_ExcludesInactiveHotels()
    {
        var repo  = new Mock<IRepository<int, Hotel>>();
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

        var hotels = new List<Hotel>
        {
            new() { HotelId = 1, HotelName = "Active",   Location = "Mumbai", StarRating = 4, IsActive = true  },
            new() { HotelId = 2, HotelName = "Inactive", Location = "Mumbai", StarRating = 5, IsActive = false }
        };
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(hotels);

        var svc    = new HotelService(repo.Object, audit.Object, new Mock<ILogger<HotelService>>().Object);
        var result = await svc.FilterPagedAsync(
            new HotelFilterDto(),
            new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Single(result.Data);
        Assert.Equal("Active", result.Data[0].HotelName);
    }

    // ════════════════════════════════════════════════════════════════════
    // PAYMENT SERVICE — GetPagedByUserAsync no matching bookings
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PaymentService_GetPagedByUserAsync_NoBookings_ReturnsEmpty()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var audit       = new Mock<IAuditLogService>();

        paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
        bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

        var svc    = new PaymentService(paymentRepo.Object, bookingRepo.Object,
                                        audit.Object, new Mock<ILogger<PaymentService>>().Object);
        var result = await svc.GetPagedByUserAsync(99, new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalRecords);
    }

   

    // ════════════════════════════════════════════════════════════════════
    // NOTIFICATION SERVICE — CreateAsync empty message
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NotificationService_CreateAsync_EmptyMessage_ThrowsBadRequest()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();

        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" });

        var svc = new NotificationService(notifRepo.Object, userRepo.Object,
                                          new Mock<ILogger<NotificationService>>().Object);

        await Assert.ThrowsAsync<HotelBookingApp.Exceptions.BadRequestException>(() =>
            svc.CreateAsync(new CreateNotificationDto { UserId = 1, Message = "   " }));
    }

    // ════════════════════════════════════════════════════════════════════
    // ROOM SERVICE — GetAllAsync page 2, FilterAsync MinPrice only
    // ════════════════════════════════════════════════════════════════════

    private static RoomService MakeRoomSvc(Mock<IRepository<int, Room>> roomRepo)
    {
        var audit = new Mock<IAuditLogService>();
        return new RoomService(roomRepo.Object, new Mock<IRepository<int, Hotel>>().Object,
                               new Mock<IRepository<int, Booking>>().Object,
                               audit.Object, new Mock<ILogger<RoomService>>().Object);
    }

    [Fact]
    public async Task RoomService_FilterAsync_MinPriceOnly_ReturnsMatchingRooms()
    {
        var roomRepo = new Mock<IRepository<int, Room>>();
        var rooms = new List<Room>
        {
            new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 500,  Capacity = 2, IsAvailable = true },
            new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Deluxe",   PricePerNight = 3000, Capacity = 2, IsAvailable = true },
            new() { RoomId = 3, HotelId = 1, RoomNumber = 103, RoomType = "Suite",    PricePerNight = 8000, Capacity = 4, IsAvailable = true }
        };
        roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = (await MakeRoomSvc(roomRepo).FilterAsync(new RoomFilterDto { MinPrice = 2000 })).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.PricePerNight >= 2000));
    }

    [Fact]
    public async Task RoomService_FilterAsync_EmptyFilter_ReturnsAllRooms()
    {
        var roomRepo = new Mock<IRepository<int, Room>>();
        var rooms = new List<Room>
        {
            new() { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2, IsAvailable = true },
            new() { RoomId = 2, HotelId = 1, RoomNumber = 102, RoomType = "Deluxe",   PricePerNight = 3000, Capacity = 3, IsAvailable = true }
        };
        roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = (await MakeRoomSvc(roomRepo).FilterAsync(new RoomFilterDto())).ToList();

        Assert.Equal(2, result.Count);
    }

    // ════════════════════════════════════════════════════════════════════
    // AUDIT LOG SERVICE — FilterAsync empty filter returns all
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AuditLogService_FilterAsync_EmptyFilter_ReturnsAllLogs()
    {
        var repo   = new Mock<IRepository<int, AuditLog>>();
        var logger = new Mock<ILogger<AuditLogService>>();

        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, Action = "A", EntityName = "Hotel",   CreatedAt = DateTime.UtcNow },
            new() { AuditLogId = 2, Action = "B", EntityName = "Booking", CreatedAt = DateTime.UtcNow },
            new() { AuditLogId = 3, Action = "C", EntityName = "Room",    CreatedAt = DateTime.UtcNow }
        };
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

        var svc    = new AuditLogService(repo.Object, logger.Object);
        var result = await svc.FilterAsync(new AuditLogFilterDto());

        Assert.Equal(3, result.Count);
    }

    // ════════════════════════════════════════════════════════════════════
    // CHAT SERVICE — ReplyAsync support intent
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChatService_ReplyAsync_SupportMessage_ReturnsSupportIntent()
    {
        var chatRepo    = new Mock<IRepository<int, ChatMessage>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var logger      = new Mock<ILogger<ChatService>>();

        chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync((ChatMessage m) => m);

        var svc    = new ChatService(chatRepo.Object, hotelRepo.Object, bookingRepo.Object, logger.Object);
        var result = await svc.ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "I have a complaint about the service"
        });

        Assert.Equal("support", result.Intent);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_MyBookingsMessage_ReturnsBookingIntent()
    {
        var chatRepo    = new Mock<IRepository<int, ChatMessage>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var logger      = new Mock<ILogger<ChatService>>();

        chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync((ChatMessage m) => m);

        var svc    = new ChatService(chatRepo.Object, hotelRepo.Object, bookingRepo.Object, logger.Object);
        var result = await svc.ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "show my booking status"
        });

        Assert.Equal("booking", result.Intent);
    }
}
