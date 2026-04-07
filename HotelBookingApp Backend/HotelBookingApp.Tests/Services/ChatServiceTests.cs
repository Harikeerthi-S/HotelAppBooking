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
        private readonly Mock<IRepository<int, Hotel>> _hotelRepoMock;
        private readonly Mock<IRepository<int, Booking>> _bookingRepoMock;
        private readonly ChatService _sut;

        public ChatServiceTests()
        {
            _chatRepoMock = new Mock<IRepository<int, ChatMessage>>();
            _hotelRepoMock = new Mock<IRepository<int, Hotel>>();
            _bookingRepoMock = new Mock<IRepository<int, Booking>>();

            _chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                         .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            _sut = new ChatService(_chatRepoMock.Object, _hotelRepoMock.Object,
                _bookingRepoMock.Object, MockLogger.Create<ChatService>());
        }

        private static ChatRequestDto Req(string msg, string session = "s1") =>
            new() { Message = msg, SessionId = session, UserId = null };

        // ── ReplyAsync — intent detection ─────────────────────────────────

        [Theory]
        [InlineData("hello", "greeting")]
        [InlineData("hi there", "greeting")]
        [InlineData("good morning", "greeting")]
        public async Task ReplyAsync_Greeting_ReturnsGreetingIntent(string msg, string expectedIntent)
        {
            var result = await _sut.ReplyAsync(Req(msg));

            Assert.Equal(expectedIntent, result.Intent);
            Assert.Contains("Welcome", result.Reply);
        }

        [Theory]
        [InlineData("how to book a room")]
        [InlineData("I want to make a reservation")]
        [InlineData("check-in date")]
        public async Task ReplyAsync_BookingQuery_ReturnsBookingIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));

            Assert.Equal("booking", result.Intent);
        }

        [Theory]
        [InlineData("cancellation policy")]
        [InlineData("how much refund will I get")]
        [InlineData("refund amount calculator")]
        public async Task ReplyAsync_CancellationQuery_ReturnsCancellationIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));

            Assert.Equal("cancellation", result.Intent);
        }

        [Theory]
        [InlineData("payment methods")]
        [InlineData("how to pay")]
        [InlineData("credit card")]
        public async Task ReplyAsync_PaymentQuery_ReturnsPaymentIntent(string msg)
        {
            var result = await _sut.ReplyAsync(Req(msg));

            Assert.Equal("payment", result.Intent);
        }

        [Theory]
        [InlineData("hotel amenities")]
        [InlineData("star rating")]
        [InlineData("hotel location address")]
        public async Task ReplyAsync_HotelQuery_ReturnsHotelIntent(string msg)
        {
            _hotelRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                          .ReturnsAsync(new List<Hotel>());

            var result = await _sut.ReplyAsync(Req(msg));

            Assert.Equal("hotel", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_UnknownMessage_ReturnsGeneralIntent()
        {
            var result = await _sut.ReplyAsync(Req("xyzzy random text"));

            Assert.Equal("general", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_HelpQuery_ReturnsSupportIntent()
        {
            var result = await _sut.ReplyAsync(Req("I need help with an issue"));

            Assert.Equal("support", result.Intent);
        }

        [Fact]
        public async Task ReplyAsync_PersistsUserAndBotMessages()
        {
            await _sut.ReplyAsync(Req("hello"));

            _chatRepoMock.Verify(r => r.AddAsync(It.Is<ChatMessage>(m => m.Sender == "user")), Times.Once);
            _chatRepoMock.Verify(r => r.AddAsync(It.Is<ChatMessage>(m => m.Sender == "bot")), Times.Once);
        }

        [Fact]
        public async Task ReplyAsync_ReturnsCorrectSessionId()
        {
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
                             new() { ChatMessageId = 2, SessionId = "s1", Sender = "bot", Message = "Hi!", CreatedAt = DateTime.UtcNow },
                             new() { ChatMessageId = 1, SessionId = "s1", Sender = "user", Message = "Hello", CreatedAt = DateTime.UtcNow.AddSeconds(-5) }
                         });

            var result = (await _sut.GetHistoryAsync("s1")).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("user", result[0].Sender); // ordered by CreatedAt ascending
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
            _chatRepoMock.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                         .ReturnsAsync(new ChatMessage());

            var result = await _sut.ClearSessionAsync("s1");

            Assert.True(result);
            _chatRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Exactly(2));
        }

        // ── GetByUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsUserMessages()
        {
            _chatRepoMock.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                         .ReturnsAsync(new List<ChatMessage>
                         {
                             new() { ChatMessageId = 1, UserId = 1, SessionId = "s1", Sender = "user", Message = "Hello", CreatedAt = DateTime.UtcNow }
                         });

            var result = (await _sut.GetByUserAsync(1)).ToList();

            Assert.Single(result);
        }
    }
}
