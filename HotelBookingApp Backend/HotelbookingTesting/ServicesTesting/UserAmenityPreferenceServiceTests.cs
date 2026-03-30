using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class UserAmenityPreferenceServiceTests
{
    private readonly Mock<IRepository<int, UserAmenityPreference>> _prefRepo    = new();
    private readonly Mock<IRepository<int, User>>                  _userRepo    = new();
    private readonly Mock<IRepository<int, Amenity>>               _amenityRepo = new();
    private readonly Mock<ILogger<UserAmenityPreferenceService>>   _logger      = new();

    private UserAmenityPreferenceService CreateService() =>
        new(_prefRepo.Object, _userRepo.Object, _amenityRepo.Object, _logger.Object);

    private static User   FakeUser(int id = 1)    => new() { UserId = id, UserName = "Alice", Email = "a@b.com", Role = "user" };
    private static Amenity FakeAmenity(int id = 1) => new() { AmenityId = id, Name = "Pool", Icon = "🏊" };

    // ── AddAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidPreference_ReturnsDto()
    {
        var dto = new CreateUserAmenityPreferenceDto { UserId = 1, AmenityId = 1 };

        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeAmenity());
        _prefRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, bool>>>()))
                 .ReturnsAsync(false);
        _prefRepo.Setup(r => r.AddAsync(It.IsAny<UserAmenityPreference>()))
                 .ReturnsAsync((UserAmenityPreference p) => { p.PreferenceId = 1; return p; });

        var result = await CreateService().AddAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(1, result.AmenityId);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task AddAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
        var dto = new CreateUserAmenityPreferenceDto { UserId = 99, AmenityId = 1 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_AmenityNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Amenity?)null);

        var dto = new CreateUserAmenityPreferenceDto { UserId = 1, AmenityId = 99 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().AddAsync(dto));
    }

    [Fact]
    public async Task AddAsync_DuplicatePreference_ThrowsAlreadyExistsException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeAmenity());
        _prefRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, bool>>>()))
                 .ReturnsAsync(true);

        var dto = new CreateUserAmenityPreferenceDto { UserId = 1, AmenityId = 1 };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().AddAsync(dto));
    }

    // ── ApproveAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_ExistingPreference_ReturnsApproved()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeAmenity());

        var result = await CreateService().ApproveAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Approved", result.Status);
    }

    [Fact]
    public async Task ApproveAsync_NotFound_ThrowsNotFoundException()
    {
        _prefRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((UserAmenityPreference?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().ApproveAsync(99));
    }

    // ── RejectAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_ExistingPreference_ReturnsRejected()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeAmenity());

        var result = await CreateService().RejectAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Rejected", result.Status);
    }

    [Fact]
    public async Task RejectAsync_NotFound_ThrowsNotFoundException()
    {
        _prefRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((UserAmenityPreference?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RejectAsync(99));
    }

    // ── GetByUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_ReturnsUserPreferences()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 3, AmenityId = 1, Status = "Pending",  CreatedAt = DateTime.UtcNow,
                    User = FakeUser(3), Amenity = FakeAmenity(1) },
            new() { PreferenceId = 2, UserId = 3, AmenityId = 2, Status = "Approved", CreatedAt = DateTime.UtcNow,
                    User = FakeUser(3), Amenity = new Amenity { AmenityId = 2, Name = "Gym", Icon = "🏋️" } }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetByUserAsync(3)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(3, r.UserId));
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllPreferences()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending",  CreatedAt = DateTime.UtcNow,
                    User = FakeUser(1), Amenity = FakeAmenity(1) },
            new() { PreferenceId = 2, UserId = 2, AmenityId = 2, Status = "Approved", CreatedAt = DateTime.UtcNow,
                    User = FakeUser(2), Amenity = new Amenity { AmenityId = 2, Name = "Gym" } },
            new() { PreferenceId = 3, UserId = 3, AmenityId = 1, Status = "Rejected", CreatedAt = DateTime.UtcNow,
                    User = FakeUser(3), Amenity = FakeAmenity(1) }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_ExistingPreference_ReturnsTrue()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1 };
        _prefRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(pref);

        var result = await CreateService().RemoveAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveAsync_NotFound_ThrowsNotFoundException()
    {
        _prefRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync((UserAmenityPreference?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RemoveAsync(99));
    }

    // ── RemoveByUserAndAmenityAsync ───────────────────────────────────────

    [Fact]
    public async Task RemoveByUserAndAmenityAsync_ExistingEntry_ReturnsTrue()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 2 };
        _prefRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, bool>>>()))
                 .ReturnsAsync(pref);
        _prefRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(pref);

        var result = await CreateService().RemoveByUserAndAmenityAsync(1, 2);

        Assert.True(result);
    }

    [Fact]
    public async Task RemoveByUserAndAmenityAsync_NotFound_ThrowsNotFoundException()
    {
        _prefRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, bool>>>()))
                 .ReturnsAsync((UserAmenityPreference?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().RemoveByUserAndAmenityAsync(1, 99));
    }

    // ── MapDtoAfterStatusChange — null user/amenity branches ──────────────

    [Fact]
    public async Task ApproveAsync_NullUserAndAmenity_ReturnsEmptyStrings()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);       // null user
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Amenity?)null); // null amenity

        var result = await CreateService().ApproveAsync(1);

        Assert.Equal("Approved", result.Status);
        Assert.Equal(string.Empty, result.UserName);
        Assert.Equal(string.Empty, result.AmenityName);
        Assert.Null(result.AmenityIcon);
    }

    [Fact]
    public async Task RejectAsync_NullUserAndAmenity_ReturnsEmptyStrings()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Amenity?)null);

        var result = await CreateService().RejectAsync(1);

        Assert.Equal("Rejected", result.Status);
        Assert.Equal(string.Empty, result.UserName);
        Assert.Equal(string.Empty, result.AmenityName);
    }

    // ── GetByUserAsync — null navigation properties ───────────────────────

    [Fact]
    public async Task GetByUserAsync_NullNavProps_ReturnsEmptyStrings()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 5, AmenityId = 1, Status = "Pending",
                    CreatedAt = DateTime.UtcNow, User = null, Amenity = null }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetByUserAsync(5)).ToList();

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].UserName);
        Assert.Equal(string.Empty, result[0].AmenityName);
        Assert.Null(result[0].AmenityIcon);
    }

    // ── GetAllAsync — null navigation properties ──────────────────────────

    [Fact]
    public async Task GetAllAsync_NullNavProps_ReturnsEmptyStrings()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending",
                    CreatedAt = DateTime.UtcNow, User = null, Amenity = null }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].UserName);
        Assert.Equal(string.Empty, result[0].AmenityName);
    }

    // ── GetByUserAsync — ordered by CreatedAt descending ─────────────────

    [Fact]
    public async Task GetByUserAsync_OrderedByCreatedAtDescending()
    {
        var older = DateTime.UtcNow.AddDays(-3);
        var newer = DateTime.UtcNow;

        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 2, AmenityId = 1, Status = "Pending",
                    CreatedAt = older, User = FakeUser(2), Amenity = FakeAmenity(1) },
            new() { PreferenceId = 2, UserId = 2, AmenityId = 2, Status = "Approved",
                    CreatedAt = newer, User = FakeUser(2), Amenity = new Amenity { AmenityId = 2, Name = "Gym" } }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetByUserAsync(2)).ToList();

        Assert.Equal(2, result[0].PreferenceId); // newer first
        Assert.Equal(1, result[1].PreferenceId);
    }

    // ── GetAllAsync — ordered by CreatedAt descending ─────────────────────

    [Fact]
    public async Task GetAllAsync_OrderedByCreatedAtDescending()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddDays(-5), User = FakeUser(1), Amenity = FakeAmenity(1) },
            new() { PreferenceId = 2, UserId = 2, AmenityId = 1, Status = "Approved",
                    CreatedAt = DateTime.UtcNow, User = FakeUser(2), Amenity = FakeAmenity(1) }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Equal(2, result[0].PreferenceId); // newer first
    }

    // ── GetByUserAsync — filters out other users ──────────────────────────

    [Fact]
    public async Task GetByUserAsync_FiltersOtherUsers()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending",
                    CreatedAt = DateTime.UtcNow, User = FakeUser(1), Amenity = FakeAmenity(1) },
            new() { PreferenceId = 2, UserId = 2, AmenityId = 1, Status = "Approved",
                    CreatedAt = DateTime.UtcNow, User = FakeUser(2), Amenity = FakeAmenity(1) }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetByUserAsync(1)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }

    // ── GetByUserAsync — no matching user returns empty ───────────────────

    [Fact]
    public async Task GetByUserAsync_NoMatchingUser_ReturnsEmpty()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending",
                    CreatedAt = DateTime.UtcNow, User = FakeUser(1), Amenity = FakeAmenity(1) }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetByUserAsync(999)).ToList();

        Assert.Empty(result);
    }

    // ── GetAllAsync — empty list ──────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmpty()
    {
        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(new List<UserAmenityPreference>());

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Empty(result);
    }

    // ── AddAsync — amenity with null icon ─────────────────────────────────

    [Fact]
    public async Task AddAsync_AmenityWithNullIcon_ReturnsDtoWithNullIcon()
    {
        var amenityNoIcon = new Amenity { AmenityId = 3, Name = "Parking", Icon = null };
        var dto = new CreateUserAmenityPreferenceDto { UserId = 1, AmenityId = 3 };

        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(amenityNoIcon);
        _prefRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, bool>>>()))
                 .ReturnsAsync(false);
        _prefRepo.Setup(r => r.AddAsync(It.IsAny<UserAmenityPreference>()))
                 .ReturnsAsync((UserAmenityPreference p) => { p.PreferenceId = 5; return p; });

        var result = await CreateService().AddAsync(dto);

        Assert.Equal("Parking", result.AmenityName);
        Assert.Null(result.AmenityIcon);
        Assert.Equal("Pending", result.Status);
    }

    // ── AddAsync — MapToDto field mapping fully verified ─────────────────

    [Fact]
    public async Task AddAsync_VerifiesAllDtoFields()
    {
        var now = DateTime.UtcNow;
        var dto = new CreateUserAmenityPreferenceDto { UserId = 2, AmenityId = 2 };
        var amenity = new Amenity { AmenityId = 2, Name = "Gym", Icon = "🏋️" };
        var user    = new User { UserId = 2, UserName = "Bob", Email = "b@b.com", Role = "user" };

        _userRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
        _amenityRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(amenity);
        _prefRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, bool>>>()))
                 .ReturnsAsync(false);
        _prefRepo.Setup(r => r.AddAsync(It.IsAny<UserAmenityPreference>()))
                 .ReturnsAsync((UserAmenityPreference p) =>
                 {
                     p.PreferenceId = 10;
                     p.CreatedAt    = now;
                     return p;
                 });

        var result = await CreateService().AddAsync(dto);

        Assert.Equal(10,      result.PreferenceId);
        Assert.Equal(2,       result.UserId);
        Assert.Equal("Bob",   result.UserName);
        Assert.Equal(2,       result.AmenityId);
        Assert.Equal("Gym",   result.AmenityName);
        Assert.Equal("🏋️",   result.AmenityIcon);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(now,     result.CreatedAt);
    }

    // ── ApproveAsync — user exists but amenity is null ────────────────────

    [Fact]
    public async Task ApproveAsync_UserExistsAmenityNull_ReturnsEmptyAmenityFields()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());   // user found
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Amenity?)null); // amenity null

        var result = await CreateService().ApproveAsync(1);

        Assert.Equal("Approved",    result.Status);
        Assert.Equal("Alice",       result.UserName);   // user name present
        Assert.Equal(string.Empty,  result.AmenityName); // amenity name empty
        Assert.Null(result.AmenityIcon);
    }

    // ── RejectAsync — amenity exists but user is null ─────────────────────

    [Fact]
    public async Task RejectAsync_AmenityExistsUserNull_ReturnsEmptyUserName()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);  // user null
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeAmenity()); // amenity found

        var result = await CreateService().RejectAsync(1);

        Assert.Equal("Rejected",   result.Status);
        Assert.Equal(string.Empty, result.UserName);  // user name empty
        Assert.Equal("Pool",       result.AmenityName); // amenity name present
        Assert.Equal("🏊",         result.AmenityIcon);
    }

    // ── RemoveByUserAndAmenityAsync — verifies correct PreferenceId used ──

    [Fact]
    public async Task RemoveByUserAndAmenityAsync_CallsDeleteWithCorrectId()
    {
        var pref = new UserAmenityPreference { PreferenceId = 42, UserId = 7, AmenityId = 3 };

        _prefRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, bool>>>()))
                 .ReturnsAsync(pref);
        _prefRepo.Setup(r => r.DeleteAsync(42)).ReturnsAsync(pref);

        var result = await CreateService().RemoveByUserAndAmenityAsync(7, 3);

        Assert.True(result);
        _prefRepo.Verify(r => r.DeleteAsync(42), Times.Once); // must use pref.PreferenceId
    }

    // ── GetByUserAsync — amenity icon present ─────────────────────────────

    [Fact]
    public async Task GetByUserAsync_AmenityIconPresent_ReturnedInDto()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Approved",
                    CreatedAt = DateTime.UtcNow,
                    User    = FakeUser(1),
                    Amenity = new Amenity { AmenityId = 1, Name = "Spa", Icon = "💆" } }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetByUserAsync(1)).ToList();

        Assert.Single(result);
        Assert.Equal("Spa", result[0].AmenityName);
        Assert.Equal("💆",  result[0].AmenityIcon);
        Assert.Equal("Approved", result[0].Status);
    }

    // ── GetAllAsync — all three statuses present ──────────────────────────

    [Fact]
    public async Task GetAllAsync_AllStatuses_ReturnedCorrectly()
    {
        var prefs = new List<UserAmenityPreference>
        {
            new() { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddDays(-3), User = FakeUser(1), Amenity = FakeAmenity(1) },
            new() { PreferenceId = 2, UserId = 2, AmenityId = 1, Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddDays(-2), User = FakeUser(2), Amenity = FakeAmenity(1) },
            new() { PreferenceId = 3, UserId = 3, AmenityId = 1, Status = "Rejected",
                    CreatedAt = DateTime.UtcNow.AddDays(-1), User = FakeUser(3), Amenity = FakeAmenity(1) }
        };

        _prefRepo.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserAmenityPreference, object>>[]>()))
                 .ReturnsAsync(prefs);

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.Status == "Pending");
        Assert.Contains(result, r => r.Status == "Approved");
        Assert.Contains(result, r => r.Status == "Rejected");
        // newest first (Rejected added most recently)
        Assert.Equal(3, result[0].PreferenceId);
    }

    // ── ApproveAsync — verifies UpdateAsync is called ─────────────────────

    [Fact]
    public async Task ApproveAsync_CallsUpdateAsync()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeAmenity());

        await CreateService().ApproveAsync(1);

        _prefRepo.Verify(r => r.UpdateAsync(1, It.Is<UserAmenityPreference>(p => p.Status == "Approved")), Times.Once);
    }

    // ── RejectAsync — verifies UpdateAsync is called ──────────────────────

    [Fact]
    public async Task RejectAsync_CallsUpdateAsync()
    {
        var pref = new UserAmenityPreference { PreferenceId = 1, UserId = 1, AmenityId = 1, Status = "Pending" };

        _prefRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pref);
        _prefRepo.Setup(r => r.UpdateAsync(1, It.IsAny<UserAmenityPreference>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeUser());
        _amenityRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(FakeAmenity());

        await CreateService().RejectAsync(1);

        _prefRepo.Verify(r => r.UpdateAsync(1, It.Is<UserAmenityPreference>(p => p.Status == "Rejected")), Times.Once);
    }
}
