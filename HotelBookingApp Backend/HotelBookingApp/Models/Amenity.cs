using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models
{
    /// <summary>An amenity (WiFi, Pool, Gym, etc.) that can be assigned to hotels.</summary>
    public class Amenity : IComparable<Amenity>, IEquatable<Amenity>
    {
        [Key]
        public int AmenityId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Emoji or icon identifier, e.g. "📶", "🏊", "💪"</summary>
        [MaxLength(50)]
        public string? Icon { get; set; }

        public ICollection<HotelAmenity>? HotelAmenities { get; set; }

        public int  CompareTo(Amenity? other) => other != null ? AmenityId.CompareTo(other.AmenityId) : 1;
        public bool Equals(Amenity? other)    => other != null && AmenityId == other.AmenityId;
        public override bool Equals(object? obj) => Equals(obj as Amenity);
        public override int  GetHashCode()  => AmenityId.GetHashCode();
        public override string ToString()   => $"AmenityId:{AmenityId} | {Icon} {Name}";
    }
}
