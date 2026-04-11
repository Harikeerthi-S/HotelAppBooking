using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class PaymentDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BookingId { get; set; }

        [Required]
        [Range(0.01, 10000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
