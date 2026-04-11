using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateCancellationDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Cancellation reason is required.")]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }
}
