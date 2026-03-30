namespace HotelBookingApp.Models.Dtos
{
    public class HotelAmenityResponseDto
    {
        public int     HotelAmenityId { get; set; }
        public int     HotelId        { get; set; }
        public int     AmenityId      { get; set; }
        public string  AmenityName    { get; set; } = string.Empty;
        public string? AmenityIcon    { get; set; }
        public string? AmenityDescription { get; set; }
    }
}
