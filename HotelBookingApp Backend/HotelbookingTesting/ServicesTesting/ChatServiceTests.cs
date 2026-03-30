using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

public class ChatServiceTests
{
    private readonly Mock<IRepository<int, ChatMessage>> _chatRepo    = new();
    private readonly Mock<IRepository<int, Hotel>>       _hotelRepo   = new();
    private readonly Mock<IRepository<int, Booking>>     _bookingRepo = new();
    private readonly Mock<ILogger<ChatService>>          _logger      = new();

    private ChatService CreateService() =>
        new(_chatRepo.Object, _hotelRepo.Object, _bookingRepo.Object, _logger.Object);

    private static ChatRequestDto Req(string msg, int? userId = null) => new()
    {
        SessionId = "test-session",
        Message   = msg,
        UserId    = userId
    };

    // ── ReplyAsync — intent detection ─────────────────────────────────────

    [Theory]
    [InlineData("hello",       "greeting")]
    [InlineData("hi there",    "greeting")]
    [InlineData("good morning","greeting")]
    public async Task ReplyAsync_GreetingMessage_ReturnsGreetingIntent(string msg, string expectedIntent)
    {
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);

        var result = await CreateService().ReplyAsync(Req(msg));

        Assert.Equal(expectedIntent, result.Intent);
        Assert.Contains("StayEase", result.Reply);
    }

    [Theory]
    [InlineData("how to book a room",  "booking")]
    [InlineData("I want to reserve",   "booking")]
    [InlineData("booking status",      "booking")]
    public async Task ReplyAsync_BookingMessage_ReturnsBookingIntent(string msg, string expectedIntent)
    {
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);

        var result = await CreateService().ReplyAsync(Req(msg));

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Theory]
    [InlineData("cancellation policy", "cancellation")]
    [InlineData("how to cancel",       "cancellation")]
    [InlineData("refund policy",       "cancellation")]
    public async Task ReplyAsync_CancellationMessage_ReturnsCancellationIntent(string msg, string expectedIntent)
    {
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);

        var result = await CreateService().ReplyAsync(Req(msg));

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Theory]
    [InlineData("payment methods", "payment")]
    [InlineData("how to pay",      "payment")]
    [InlineData("payment failed",  "payment")]
    public async Task ReplyAsync_PaymentMessage_ReturnsPaymentIntent(string msg, string expectedIntent)
    {
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);

        var result = await CreateService().ReplyAsync(Req(msg));

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Theory]
    [InlineData("hotel amenities",  "hotel")]
    [InlineData("star rating",      "hotel")]
    [InlineData("hotel facilities", "hotel")]   // avoids "check in" which triggers booking intent
    public async Task ReplyAsync_HotelMessage_ReturnsHotelIntent(string msg, string expectedIntent)
    {
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);
        _hotelRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Hotel, bool>>>()))
                  .ReturnsAsync(new List<Hotel>());

        var result = await CreateService().ReplyAsync(Req(msg));

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Fact]
    public async Task ReplyAsync_UnknownMessage_ReturnsGeneralIntent()
    {
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);

        var result = await CreateService().ReplyAsync(Req("xyzzy random text"));

        Assert.Equal("general", result.Intent);
    }

    [Fact]
    public async Task ReplyAsync_WithUserId_IncludesBookingCount()
    {
        var bookings = new List<Booking>
        {
            new() { BookingId = 1, UserId = 5, Status = "Confirmed" },
            new() { BookingId = 2, UserId = 5, Status = "Pending"   }
        };

        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);
        _bookingRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                    .ReturnsAsync(bookings);

        var result = await CreateService().ReplyAsync(Req("my bookings", userId: 5));

        Assert.Equal("booking", result.Intent);
        Assert.Contains("2", result.Reply);
    }

    [Fact]
    public async Task ReplyAsync_DbFailure_StillReturnsReply()
    {
        // Simulate DB table not existing
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ThrowsAsync(new Exception("Table does not exist"));

        var result = await CreateService().ReplyAsync(Req("hello"));

        // Should not throw — graceful degradation
        Assert.NotNull(result);
        Assert.NotEmpty(result.Reply);
    }

    [Fact]
    public async Task ReplyAsync_SetsSessionId()
    {
        _chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                 .ReturnsAsync((ChatMessage m) => m);

        var result = await CreateService().ReplyAsync(Req("hi", userId: 1));

        Assert.Equal("test-session", result.SessionId);
    }

    // ── GetHistoryAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_ReturnsOrderedMessages()
    {
        var msgs = new List<ChatMessage>
        {
            new() { ChatMessageId = 2, SessionId = "s1", Sender = "bot",  Message = "Hi!", CreatedAt = DateTime.UtcNow.AddSeconds(1) },
            new() { ChatMessageId = 1, SessionId = "s1", Sender = "user", Message = "Hello", CreatedAt = DateTime.UtcNow }
        };

        _chatRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                 .ReturnsAsync(msgs);

        var result = (await CreateService().GetHistoryAsync("s1")).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("user", result[0].Sender); // ordered by CreatedAt ascending
    }

    [Fact]
    public async Task GetHistoryAsync_DbFailure_ReturnsEmpty()
    {
        _chatRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                 .ThrowsAsync(new Exception("DB error"));

        var result = await CreateService().GetHistoryAsync("session-x");

        Assert.Empty(result);
    }

    // ── ClearSessionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ClearSessionAsync_DeletesAllMessages_ReturnsTrue()
    {
        var msgs = new List<ChatMessage>
        {
            new() { ChatMessageId = 1, SessionId = "s1" },
            new() { ChatMessageId = 2, SessionId = "s1" }
        };

        _chatRepo.Setup(r => r.FindAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>()))
                 .ReturnsAsync(msgs);
        _chatRepo.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                 .ReturnsAsync((int id) => msgs.FirstOrDefault(m => m.ChatMessageId == id));

        var result = await CreateService().ClearSessionAsync("s1");

        Assert.True(result);
        _chatRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Exactly(2));
    }
}
