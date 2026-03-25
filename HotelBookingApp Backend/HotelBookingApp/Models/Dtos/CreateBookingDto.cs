using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateBookingDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int HotelId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int RoomId { get; set; }

        [Range(1, 100, ErrorMessage = "Number of rooms must be between 1 and 100.")]
        public int NumberOfRooms { get; set; } = 1;

        [Required(ErrorMessage = "Check-in date is required.")]
        public DateTime CheckIn { get; set; }

        [Required(ErrorMessage = "Check-out date is required.")]
        public DateTime CheckOut { get; set; }
    }
}
