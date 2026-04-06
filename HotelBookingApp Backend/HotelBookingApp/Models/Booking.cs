using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    public class Booking : IComparable<Booking>, IEquatable<Booking>
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required]
        public int RoomId { get; set; }

        // ✅ HOW MANY ROOMS USER BOOKED
        [Required, Range(1, 100)]
        public int NumberOfRooms { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // Navigation
        [ForeignKey(nameof(UserId))]
        [System.Text.Json.Serialization.JsonIgnore]
        public User? User { get; set; }

        [ForeignKey(nameof(HotelId))]
        [System.Text.Json.Serialization.JsonIgnore]
        public Hotel? Hotel { get; set; }

        [ForeignKey(nameof(RoomId))]
        [System.Text.Json.Serialization.JsonIgnore]
        public Room? Room { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Payment>? Payments { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Cancellation>? Cancellations { get; set; }
        public int CompareTo(Booking? other) => other != null ? BookingId.CompareTo(other.BookingId) : 1;
        public bool Equals(Booking? other) => other != null && BookingId == other.BookingId;
        public override bool Equals(object? obj) => Equals(obj as Booking);
        public override int GetHashCode() => BookingId.GetHashCode();
    }
}