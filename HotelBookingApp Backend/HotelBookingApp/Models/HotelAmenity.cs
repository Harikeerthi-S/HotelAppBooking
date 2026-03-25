using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Junction table linking a hotel to an amenity (many-to-many).</summary>
    public class HotelAmenity : IComparable<HotelAmenity>, IEquatable<HotelAmenity>
    {
        [Key]
        public int HotelAmenityId { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required]
        public int AmenityId { get; set; }

        [ForeignKey(nameof(HotelId))]   public Hotel?   Hotel   { get; set; }
        [ForeignKey(nameof(AmenityId))] public Amenity? Amenity { get; set; }

        public int  CompareTo(HotelAmenity? other) => other != null ? HotelAmenityId.CompareTo(other.HotelAmenityId) : 1;
        public bool Equals(HotelAmenity? other)    => other != null && HotelAmenityId == other.HotelAmenityId;
        public override bool Equals(object? obj)   => Equals(obj as HotelAmenity);
        public override int  GetHashCode()  => HotelAmenityId.GetHashCode();
        public override string ToString()   => $"HotelAmenityId:{HotelAmenityId} | Hotel:{HotelId} | Amenity:{AmenityId}";
    }
}
