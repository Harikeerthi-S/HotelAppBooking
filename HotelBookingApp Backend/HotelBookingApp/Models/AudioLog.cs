using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>
    /// Stores audit trail for important actions like Booking, Room updates, Payments, etc.
    /// </summary>
    public class AuditLog : IComparable<AuditLog>, IEquatable<AuditLog>
    {
        [Key]
        public int AuditLogId { get; set; }

        // Who performed action
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // Action performed
        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;
        // Examples: "BookingCreated", "RoomUpdated", "PaymentCompleted"

        // Entity affected
        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;
        // Examples: "Booking", "Room", "Hotel", "Payment"

        public int? EntityId { get; set; }

        /// <summary>
        /// JSON string describing changes (old/new values or summary)
        /// </summary>
        public string? Changes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Comparable / Equatable
        public int CompareTo(AuditLog? other) => other != null ? AuditLogId.CompareTo(other.AuditLogId) : 1;
        public bool Equals(AuditLog? other) => other != null && AuditLogId == other.AuditLogId;
        public override bool Equals(object? obj) => Equals(obj as AuditLog);
        public override int GetHashCode() => AuditLogId.GetHashCode();
    }
}