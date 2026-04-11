using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models
{
    /// <summary>Application user — supports admin, hotelmanager and user roles.</summary>
    public class User : IComparable<User>, IEquatable<User>
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Role values: admin | hotelmanager | user</summary>
        [Required, MaxLength(50)]
        public string Role { get; set; } = "user";

        [MaxLength(10)]
        public string? Phone { get; set; }

        /// <summary>SHA-256 hash of the password (stored as byte array).</summary>
        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        // Navigation Properties
        public ICollection<Booking>?      Bookings      { get; set; }
        public ICollection<Review>?       Reviews       { get; set; }
        public ICollection<Wishlist>?     Wishlists     { get; set; }
        public ICollection<Notification>? Notifications { get; set; }

        // IComparable / IEquatable
        public int  CompareTo(User? other)  => other != null ? UserId.CompareTo(other.UserId) : 1;
        public bool Equals(User? other)     => other != null && UserId == other.UserId;
        public override bool Equals(object? obj) => Equals(obj as User);
        public override int  GetHashCode()  => UserId.GetHashCode();
        public override string ToString()   => $"UserId:{UserId} | {UserName} | {Email} | {Role}";
    }
}
