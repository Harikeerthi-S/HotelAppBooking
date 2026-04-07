using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HotelBookingApp.Models
{
    /// <summary>Payment record for a booking.</summary>
    public class Payment : IComparable<Payment>, IEquatable<Payment>
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required, Column(TypeName = "decimal(18,2)"), Range(0.01, 10000000)]
        public decimal Amount { get; set; }

        /// <summary>CreditCard | DebitCard | NetBanking</summary>
        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>Pending | Completed | Failed | Refunded</summary>
        [Required, MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(BookingId))]
        [JsonIgnore]
        public Booking? Booking { get; set; }

        public int  CompareTo(Payment? other) => other != null ? PaymentId.CompareTo(other.PaymentId) : 1;
        public bool Equals(Payment? other)    => other != null && PaymentId == other.PaymentId;
        public override bool Equals(object? obj) => Equals(obj as Payment);
        public override int  GetHashCode()  => PaymentId.GetHashCode();
        public override string ToString()   => $"PaymentId:{PaymentId} | Booking:{BookingId} | ₹{Amount} | {PaymentStatus}";
    }
}
