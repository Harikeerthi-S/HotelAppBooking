using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class ChatRequestDto
    {
        /// <summary>Null for guest users.</summary>
        public int? UserId { get; set; }

        [Required, MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
