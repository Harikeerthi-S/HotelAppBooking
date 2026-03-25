using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Phone { get; set; }

        /// <summary>Accepted values: user | hotelmanager</summary>
        [MaxLength(50)]
        public string Role { get; set; } = "user";
    }
}
