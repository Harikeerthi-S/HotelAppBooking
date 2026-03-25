using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateRoomDto
    {
        [Required]
        public int HotelId { get; set; }

        [Required]
        [Range(1, 99999)]
        public int RoomNumber { get; set; }

        [Required, MaxLength(50)]
        public string RoomType { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        public decimal PricePerNight { get; set; }

        [Required]
        [Range(1, 50)]
        public int Capacity { get; set; }

        // ? INVENTORY
        [Required]
        [Range(1, 1000)]
        public int TotalRooms { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }
    }
}