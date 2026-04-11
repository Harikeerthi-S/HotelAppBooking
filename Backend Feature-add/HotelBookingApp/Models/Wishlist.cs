using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Saved hotel for a user (wishlist / favourites).</summary>
    public class Wishlist : IComparable<Wishlist>, IEquatable<Wishlist>
    {
        [Key]
        public int WishlistId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int HotelId { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]  public User?  User  { get; set; }
        [ForeignKey(nameof(HotelId))] public Hotel? Hotel { get; set; }

        public int  CompareTo(Wishlist? other) => other != null ? WishlistId.CompareTo(other.WishlistId) : 1;
        public bool Equals(Wishlist? other)    => other != null && WishlistId == other.WishlistId;
        public override bool Equals(object? obj) => Equals(obj as Wishlist);
        public override int  GetHashCode()  => WishlistId.GetHashCode();
        public override string ToString()   => $"WishlistId:{WishlistId} | User:{UserId} | Hotel:{HotelId}";
    }
}
