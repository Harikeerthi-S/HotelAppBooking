using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    public class ChatServiceTests
    {
        private readonly Mock<IRepository<int, ChatMessage>> _chatRepoMock;
        private readonly Mock<IRepository<int, Hotel>>       _hotelRepoMock;
        private readonly Mock<IRepository<int, Booking>>     _bookingRepoMock;
        private readonly ChatService                         _sut;

        public ChatServiceTests()
        {
            _chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            _hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            _bookingRepoMock = new Mock<IRepository<int, Booking>>();
            _chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                         .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });
            _sut = new ChatService(_chatRepoMock.Object, _hotelRepoMock.Object,
                _bookingRepoMock.Object, MockLogger.Create<ChatService>());
        }

        private static ChatRequestDto Req(string msg, string session = "s1", int? userId = null) =>
            new() { Message = msg, SessionId = session, UserId = userId };

        // ── Greeting ─────────────────────────────────────────────────────

        [Theory]
        [InlineData("hello")]
        [InlineData("hi there")]
        [InlineData("hey")]
        [InlineData("good morning")]
        [InlineData("good evening")]
        [InlineData("start")]
        public async Task ReplyAsync_Greeting_ReturnsGreetingIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("greeting", result.Intent);
        }

        // ── Booking intents ───────────────────────────────────────────────

        [Theory]
        [InlineData("how to book a room")]
        [InlineData("steps to reserve")]
        [InlineData("guide to booking")]
        public async Task ReplyAsync_BookingHowTo_ReturnsBookingIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("booking", result.Intent);
        }

        [Theory]
        [InlineData("modify my booking")]
        [InlineData("change my reservation")]
        [InlineData("edit booking")]
        public async Task ReplyAsync_BookingModify_ReturnsBookingIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("booking", result.Intent);
        }

        [Theory]
        [InlineData("booking status")]
        [InlineData("is my booking pending")]
        [InlineData("confirmed booking")]
        public async Task ReplyAsync_BookingStatus_ReturnsBookingIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("booking", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_BookingWithUserId_ReturnsCountFromRepo()
        {
            _bookingRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                            .ReturnsAsync(new List<Booking> { new(), new() });

            var result = await _sut.ReplyAsync(Req("I want to book", userId: 1));
            Assert.Equal("booking", result.Intent);
            Assert.Contains("2", result.Reply);
        }

        [Fact]
        public async Task ReplyAsync_BookingWithUserId_RepoThrows_FallsThrough()
        {
            _bookingRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                            .ThrowsAsync(new Exception("DB error"));

            var result = await _sut.ReplyAsync(Req("I want to book", userId: 1));
            Assert.Equal("booking", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_BookingNoUserId_ReturnsGenericBookingReply()
        {
            var result = await _sut.ReplyAsync(Req("I want to book"));
            Assert.Equal("booking", result.Intent);
        }

        // ── Cancellation intents ──────────────────────────────────────────

        [Theory]
        [InlineData("cancellation policy")]
        [InlineData("refund amount calculator")]
        [InlineData("how much refund will I get")]
        public async Task ReplyAsync_CancellationQuery_ReturnsCancellationIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("cancellation", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_RefundKeyword_ReturnsCancellationIntent()
        {
            var result = await _sut.ReplyAsync(Req("I want my money back"));
            Assert.Equal("cancellation", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_HowToCancel_ReturnsCancellationIntent()
        {
            var result = await _sut.ReplyAsync(Req("steps to cancel refund request"));
            Assert.Equal("cancellation", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_DefaultCancellationPolicy_ReturnsCancellationIntent()
        {
            var result = await _sut.ReplyAsync(Req("cancellation"));
            Assert.Equal("cancellation", result.Intent);
        }

        // ── Payment intents ───────────────────────────────────────────────

        [Theory]
        [InlineData("payment methods")]
        [InlineData("how to pay")]
        [InlineData("payment option")]
        public async Task ReplyAsync_PaymentMethod_ReturnsPaymentIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("payment", result.Intent);
        }

        [Theory]
        [InlineData("payment failed")]
        [InlineData("payment not working")]
        [InlineData("payment error")]
        public async Task ReplyAsync_PaymentFailed_ReturnsPaymentIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("payment", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_GenericPayment_ReturnsPaymentIntent()
        {
            var result = await _sut.ReplyAsync(Req("what is the price"));
            Assert.Equal("payment", result.Intent);
        }

        // ── Hotel intents ─────────────────────────────────────────────────

        [Theory]
        [InlineData("hotel amenities")]
        [InlineData("hotel has wifi")]
        [InlineData("hotel has pool")]
        public async Task ReplyAsync_HotelAmenities_ReturnsHotelIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("hotel", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_HowManyHotels_ReturnsHotelIntent()
        {
            _hotelRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                          .ReturnsAsync(new List<Hotel> { new(), new(), new() });

            var result = await _sut.ReplyAsync(Req("how many hotels available"));
            Assert.Equal("hotel", result.Intent);
            Assert.Contains("3", result.Reply);
        }

        [Fact]
        public async Task ReplyAsync_HowManyHotels_RepoThrows_FallsThrough()
        {
            _hotelRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                          .ThrowsAsync(new Exception("DB error"));

            var result = await _sut.ReplyAsync(Req("list of hotels available"));
            Assert.Equal("hotel", result.Intent);
        }

        [Theory]
        [InlineData("star rating")]
        [InlineData("best hotel")]
        [InlineData("top hotel")]
        public async Task ReplyAsync_HotelRating_ReturnsHotelIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("hotel", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_HotelContactAddress_ReturnsHotelIntent()
        {
            var result = await _sut.ReplyAsync(Req("what is the hotel address and contact"));
            Assert.Equal("hotel", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_GenericHotelQuery_ReturnsHotelIntent()
        {
            var result = await _sut.ReplyAsync(Req("tell me about the hotel location"));
            Assert.Equal("hotel", result.Intent);
        }

        // ── My bookings ───────────────────────────────────────────────────

        [Fact]
        public async Task ReplyAsync_MyBookingNoUserId_ReturnsLoginPrompt()
        {
            var result = await _sut.ReplyAsync(Req("my booking"));
            Assert.Equal("booking", result.Intent);
            Assert.Contains("login", result.Reply, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReplyAsync_MyBookingWithUserId_ReturnsDashboardPrompt()
        {
            var result = await _sut.ReplyAsync(Req("my booking", userId: 5));
            Assert.Equal("booking", result.Intent);
            Assert.Contains("Dashboard", result.Reply);
        }

        // ── Support ───────────────────────────────────────────────────────

        [Theory]
        [InlineData("I need help")]
        [InlineData("support please")]
        [InlineData("I have a complaint")]
        public async Task ReplyAsync_SupportQuery_ReturnsSupportIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));
            Assert.Equal("support", result.Intent);
        }

        // ── Fallback ──────────────────────────────────────────────────────

        [Fact]
        public async Task ReplyAsync_UnknownMessage_ReturnsGeneralIntent()
        {
            var result = await _sut.ReplyAsync(Req("xyzzy random text 12345"));
            Assert.Equal("general", result.Intent);
        }

        // ── Persistence ───────────────────────────────────────────────────

        [Fact]
        public async Task ReplyAsync_PersistsBothMessages()
        {
            await _sut.ReplyAsync(Req("hello"));
            _chatRepoMock.Verify(r => r.AddAsync(It.Is<ChatMessage>(m => m.Sender == "user")), Times.Once);
            _chatRepoMock.Verify(r => r.AddAsync(It.Is<ChatMessage>(m => m.Sender == "bot")),  Times.Once);
        }

        [Fact]
        public async Task ReplyAsync_PersistenceThrows_StillReturnsReply()
        {
            _chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                         .ThrowsAsync(new Exception("Table missing"));

            var result = await _sut.ReplyAsync(Req("hello"));
            Assert.Equal("greeting", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_ReturnsCorrectSessionId()
        {
            _chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                         .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var result = await _sut.ReplyAsync(Req("hello", "session-abc"));
            Assert.Equal("session-abc", result.SessionId);
        }

        // ── GetHistoryAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetHistoryAsync_ReturnsOrderedMessages()
        {
            _chatRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                         .ReturnsAsync(new List<ChatMessage>
                         {
                             new() { ChatMessageId = 2, SessionId = "s1", Sender = "bot",  Message = "Hi!", CreatedAt = DateTime.UtcNow },
                             new() { ChatMessageId = 1, SessionId = "s1", Sender = "user", Message = "Hello", CreatedAt = DateTime.UtcNow.AddSeconds(-5) }
                         });

            var result = (await _sut.GetHistoryAsync("s1")).ToList();
            Assert.Equal(2, result.Count);
            Assert.Equal("user", result[0].Sender);
        }

        [Fact]
        public async Task GetHistoryAsync_RepoThrows_ReturnsEmpty()
        {
            _chatRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                         .ThrowsAsync(new Exception("Table missing"));

            var result = await _sut.GetHistoryAsync("s1");
            Assert.Empty(result);
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsMessages()
        {
            _chatRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                         .ReturnsAsync(new List<ChatMessage>
                         {
                             new() { ChatMessageId = 1, UserId = 1, SessionId = "s1", Sender = "user", Message = "Hello", CreatedAt = DateTime.UtcNow }
                         });

            var result = (await _sut.GetByUserAsync(1)).ToList();
            Assert.Single(result);
        }

        [Fact]
        public async Task GetByUserAsync_RepoThrows_ReturnsEmpty()
        {
            _chatRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                         .ThrowsAsync(new Exception("Table missing"));

            var result = await _sut.GetByUserAsync(1);
            Assert.Empty(result);
        }

        // ── ClearSessionAsync ─────────────────────────────────────────────

        [Fact]
        public async Task ClearSessionAsync_DeletesAllMessages_ReturnsTrue()
        {
            _chatRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                         .ReturnsAsync(new List<ChatMessage>
                         {
                             new() { ChatMessageId = 1, SessionId = "s1" },
                             new() { ChatMessageId = 2, SessionId = "s1" }
                         });
            _chatRepoMock.Setup(r => r.DeleteAsync(It.IsAny<int>())).ReturnsAsync(new ChatMessage());

            Assert.True(await _sut.ClearSessionAsync("s1"));
            _chatRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ClearSessionAsync_RepoThrows_StillReturnsTrue()
        {
            _chatRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                         .ThrowsAsync(new Exception("Table missing"));

            Assert.True(await _sut.ClearSessionAsync("s1"));
        }
    }
}
