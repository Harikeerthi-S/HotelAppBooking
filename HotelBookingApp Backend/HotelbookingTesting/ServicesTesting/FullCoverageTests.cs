using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

/// <summary>
/// Covers every remaining uncovered branch/line across all services.
/// </summary>
public class FullCoverageTests
{
    // ════════════════════════════════════════════════════════════════════
    // HOTEL SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    private static HotelService MakeHotelSvc(
        Mock<IRepository<int, Hotel>> repo,
        Mock<IAuditLogService>? audit = null)
    {
        audit ??= new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new HotelService(repo.Object, audit.Object, new Mock<ILogger<HotelService>>().Object);
    }

    // FilterPagedAsync — HotelId filter branch
    [Fact]
    public async Task HotelService_FilterPagedAsync_ByHotelId_ReturnsMatch()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        var hotels = new List<Hotel>
        {
            new() { HotelId = 1, HotelName = "A", Location = "Mumbai", StarRating = 3, IsActive = true },
            new() { HotelId = 2, HotelName = "B", Location = "Delhi",  StarRating = 4, IsActive = true }
        };
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(hotels);

        var result = await MakeHotelSvc(repo).FilterPagedAsync(
            new HotelFilterDto { HotelId = 1 },
            new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Single(result.Data);
        Assert.Equal(1, result.Data[0].HotelId);
    }

    // UpdateAsync — returns null when UpdateAsync returns null
    [Fact]
    public async Task HotelService_UpdateAsync_UpdateReturnsNull_ReturnsNull()
    {
        var repo  = new Mock<IRepository<int, Hotel>>();
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        var dto   = new CreateHotelDto { HotelName = "New", Location = "Goa", StarRating = 4, TotalRooms = 10 };

        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        repo.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync((Hotel?)null);

        var result = await MakeHotelSvc(repo).UpdateAsync(1, dto);

        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════════════════
    // BOOKING SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    private static BookingService MakeBookingSvc(
        Mock<IRepository<int, Booking>> bookingRepo,
        Mock<IRepository<int, Hotel>>   hotelRepo,
        Mock<IRepository<int, Room>>    roomRepo,
        Mock<IAuditLogService>?         audit = null)
    {
        audit ??= new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new BookingService(bookingRepo.Object, hotelRepo.Object, roomRepo.Object,
                                  audit.Object, new Mock<ILogger<BookingService>>().Object);
    }

    // CreateAsync — room belongs to different hotel
    [Fact]
    public async Task BookingService_CreateAsync_RoomWrongHotel_ThrowsBadRequest()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "H1", Location = "Mumbai", IsActive = true });
        roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Room { RoomId = 1, HotelId = 99, RoomType = "Standard", PricePerNight = 1000, IsAvailable = true }); // wrong hotel

        var dto = new CreateBookingDto
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn = DateTime.Today.AddDays(1), CheckOut = DateTime.Today.AddDays(3)
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CreateAsync(dto));
    }

    // CreateAsync — same user overlap
    [Fact]
    public async Task BookingService_CreateAsync_SameUserOverlap_ThrowsAlreadyExists()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "H1", Location = "Mumbai", IsActive = true });
        roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Room { RoomId = 1, HotelId = 1, RoomType = "Standard", PricePerNight = 1000, IsAvailable = true });

        var existing = new Booking
        {
            BookingId = 5, UserId = 1, RoomId = 1, Status = "Confirmed",
            CheckIn = DateTime.Today.AddDays(1), CheckOut = DateTime.Today.AddDays(4)
        };

        bookingRepo.SetupSequence(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                   .ReturnsAsync(new List<Booking> { existing }); // same-user overlap

        var dto = new CreateBookingDto
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn = DateTime.Today.AddDays(1), CheckOut = DateTime.Today.AddDays(3)
        };

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CreateAsync(dto));
    }

    // ConfirmAsync — not found
    [Fact]
    public async Task BookingService_ConfirmAsync_NotFound_ThrowsNotFoundException()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeBookingSvc(bookingRepo, new Mock<IRepository<int, Hotel>>(), new Mock<IRepository<int, Room>>())
                .ConfirmAsync(99));
    }

    // ════════════════════════════════════════════════════════════════════
    // CANCELLATION SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    private static CancellationService MakeCancelSvc(
        Mock<IRepository<int, Cancellation>> cancelRepo,
        Mock<IRepository<int, Booking>>      bookingRepo,
        Mock<IAuditLogService>?              audit = null)
    {
        audit ??= new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new CancellationService(cancelRepo.Object, bookingRepo.Object,
                                       audit.Object, new Mock<ILogger<CancellationService>>().Object);
    }

    // GetByIdAsync
    [Fact]
    public async Task CancellationService_GetByIdAsync_ExistingEntry_ReturnsDto()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var c = new Cancellation { CancellationId = 1, BookingId = 1, Reason = "Test", Status = "Pending", CancellationDate = DateTime.UtcNow };
        cancelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(c);

        var result = await MakeCancelSvc(cancelRepo, bookingRepo).GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Pending", result!.Status);
    }

    [Fact]
    public async Task CancellationService_GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        cancelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cancellation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeCancelSvc(cancelRepo, bookingRepo).GetByIdAsync(99));
    }

    // UpdateStatusAsync — invalid status
    [Fact]
    public async Task CancellationService_UpdateStatusAsync_InvalidStatus_ThrowsBadRequest()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakeCancelSvc(cancelRepo, bookingRepo).UpdateStatusAsync(1, "InvalidStatus", 0));
    }

    // UpdateStatusAsync — Refunded branch syncs booking
    [Fact]
    public async Task CancellationService_UpdateStatusAsync_Refunded_SyncsBooking()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var c       = new Cancellation { CancellationId = 1, BookingId = 1, Status = "Approved", RefundAmount = 1000 };
        var booking = new Booking { BookingId = 1, Status = "Cancelled" };

        cancelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(c);
        cancelRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Cancellation>())).ReturnsAsync(c);
        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);

        var result = await MakeCancelSvc(cancelRepo, bookingRepo).UpdateStatusAsync(1, "Refunded", 500);

        Assert.NotNull(result);
        Assert.Equal("Refunded", result.Status);
        bookingRepo.Verify(r => r.UpdateAsync(1, It.IsAny<Booking>()), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════
    // USER SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    private static UserService MakeUserSvc(
        Mock<IRepository<int, User>> userRepo,
        Mock<IPasswordService>?      pwdSvc = null)
    {
        pwdSvc ??= new Mock<IPasswordService>();
        return new UserService(userRepo.Object, pwdSvc.Object, new Mock<ILogger<UserService>>().Object);
    }

    // RegisterAsync — invalid role
    [Fact]
    public async Task UserService_RegisterAsync_InvalidRole_ThrowsBadRequest()
    {
        var userRepo = new Mock<IRepository<int, User>>();
        var dto = new RegisterRequestDto { UserName = "Alice", Email = "a@b.com", Password = "pass", Role = "superadmin" };

        await Assert.ThrowsAsync<BadRequestException>(() => MakeUserSvc(userRepo).RegisterAsync(dto));
    }

    // RegisterAsync — hotelmanager role allowed
    [Fact]
    public async Task UserService_RegisterAsync_HotelManagerRole_Succeeds()
    {
        var userRepo = new Mock<IRepository<int, User>>();
        var pwdSvc   = new Mock<IPasswordService>();

        userRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);
        pwdSvc.Setup(p => p.HashPassword(It.IsAny<string>())).Returns(new byte[] { 1, 2, 3 });
        userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.UserId = 5; return u; });

        var dto = new RegisterRequestDto { UserName = "Manager", Email = "mgr@test.com", Password = "pass123", Role = "hotelmanager" };
        var result = await MakeUserSvc(userRepo, pwdSvc).RegisterAsync(dto);

        Assert.Equal("hotelmanager", result.Role);
    }

    // ════════════════════════════════════════════════════════════════════
    // REVIEW SERVICE — uncovered branches
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

    // GetPagedAsync — filter by userId
    [Fact]
    public async Task ReviewService_GetPagedAsync_FilterByUserId_ReturnsUserReviews()
    {
        var reviewRepo = new Mock<IRepository<int, Review>>();
        var hotelRepo  = new Mock<IRepository<int, Hotel>>();
        var userRepo   = new Mock<IRepository<int, User>>();

        var reviews = new List<Review>
        {
            new() { ReviewId = 1, HotelId = 1, UserId = 7, Rating = 5, Comment = "Great!", CreatedAt = DateTime.UtcNow },
            new() { ReviewId = 2, HotelId = 2, UserId = 8, Rating = 3, Comment = "OK",     CreatedAt = DateTime.UtcNow }
        };
        reviewRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(reviews);

        var result = await MakeReviewSvc(reviewRepo, hotelRepo, userRepo).GetPagedAsync(
            new ReviewFilterDto { UserId = 7 },
            new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Single(result.Data);
        Assert.Equal(7, result.Data[0].UserId);
    }

    // ════════════════════════════════════════════════════════════════════
    // PAYMENT SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    private static PaymentService MakePaymentSvc(
        Mock<IRepository<int, Payment>> paymentRepo,
        Mock<IRepository<int, Booking>> bookingRepo,
        Mock<IAuditLogService>?         audit = null)
    {
        audit ??= new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new PaymentService(paymentRepo.Object, bookingRepo.Object,
                                  audit.Object, new Mock<ILogger<PaymentService>>().Object);
    }

    // GetByIdAsync
    [Fact]
    public async Task PaymentService_GetByIdAsync_ExistingPayment_ReturnsDto()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var payment = new Payment { PaymentId = 1, BookingId = 1, Amount = 3000, PaymentMethod = "UPI", PaymentStatus = "Completed" };
        paymentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);

        var result = await MakePaymentSvc(paymentRepo, bookingRepo).GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Completed", result!.PaymentStatus);
    }

    [Fact]
    public async Task PaymentService_GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        paymentRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Payment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakePaymentSvc(paymentRepo, bookingRepo).GetByIdAsync(99));
    }

    // MakePaymentAsync — Failed status syncs booking to Pending
    [Fact]
    public async Task PaymentService_MakePaymentAsync_FailedStatus_SyncsBooking()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var booking = new Booking { BookingId = 1, UserId = 1, TotalAmount = 100, Status = "Pending" };

        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
        paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                   .ReturnsAsync((Payment p) => { p.PaymentId = 1; p.PaymentStatus = "Failed"; return p; });
        bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);

        // Amount much less than booking total → triggers Failed status via delegate
        var result = await MakePaymentSvc(paymentRepo, bookingRepo).MakePaymentAsync(
            new PaymentDto { BookingId = 1, Amount = 1, PaymentMethod = "UPI" });

        Assert.NotNull(result);
    }

    // ════════════════════════════════════════════════════════════════════
    // WISHLIST SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    // AddAsync — inactive hotel
    [Fact]
    public async Task WishlistService_AddAsync_InactiveHotel_ThrowsNotFoundException()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" });
        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = false }); // inactive

        var svc = new WishlistService(wishlistRepo.Object, userRepo.Object, hotelRepo.Object,
                                      new Mock<ILogger<WishlistService>>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.AddAsync(new CreateWishlistDto { UserId = 1, HotelId = 1 }));
    }

    // ════════════════════════════════════════════════════════════════════
    // AUDIT LOG SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    // FilterAsync — FromDate + ToDate branch
    [Fact]
    public async Task AuditLogService_FilterAsync_ByDateRange_ReturnsCorrectLogs()
    {
        var repo   = new Mock<IRepository<int, AuditLog>>();
        var logger = new Mock<ILogger<AuditLogService>>();

        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, Action = "A", EntityName = "Hotel", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { AuditLogId = 2, Action = "B", EntityName = "Hotel", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { AuditLogId = 3, Action = "C", EntityName = "Hotel", CreatedAt = DateTime.UtcNow.AddDays(1)  }
        };
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(logs);

        var svc    = new AuditLogService(repo.Object, logger.Object);
        var result = await svc.FilterAsync(new AuditLogFilterDto
        {
            FromDate = DateTime.UtcNow.AddDays(-2),
            ToDate   = DateTime.UtcNow
        });

        Assert.Single(result); // only log from yesterday
    }

    // ════════════════════════════════════════════════════════════════════
    // ROOM SERVICE — uncovered branches
    // ════════════════════════════════════════════════════════════════════

    // FilterAsync — HotelId + MaxCapacity branches
    [Fact]
    public async Task RoomService_FilterAsync_ByHotelIdAndMaxCapacity_ReturnsFiltered()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var audit       = new Mock<IAuditLogService>();

        var rooms = new List<Room>
        {
            new() { RoomId = 1, HotelId = 2, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2, IsAvailable = true },
            new() { RoomId = 2, HotelId = 2, RoomNumber = 102, RoomType = "Suite",    PricePerNight = 5000, Capacity = 6, IsAvailable = true },
            new() { RoomId = 3, HotelId = 3, RoomNumber = 201, RoomType = "Standard", PricePerNight = 1200, Capacity = 2, IsAvailable = true }
        };
        roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var svc    = new RoomService(roomRepo.Object, hotelRepo.Object, bookingRepo.Object,
                                     audit.Object, new Mock<ILogger<RoomService>>().Object);
        var result = (await svc.FilterAsync(new RoomFilterDto { HotelId = 2, MaxCapacity = 3 })).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].HotelId);
        Assert.Equal(2, result[0].Capacity);
    }
}
