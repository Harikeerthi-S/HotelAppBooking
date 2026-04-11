namespace HotelBookingApp.Models.Dtos
{
    public class LoginResponseDto
    {
        public int    UserId   { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;
        public string Role     { get; set; } = string.Empty;
    }
}
