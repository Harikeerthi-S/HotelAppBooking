namespace HotelBookingApp.Models.Dtos
{
    public class AmenityResponseDto
    {
        public int     AmenityId   { get; set; }
        public string  Name        { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon        { get; set; }
        public bool    IsAvailable { get; set; } = true;
    }
}
