using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Cancellation request raised by a user for a confirmed booking.</summary>
    public class Cancellation : IComparable<Cancellation>, IEquatable<Cancellation>
    {
        [Key]
        public int CancellationId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        /// <summary>Pending | Approved | Rejected | Refunded</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CancellationDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        public int  CompareTo(Cancellation? other) => other != null ? CancellationId.CompareTo(other.CancellationId) : 1;
        public bool Equals(Cancellation? other)    => other != null && CancellationId == other.CancellationId;
        public override bool Equals(object? obj)   => Equals(obj as Cancellation);
        public override int  GetHashCode()  => CancellationId.GetHashCode();
        public override string ToString()   => $"CancellationId:{CancellationId} | Booking:{BookingId} | Refund:₹{RefundAmount} | {Status}";
    }
}
