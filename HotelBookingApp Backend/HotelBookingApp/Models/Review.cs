using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Guest review for a hotel — one review per user per hotel.</summary>
    public class Review : IComparable<Review>, IEquatable<Review>
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [Required, MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(HotelId))] public Hotel? Hotel { get; set; }
        [ForeignKey(nameof(UserId))]  public User?  User  { get; set; }

        public int  CompareTo(Review? other) => other != null ? ReviewId.CompareTo(other.ReviewId) : 1;
        public bool Equals(Review? other)    => other != null && ReviewId == other.ReviewId;
        public override bool Equals(object? obj) => Equals(obj as Review);
        public override int  GetHashCode()  => ReviewId.GetHashCode();
        public override string ToString()   => $"ReviewId:{ReviewId} | Hotel:{HotelId} | User:{UserId} | {Rating}★";
    }
}
