using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateReviewDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int HotelId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required.")]
        [MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;
    }
}
