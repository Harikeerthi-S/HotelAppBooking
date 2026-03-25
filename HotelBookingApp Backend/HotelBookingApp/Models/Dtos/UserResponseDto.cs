namespace HotelBookingApp.Models.Dtos
{
    public class UserResponseDto
    {
        public int     UserId   { get; set; }
        public string  UserName { get; set; } = string.Empty;
        public string  Email    { get; set; } = string.Empty;
        public string? Phone    { get; set; }
        public string  Role     { get; set; } = string.Empty;
    }
}
