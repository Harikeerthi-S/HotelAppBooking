using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateNotificationDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
    }
}
