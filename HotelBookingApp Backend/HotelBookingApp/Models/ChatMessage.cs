using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Stores a single chat message in a user's support session.</summary>
    public class ChatMessage : IComparable<ChatMessage>, IEquatable<ChatMessage>
    {
        [Key]
        public int ChatMessageId { get; set; }

        /// <summary>Null = guest (unauthenticated) session.</summary>
        public int? UserId { get; set; }

        /// <summary>Groups messages into a conversation session.</summary>
        [Required, MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>user | bot</summary>
        [Required, MaxLength(10)]
        public string Sender { get; set; } = "user";

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>Intent detected: booking | cancellation | hotel | general</summary>
        [MaxLength(50)]
        public string? Intent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public int  CompareTo(ChatMessage? other) => other != null ? ChatMessageId.CompareTo(other.ChatMessageId) : 1;
        public bool Equals(ChatMessage? other)    => other != null && ChatMessageId == other.ChatMessageId;
        public override bool Equals(object? obj)  => Equals(obj as ChatMessage);
        public override int  GetHashCode()        => ChatMessageId.GetHashCode();
    }
}
