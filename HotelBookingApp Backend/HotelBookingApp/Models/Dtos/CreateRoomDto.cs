using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateRoomDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "HotelId must be valid.")]
        public int HotelId { get; set; }

        [Required]
        [Range(1, 99999, ErrorMessage = "Room number must be between 1 and 99999.")]
        public int RoomNumber { get; set; }

        [Required, MaxLength(50)]
        public string RoomType { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0.")]
        public decimal PricePerNight { get; set; }
        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        [Required]
        [Range(1, 50, ErrorMessage = "Capacity must be between 1 and 50.")]
        public int Capacity { get; set; }
    }
}
