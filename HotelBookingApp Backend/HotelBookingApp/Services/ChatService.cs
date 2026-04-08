using HotelBookingApp.Interfaces.IRepositories;
using HotelBookingApp.Interfaces.IServices;
using HotelBookingApp.Models;
using HotelBookingApp.Models.Dtos;

namespace HotelBookingApp.Services
{
    /// <summary>
    /// Rule-based AI chatbot — handles booking queries, cancellation policies,
    /// and hotel details. No external AI dependency required.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IRepository<int, ChatMessage> _chatRepo;
        private readonly IRepository<int, Hotel>       _hotelRepo;
        private readonly IRepository<int, Booking>     _bookingRepo;
        private readonly ILogger<ChatService>          _logger;

        public ChatService(
            IRepository<int, ChatMessage> chatRepo,
            IRepository<int, Hotel>       hotelRepo,
            IRepository<int, Booking>     bookingRepo,
            ILogger<ChatService>          logger)
        {
            _chatRepo    = chatRepo;
            _hotelRepo   = hotelRepo;
            _bookingRepo = bookingRepo;
            _logger      = logger;
        }

        // ── REPLY ─────────────────────────────────────────────────────────
        public async Task<ChatResponseDto> ReplyAsync(ChatRequestDto request)
        {
            _logger.LogInformation("Chat message from Session:{SessionId}", request.SessionId);

            // Persist user message — gracefully skip if table doesn't exist yet
            try
            {
                await _chatRepo.AddAsync(new ChatMessage
                {
                    UserId    = request.UserId,
                    SessionId = request.SessionId,
                    Sender    = "user",
                    Message   = request.Message.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatMessages table may not exist yet — skipping persistence");
            }

            var (reply, intent) = await BuildReplyAsync(request.Message.Trim(), request.UserId);

            // Persist bot reply — gracefully skip if table doesn't exist yet
            try
            {
                await _chatRepo.AddAsync(new ChatMessage
                {
                    UserId    = request.UserId,
                    SessionId = request.SessionId,
                    Sender    = "bot",
                    Message   = reply,
                    Intent    = intent,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatMessages table may not exist yet — skipping persistence");
            }

            return new ChatResponseDto
            {
                SessionId = request.SessionId,
                Reply     = reply,
                Intent    = intent,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ── HISTORY ───────────────────────────────────────────────────────
        public async Task<IEnumerable<ChatHistoryDto>> GetHistoryAsync(string sessionId)
        {
            try
            {
                var msgs = await _chatRepo.FindAllAsync(m => m.SessionId == sessionId);
                return msgs.OrderBy(m => m.CreatedAt).Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatMessages table may not exist yet");
                return Enumerable.Empty<ChatHistoryDto>();
            }
        }

        public async Task<IEnumerable<ChatHistoryDto>> GetByUserAsync(int userId)
        {
            try
            {
                var msgs = await _chatRepo.FindAllAsync(m => m.UserId == userId);
                return msgs.OrderByDescending(m => m.CreatedAt).Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatMessages table may not exist yet");
                return Enumerable.Empty<ChatHistoryDto>();
            }
        }

        public async Task<bool> ClearSessionAsync(string sessionId)
        {
            try
            {
                var msgs = await _chatRepo.FindAllAsync(m => m.SessionId == sessionId);
                foreach (var m in msgs)
                    await _chatRepo.DeleteAsync(m.ChatMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatMessages table may not exist yet");
            }
            return true;
        }

        // ── INTENT ENGINE ─────────────────────────────────────────────────
        private async Task<(string reply, string intent)> BuildReplyAsync(string msg, int? userId)
        {
            var lower = msg.ToLower();

            // ── GREETING ──────────────────────────────────────────────────
            if (ContainsAny(lower, "hello", "hi", "hey", "good morning", "good evening", "start"))
                return (
                    "👋 Hello! Welcome to StayEase support. I can help you with:\n" +
                    "• 📅 **Booking queries** — how to book, check-in/out, modify\n" +
                    "• ❌ **Cancellation policies** — refunds, timelines\n" +
                    "• 🏨 **Hotel details** — amenities, locations, ratings\n\n" +
                    "What can I help you with today?",
                    "greeting");

            // ── BOOKING ───────────────────────────────────────────────────
            if (ContainsAny(lower, "book", "booking", "reserve", "reservation", "check-in", "check in", "check-out", "check out", "how to book", "make a booking"))
                return await HandleBookingQueryAsync(lower, userId);

        // ── CANCELLATION ──────────────────────────────────────────────
            if (ContainsAny(lower, "cancel", "cancellation", "refund", "money back", "cancel booking", "cancellation policy", "calculate refund", "how much refund", "refund amount", "refund calculator"))
                return (HandleCancellationPolicy(lower), "cancellation");

            // ── PAYMENT ───────────────────────────────────────────────────
            if (ContainsAny(lower, "pay", "payment", "price", "cost", "charge", "fee", "amount", "upi", "credit card", "debit card"))
                return (HandlePaymentQuery(lower), "payment");

            // ── HOTEL ─────────────────────────────────────────────────────
            if (ContainsAny(lower, "hotel", "room", "amenity", "amenities", "facility", "facilities", "star", "rating", "location", "address", "contact"))
                return await HandleHotelQueryAsync(lower);

            // ── MY BOOKINGS ───────────────────────────────────────────────
            if (ContainsAny(lower, "my booking", "my reservation", "my order", "booking status", "booking id"))
                return (HandleMyBookingsQuery(userId), "booking");

            // ── SUPPORT / HELP ────────────────────────────────────────────
            if (ContainsAny(lower, "help", "support", "assist", "problem", "issue", "complaint"))
                return (
                    "🛎️ I'm here to help! You can ask me about:\n" +
                    "• How to make or modify a booking\n" +
                    "• Cancellation and refund policies\n" +
                    "• Hotel amenities and details\n" +
                    "• Payment methods\n\n" +
                    "For urgent issues, please contact our support team at support@stayease.com",
                    "support");

            // ── FALLBACK ──────────────────────────────────────────────────
            return (
                "🤔 I'm not sure I understood that. I can help with:\n" +
                "• **Bookings** — type 'how to book'\n" +
                "• **Cancellations** — type 'cancellation policy'\n" +
                "• **Hotels** — type 'hotel amenities'\n" +
                "• **Payments** — type 'payment methods'\n\n" +
                "Or type **help** for more options.",
                "general");
        }

        private async Task<(string, string)> HandleBookingQueryAsync(string lower, int? userId)
        {
            if (ContainsAny(lower, "how to", "how do", "steps", "process", "guide"))
                return (
                    "📅 **How to Book a Room:**\n\n" +
                    "1. Browse hotels on the Hotels page\n" +
                    "2. Select a hotel and choose a room\n" +
                    "3. Pick your check-in and check-out dates\n" +
                    "4. Click **Book Now** and confirm\n" +
                    "5. Complete payment to confirm your reservation\n\n" +
                    "💡 Tip: Your booking stays **Pending** until payment is made.",
                    "booking");

            if (ContainsAny(lower, "modify", "change", "update", "edit"))
                return (
                    "✏️ **Modifying a Booking:**\n\n" +
                    "Currently, bookings cannot be directly modified. To change dates:\n" +
                    "1. Cancel your existing booking (refund may apply)\n" +
                    "2. Create a new booking with the desired dates\n\n" +
                    "For assistance, contact support@stayease.com",
                    "booking");

            if (ContainsAny(lower, "status", "pending", "confirmed", "completed"))
                return (
                    "📊 **Booking Statuses:**\n\n" +
                    "• **Pending** — Awaiting payment\n" +
                    "• **Confirmed** — Payment received, booking active\n" +
                    "• **Completed** — Stay finished\n" +
                    "• **Cancelled** — Booking cancelled\n\n" +
                    "View your bookings in **My Dashboard → Bookings**.",
                    "booking");

            if (userId.HasValue)
            {
                try
                {
                    var count = (await _bookingRepo.FindAllAsync(b => b.UserId == userId.Value)).Count();
                    return (
                        $"📋 You have **{count} booking(s)** in total.\n\n" +
                        "Visit **My Dashboard → Bookings** to view details, make payments, or request cancellations.",
                        "booking");
                }
                catch { /* fall through to generic reply */ }
            }

            return (
                "📅 You can book rooms by browsing our hotels, selecting a room, and choosing your dates.\n\n" +
                "Please **login** to view your existing bookings or make a new reservation.",
                "booking");
        }

        private static string HandleCancellationPolicy(string lower)
                {
                    // ── REFUND CALCULATOR ─────────────────────────────────────────
                    // Uses the same logic as AppDelegateFactory.StandardRefundPolicy:
                    //   ≥ 5 days → 100% full refund
                    //   3–5 days → 50% refund
                    //   1–3 days → 25% refund
                    //   ≤ 1 day  → 100% full refund
                    if (ContainsAny(lower, "calculate refund", "how much refund", "refund amount", "refund calculator", "refund if i cancel"))
                        return
                            "🧮 **Refund Calculator:**\n\n" +
                            "Your refund is calculated automatically when you request a cancellation:\n\n" +
                            "| Time Before Check-in | Refund |\n" +
                            "|---|---|\n" +
                            "| **5 days or more** | **100% full refund** |\n" +
                            "| **3 to 5 days** | **50% of booking amount** |\n" +
                            "| **1 to 3 days** | **25% of booking amount** |\n" +
                            "| **Within 1 day** | **100% full refund** |\n\n" +
                            "**Example (₹5,000 booking):**\n" +
                            "• Cancel 6 days before → ₹5,000 refund (100%)\n" +
                            "• Cancel 4 days before → ₹2,500 refund (50%)\n" +
                            "• Cancel 2 days before → ₹1,250 refund (25%)\n" +
                            "• Cancel same day → ₹5,000 refund (100%)\n\n" +
                            "💡 Refund is credited instantly to your **Wallet Balance**.";

                    if (ContainsAny(lower, "refund", "money", "get back", "money back"))
                        return
                            "💰 **Refund Policy:**\n\n" +
                            "StayEase uses an automatic refund calculator based on how close to check-in you cancel:\n\n" +
                            "• **5 days or more before check-in** → **100% full refund**\n" +
                            "• **3 to 5 days before check-in** → **50% refund**\n" +
                            "• **1 to 3 days before check-in** → **25% refund**\n" +
                            "• **Within 1 day of check-in** → **100% full refund**\n\n" +
                            "Refunds are credited **instantly** to your Wallet Balance.\n\n" +
                            "Type **calculate refund** to see examples.";

                    if (ContainsAny(lower, "how to cancel", "cancel my", "request cancel", "steps to cancel"))
                        return
                            "❌ **How to Cancel a Booking:**\n\n" +
                            "1. Go to **My Dashboard → Bookings**\n" +
                            "2. Find your booking and click **Request Cancellation**\n" +
                            "3. Enter a reason and submit\n" +
                            "4. The system auto-calculates your refund:\n" +
                            "   • Cancel **5+ days before** → **100% full refund**\n" +
                            "   • Cancel **3–5 days before** → **50% refund**\n" +
                            "   • Cancel **1–3 days before** → **25% refund**\n" +
                            "   • Cancel **within 1 day** → **100% full refund**\n" +
                            "5. Refund is credited instantly to your **Wallet Balance**\n\n" +
                            "💡 **Pending** bookings can be cancelled directly without a request.";

                    // Default cancellation policy summary
                    return
                        "📋 **Cancellation Policy:**\n\n" +
                        "Our refund policy is calculated automatically:\n\n" +
                        "• Cancel **5 days or more** before check-in → **100% full refund**\n" +
                        "• Cancel **3 to 5 days** before check-in → **50% refund**\n" +
                        "• Cancel **1 to 3 days** before check-in → **25% refund**\n" +
                        "• Cancel **within 1 day** of check-in → **100% full refund**\n\n" +
                        "Refunds are credited instantly to your **Wallet Balance**.\n\n" +
                        "Ask me:\n• **How to cancel** — step-by-step guide\n• **Calculate refund** — see refund examples";
                }


        private static string HandlePaymentQuery(string lower)
        {
            if (ContainsAny(lower, "method", "how to pay", "payment option"))
                return
                    "💳 **Accepted Payment Methods:**\n\n" +
                    "• Credit Card\n• Debit Card\n• UPI\n• Net Banking\n• Wallet\n• PayPal\n\n" +
                    "All payments are processed securely. No hidden charges.";

            if (ContainsAny(lower, "fail", "failed", "not working", "error"))
                return
                    "⚠️ **Payment Failed?**\n\n" +
                    "1. Check your card/UPI details\n" +
                    "2. Ensure sufficient balance\n" +
                    "3. Try a different payment method\n" +
                    "4. If the issue persists, contact support@stayease.com\n\n" +
                    "Your booking remains **Pending** until payment succeeds.";

            return
                "💰 **Pricing & Payments:**\n\n" +
                "• Prices are shown per night per room\n" +
                "• Total = Price × Nights × Rooms\n" +
                "• Payment is required to confirm a booking\n" +
                "• Accepted: Credit/Debit Card, UPI, Net Banking, Wallet, PayPal\n\n" +
                "No hidden fees — what you see is what you pay.";
        }

        private async Task<(string, string)> HandleHotelQueryAsync(string lower)
        {
            if (ContainsAny(lower, "amenity", "amenities", "facility", "facilities", "wifi", "pool", "gym", "spa"))
                return (
                    "🏨 **Hotel Amenities:**\n\n" +
                    "Our hotels offer a range of amenities including:\n" +
                    "• Free WiFi • Swimming Pool • Gym & Spa\n" +
                    "• Restaurant & Room Service • Parking\n" +
                    "• 24/7 Front Desk • Air Conditioning\n\n" +
                    "Specific amenities vary by hotel. Check the hotel detail page for the full list.",
                    "hotel");

            if (ContainsAny(lower, "how many hotel", "list of hotel", "available hotel", "hotels available"))
            {
                try
                {
                    var count = (await _hotelRepo.FindAllAsync(h => h.IsActive)).Count();
                    return ($"🏨 We currently have **{count} active hotel(s)** available.\n\nBrowse them on the **Hotels** page to find the perfect stay!", "hotel");
                }
                catch
                {
                    return ("🏨 We have many hotels available. Browse them on the **Hotels** page!", "hotel");
                }
            }

            if (ContainsAny(lower, "star", "rating", "best hotel", "top hotel"))
                return (
                    "⭐ **Hotel Ratings:**\n\n" +
                    "Our hotels are rated 1–5 stars based on facilities and service quality.\n" +
                    "• 5★ — Luxury\n• 4★ — Superior\n• 3★ — Standard\n• 1–2★ — Budget\n\n" +
                    "Filter by star rating on the Hotels page to find your ideal stay.",
                    "hotel");

            if (ContainsAny(lower, "check-in time", "check in time", "check-out time", "check out time"))
                return (
                    "🕐 **Check-in & Check-out Times:**\n\n" +
                    "• Standard check-in: **2:00 PM**\n" +
                    "• Standard check-out: **12:00 PM (noon)**\n\n" +
                    "Early check-in or late check-out may be available on request (subject to availability).",
                    "hotel");

            return (
                "🏨 **About Our Hotels:**\n\n" +
                "StayEase offers a wide range of hotels across multiple locations.\n" +
                "You can filter by location, star rating, and price on the Hotels page.\n\n" +
                "Ask me about:\n• Hotel amenities\n• Check-in/out times\n• Star ratings\n• Available hotels",
                "hotel");
        }

        private static string HandleMyBookingsQuery(int? userId)
        {
            if (!userId.HasValue)
                return "🔐 Please **login** to view your bookings. Once logged in, visit **My Dashboard → Bookings**.";

            return
                "📋 **Your Bookings:**\n\n" +
                "Visit **My Dashboard → Bookings** to:\n" +
                "• View all your bookings and their status\n" +
                "• Make payments for pending bookings\n" +
                "• Request cancellations\n" +
                "• See check-in/out dates and amounts";
        }

        // ── HELPERS ───────────────────────────────────────────────────────
        private static bool ContainsAny(string text, params string[] keywords)
            => keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

        private static ChatHistoryDto MapToDto(ChatMessage m) => new()
        {
            ChatMessageId = m.ChatMessageId,
            UserId        = m.UserId,
            SessionId     = m.SessionId,
            Sender        = m.Sender,
            Message       = m.Message,
            Intent        = m.Intent,
            CreatedAt     = m.CreatedAt
        };
    }
}
