using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateHotelAmenityDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int HotelId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int AmenityId { get; set; }
    }
}
