using HotelBookingApp.Delegates;
using HotelBookingApp.Exceptions;
using HotelBookingApp.Helpers;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

/// <summary>Covers every remaining uncovered line/branch across all services.</summary>
public class FullBranchCoverageTests
{
    // ════════════════════════════════════════════════════════════════════
    // APP DELEGATES — all branches
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void StrictDateRangeValidator_PastCheckIn_ReturnsError()
    {
        var result = AppDelegateFactory.StrictDateRangeValidator(
            DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(2));
        Assert.NotNull(result);
        Assert.Contains("past", result);
    }

    [Fact]
    public void StrictDateRangeValidator_CheckOutBeforeCheckIn_ReturnsError()
    {
        var result = AppDelegateFactory.StrictDateRangeValidator(
            DateTime.UtcNow.Date.AddDays(3), DateTime.UtcNow.Date.AddDays(1));
        Assert.NotNull(result);
        Assert.Contains("after", result);
    }

    [Fact]
    public void StrictDateRangeValidator_ValidDates_ReturnsNull()
    {
        var result = AppDelegateFactory.StrictDateRangeValidator(
            DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(3));
        Assert.Null(result);
    }

    [Fact]
    public void StandardRefundPolicy_Over24Hours_Returns80Percent()
    {
        var refund = AppDelegateFactory.StandardRefundPolicy(1000m, 48);
        Assert.Equal(800m, refund);
    }

    [Fact]
    public void StandardRefundPolicy_Under24Hours_ReturnsZero()
    {
        var refund = AppDelegateFactory.StandardRefundPolicy(1000m, 10);
        Assert.Equal(0m, refund);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_CreditCard_FullAmount_ReturnsCompleted()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("CreditCard", 1000m, 1000m);
        Assert.Equal("Completed", status);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_DebitCard_FullAmount_ReturnsCompleted()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("DebitCard", 1000m, 1000m);
        Assert.Equal("Completed", status);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_UPI_FullAmount_ReturnsPending()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("UPI", 1000m, 1000m);
        Assert.Equal("Pending", status);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_Wallet_FullAmount_ReturnsPending()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("Wallet", 1000m, 1000m);
        Assert.Equal("Pending", status);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_PayPal_FullAmount_ReturnsPending()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("PayPal", 1000m, 1000m);
        Assert.Equal("Pending", status);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_NetBanking_FullAmount_ReturnsPending()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("NetBanking", 1000m, 1000m);
        Assert.Equal("Pending", status);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_UnknownMethod_ReturnsFailed()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("Bitcoin", 1000m, 1000m);
        Assert.Equal("Failed", status);
    }

    [Fact]
    public void DefaultPaymentStatusResolver_AmountLessThanBooking_ReturnsFailed()
    {
        var status = AppDelegateFactory.DefaultPaymentStatusResolver("CreditCard", 500m, 1000m);
        Assert.Equal("Failed", status);
    }

    [Fact]
    public void StandardLogFormatter_ProducesFormattedString()
    {
        var result = AppDelegateFactory.StandardLogFormatter("INFO", "TestContext", "Test message");
        Assert.Contains("INFO", result);
        Assert.Contains("TestContext", result);
        Assert.Contains("Test message", result);
    }

    // ════════════════════════════════════════════════════════════════════
    // PASSWORD HELPER — all branches
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void PasswordHelper_HashPassword_ValidPassword_ReturnsHash()
    {
        var helper = new PasswordHelper();
        var hash = helper.HashPassword("mypassword");
        Assert.NotNull(hash);
        Assert.Equal(32, hash.Length); // SHA-256 = 32 bytes
    }

