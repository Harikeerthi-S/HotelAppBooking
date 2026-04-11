using HotelBookingApp.Context;
using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;
using HotelBookingApp.Services;
using HotelBookingApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotelBookingApp.Tests.Services
{
    /// <summary>
    /// Targets the remaining uncovered lines identified from coverage report:
    ///  - CancellationService Log() catch block (lines 51-54)
    ///  - ChatService check-in/out time branch (lines 266-273)
    ///  - ChatService "how to cancel" branch (lines 346-351)
    ///  - ChatService HandleMyBookingsQuery userId=HasValue return (line 364)
    ///  - PaymentService MakePaymentAsync switch default arm (line 101)
    ///  - PaymentService GetRevenueAsync byMonth grouping (lines 324, 337)
    /// </summary>
    public class CoverageFinalTests : IDisposable
    {
        private readonly HotelBookingContext _context;

        public CoverageFinalTests()
        {
            var options = new DbContextOptionsBuilder<HotelBookingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new HotelBookingContext(options);
        }

        public void Dispose() => _context.Dispose();

        // ─────────────────────────────────────────────────────────────────
        // CancellationService: Log() catch block (lines 51-54)
        // The Task.Run audit log fires-and-forgets. To cover the catch block
        // we need the IAuditLogService inside the scope to throw.
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CancellationService_Log_AuditThrows_CatchBlockCovered()
        {
            // Build a scope factory where IAuditLogService throws
            var services = new ServiceCollection();
            services.AddDbContext<HotelBookingContext>(o =>
                o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddScoped<IAuditLogService>(_ => throw new InvalidOperationException("audit boom"));
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var walletMock = new Mock<IWalletService>();
            walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
                It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(new WalletResponseDto());

            var sut = new CancellationService(
                _context, walletMock.Object, scopeFactory,
                MockLogger.Create<CancellationService>());

            // Seed a booking
            var booking = new Booking
            {
                UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
                TotalAmount = 5000, Status = "Confirmed",
                CheckIn  = DateTime.UtcNow.AddHours(130),
                CheckOut = DateTime.UtcNow.AddHours(154)
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // CreateAsync calls Log() which fires Task.Run → audit throws → catch block hit
            var result = await sut.CreateAsync(new CreateCancellationDto
            {
                BookingId = booking.BookingId, Reason = "Test audit catch"
            });

            // Give the background Task.Run a moment to complete
            await Task.Delay(200);

            Assert.Equal("Pending", result.Status);
            Assert.Equal(5000m, result.RefundAmount); // 100% refund
        }

        [Fact]
        public async Task CancellationService_UpdateStatus_Log_AuditThrows_CatchBlockCovered()
        {
            // Same pattern for UpdateStatusAsync's Log() call
            var services = new ServiceCollection();
            services.AddDbContext<HotelBookingContext>(o =>
                o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddScoped<IAuditLogService>(_ => throw new InvalidOperationException("audit boom"));
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            var walletMock = new Mock<IWalletService>();
            walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(),
                It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(new WalletResponseDto());

            var sut = new CancellationService(
                _context, walletMock.Object, scopeFactory,
                MockLogger.Create<CancellationService>());

            var booking = new Booking
            {
                UserId = 1, HotelId = 1, RoomId = 1, NumberOfRooms = 1,
                TotalAmount = 2000, Status = "Confirmed",
                CheckIn  = DateTime.UtcNow.AddHours(48),
                CheckOut = DateTime.UtcNow.AddHours(72)
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var cancellation = new Cancellation
            {
                BookingId = booking.BookingId, Reason = "Test",
                RefundAmount = 500, Status = "Pending",
                CancellationDate = DateTime.UtcNow
            };
            _context.Cancellations.Add(cancellation);
            await _context.SaveChangesAsync();

            var result = await sut.UpdateStatusAsync(cancellation.CancellationId, "Rejected");
            await Task.Delay(200);

            Assert.Equal("Rejected", result.Status);
        }

        // ─────────────────────────────────────────────────────────────────
        // ChatService: check-in time / check-out time branch (lines 266-273)
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Chat_CheckInTime_ReturnsHotelIntent()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            // "hotel" triggers hotel branch; "check-in time" sub-branch inside HandleHotelQueryAsync
            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "hotel check-in time please", SessionId = "s1"
            });

            Assert.Equal("hotel", result.Intent);
            Assert.Contains("2:00 PM", result.Reply);
        }

        [Fact]
        public async Task Chat_CheckOutTime_ReturnsHotelIntent()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            // "hotel" triggers hotel branch; "check-out time" sub-branch inside HandleHotelQueryAsync
            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "hotel check-out time", SessionId = "s1"
            });

            Assert.Equal("hotel", result.Intent);
            Assert.Contains("12:00 PM", result.Reply);
        }

        // ─────────────────────────────────────────────────────────────────
        // ChatService: "how to cancel" / "cancel my" branch (lines 346-351)
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Chat_HowToCancel_ReturnsCancellationSteps()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            // "cancel" triggers cancellation branch; "how to cancel" sub-branch inside HandleCancellationPolicy
            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "how to cancel my stay", SessionId = "s1"
            });

            Assert.Equal("cancellation", result.Intent);
            Assert.Contains("Dashboard", result.Reply);
        }

        [Fact]
        public async Task Chat_CancelMy_ReturnsCancellationSteps()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            // "cancel my" triggers the how-to-cancel sub-branch
            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "cancel my stay please", SessionId = "s1"
            });

            Assert.Equal("cancellation", result.Intent);
            Assert.Contains("wallet", result.Reply, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Chat_StepsToCancel_ReturnsCancellationSteps()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            // "steps to cancel" triggers the how-to-cancel sub-branch
            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "steps to cancel my stay", SessionId = "s1"
            });

            Assert.Equal("cancellation", result.Intent);
        }

        // ─────────────────────────────────────────────────────────────────
        // ChatService: HandleMyBookingsQuery with userId (line 364-365)
        // Ensure the full multi-line return string is covered
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Chat_MyBooking_WithUserId_ReturnsYourBookingsDashboard()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            // "my reservation" with userId → HandleMyBookingsQuery → userId.HasValue = true
            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "my reservation details", SessionId = "s1", UserId = 42
            });

            Assert.Equal("booking", result.Intent);
            Assert.Contains("Dashboard", result.Reply);
            Assert.Contains("Bookings", result.Reply);
        }

        [Fact]
        public async Task Chat_MyOrder_WithUserId_FullReplyContent()
        {
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "my order", SessionId = "s1", UserId = 7
            });

            Assert.Equal("booking", result.Intent);
            // Verify the full multi-line return is executed (covers line 365)
            Assert.Contains("cancellations", result.Reply, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("check-in", result.Reply, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Chat_MyOrder_NoUserId_ReturnsLoginPrompt()
        {
            // "my order" with no userId → HandleMyBookingsQuery(null) → !userId.HasValue = true → login prompt
            var chatRepoMock    = new Mock<IRepository<int, ChatMessage>>();
            var hotelRepoMock   = new Mock<IRepository<int, Hotel>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            chatRepoMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>()))
                        .ReturnsAsync((ChatMessage m) => { m.ChatMessageId = 1; return m; });

            var sut = new ChatService(chatRepoMock.Object, hotelRepoMock.Object,
                bookingRepoMock.Object, MockLogger.Create<ChatService>());

            var result = await sut.ReplyAsync(new ChatRequestDto
            {
                Message = "my order", SessionId = "s1", UserId = null
            });

            Assert.Equal("booking", result.Intent);
            Assert.Contains("login", result.Reply, StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────────
        // PaymentService: MakePaymentAsync switch default arm (line 101)
        // The `_ => booking.Status` arm is hit when status is neither
        // "Completed" nor "Failed". The real delegate only returns those two,
        // so we inject a custom delegate via a subclass to force the default arm.
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Payment_MakePaymentAsync_DefaultSwitchArm_BookingStatusPreserved()
        {
            var paymentRepoMock = new Mock<IRepository<int, Payment>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                     .ReturnsAsync(new AuditLogResponseDto());

            // Booking with status "Confirmed" — the default arm preserves this
            var booking = new Booking
            {
                BookingId = 1, UserId = 1, HotelId = 1, RoomId = 1,
                TotalAmount = 2000, Status = "Confirmed",
                CheckIn = DateTime.Today.AddDays(2), CheckOut = DateTime.Today.AddDays(4)
            };
            bookingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);
            paymentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Payment>());
            paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                           .ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });

            Booking? capturedBookingCopy = null;
            bookingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Booking>()))
                           .Callback<int, Booking>((_, b) => capturedBookingCopy = b)
                           .ReturnsAsync((int _, Booking b) => b);

            // Inject a custom resolver that returns "Pending" → hits the `_ => booking.Status` arm
            Delegates.PaymentStatusResolverDelegate pendingResolver = (method, amount, total) => "Pending";

            var sut = new PaymentService(
                paymentRepoMock.Object, bookingRepoMock.Object,
                auditMock.Object, MockLogger.Create<PaymentService>(),
                statusResolver: pendingResolver);

            var result = await sut.MakePaymentAsync(new PaymentDto
            {
                BookingId = 1, Amount = 2000, PaymentMethod = "CreditCard"
            });

            Assert.Equal("Pending", result.PaymentStatus);
            // The default arm preserves booking.Status = "Confirmed"
            Assert.Equal("Confirmed", capturedBookingCopy?.Status);
        }

        // ─────────────────────────────────────────────────────────────────
        // PaymentService: GetRevenueAsync byMonth grouping (lines 324, 337)
        // Need multiple completed payments in the same month to exercise grouping
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Payment_GetRevenueAsync_MultiplePaymentsSameMonth_ByMonthGrouped()
        {
            var paymentRepoMock = new Mock<IRepository<int, Payment>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                     .ReturnsAsync(new AuditLogResponseDto());

            var sameMonth = new DateTime(2025, 6, 15);
            paymentRepoMock.Setup(r => r.GetAllAsync())
                           .ReturnsAsync(new List<Payment>
                           {
                               new() { PaymentId = 1, BookingId = 1, Amount = 1000, PaymentMethod = "CreditCard",  PaymentStatus = "Completed", CreatedAt = sameMonth },
                               new() { PaymentId = 2, BookingId = 2, Amount = 2000, PaymentMethod = "DebitCard",   PaymentStatus = "Completed", CreatedAt = sameMonth.AddDays(5) },
                               new() { PaymentId = 3, BookingId = 3, Amount = 500,  PaymentMethod = "NetBanking",  PaymentStatus = "Completed", CreatedAt = sameMonth.AddDays(10) },
                               new() { PaymentId = 4, BookingId = 4, Amount = 300,  PaymentMethod = "CreditCard",  PaymentStatus = "Completed", CreatedAt = new DateTime(2025, 7, 1) }
                           });

            var sut = new PaymentService(paymentRepoMock.Object, bookingRepoMock.Object,
                auditMock.Object, MockLogger.Create<PaymentService>());

            var result = await sut.GetRevenueAsync();

            // 3 payments in June + 1 in July → 2 month groups
            Assert.Equal(2, result.ByMonth.Count);
            Assert.Equal(3500m, result.ByMonth[0].Amount); // June: 1000+2000+500
            Assert.Equal(3, result.ByMonth[0].Count);
            Assert.Equal(300m, result.ByMonth[1].Amount);  // July
            Assert.Equal("Jun 2025", result.ByMonth[0].MonthName);
            Assert.Equal("Jul 2025", result.ByMonth[1].MonthName);
        }

        [Fact]
        public async Task Payment_GetRevenueAsync_MultipleMethodsSameMonth_ByMethodGrouped()
        {
            var paymentRepoMock = new Mock<IRepository<int, Payment>>();
            var bookingRepoMock = new Mock<IRepository<int, Booking>>();
            var auditMock       = new Mock<IAuditLogService>();
            auditMock.Setup(a => a.CreateAsync(It.IsAny<CreateAuditLogDto>()))
                     .ReturnsAsync(new AuditLogResponseDto());

            var date = new DateTime(2025, 3, 10);
            paymentRepoMock.Setup(r => r.GetAllAsync())
                           .ReturnsAsync(new List<Payment>
                           {
                               new() { PaymentId = 1, BookingId = 1, Amount = 1000, PaymentMethod = "CreditCard", PaymentStatus = "Completed", CreatedAt = date },
                               new() { PaymentId = 2, BookingId = 2, Amount = 1500, PaymentMethod = "CreditCard", PaymentStatus = "Completed", CreatedAt = date.AddDays(1) },
                               new() { PaymentId = 3, BookingId = 3, Amount = 800,  PaymentMethod = "DebitCard",  PaymentStatus = "Completed", CreatedAt = date.AddDays(2) }
                           });

            var sut = new PaymentService(paymentRepoMock.Object, bookingRepoMock.Object,
                auditMock.Object, MockLogger.Create<PaymentService>());

            var result = await sut.GetRevenueAsync();

            Assert.Equal(2, result.ByMethod.Count);
            // CreditCard has higher total → comes first (ordered by descending amount)
            Assert.Equal("CreditCard", result.ByMethod[0].PaymentMethod);
            Assert.Equal(2500m, result.ByMethod[0].Amount);
            Assert.Equal(2, result.ByMethod[0].Count);
            Assert.Equal("DebitCard", result.ByMethod[1].PaymentMethod);
            Assert.Equal(800m, result.ByMethod[1].Amount);
        }
    }
}
