using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Push notification sent to a user about booking events.</summary>
    public class Notification : IComparable<Notification>, IEquatable<Notification>
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public int  CompareTo(Notification? other) => other != null ? NotificationId.CompareTo(other.NotificationId) : 1;
        public bool Equals(Notification? other)    => other != null && NotificationId == other.NotificationId;
        public override bool Equals(object? obj)   => Equals(obj as Notification);
        public override int  GetHashCode()  => NotificationId.GetHashCode();
        public override string ToString()   => $"NotificationId:{NotificationId} | User:{UserId} | IsRead:{IsRead}";
    }
}
