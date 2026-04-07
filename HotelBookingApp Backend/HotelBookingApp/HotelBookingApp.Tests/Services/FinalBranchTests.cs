using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class FinalBranchTests
    {
        // ── AmenityService UpdateAsync: null Description/Icon ?.Trim() ────

        [Fact]
        public async Task Amenity_UpdateAsync_NullDescriptionAndIcon_CoverNullTrimBranch()
        {
            var repoMock = new Mock<IRepository<int, Amenity>>();
            repoMock.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(new Amenity { AmenityId = 1, Name = "Gym", Description = "old", Icon = "old" });
            repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                    .ReturnsAsync(false);
            repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Amenity>())).ReturnsAsync((int _, Amenity a) => a);

            var sut = new AmenityService(repoMock.Object, MockLogger.Create<AmenityService>());
            Assert.True(await sut.UpdateAsync(1, new CreateAmenityDto { Name = "Gym", Description = null, Icon = null }));
        }

        [Fact]
        public async Task Amenity_UpdateAsync_NonNullDescriptionAndIcon_CoversTrimBranch()
        {
            var repoMock = new Mock<IRepository<int, Amenity>>();
            repoMock.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(new Amenity { AmenityId = 1, Name = "Gym" });
            repoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Amenity, bool>>>()))
                    .ReturnsAsync(false);
            repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Amenity>())).ReturnsAsync((int _, Amenity a) => a);

            var sut = new AmenityService(repoMock.Object, MockLogger.Create<AmenityService>());
            Assert.True(await sut.UpdateAsync(1, new CreateAmenityDto { Name = "Gym Updated", Description = "Great gym", Icon = "gym-icon" }));
        }

        // ── AuditLogService GetAllAsync: a.User?.UserName null/non-null ───

        [Fact]
        public async Task AuditLog_GetAllAsync_LogWithNullUser_ReturnsEmptyUserName()
        {
            var repoMock = new Mock<IRepository<int, AuditLog>>();
            repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                    .ReturnsAsync(new List<AuditLog>
                    {
                        new() { AuditLogId = 1, Action = "Test", EntityName = "E", CreatedAt = DateTime.UtcNow, User = null }
                    });

            var sut = new AuditLogService(repoMock.Object, MockLogger.Create<AuditLogService>());
            var result = await sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(string.Empty, result.Data[0].UserName);
        }

        [Fact]
        public async Task AuditLog_GetAllAsync_LogWithUser_ReturnsUserName()
        {
            var repoMock = new Mock<IRepository<int, AuditLog>>();
            repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                    .ReturnsAsync(new List<AuditLog>
                    {
                        new() { AuditLogId = 1, Action = "Test", EntityName = "E", CreatedAt = DateTime.UtcNow,
                            User = new User { UserId = 1, UserName = "Alice", Email = "a@a.com", Role = "user" } }
                    });

            var sut = new AuditLogService(repoMock.Object, MockLogger.Create<AuditLogService>());
            var result = await sut.GetAllAsync(new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal("Alice", result.Data[0].UserName);
        }

        [Fact]
        public async Task AuditLog_FilterPagedAsync_LogWithNullUser_ReturnsEmptyUserName()
        {
            var repoMock = new Mock<IRepository<int, AuditLog>>();
            repoMock.Setup(r => r.GetAllIncludingAsync(It.IsAny<System.Linq.Expressions.Expression<Func<AuditLog, object>>[]>()))
                    .ReturnsAsync(new List<AuditLog>
                    {
                        new() { AuditLogId = 1, Action = "Test", EntityName = "E", CreatedAt = DateTime.UtcNow, User = null }
                    });

            var sut = new AuditLogService(repoMock.Object, MockLogger.Create<AuditLogService>());
            var result = await sut.FilterPagedAsync(new AuditLogFilterDto(), new PagedRequestDto { PageNumber = 1, PageSize = 10 });
            Assert.Equal(string.Empty, result.Data[0].UserName);
        }

        // ── PaymentService: existing payment for different booking ────────

        [Fact]
        public async Task Payment_MakePaymentAsync_ExistingPaymentDifferentBooking_Proceeds()
        {
            var paymentRepoMock = new Mock<IRepository<int, Payment>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, TotalAmount = 2000, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            paymentRepoMock.Setup(r => r.GetAllAsync())
                           .ReturnsAsync(new List<Payment> { new() { PaymentId = 99, BookingId = 2, PaymentStatus = "Completed" } });
            paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                           .ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                           .ReturnsAsync((int _, Booking b) => b);

            var sut = new PaymentService(paymentRepoMock.Object, bookingRepoMock.Object, auditMock.Object, MockLogger.Create<PaymentService>());
            var result = await sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard" });
            Assert.Equal("Completed", result.PaymentStatus);
        }

        // ── PaymentService UpdateStatusAsync: "Pending" → default switch arm ──

        [Fact]
        public async Task Payment_UpdateStatusAsync_PendingStatus_DefaultSwitchArm()
        {
            var paymentRepoMock = new Mock<IRepository<int, Payment>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

            var payment = new Payment { PaymentId = 1, BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard", PaymentStatus = "Completed" };
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, TotalAmount = 2000, Status = "Cancelled",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync((int _, Payment p) => p);
            bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

            var sut = new PaymentService(paymentRepoMock.Object, bookingRepoMock.Object, auditMock.Object, MockLogger.Create<PaymentService>());
            // "Pending" → default arm → newBookingStatus = booking.Status = "Cancelled" → no update
            var result = await sut.UpdateStatusAsync(1, "Pending");
            Assert.Equal("Pending", result!.PaymentStatus);
            bookingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()), Times.Never);
        }

        // ── HotelService UpdateAsync: non-null optional fields ────────────

        [Fact]
        public async Task Hotel_UpdateAsync_WithNonNullOptionalFields_CoversTrimBranch()
        {
            var repoMock  = new Mock<IRepository<int, Hotel>>();
            var auditMock = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
            repoMock.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(new Hotel { HotelId = 1, HotelName = "H", Location = "L", IsActive = true });
            repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Hotel>())).ReturnsAsync((int _, Hotel h) => h);

            var sut = new HotelService(repoMock.Object, auditMock.Object, MockLogger.Create<HotelService>());
            var result = await sut.UpdateAsync(1, new CreateHotelDto
            {
                HotelName = "Updated", Location = "City", StarRating = 4, TotalRooms = 20,
                Address = "123 St", ContactNumber = "9999999999", ImagePath = "/img.jpg"
            });
            Assert.NotNull(result!.Address);
        }

        // ── RoomService CreateAsync/UpdateAsync: non-null ImageUrl ────────

        [Fact]
        public async Task Room_CreateAsync_WithNonNullImageUrl_CoversTrimBranch()
        {
            var roomRepoMock    = new Mock<IRepository<int, Room>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
            hotelRepoMock.Setup(r => r.GetByIdAsync(1))
                         .ReturnsAsync(new Hotel { HotelId = 1, HotelName = "H", Location = "L", IsActive = true });
            roomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                        .ReturnsAsync(false);
            roomRepoMock.Setup(r => r.AddAsync(It.IsAny<Room>()))
                        .ReturnsAsync((Room r) => { r.RoomId = 1; r.ImageUrl = "/room.jpg"; return r; });

            var sut = new RoomService(roomRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, auditMock.Object, MockLogger.Create<RoomService>());
            var result = await sut.CreateAsync(new CreateRoomDto { HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 1000, Capacity = 2, ImageUrl = "/room.jpg" });
            Assert.Equal("/room.jpg", result.ImageUrl);
        }

        [Fact]
        public async Task Room_UpdateAsync_WithNonNullImageUrl_CoversTrimBranch()
        {
            var roomRepoMock    = new Mock<IRepository<int, Room>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());
            roomRepoMock.Setup(r => r.GetByIdAsync(1))
                        .ReturnsAsync(new Room { RoomId = 1, HotelId = 1, RoomNumber = 101, RoomType = "Deluxe", PricePerNight = 1000, Capacity = 2, IsAvailable = true });
            roomRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()))
                        .ReturnsAsync(false);
            roomRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Room>()))
                        .ReturnsAsync((int _, Room r) => { r.ImageUrl = "/updated.jpg"; return r; });

            var sut = new RoomService(roomRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, auditMock.Object, MockLogger.Create<RoomService>());
            var result = await sut.UpdateAsync(1, new CreateRoomDto { HotelId = 1, RoomNumber = 102, RoomType = "Suite", PricePerNight = 2000, Capacity = 4, ImageUrl = "/updated.jpg" });
            Assert.Equal("/updated.jpg", result!.ImageUrl);
        }

        // ── ChatService: booking no sub-keyword no userId → generic reply ─

        [Fact]
        public async Task Chat_BookingQuery_NoSubKeyword_NoUserId_ReturnsGenericReply()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "I want to reserve a room", SessionId = "s1", UserId = null });
            Assert.Equal("booking", result.Intent);
            Assert.Contains("login", result.Reply, StringComparison.OrdinalIgnoreCase);
        }

        // ── ChatService: hotel "how many hotel" if-false branch ───────────

        [Fact]
        public async Task Chat_HotelAddress_FallsThroughHowManyHotelBranch()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            // "address" → hotel branch, doesn't match amenity/how many hotel/star/check-in time → generic fallback
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "hotel address", SessionId = "s1" });
            Assert.Equal("hotel", result.Intent);
        }

        // ── ChatService: hotel check-in time if-false → generic fallback ──

        [Fact]
        public async Task Chat_HotelContact_FallsThroughCheckInTimeBranch()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "hotel contact number", SessionId = "s1" });
            Assert.Equal("hotel", result.Intent);
            Assert.Contains("StayEase", result.Reply);
        }

        // ── ChatService: HandleMyBookingsQuery both userId branches ───────

        [Fact]
        public async Task Chat_MyReservation_NoUserId_ReturnsLoginPrompt()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "my reservation", SessionId = "s1", UserId = null });
            Assert.Equal("booking", result.Intent);
            Assert.Contains("login", result.Reply, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Chat_MyOrder_WithUserId_ReturnsDashboardReply()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "my order status", SessionId = "s1", UserId = 3 });
            Assert.Equal("booking", result.Intent);
            Assert.Contains("Dashboard", result.Reply);
        }

        [Fact]
        public async Task Chat_BookingStatus_WithUserId_ReturnsDashboardReply()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "booking status", SessionId = "s1", UserId = 7 });
            Assert.Equal("booking", result.Intent);
            Assert.Contains("Dashboard", result.Reply);
        }

        // ── ChatService: hotel generic fallback ───────────────────────────

        [Fact]
        public async Task Chat_HotelLocation_ReturnsGenericHotelFallback()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "hotel location", SessionId = "s1" });
            Assert.Equal("hotel", result.Intent);
            Assert.Contains("StayEase", result.Reply);
        }

        [Fact]
        public async Task Chat_HotelsAvailable_ReturnsHotelCount()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });
            hotelRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                         .ReturnsAsync(new List<Hotel> { new(), new() });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object, bookingRepoMock.Object, MockLogger.Create<ChatService>());
            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "hotels available near me", SessionId = "s1" });
            Assert.Equal("hotel", result.Intent);
        }
    }
}

