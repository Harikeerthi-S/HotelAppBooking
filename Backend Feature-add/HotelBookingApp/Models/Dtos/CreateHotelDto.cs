using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateHotelDto
    {
        [Required(ErrorMessage = "Hotel name is required.")]
        [MaxLength(200)]
        public string HotelName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [Range(1, 5, ErrorMessage = "Star rating must be between 1 and 5.")]
        public double StarRating { get; set; }

        [Range(1, 10000, ErrorMessage = "Total rooms must be between 1 and 10000.")]
        public int TotalRooms { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        [MaxLength(500)]
        public string? ImagePath { get; set; }
    }
}
