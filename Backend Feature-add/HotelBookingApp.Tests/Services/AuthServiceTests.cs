using HotelBookingApp.Exceptions;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IRepository<int, User>> _userRepoMock;
        private readonly Mock<IPasswordService> _passwordMock;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IRepository<int, User>>();
            _passwordMock = new Mock<IPasswordService>();
            _sut = new AuthService(_userRepoMock.Object, _passwordMock.Object, MockLogger.Create<AuthService>());
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
        {
            var hash = new byte[] { 1, 2, 3 };
            var user = new User { UserId = 1, UserName = "Alice", Email = "alice@test.com", Role = "user", PasswordHash = hash };

            _userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user);
            _passwordMock.Setup(p => p.VerifyPassword("pass123", hash)).Returns(true);

            var result = await _sut.LoginAsync(new LoginRequestDto { Email = "alice@test.com", Password = "pass123" });

            Assert.Equal(1, result.UserId);
            Assert.Equal("Alice", result.UserName);
            Assert.Equal("user", result.Role);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedException()
        {
            _userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                         .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _sut.LoginAsync(new LoginRequestDto { Email = "x@x.com", Password = "pass" }));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedException()
        {
            var user = new User { UserId = 1, Email = "alice@test.com", PasswordHash = new byte[] { 1 } };
            _userRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user);
            _passwordMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(false);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _sut.LoginAsync(new LoginRequestDto { Email = "alice@test.com", Password = "wrong" }));
        }
    }
}
