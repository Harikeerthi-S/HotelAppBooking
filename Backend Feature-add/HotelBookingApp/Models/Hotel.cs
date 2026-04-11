using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models
{
    /// <summary>Represents a hotel property on the platform.</summary>
    public class Hotel : IComparable<Hotel>, IEquatable<Hotel>
    {
        [Key]
        public int HotelId { get; set; }

        [Required, MaxLength(200)]
        public string HotelName { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [Range(1, 5)]
        public double StarRating { get; set; }

        [Range(1, 10000)]
        public int TotalRooms { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        [MaxLength(500)]
        public string? ImagePath { get; set; }

        /// <summary>Soft-delete flag — deactivated hotels are hidden from listings.</summary>
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<Room>?         Rooms          { get; set; }
        public ICollection<Booking>       Bookings       { get; set; } = new List<Booking>();
        public ICollection<Review>?       Reviews        { get; set; }
        public ICollection<HotelAmenity>? HotelAmenities { get; set; }
        public ICollection<Wishlist>?     Wishlists      { get; set; }

        public int  CompareTo(Hotel? other) => other != null ? HotelId.CompareTo(other.HotelId) : 1;
        public bool Equals(Hotel? other)    => other != null && HotelId == other.HotelId;
        public override bool Equals(object? obj) => Equals(obj as Hotel);
        public override int  GetHashCode()  => HotelId.GetHashCode();
        public override string ToString()   => $"HotelId:{HotelId} | {HotelName} | {Location} | {StarRating}★";
    }
}
