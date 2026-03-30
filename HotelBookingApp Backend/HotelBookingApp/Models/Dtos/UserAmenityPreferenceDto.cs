using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class UserAmenityPreferenceResponseDto
    {
        public int      PreferenceId { get; set; }
        public int      UserId       { get; set; }
        public string   UserName     { get; set; } = string.Empty;
        public int      AmenityId    { get; set; }
        public string   AmenityName  { get; set; } = string.Empty;
        public string?  AmenityIcon  { get; set; }
        public DateTime CreatedAt    { get; set; }
        public string   Status       { get; set; } = "Pending";
    }

    public class CreateUserAmenityPreferenceDto
    {
        [Required, Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int AmenityId { get; set; }
    }
}
