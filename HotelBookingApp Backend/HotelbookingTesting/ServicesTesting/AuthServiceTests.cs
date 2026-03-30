using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class AuthServiceTests
{
    private readonly Mock<IRepository<int, User>> _userRepo        = new();
    private readonly Mock<IPasswordService>       _passwordService = new();
    private readonly Mock<ILogger<AuthService>>   _logger          = new();

    private AuthService CreateService() =>
        new(_userRepo.Object, _passwordService.Object, _logger.Object);

    private static byte[] FakeHash() => new byte[] { 1, 2, 3, 4 };

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        var user = new User
        {
            UserId = 1, UserName = "John", Email = "john@test.com",
            PasswordHash = FakeHash(), Role = "user"
        };

        _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                 .ReturnsAsync(user);
        _passwordService.Setup(p => p.VerifyPassword("password123", FakeHash())).Returns(true);

        var result = await CreateService().LoginAsync(new LoginRequestDto
        {
            Email = "john@test.com", Password = "password123"
        });

        Assert.NotNull(result);
        Assert.Equal("john@test.com", result.Email);
        Assert.Equal("user", result.Role);
        Assert.Equal(1, result.UserId);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedException()
    {
        _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                 .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().LoginAsync(new LoginRequestDto { Email = "x@x.com", Password = "pass" }));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedException()
    {
        var user = new User { UserId = 1, Email = "a@b.com", PasswordHash = FakeHash(), Role = "user" };
        _userRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                 .ReturnsAsync(user);
        _passwordService.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().LoginAsync(new LoginRequestDto { Email = "a@b.com", Password = "wrong" }));
    }
}