    [Fact]
    public void PasswordHelper_HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        var helper = new PasswordHelper();
        Assert.Throws<ArgumentException>(() => helper.HashPassword("   "));
    }

    [Fact]
    public void PasswordHelper_VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var helper = new PasswordHelper();
        var hash = helper.HashPassword("secret123");
        Assert.True(helper.VerifyPassword("secret123", hash));
    }

    [Fact]
    public void PasswordHelper_VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var helper = new PasswordHelper();
        var hash = helper.HashPassword("secret123");
        Assert.False(helper.VerifyPassword("wrongpass", hash));
    }

    [Fact]
    public void PasswordHelper_VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        var helper = new PasswordHelper();
        var hash = helper.HashPassword("secret123");
        Assert.False(helper.VerifyPassword("   ", hash));
    }

    [Fact]
    public void PasswordHelper_VerifyPassword_NullHash_ReturnsFalse()
    {
        var helper = new PasswordHelper();
        Assert.False(helper.VerifyPassword("password", null!));
    }

    [Fact]
    public void PasswordHelper_VerifyPassword_EmptyHash_ReturnsFalse()
    {
        var helper = new PasswordHelper();
        Assert.False(helper.VerifyPassword("password", Array.Empty<byte>()));
    }

    // ════════════════════════════════════════════════════════════════════
    // AUTH SERVICE — all branches
    // ════════════════════════════════════════════════════════════════════

    private static AuthService MakeAuthSvc(
        Mock<IRepository<int, User>> userRepo,
        Mock<IPasswordService> pwdSvc) =>
        new(userRepo.Object, pwdSvc.Object, new Mock<ILogger<AuthService>>().Object);

    [Fact]
    public async Task AuthService_LoginAsync_ValidCredentials_ReturnsDto()
    {
        var userRepo = new Mock<IRepository<int, User>>();
        var pwdSvc   = new Mock<IPasswordService>();
        var hash     = new byte[] { 1, 2, 3 };
        var user     = new User { UserId = 1, UserName = "Alice", Email = "alice@test.com", Role = "user", PasswordHash = hash };

        userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);
        pwdSvc.Setup(p => p.VerifyPassword("pass123", hash)).Returns(true);

        var result = await MakeAuthSvc(userRepo, pwdSvc).LoginAsync(
            new LoginRequestDto { Email = "alice@test.com", Password = "pass123" });

        Assert.Equal("Alice", result.UserName);
        Assert.Equal("user", result.Role);
    }

    [Fact]
    public async Task AuthService_LoginAsync_UserNotFound_ThrowsUnauthorized()
    {
        var userRepo = new Mock<IRepository<int, User>>();
        var pwdSvc   = new Mock<IPasswordService>();

        userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            MakeAuthSvc(userRepo, pwdSvc).LoginAsync(
                new LoginRequestDto { Email = "nobody@test.com", Password = "pass" }));
    }

    [Fact]
    public async Task AuthService_LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var userRepo = new Mock<IRepository<int, User>>();
        var pwdSvc   = new Mock<IPasswordService>();
        var hash     = new byte[] { 1, 2, 3 };
        var user     = new User { UserId = 1, UserName = "Alice", Email = "alice@test.com", Role = "user", PasswordHash = hash };

        userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);
        pwdSvc.Setup(p => p.VerifyPassword("wrongpass", hash)).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            MakeAuthSvc(userRepo, pwdSvc).LoginAsync(
                new LoginRequestDto { Email = "alice@test.com", Password = "wrongpass" }));
    }

    // ════════════════════════════════════════════════════════════════════
    // AMENITY SERVICE — CreateAsync duplicate, DeleteAsync, GetAllAsync
    // ════════════════════════════════════════════════════════════════════

    private static AmenityService MakeAmenitySvc(Mock<IRepository<int, Amenity>> repo) =>
        new(repo.Object, new Mock<ILogger<AmenityService>>().Object);

    [Fact]
    public async Task AmenityService_CreateAsync_DuplicateName_ThrowsAlreadyExists()
    {
        var repo = new Mock<IRepository<int, Amenity>>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            MakeAmenitySvc(repo).CreateAsync(new CreateAmenityDto { Name = "Pool" }));
    }

    [Fact]
    public async Task AmenityService_CreateAsync_Valid_ReturnsDto()
    {
        var repo = new Mock<IRepository<int, Amenity>>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<Amenity>()))
            .ReturnsAsync((Amenity a) => { a.AmenityId = 1; return a; });

        var result = await MakeAmenitySvc(repo).CreateAsync(
            new CreateAmenityDto { Name = "Pool", Description = "Outdoor pool", Icon = "🏊" });

        Assert.Equal("Pool", result.Name);
        Assert.Equal("🏊", result.Icon);
    }

    [Fact]
    public async Task AmenityService_DeleteAsync_Valid_ReturnsTrue()
    {
        var repo    = new Mock<IRepository<int, Amenity>>();
        var amenity = new Amenity { AmenityId = 1, Name = "Pool" };
        repo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(amenity);

        var result = await MakeAmenitySvc(repo).DeleteAsync(1);
        Assert.True(result);
    }

    [Fact]
    public async Task AmenityService_DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IRepository<int, Amenity>>();
        repo.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Amenity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => MakeAmenitySvc(repo).DeleteAsync(99));
    }

    [Fact]
    public async Task AmenityService_GetAllAsync_ReturnsOrderedByName()
    {
        var repo = new Mock<IRepository<int, Amenity>>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Amenity>
        {
            new() { AmenityId = 1, Name = "WiFi" },
            new() { AmenityId = 2, Name = "Gym"  },
            new() { AmenityId = 3, Name = "Pool" }
        });

        var result = (await MakeAmenitySvc(repo).GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Gym",  result[0].Name);
        Assert.Equal("Pool", result[1].Name);
        Assert.Equal("WiFi", result[2].Name);
    }

    // ════════════════════════════════════════════════════════════════════
    // HOTEL SERVICE — CreateAsync duplicate, GetByIdAsync inactive,
    //                 SearchAsync empty, DeactivateAsync already inactive
    // ════════════════════════════════════════════════════════════════════

    private static HotelService MakeHotelSvc(Mock<IRepository<int, Hotel>> repo)
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new HotelService(repo.Object, audit.Object, new Mock<ILogger<HotelService>>().Object);
    }

    [Fact]
    public async Task HotelService_CreateAsync_Duplicate_ThrowsAlreadyExists()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            MakeHotelSvc(repo).CreateAsync(
                new CreateHotelDto { HotelName = "Test", Location = "Mumbai", StarRating = 3, TotalRooms = 10 }));
    }

    [Fact]
    public async Task HotelService_CreateAsync_Valid_ReturnsDto()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<Hotel>()))
            .ReturnsAsync((Hotel h) => { h.HotelId = 1; return h; });

        var result = await MakeHotelSvc(repo).CreateAsync(
            new CreateHotelDto { HotelName = "Grand", Location = "Goa", StarRating = 5, TotalRooms = 50 });

        Assert.Equal("Grand", result.HotelName);
    }

    [Fact]
    public async Task HotelService_GetByIdAsync_InactiveHotel_ThrowsNotFoundException()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = false });

        await Assert.ThrowsAsync<NotFoundException>(() => MakeHotelSvc(repo).GetByIdAsync(1));
    }

    [Fact]
    public async Task HotelService_GetByIdAsync_NullHotel_ThrowsNotFoundException()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => MakeHotelSvc(repo).GetByIdAsync(99));
    }

    [Fact]
    public async Task HotelService_SearchAsync_EmptyTerm_ThrowsBadRequest()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        await Assert.ThrowsAsync<BadRequestException>(() => MakeHotelSvc(repo).SearchAsync("   "));
    }

    [Fact]
    public async Task HotelService_SearchAsync_ValidTerm_ReturnsResults()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        repo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
            .ReturnsAsync(new List<Hotel>
            {
                new() { HotelId = 1, HotelName = "Goa Beach", Location = "Goa", IsActive = true }
            });

        var result = (await MakeHotelSvc(repo).SearchAsync("Goa")).ToList();
        Assert.Single(result);
    }

    [Fact]
    public async Task HotelService_DeactivateAsync_NullHotel_ThrowsNotFoundException()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => MakeHotelSvc(repo).DeactivateAsync(99));
    }

    [Fact]
    public async Task HotelService_DeactivateAsync_AlreadyInactive_ThrowsBadRequest()
    {
        var repo = new Mock<IRepository<int, Hotel>>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = false });

        await Assert.ThrowsAsync<BadRequestException>(() => MakeHotelSvc(repo).DeactivateAsync(1));
    }

    [Fact]
    public async Task HotelService_DeactivateAsync_Active_ReturnsTrue()
    {
        var repo  = new Mock<IRepository<int, Hotel>>();
        var hotel = new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true };
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        repo.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync(hotel);

        var result = await MakeHotelSvc(repo).DeactivateAsync(1);
        Assert.True(result);
    }

    // ════════════════════════════════════════════════════════════════════
    // HOTEL AMENITY SERVICE — CreateAsync duplicate, DeleteAsync,
    //                         GetByHotelAsync, null Amenity nav prop
    // ════════════════════════════════════════════════════════════════════

    private static HotelAmenityService MakeHotelAmenitySvc(
        Mock<IRepository<int, HotelAmenity>> haRepo,
        Mock<IRepository<int, Hotel>>        hotelRepo,
        Mock<IRepository<int, Amenity>>      amenityRepo) =>
        new(haRepo.Object, hotelRepo.Object, amenityRepo.Object,
            new Mock<ILogger<HotelAmenityService>>().Object);

    [Fact]
    public async Task HotelAmenityService_CreateAsync_DuplicateAssignment_ThrowsAlreadyExists()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Amenity { AmenityId = 1, Name = "Pool" });
        haRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, bool>>>()))
              .ReturnsAsync(true); // already assigned

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo)
                .CreateAsync(new CreateHotelAmenityDto { HotelId = 1, AmenityId = 1 }));
    }

    [Fact]
    public async Task HotelAmenityService_CreateAsync_NullAmenity_ThrowsNotFoundException()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        amenityRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo)
                .CreateAsync(new CreateHotelAmenityDto { HotelId = 1, AmenityId = 99 }));
    }

    [Fact]
    public async Task HotelAmenityService_CreateAsync_Valid_ReturnsDto()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Amenity { AmenityId = 1, Name = "Pool", Icon = "🏊" });
        haRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, bool>>>()))
              .ReturnsAsync(false);
        haRepo.Setup(r => r.AddAsync(It.IsAny<HotelAmenity>()))
              .ReturnsAsync((HotelAmenity ha) => { ha.HotelAmenityId = 1; return ha; });

        var result = await MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo)
            .CreateAsync(new CreateHotelAmenityDto { HotelId = 1, AmenityId = 1 });

        Assert.Equal(1, result.HotelId);
        Assert.Equal(1, result.AmenityId);
    }

    [Fact]
    public async Task HotelAmenityService_DeleteAsync_Valid_ReturnsTrue()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();
        var ha          = new HotelAmenity { HotelAmenityId = 1, HotelId = 1, AmenityId = 1 };
        haRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(ha);

        var result = await MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo).DeleteAsync(1);
        Assert.True(result);
    }

    [Fact]
    public async Task HotelAmenityService_DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();
        haRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync((HotelAmenity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo).DeleteAsync(99));
    }

    [Fact]
    public async Task HotelAmenityService_GetByHotelAsync_ReturnsFilteredByHotel()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();

        var items = new List<HotelAmenity>
        {
            new() { HotelAmenityId = 1, HotelId = 1, AmenityId = 1, Amenity = new Amenity { Name = "Pool" } },
            new() { HotelAmenityId = 2, HotelId = 2, AmenityId = 1, Amenity = new Amenity { Name = "Pool" } }
        };
        haRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, object>>[]>()))
              .ReturnsAsync(items);

        var result = (await MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo).GetByHotelAsync(1)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].HotelId);
    }

    [Fact]
    public async Task HotelAmenityService_MapToDto_NullAmenityNavProp_ReturnsEmptyStrings()
    {
        var haRepo      = new Mock<IRepository<int, HotelAmenity>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var amenityRepo = new Mock<IRepository<int, Amenity>>();

        // Amenity nav prop is null — tests the ?? fallback in MapToDto
        var items = new List<HotelAmenity>
        {
            new() { HotelAmenityId = 1, HotelId = 1, AmenityId = 1, Amenity = null }
        };
        haRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<HotelAmenity, object>>[]>()))
              .ReturnsAsync(items);

        var result = (await MakeHotelAmenitySvc(haRepo, hotelRepo, amenityRepo).GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].AmenityName);
        Assert.Null(result[0].AmenityIcon);
        Assert.Null(result[0].AmenityDescription);
    }

    // ════════════════════════════════════════════════════════════════════
    // BOOKING SERVICE — CreateAsync invalid dates, unavailable room,
    //                   other-user overlap, GetByIdAsync null hotel,
    //                   CancelAsync completed, ConfirmAsync invalid status
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
    public async Task BookingService_CreateAsync_PastCheckIn_ThrowsBadRequest()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        var dto = new CreateBookingDto
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn  = DateTime.UtcNow.Date.AddDays(-1),
            CheckOut = DateTime.UtcNow.Date.AddDays(2)
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CreateAsync(dto));
    }

    [Fact]
    public async Task BookingService_CreateAsync_NullHotel_ThrowsNotFoundException()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Hotel?)null);

        var dto = new CreateBookingDto
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn  = DateTime.UtcNow.Date.AddDays(1),
            CheckOut = DateTime.UtcNow.Date.AddDays(3)
        };

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CreateAsync(dto));
    }

    [Fact]
    public async Task BookingService_CreateAsync_UnavailableRoom_ThrowsBadRequest()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "H1", Location = "Mumbai", IsActive = true });
        roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Room { RoomId = 1, HotelId = 1, RoomType = "Standard", PricePerNight = 1000, IsAvailable = false });

        var dto = new CreateBookingDto
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn  = DateTime.UtcNow.Date.AddDays(1),
            CheckOut = DateTime.UtcNow.Date.AddDays(3)
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CreateAsync(dto));
    }

    [Fact]
    public async Task BookingService_CreateAsync_OtherUserOverlap_ThrowsBadRequest()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "H1", Location = "Mumbai", IsActive = true });
        roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Room { RoomId = 1, HotelId = 1, RoomType = "Standard", PricePerNight = 1000, IsAvailable = true });

        var otherBooking = new Booking
        {
            BookingId = 10, UserId = 99, RoomId = 1, Status = "Confirmed",
            CheckIn  = DateTime.Today.AddDays(1),
            CheckOut = DateTime.Today.AddDays(4)
        };

        // First call (same-user check) returns empty, second call (other-user check) returns overlap
        bookingRepo.SetupSequence(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                   .ReturnsAsync(new List<Booking>())
                   .ReturnsAsync(new List<Booking> { otherBooking });

        var dto = new CreateBookingDto
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn  = DateTime.Today.AddDays(1),
            CheckOut = DateTime.Today.AddDays(3)
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CreateAsync(dto));
    }

    [Fact]
    public async Task BookingService_CreateAsync_Valid_ReturnsDto()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "H1", Location = "Mumbai", IsActive = true });
        roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Room { RoomId = 1, HotelId = 1, RoomType = "Standard", PricePerNight = 1000, IsAvailable = true });
        bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                   .ReturnsAsync(new List<Booking>());
        bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>()))
                   .ReturnsAsync((Booking b) => { b.BookingId = 1; return b; });

        var dto = new CreateBookingDto
        {
            UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
            CheckIn  = DateTime.Today.AddDays(1),
            CheckOut = DateTime.Today.AddDays(3)
        };

        var result = await MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CreateAsync(dto);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task BookingService_GetByIdAsync_NullHotel_ReturnsEmptyHotelName()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 99, RoomId = 1,
            CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 2000, Status = "Confirmed" };
        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        var result = await MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.HotelName);
    }

    [Fact]
    public async Task BookingService_CancelAsync_CompletedBooking_ThrowsBadRequest()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Booking { BookingId = 1, Status = "Completed" });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).CancelAsync(1));
    }

    [Fact]
    public async Task BookingService_ConfirmAsync_InvalidStatus_ThrowsBadRequest()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Booking { BookingId = 1, Status = "Completed" }); // not Pending

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).ConfirmAsync(1));
    }

    [Fact]
    public async Task BookingService_GetPendingByHotelAsync_NotFound_ThrowsNotFoundException()
    {
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var roomRepo    = new Mock<IRepository<int, Room>>();

        hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeBookingSvc(bookingRepo, hotelRepo, roomRepo).GetPendingByHotelAsync(99));
    }

    // ════════════════════════════════════════════════════════════════════
    // PAYMENT SERVICE — MakePaymentAsync duplicate, invalid method,
    //                   UpdateStatusAsync all status branches,
    //                   GetByBookingIdAsync not found
    // ════════════════════════════════════════════════════════════════════

    private static PaymentService MakePaymentSvc(
        Mock<IRepository<int, Payment>> paymentRepo,
        Mock<IRepository<int, Booking>> bookingRepo)
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new PaymentService(paymentRepo.Object, bookingRepo.Object,
                                  audit.Object, new Mock<ILogger<PaymentService>>().Object);
    }

    [Fact]
    public async Task PaymentService_MakePaymentAsync_DuplicateCompleted_ThrowsBadRequest()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Booking { BookingId = 1, TotalAmount = 1000, Status = "Confirmed" });
        paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>
        {
            new() { PaymentId = 1, BookingId = 1, PaymentStatus = "Completed" }
        });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakePaymentSvc(paymentRepo, bookingRepo).MakePaymentAsync(
                new PaymentDto { BookingId = 1, Amount = 1000, PaymentMethod = "UPI" }));
    }

    [Fact]
    public async Task PaymentService_MakePaymentAsync_InvalidMethod_ThrowsBadRequest()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Booking { BookingId = 1, TotalAmount = 1000, Status = "Pending" });
        paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            MakePaymentSvc(paymentRepo, bookingRepo).MakePaymentAsync(
                new PaymentDto { BookingId = 1, Amount = 1000, PaymentMethod = "Bitcoin" }));
    }

    [Fact]
    public async Task PaymentService_MakePaymentAsync_BookingNotFound_ThrowsNotFoundException()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakePaymentSvc(paymentRepo, bookingRepo).MakePaymentAsync(
                new PaymentDto { BookingId = 99, Amount = 1000, PaymentMethod = "UPI" }));
    }

    [Fact]
    public async Task PaymentService_GetByBookingIdAsync_NotFound_ThrowsNotFoundException()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakePaymentSvc(paymentRepo, bookingRepo).GetByBookingIdAsync(99));
    }

    [Fact]
    public async Task PaymentService_GetByBookingIdAsync_Found_ReturnsDto()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        paymentRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>
        {
            new() { PaymentId = 1, BookingId = 5, Amount = 2000, PaymentMethod = "UPI", PaymentStatus = "Completed", CreatedAt = DateTime.UtcNow }
        });

        var result = await MakePaymentSvc(paymentRepo, bookingRepo).GetByBookingIdAsync(5);
        Assert.NotNull(result);
        Assert.Equal(5, result!.BookingId);
    }

    [Fact]
    public async Task PaymentService_UpdateStatusAsync_Completed_SyncsBookingToConfirmed()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        var payment = new Payment { PaymentId = 1, BookingId = 1, Amount = 1000, PaymentMethod = "UPI", PaymentStatus = "Pending" };
        var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Pending",
            CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 1000 };

        paymentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        paymentRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);

        var result = await MakePaymentSvc(paymentRepo, bookingRepo).UpdateStatusAsync(1, "Completed");

        Assert.NotNull(result);
        Assert.Equal("Completed", result!.PaymentStatus);
        bookingRepo.Verify(r => r.UpdateAsync(1, It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task PaymentService_UpdateStatusAsync_Refunded_SyncsBookingToCancelled()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        var payment = new Payment { PaymentId = 1, BookingId = 1, Amount = 1000, PaymentMethod = "UPI", PaymentStatus = "Completed" };
        var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Confirmed",
            CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 1000 };

        paymentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        paymentRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);

        var result = await MakePaymentSvc(paymentRepo, bookingRepo).UpdateStatusAsync(1, "Refunded");

        Assert.NotNull(result);
        bookingRepo.Verify(r => r.UpdateAsync(1, It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task PaymentService_UpdateStatusAsync_Failed_SyncsBookingToPending()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        var payment = new Payment { PaymentId = 1, BookingId = 1, Amount = 1000, PaymentMethod = "UPI", PaymentStatus = "Pending" };
        var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, Status = "Confirmed",
            CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(2), TotalAmount = 1000 };

        paymentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        paymentRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
        bookingRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Booking>())).ReturnsAsync(booking);

        var result = await MakePaymentSvc(paymentRepo, bookingRepo).UpdateStatusAsync(1, "Failed");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task PaymentService_UpdateStatusAsync_NullBooking_DoesNotThrow()
    {
        var paymentRepo = new Mock<IRepository<int, Payment>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        var payment = new Payment { PaymentId = 1, BookingId = 99, Amount = 1000, PaymentMethod = "UPI", PaymentStatus = "Pending" };

        paymentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
        paymentRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
        bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null); // null booking

        var result = await MakePaymentSvc(paymentRepo, bookingRepo).UpdateStatusAsync(1, "Completed");

        Assert.NotNull(result); // should not throw
    }

    // ════════════════════════════════════════════════════════════════════
    // CANCELLATION SERVICE — CreateAsync booking not found,
    //                        UpdateStatusAsync refundAmount > 0,
    //                        UpdateStatusAsync null booking
    // ════════════════════════════════════════════════════════════════════

    private static CancellationService MakeCancelSvc(
        Mock<IRepository<int, Cancellation>> cancelRepo,
        Mock<IRepository<int, Booking>>      bookingRepo)
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new CancellationService(cancelRepo.Object, bookingRepo.Object,
                                       audit.Object, new Mock<ILogger<CancellationService>>().Object);
    }

    [Fact]
    public async Task CancellationService_CreateAsync_BookingNotFound_ThrowsNotFoundException()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeCancelSvc(cancelRepo, bookingRepo)
                .CreateAsync(new CreateCancellationDto { BookingId = 99, Reason = "Test" }));
    }

    [Fact]
    public async Task CancellationService_UpdateStatusAsync_WithRefundAmount_UpdatesRefund()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var c = new Cancellation { CancellationId = 1, BookingId = 1, Status = "Pending", RefundAmount = 500 };

        cancelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(c);
        cancelRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Cancellation>())).ReturnsAsync(c);
        bookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Booking?)null); // null booking — Approved path

        var result = await MakeCancelSvc(cancelRepo, bookingRepo).UpdateStatusAsync(1, "Approved", 800);

        Assert.Equal("Approved", result.Status);
        Assert.Equal(800, result.RefundAmount);
    }

    [Fact]
    public async Task CancellationService_UpdateStatusAsync_Refunded_NullBooking_DoesNotThrow()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();
        var c = new Cancellation { CancellationId = 1, BookingId = 99, Status = "Approved", RefundAmount = 500 };

        cancelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(c);
        cancelRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Cancellation>())).ReturnsAsync(c);
        bookingRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Booking?)null);

        var result = await MakeCancelSvc(cancelRepo, bookingRepo).UpdateStatusAsync(1, "Refunded");

        Assert.Equal("Refunded", result.Status); // should not throw even with null booking
    }

    [Fact]
    public async Task CancellationService_GetAllAsync_Page2_ReturnsCorrectSlice()
    {
        var cancelRepo  = new Mock<IRepository<int, Cancellation>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        var cancellations = Enumerable.Range(1, 15).Select(i => new Cancellation
        {
            CancellationId = i, BookingId = i, Reason = "Test",
            Status = "Pending", CancellationDate = DateTime.UtcNow
        }).ToList();
        cancelRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(cancellations);

        var result = await MakeCancelSvc(cancelRepo, bookingRepo)
            .GetAllAsync(new PagedRequestDto { PageNumber = 2, PageSize = 10 });

        Assert.Equal(5, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
    }

    // ════════════════════════════════════════════════════════════════════
    // REVIEW SERVICE — CreateAsync null hotel, user not found
    // ════════════════════════════════════════════════════════════════════

    private static ReviewService MakeReviewSvc(
        Mock<IRepository<int, Review>> reviewRepo,
        Mock<IRepository<int, Hotel>>  hotelRepo,
        Mock<IRepository<int, User>>   userRepo)
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new ReviewService(reviewRepo.Object, hotelRepo.Object, userRepo.Object,
                                 audit.Object, new Mock<ILogger<ReviewService>>().Object);
    }

    [Fact]
    public async Task ReviewService_CreateAsync_UserNotFound_ThrowsNotFoundException()
    {
        var reviewRepo = new Mock<IRepository<int, Review>>();
        var hotelRepo  = new Mock<IRepository<int, Hotel>>();
        var userRepo   = new Mock<IRepository<int, User>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeReviewSvc(reviewRepo, hotelRepo, userRepo)
                .CreateAsync(new CreateReviewDto { UserId = 99, HotelId = 1, Rating = 4, Comment = "Good" }));
    }

    [Fact]
    public async Task ReviewService_CreateAsync_Valid_ReturnsDto()
    {
        var reviewRepo = new Mock<IRepository<int, Review>>();
        var hotelRepo  = new Mock<IRepository<int, Hotel>>();
        var userRepo   = new Mock<IRepository<int, User>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" });
        reviewRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Review, bool>>>()))
                  .ReturnsAsync(false);
        reviewRepo.Setup(r => r.AddAsync(It.IsAny<Review>()))
                  .ReturnsAsync((Review r) => { r.ReviewId = 1; return r; });

        var result = await MakeReviewSvc(reviewRepo, hotelRepo, userRepo)
            .CreateAsync(new CreateReviewDto { UserId = 1, HotelId = 1, Rating = 5, Comment = "Excellent!" });

        Assert.Equal(5, result.Rating);
    }

    // ════════════════════════════════════════════════════════════════════
    // NOTIFICATION SERVICE — CreateAsync user not found,
    //                        MarkAsReadAsync not found, DeleteAsync valid
    // ════════════════════════════════════════════════════════════════════

    private static NotificationService MakeNotifSvc(
        Mock<IRepository<int, Notification>> notifRepo,
        Mock<IRepository<int, User>>         userRepo) =>
        new(notifRepo.Object, userRepo.Object, new Mock<ILogger<NotificationService>>().Object);

    [Fact]
    public async Task NotificationService_CreateAsync_UserNotFound_ThrowsNotFoundException()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();
        userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeNotifSvc(notifRepo, userRepo)
                .CreateAsync(new CreateNotificationDto { UserId = 99, Message = "Hello" }));
    }

    [Fact]
    public async Task NotificationService_CreateAsync_Valid_ReturnsDto()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();

        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" });
        notifRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                 .ReturnsAsync((Notification n) => { n.NotificationId = 1; return n; });

        var result = await MakeNotifSvc(notifRepo, userRepo)
            .CreateAsync(new CreateNotificationDto { UserId = 1, Message = "Booking confirmed!" });

        Assert.Equal("Booking confirmed!", result.Message);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task NotificationService_MarkAsReadAsync_NotFound_ThrowsNotFoundException()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();
        notifRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeNotifSvc(notifRepo, userRepo).MarkAsReadAsync(99));
    }

    [Fact]
    public async Task NotificationService_MarkAsReadAsync_Unread_MarksAndReturnsTrue()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();
        var notif = new Notification { NotificationId = 1, UserId = 1, Message = "Test", IsRead = false };

        notifRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notif);
        notifRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Notification>())).ReturnsAsync(notif);

        var result = await MakeNotifSvc(notifRepo, userRepo).MarkAsReadAsync(1);

        Assert.True(result);
        notifRepo.Verify(r => r.UpdateAsync(1, It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task NotificationService_DeleteAsync_Valid_ReturnsTrue()
    {
        var notifRepo = new Mock<IRepository<int, Notification>>();
        var userRepo  = new Mock<IRepository<int, User>>();
        var notif = new Notification { NotificationId = 1, UserId = 1, Message = "Test" };

        notifRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(notif);

        var result = await MakeNotifSvc(notifRepo, userRepo).DeleteAsync(1);
        Assert.True(result);
    }

    // ════════════════════════════════════════════════════════════════════
    // ROOM SERVICE — CreateAsync null hotel, duplicate room,
    //                UpdateAsync returns null, DeactivateAsync valid
    // ════════════════════════════════════════════════════════════════════

    private static RoomService MakeRoomSvc(
        Mock<IRepository<int, Room>>    roomRepo,
        Mock<IRepository<int, Hotel>>   hotelRepo,
        Mock<IRepository<int, Booking>> bookingRepo)
    {
        var audit = new Mock<IAuditLogService>();
        audit.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
        return new RoomService(roomRepo.Object, hotelRepo.Object, bookingRepo.Object,
                               audit.Object, new Mock<ILogger<RoomService>>().Object);
    }

    [Fact]
    public async Task RoomService_CreateAsync_NullHotel_ThrowsNotFoundException()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeRoomSvc(roomRepo, hotelRepo, bookingRepo)
                .CreateAsync(new CreateRoomDto { HotelId = 99, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 }));
    }

    [Fact]
    public async Task RoomService_CreateAsync_InactiveHotel_ThrowsNotFoundException()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = false });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeRoomSvc(roomRepo, hotelRepo, bookingRepo)
                .CreateAsync(new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 }));
    }

    [Fact]
    public async Task RoomService_CreateAsync_DuplicateRoomNumber_ThrowsAlreadyExists()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        roomRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                .ReturnsAsync(true);

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            MakeRoomSvc(roomRepo, hotelRepo, bookingRepo)
                .CreateAsync(new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 }));
    }

    [Fact]
    public async Task RoomService_CreateAsync_Valid_ReturnsDto()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        roomRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                .ReturnsAsync(false);
        roomRepo.Setup(r => r.AddAsync(It.IsAny<Room>()))
                .ReturnsAsync((Room r) => { r.RoomId = 1; return r; });

        var result = await MakeRoomSvc(roomRepo, hotelRepo, bookingRepo)
            .CreateAsync(new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, Capacity = 2 });

        Assert.Equal("Standard", result.RoomType);
        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task RoomService_UpdateAsync_ReturnsNull_WhenRepoReturnsNull()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        var room = new Room { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, IsAvailable = true };
        roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        roomRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                .ReturnsAsync(false);
        roomRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Room>())).ReturnsAsync((Room?)null);

        var result = await MakeRoomSvc(roomRepo, hotelRepo, bookingRepo)
            .UpdateAsync(1, new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 2000, Capacity = 2 });

        Assert.Null(result);
    }

    [Fact]
    public async Task RoomService_DeactivateAsync_Valid_ReturnsTrue()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        var room = new Room { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Standard", PricePerNight = 1000, IsAvailable = true };
        roomRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        roomRepo.Setup(r => r.UpdateAsync(1, It.IsAny<Room>())).ReturnsAsync(room);

        var result = await MakeRoomSvc(roomRepo, hotelRepo, bookingRepo).DeactivateAsync(1);
        Assert.True(result);
    }

    [Fact]
    public async Task RoomService_DeactivateAsync_NotFound_ThrowsNotFoundException()
    {
        var roomRepo    = new Mock<IRepository<int, Room>>();
        var hotelRepo   = new Mock<IRepository<int, Hotel>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        roomRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Room?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeRoomSvc(roomRepo, hotelRepo, bookingRepo).DeactivateAsync(99));
    }

    // ════════════════════════════════════════════════════════════════════
    // WISHLIST SERVICE — AddAsync null user, duplicate, RemoveAsync valid,
    //                    RemoveByUserAndHotel valid
    // ════════════════════════════════════════════════════════════════════

    private static WishlistService MakeWishlistSvc(
        Mock<IRepository<int, Wishlist>> wishlistRepo,
        Mock<IRepository<int, User>>     userRepo,
        Mock<IRepository<int, Hotel>>    hotelRepo) =>
        new(wishlistRepo.Object, userRepo.Object, hotelRepo.Object,
            new Mock<ILogger<WishlistService>>().Object);

    [Fact]
    public async Task WishlistService_AddAsync_UserNotFound_ThrowsNotFoundException()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo)
                .AddAsync(new CreateWishlistDto { UserId = 99, HotelId = 1 }));
    }

    [Fact]
    public async Task WishlistService_AddAsync_NullHotel_ThrowsNotFoundException()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" });
        hotelRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Hotel?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo)
                .AddAsync(new CreateWishlistDto { UserId = 1, HotelId = 99 }));
    }

    [Fact]
    public async Task WishlistService_AddAsync_Duplicate_ThrowsAlreadyExists()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" });
        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        wishlistRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                    .ReturnsAsync(true);

        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo)
                .AddAsync(new CreateWishlistDto { UserId = 1, HotelId = 1 }));
    }

    [Fact]
    public async Task WishlistService_AddAsync_Valid_ReturnsDto()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" });
        hotelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new Hotel { HotelId = 1, HotelName = "Test", Location = "Mumbai", IsActive = true });
        wishlistRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                    .ReturnsAsync(false);
        wishlistRepo.Setup(r => r.AddAsync(It.IsAny<Wishlist>()))
                    .ReturnsAsync((Wishlist w) => { w.WishlistId = 1; return w; });

        var result = await MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo)
            .AddAsync(new CreateWishlistDto { UserId = 1, HotelId = 1 });

        Assert.Equal(1, result.UserId);
        Assert.Equal(1, result.HotelId);
    }

    [Fact]
    public async Task WishlistService_RemoveAsync_Valid_ReturnsTrue()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        var w = new Wishlist { WishlistId = 1, UserId = 1, HotelId = 1 };
        wishlistRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(w);

        var result = await MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo).RemoveAsync(1);
        Assert.True(result);
    }

    [Fact]
    public async Task WishlistService_RemoveAsync_NotFound_ThrowsNotFoundException()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        wishlistRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Wishlist?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo).RemoveAsync(99));
    }

    [Fact]
    public async Task WishlistService_RemoveByUserAndHotelAsync_Valid_ReturnsTrue()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        var w = new Wishlist { WishlistId = 1, UserId = 1, HotelId = 2 };
        wishlistRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                    .ReturnsAsync(w);
        wishlistRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(w);

        var result = await MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo).RemoveByUserAndHotelAsync(1, 2);
        Assert.True(result);
    }

    [Fact]
    public async Task WishlistService_RemoveByUserAndHotelAsync_NotFound_ThrowsNotFoundException()
    {
        var wishlistRepo = new Mock<IRepository<int, Wishlist>>();
        var userRepo     = new Mock<IRepository<int, User>>();
        var hotelRepo    = new Mock<IRepository<int, Hotel>>();

        wishlistRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wishlist, bool>>>()))
                    .ReturnsAsync((Wishlist?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            MakeWishlistSvc(wishlistRepo, userRepo, hotelRepo).RemoveByUserAndHotelAsync(1, 99));
    }

    // ════════════════════════════════════════════════════════════════════
    // CHAT SERVICE — all intent branches, GetHistoryAsync, ClearSessionAsync
    // ════════════════════════════════════════════════════════════════════

    private static ChatService MakeChatSvc(
        Mock<IRepository<int, ChatMessage>> chatRepo,
        Mock<IRepository<int, Hotel>>?      hotelRepo   = null,
        Mock<IRepository<int, Booking>>?    bookingRepo = null)
    {
        hotelRepo   ??= new Mock<IRepository<int, Hotel>>();
        bookingRepo ??= new Mock<IRepository<int, Booking>>();
        chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync((ChatMessage m) => m);
        return new ChatService(chatRepo.Object, hotelRepo.Object, bookingRepo.Object,
                               new Mock<ILogger<ChatService>>().Object);
    }

    [Theory]
    [InlineData("hello there",          "greeting")]
    [InlineData("hi",                   "greeting")]
    [InlineData("good morning",         "greeting")]
    [InlineData("cancel please",        "cancellation")]
    [InlineData("refund policy",        "cancellation")]
    [InlineData("how to pay",           "payment")]
    [InlineData("credit card payment",  "payment")]
    [InlineData("hotel amenities",      "hotel")]
    [InlineData("star rating",          "hotel")]
    [InlineData("hotel location address",  "hotel")]
    [InlineData("my reservation",       "booking")]
    [InlineData("booking status",       "booking")]
    [InlineData("I have a complaint",   "support")]
    [InlineData("random gibberish xyz", "general")]
    public async Task ChatService_ReplyAsync_IntentRouting(string message, string expectedIntent)
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = message });

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_HowToBook_ReturnsBookingIntent()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = "how to book a room" });

        Assert.Equal("booking", result.Intent);
        Assert.Contains("Book Now", result.Reply);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_ModifyBooking_ReturnsBookingIntent()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = "I want to modify my booking" });

        Assert.Equal("booking", result.Intent);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_BookingStatusQuery_ReturnsBookingIntent()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = "show my booking status" });

        Assert.Equal("booking", result.Intent);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_WithUserId_FetchesBookingCount()
    {
        var chatRepo    = new Mock<IRepository<int, ChatMessage>>();
        var bookingRepo = new Mock<IRepository<int, Booking>>();

        bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                   .ReturnsAsync(new List<Booking> { new(), new() });

        var svc = MakeChatSvc(chatRepo, bookingRepo: bookingRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "show my booking", UserId = 5
        });

        Assert.Equal("booking", result.Intent);
        Assert.Contains("2", result.Reply);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_HowManyHotels_FetchesCount()
    {
        var chatRepo  = new Mock<IRepository<int, ChatMessage>>();
        var hotelRepo = new Mock<IRepository<int, Hotel>>();

        hotelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                 .ReturnsAsync(new List<Hotel> { new(), new(), new() });

        var svc = MakeChatSvc(chatRepo, hotelRepo: hotelRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "how many hotels available"
        });

        Assert.Equal("hotel", result.Intent);
        Assert.Contains("3", result.Reply);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_RefundQuery_ReturnsCancellationIntent()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = "I want my money back" });

        Assert.Equal("cancellation", result.Intent);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_HowToCancel_ReturnsCancellationIntent()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = "how to cancel and get refund" });

        Assert.Equal("cancellation", result.Intent);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_PaymentFailed_ReturnsPaymentIntent()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = "payment failed error" });

        Assert.Equal("payment", result.Intent);
    }

    [Fact]
    public async Task ChatService_ReplyAsync_NoUserId_ReturnsLoginPrompt()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var svc      = MakeChatSvc(chatRepo);

        var result = await svc.ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "show my booking", UserId = null
        });

        Assert.Equal("booking", result.Intent);
        Assert.Contains("login", result.Reply.ToLower());
    }

    [Fact]
    public async Task ChatService_GetHistoryAsync_ReturnsOrderedMessages()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var msgs = new List<ChatMessage>
        {
            new() { ChatMessageId = 2, SessionId = "s1", Sender = "bot",  Message = "Hi!", CreatedAt = DateTime.UtcNow.AddSeconds(1) },
            new() { ChatMessageId = 1, SessionId = "s1", Sender = "user", Message = "Hello", CreatedAt = DateTime.UtcNow }
        };
        chatRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                .ReturnsAsync(msgs);

        var svc    = MakeChatSvc(chatRepo);
        var result = (await svc.GetHistoryAsync("s1")).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].ChatMessageId); // ordered ascending
    }

    [Fact]
    public async Task ChatService_ClearSessionAsync_DeletesMessages_ReturnsTrue()
    {
        var chatRepo = new Mock<IRepository<int, ChatMessage>>();
        var msgs = new List<ChatMessage>
        {
            new() { ChatMessageId = 1, SessionId = "s1" },
            new() { ChatMessageId = 2, SessionId = "s1" }
        };
        chatRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                .ReturnsAsync(msgs);
        chatRepo.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => msgs.FirstOrDefault(m => m.ChatMessageId == id));

        var svc    = MakeChatSvc(chatRepo);
        var result = await svc.ClearSessionAsync("s1");

        Assert.True(result);
        chatRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Exactly(2));
    }

    // ════════════════════════════════════════════════════════════════════
    // AUDIT LOG SERVICE — GetByIdAsync, GetByUserAsync, DeleteAsync,
    //                     FilterAsync all filter branches
    // ════════════════════════════════════════════════════════════════════

    private static AuditLogService MakeAuditSvc(Mock<IRepository<int, AuditLog>> repo) =>
        new(repo.Object, new Mock<ILogger<AuditLogService>>().Object);

    [Fact]
    public async Task AuditLogService_GetByIdAsync_Found_ReturnsDto()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        var log  = new AuditLog { AuditLogId = 1, Action = "Test", EntityName = "Hotel", CreatedAt = DateTime.UtcNow,
                                  User = new User { UserName = "Alice" } };
        repo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
            .ReturnsAsync(new List<AuditLog> { log });

        var result = await MakeAuditSvc(repo).GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.UserName);
    }

    [Fact]
    public async Task AuditLogService_GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        repo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
            .ReturnsAsync(new List<AuditLog>());

        await Assert.ThrowsAsync<NotFoundException>(() => MakeAuditSvc(repo).GetByIdAsync(99));
    }

    [Fact]
    public async Task AuditLogService_GetByUserAsync_ReturnsUserLogs()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, UserId = 3, Action = "Test", EntityName = "Hotel", CreatedAt = DateTime.UtcNow }
        };
        repo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, bool>>>()))
            .ReturnsAsync(logs);

        var result = await MakeAuditSvc(repo).GetByUserAsync(3);

        Assert.Single(result);
        Assert.Equal(3, result[0].UserId);
    }

    [Fact]
    public async Task AuditLogService_DeleteAsync_Valid_ReturnsTrue()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        var log  = new AuditLog { AuditLogId = 1, Action = "Test", EntityName = "Hotel" };
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(log);
        repo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(log);

        var result = await MakeAuditSvc(repo).DeleteAsync(1);
        Assert.True(result);
    }

    [Fact]
    public async Task AuditLogService_DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((AuditLog?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => MakeAuditSvc(repo).DeleteAsync(99));
    }

    [Fact]
    public async Task AuditLogService_CreateAsync_Valid_ReturnsDto()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        repo.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync((AuditLog l) => { l.AuditLogId = 1; return l; });

        var result = await MakeAuditSvc(repo).CreateAsync(
            new CreateAuditLogDto { Action = "Test", EntityName = "Hotel", EntityId = 1 });

        Assert.Equal("Test", result.Action);
    }

    [Fact]
    public async Task AuditLogService_FilterAsync_NullChanges_ReturnsEmptyString()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<AuditLog>
        {
            new() { AuditLogId = 1, Action = "Test", EntityName = "Hotel", Changes = null, CreatedAt = DateTime.UtcNow }
        });

        var result = await MakeAuditSvc(repo).FilterAsync(new AuditLogFilterDto());

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].Changes);
    }

    [Fact]
    public async Task AuditLogService_GetAllAsync_UserNullNavProp_ReturnsEmptyUserName()
    {
        var repo = new Mock<IRepository<int, AuditLog>>();
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = 1, Action = "Test", EntityName = "Hotel", CreatedAt = DateTime.UtcNow, User = null }
        };
        repo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
            .ReturnsAsync(logs);

        var result = await MakeAuditSvc(repo).GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Single(result.Data);
        Assert.Equal(string.Empty, result.Data[0].UserName);
    }

    // ════════════════════════════════════════════════════════════════════
    // USER SERVICE — DeleteAsync valid
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UserService_DeleteAsync_Valid_ReturnsTrue()
    {
        var userRepo = new Mock<IRepository<int, User>>();
        var pwdSvc   = new Mock<IPasswordService>();
        var user     = new User { UserId = 1, UserName = "Alice", Email = "a@b.com", Role = "user" };
        userRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(user);

        var svc    = new UserService(userRepo.Object, pwdSvc.Object, new Mock<ILogger<UserService>>().Object);
        var result = await svc.DeleteAsync(1);

        Assert.True(result);
    }
}
