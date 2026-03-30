using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class UserServiceTests
{
    private readonly Mock<IRepository<int, User>> _userRepo        = new();
    private readonly Mock<IPasswordService>       _passwordService = new();
    private readonly Mock<ILogger<UserService>>   _logger          = new();

    private UserService CreateService() =>
        new(_userRepo.Object, _passwordService.Object, _logger.Object);

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsUserResponseDto()
    {
        var dto = new RegisterRequestDto
        {
            UserName = "Alice", Email = "alice@test.com",
            Password = "pass123", Role = "user"
        };

        _userRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                 .ReturnsAsync(false);
        _passwordService.Setup(p => p.HashPassword("pass123")).Returns(new byte[] { 1, 2, 3 });
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { u.UserId = 1; return u; });

        var result = await CreateService().RegisterAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Alice", result.UserName);
        Assert.Equal("alice@test.com", result.Email);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsAlreadyExistsException()
    {
        _userRepo.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                 .ReturnsAsync(true);

        var dto = new RegisterRequestDto { UserName = "Bob", Email = "bob@test.com", Password = "pass", Role = "user" };

        await Assert.ThrowsAsync<AlreadyExistsException>(() => CreateService().RegisterAsync(dto));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsDto()
    {
        var user = new User { UserId = 1, UserName = "Alice", Email = "alice@test.com", Role = "user" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await CreateService().GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.UserName);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_ReturnsTrue()
    {
        var user = new User { UserId = 1, UserName = "Alice", Email = "alice@test.com", Role = "user" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(user);

        var result = await CreateService().DeleteAsync(1);

        Assert.True(result);
    }
}
