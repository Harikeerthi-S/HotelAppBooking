using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class UserServiceExtendedTests
{
    private readonly Mock<IRepository<int, User>> _userRepo        = new();
    private readonly Mock<IPasswordService>       _passwordService = new();
    private readonly Mock<ILogger<UserService>>   _logger          = new();

    private UserService CreateService() =>
        new(_userRepo.Object, _passwordService.Object, _logger.Object);

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        var users = new List<User>
        {
            new() { UserId = 1, UserName = "Alice", Email = "alice@test.com", Role = "user" },
            new() { UserId = 2, UserName = "Bob",   Email = "bob@test.com",   Role = "admin" },
            new() { UserId = 3, UserName = "Carol", Email = "carol@test.com", Role = "hotelmanager" }
        };
        _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = (await CreateService().GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedUsers()
    {
        var users = Enumerable.Range(1, 25).Select(i => new User
        {
            UserId = i, UserName = $"User{i}", Email = $"user{i}@test.com", Role = "user"
        }).ToList();

        _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await CreateService().GetPagedAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(25, result.TotalRecords);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_ReturnsCorrectSlice()
    {
        var users = Enumerable.Range(1, 15).Select(i => new User
        {
            UserId = i, UserName = $"User{i}", Email = $"user{i}@test.com", Role = "user"
        }).ToList();

        _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await CreateService().GetPagedAsync(new PagedRequestDto { PageNumber = 2, PageSize = 10 });

        Assert.Equal(5, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
    }

    // ── RegisterAsync edge cases ──────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsDto()
    {
        _userRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                 .ReturnsAsync(false);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns(new byte[] { 1, 2, 3 });
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { u.UserId = 10; return u; });

        var dto = new RegisterRequestDto { UserName = "TestUser", Email = "test@test.com", Password = "pass123", Role = "user" };
        var result = await CreateService().RegisterAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("TestUser", result.UserName);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsAlreadyExistsException()
    {
        _userRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                 .ReturnsAsync(true);

        var dto = new RegisterRequestDto { UserName = "Alice", Email = "alice@test.com", Password = "pass123", Role = "user" };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().RegisterAsync(dto));
    }

    // ── DeleteAsync edge cases ────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectRole()
    {
        var user = new User { UserId = 1, UserName = "Admin", Email = "admin@test.com", Role = "admin" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await CreateService().GetByIdAsync(1);

        Assert.Equal("admin", result!.Role);
    }
}
