using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>A room inside a hotel with pricing and availability.</summary>
    public class Room : IComparable<Room>, IEquatable<Room>
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required, Range(1, 99999)]
        public int RoomNumber { get; set; }

        [Required, MaxLength(50)]
        public string RoomType { get; set; } = string.Empty; // Standard | Deluxe | Suite | Single | Double

        [Required, Column(TypeName = "decimal(18,2)"), Range(0.01, 1000000)]
        public decimal PricePerNight { get; set; }

        [Required, Range(1, 50)]
        public int Capacity { get; set; }
        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation Properties
        [ForeignKey(nameof(HotelId))]
        public Hotel?                   Hotel        { get; set; }
        public ICollection<Booking>?    Bookings     { get; set; }
        public ICollection<BookingRoom>? BookingRooms { get; set; }

        public int  CompareTo(Room? other) => other != null ? RoomId.CompareTo(other.RoomId) : 1;
        public bool Equals(Room? other)    => other != null && RoomId == other.RoomId;
        public override bool Equals(object? obj) => Equals(obj as Room);
        public override int  GetHashCode()  => RoomId.GetHashCode();
        public override string ToString()   => $"RoomId:{RoomId} | #{RoomNumber} | {RoomType} | ₹{PricePerNight}/night";
    }
}
