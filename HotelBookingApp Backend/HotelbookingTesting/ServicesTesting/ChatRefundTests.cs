using HotelBookingApp.Delegates;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelbookingTesting.ServicesTesting;

/// <summary>
/// Tests for the refund calculator integration in ChatService.
/// Verifies chatbot replies match AppDelegateFactory.StandardRefundPolicy:
///   >= 24 hours → 80% refund
///   <  24 hours → No refund
/// </summary>
public class ChatRefundTests
{
    private static ChatService MakeSvc(
        Mock<IRepository<int, ChatMessage>>? chatRepo    = null,
        Mock<IRepository<int, Hotel>>?       hotelRepo   = null,
        Mock<IRepository<int, Booking>>?     bookingRepo = null)
    {
        chatRepo    ??= new Mock<IRepository<int, ChatMessage>>();
        hotelRepo   ??= new Mock<IRepository<int, Hotel>>();
        bookingRepo ??= new Mock<IRepository<int, Booking>>();
        chatRepo.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync((ChatMessage m) => m);
        return new ChatService(chatRepo.Object, hotelRepo.Object, bookingRepo.Object,
                               new Mock<ILogger<ChatService>>().Object);
    }

    // ── Refund calculator intent ──────────────────────────────────────────

    [Theory]
    [InlineData("calculate refund")]
    [InlineData("how much refund")]
    [InlineData("refund amount")]
    [InlineData("refund calculator")]
    [InlineData("refund if i cancel")]
    public async Task ChatService_RefundCalculator_ReturnsCancellationIntent(string msg)
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = msg });

        Assert.Equal("cancellation", result.Intent);
    }

    [Fact]
    public async Task ChatService_RefundCalculator_ReplyContains80Percent()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "calculate refund"
        });

        Assert.Contains("80%", result.Reply);
        Assert.Contains("24", result.Reply);
    }

    [Fact]
    public async Task ChatService_RefundCalculator_ReplyContainsExample()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "how much refund will I get"
        });

        Assert.Equal("cancellation", result.Intent);
        Assert.Contains("80%", result.Reply);
    }

    // ── Refund policy matches delegate ────────────────────────────────────

    [Fact]
    public async Task ChatService_RefundPolicy_ReplyMatchesDelegatePolicy()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "what is the refund policy"
        });

        Assert.Equal("cancellation", result.Intent);
        // Must say 80% (matching StandardRefundPolicy delegate)
        Assert.Contains("80%", result.Reply);
        // Must NOT say 100% or 50% (old incorrect policy)
        Assert.DoesNotContain("100%", result.Reply);
        Assert.DoesNotContain("50%", result.Reply);
    }

    [Fact]
    public async Task ChatService_RefundPolicy_ReplyMentions24Hours()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "refund policy"
        });

        Assert.Equal("cancellation", result.Intent);
        Assert.Contains("24", result.Reply);
    }

    [Fact]
    public async Task ChatService_MoneyBack_ReplyContainsRefundInfo()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "I want my money back"
        });

        Assert.Equal("cancellation", result.Intent);
        Assert.Contains("80%", result.Reply);
    }

    // ── How to cancel ─────────────────────────────────────────────────────

    [Fact]
    public async Task ChatService_HowToCancel_ReplyContainsRefundInfo()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "how to cancel and get refund"
            // "cancel" + "refund" → cancellation intent (no booking keywords)
        });

        Assert.Equal("cancellation", result.Intent);
        Assert.Contains("80%", result.Reply);
        Assert.Contains("24", result.Reply);
    }

    [Fact]
    public async Task ChatService_CancelSteps_ReplyContainsDashboardLink()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "steps to cancel please"
            // "cancel" → cancellation intent, no booking keywords
        });

        Assert.Equal("cancellation", result.Intent);
        Assert.Contains("Dashboard", result.Reply);
    }

    // ── Default cancellation summary ──────────────────────────────────────

    [Fact]
    public async Task ChatService_CancellationDefault_ReplyContainsPolicyAndLink()
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto
        {
            SessionId = "s1", Message = "cancel please"
        });

        Assert.Equal("cancellation", result.Intent);
        Assert.Contains("80%", result.Reply);
        Assert.Contains("Request Cancellation", result.Reply);
    }

    // ── AppDelegateFactory.StandardRefundPolicy consistency ───────────────

    [Fact]
    public void Delegate_StandardRefundPolicy_Over24Hours_Returns80Percent()
    {
        // 48 hours before check-in
        var refund = AppDelegateFactory.StandardRefundPolicy(5000m, 48);
        Assert.Equal(4000m, refund); // 80% of 5000
    }

    [Fact]
    public void Delegate_StandardRefundPolicy_Exactly24Hours_Returns80Percent()
    {
        var refund = AppDelegateFactory.StandardRefundPolicy(1000m, 24);
        Assert.Equal(800m, refund); // 80% of 1000
    }

    [Fact]
    public void Delegate_StandardRefundPolicy_Under24Hours_ReturnsZero()
    {
        var refund = AppDelegateFactory.StandardRefundPolicy(5000m, 12);
        Assert.Equal(0m, refund); // no refund
    }

    [Fact]
    public void Delegate_StandardRefundPolicy_ZeroHours_ReturnsZero()
    {
        var refund = AppDelegateFactory.StandardRefundPolicy(3000m, 0);
        Assert.Equal(0m, refund);
    }

    [Fact]
    public void Delegate_DescribeRefund_Over24Hours_Contains80Percent()
    {
        var desc = AppDelegateFactory.DescribeRefund(5000m, 48);
        Assert.Contains("80%", desc);
        Assert.Contains("4", desc); // refund amount present (4000)
    }

    [Fact]
    public void Delegate_DescribeRefund_Under24Hours_ContainsNoRefund()
    {
        var desc = AppDelegateFactory.DescribeRefund(5000m, 10);
        Assert.Contains("No refund", desc);
    }

    // ── New keywords trigger cancellation intent ──────────────────────────

    [Theory]
    [InlineData("calculate refund",    "cancellation")]
    [InlineData("how much refund",     "cancellation")]
    [InlineData("refund amount",       "cancellation")]
    [InlineData("refund calculator",   "cancellation")]
    [InlineData("cancellation policy", "cancellation")]
    [InlineData("cancel please",       "cancellation")]
    public async Task ChatService_NewCancellationKeywords_ReturnCancellationIntent(string msg, string expected)
    {
        var result = await MakeSvc().ReplyAsync(new ChatRequestDto { SessionId = "s1", Message = msg });
        Assert.Equal(expected, result.Intent);
    }
}
