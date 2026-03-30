using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Stores a user's preferred amenity selection.</summary>
    public class UserAmenityPreference : IComparable<UserAmenityPreference>, IEquatable<UserAmenityPreference>
    {
        [Key]
        public int PreferenceId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int AmenityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Pending, Approved, or Rejected (admin/manager review).</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [ForeignKey(nameof(UserId))]   public User?    User    { get; set; }
        [ForeignKey(nameof(AmenityId))] public Amenity? Amenity { get; set; }

        public int  CompareTo(UserAmenityPreference? other) => other != null ? PreferenceId.CompareTo(other.PreferenceId) : 1;
        public bool Equals(UserAmenityPreference? other)    => other != null && PreferenceId == other.PreferenceId;
        public override bool Equals(object? obj) => Equals(obj as UserAmenityPreference);
        public override int  GetHashCode() => PreferenceId.GetHashCode();
    }
}
