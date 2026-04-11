using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateAmenityDto
    {
        [Required(ErrorMessage = "Amenity name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Icon { get; set; }
    }
}
