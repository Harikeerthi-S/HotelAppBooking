using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Line-item: links a room to a booking with its snapshot price.</summary>
    public class BookingRoom : IComparable<BookingRoom>, IEquatable<BookingRoom>
    {
        [Key]
        public int BookingRoomId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required, Column(TypeName = "decimal(18,2)"), Range(0.01, 1000000)]
        public decimal PricePerNight { get; set; }

        [Required, Range(1, 100)]
        public int NumberOfRooms { get; set; } = 1;

        // Navigation Properties
        [ForeignKey(nameof(BookingId))] public Booking? Booking { get; set; }
        [ForeignKey(nameof(RoomId))]    public Room?    Room    { get; set; }

        public int  CompareTo(BookingRoom? other) => other != null ? BookingRoomId.CompareTo(other.BookingRoomId) : 1;
        public bool Equals(BookingRoom? other)    => other != null && BookingRoomId == other.BookingRoomId;
        public override bool Equals(object? obj)  => Equals(obj as BookingRoom);
        public override int  GetHashCode()  => BookingRoomId.GetHashCode();
        public override string ToString()   => $"BookingRoomId:{BookingRoomId} | Booking:{BookingId} | Room:{RoomId} | ₹{PricePerNight} x {NumberOfRooms}";
    }
}
