using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models
{
    public class Amenity
    {
        [Key]
        public int AmenityId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Icon { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();
    }
}