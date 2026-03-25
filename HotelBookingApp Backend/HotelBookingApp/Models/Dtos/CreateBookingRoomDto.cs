using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateBookingRoomDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BookingId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int RoomId { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal PricePerNight { get; set; }

        [Range(1, 100)]
        public int NumberOfRooms { get; set; } = 1;
    }
}
