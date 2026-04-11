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
    public class UserServiceTests
    {
        private readonly Mock<IRepository<int, User>> _repoMock;
        private readonly Mock<IPasswordService> _passwordMock;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _repoMock = new Mock<IRepository<int, User>>();
            _passwordMock = new Mock<IPasswordService>();
            _sut = new UserService(_repoMock.Object, _passwordMock.Object, MockLogger.Create<UserService>());
        }

        // ── RegisterAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task RegisterAsync_NewUser_ReturnsRegisterResponse()
        {
            var req = new RegisterRequestDto { UserName = "Bob", Email = "bob@test.com", Password = "pass1234", Role = "user" };
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(false);
            _passwordMock.Setup(p => p.HashPassword("pass1234")).Returns(new byte[] { 1 });
            _repoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                     .ReturnsAsync((User u) => { u.UserId = 5; return u; });

            var result = await _sut.RegisterAsync(req);

            Assert.Equal(5, result.UserId);
            Assert.Equal("Bob", result.UserName);
            Assert.Equal("user", result.Role);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ThrowsAlreadyExistsException()
        {
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                     .ReturnsAsync(true);

            await Assert.ThrowsAsync<AlreadyExistsException>(() =>
                _sut.RegisterAsync(new RegisterRequestDto { UserName = "X", Email = "dup@test.com", Password = "pass1234", Role = "user" }));
        }

        [Fact]
        public async Task RegisterAsync_InvalidRole_ThrowsBadRequestException()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                _sut.RegisterAsync(new RegisterRequestDto { UserName = "X", Email = "x@x.com", Password = "pass1234", Role = "superadmin" }));
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_Exists_ReturnsUserDto()
        {
            _repoMock.Setup(r => r.GetByIdAsync(1))
                     .ReturnsAsync(new User { UserId = 1, UserName = "Alice", Email = "a@a.com", Role = "user" });

            var result = await _sut.GetByIdAsync(1);

            Assert.Equal("Alice", result.UserName);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
        }

        // ── GetAllAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            _repoMock.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<User>
                     {
                         new() { UserId = 1, UserName = "A", Email = "a@a.com", Role = "user" },
                         new() { UserId = 2, UserName = "B", Email = "b@b.com", Role = "admin" }
                     });

            var result = (await _sut.GetAllAsync()).ToList();

            Assert.Equal(2, result.Count);
        }

        // ── GetPagedAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetPagedAsync_ReturnsCorrectPage()
        {
            var users = Enumerable.Range(1, 15)
                .Select(i => new User { UserId = i, UserName = $"User{i}", Email = $"u{i}@t.com", Role = "user" })
                .ToList();
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            var result = await _sut.GetPagedAsync(new PagedRequestDto { PageNumber = 2, PageSize = 5 });

            Assert.Equal(5, result.Data.Count);
            Assert.Equal(15, result.TotalRecords);
            Assert.Equal(3, result.TotalPages);
        }

        // ── DeleteAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_Exists_ReturnsTrue()
        {
            _repoMock.Setup(r => r.DeleteAsync(1))
                     .ReturnsAsync(new User { UserId = 1, UserName = "A", Email = "a@a.com", Role = "user" });

            Assert.True(await _sut.DeleteAsync(1));
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
        }
    }
}
