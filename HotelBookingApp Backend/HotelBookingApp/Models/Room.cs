using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    public class Room : IComparable<Room>, IEquatable<Room>
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required, Range(1, 99999)]
        public int RoomNumber { get; set; }

        [Required, MaxLength(50)]
        public string RoomType { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        [Required, Range(1, 50)]
        public int Capacity { get; set; }

        // ✅ INVENTORY SYSTEM
        [Required]
        public int TotalRooms { get; set; }

        [Required]
        public int AvailableRooms { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(HotelId))]
        public Hotel? Hotel { get; set; }

        public ICollection<Booking>? Bookings { get; set; }

        public int CompareTo(Room? other) => other != null ? RoomId.CompareTo(other.RoomId) : 1;
        public bool Equals(Room? other) => other != null && RoomId == other.RoomId;
        public override bool Equals(object? obj) => Equals(obj as Room);
        public override int GetHashCode() => RoomId.GetHashCode();
    }
}