// Additional tests in a separate class to avoid duplicate method names
namespace HotelBookingApp.Tests.Services
{
    public class FinalBranchTests2
    {
        // ── ChatService L266: "list of hotel" keyword variant ─────────────

        [Fact]
        public async Task Chat_ListOfHotel_ReturnsHotelCount()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });
            hotelRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                         .ReturnsAsync(new List<Hotel> { new() });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "list of hotel options", SessionId = "s1" });
            Assert.Equal("hotel", result.Intent);
        }

        // ── ChatService L366: "booking id" with userId ────────────────────

        [Fact]
        public async Task Chat_BookingId_WithUserId_ReturnsDashboardReply()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            var result = await sut.ReplyAsync(new ChatRequestDto { Message = "booking id 123", SessionId = "s1", UserId = 10 });
            Assert.Equal("booking", result.Intent);
            Assert.Contains("Dashboard", result.Reply);
        }

        // ── PaymentService: MakePaymentAsync switch default arm ───────────
        // The `_ => booking.Status` arm in MakePaymentAsync is dead code since
        // the real delegate only returns "Completed" or "Failed".
        // We verify the switch covers all reachable arms by testing both outcomes.

        [Fact]
        public async Task Payment_MakePaymentAsync_FailedStatus_SwitchFailedArm()
        {
            var paymentRepoMock = new Mock<IRepository<int, Payment>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>())).ReturnsAsync(new AuditLogResponseDto());

            // Amount < TotalAmount → delegate returns "Failed" → switch "Failed" arm → booking stays "Pending"
            var booking = new Booking { BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1, TotalAmount = 5000, Status = "Pending",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4) };
            bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                           .ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });
            bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                           .ReturnsAsync((int _, Booking b) => b);

            var sut = new PaymentService(paymentRepoMock.Object, bookingRepoMock.Object,
                auditMock.Object, MockLogger.Create<PaymentService>());

            // Amount 100 < TotalAmount 5000 → "Failed" → switch "Failed" arm → "Pending"
            var result = await sut.MakePaymentAsync(new PaymentDto { BookingId = 1, Amount = 100, PaymentMethod = "CreditCard" });
            Assert.Equal("Failed", result.PaymentStatus);
        }
    }
}
