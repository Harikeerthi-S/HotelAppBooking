using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    public class HotelAmenity
    {
        [Key]
        public int HotelAmenityId { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required]
        public int AmenityId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("HotelId")]
        public virtual Hotel? Hotel { get; set; }

        [ForeignKey("AmenityId")]
        public virtual Amenity? Amenity { get; set; }
    }
}