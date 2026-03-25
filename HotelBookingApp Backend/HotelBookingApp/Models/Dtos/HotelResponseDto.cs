namespace HotelBookingApp.Models.Dtos
{
    public class HotelResponseDto
    {
        public int     HotelId       { get; set; }
        public string  HotelName     { get; set; } = string.Empty;
        public string  Location      { get; set; } = string.Empty;
        public string? Address       { get; set; }
        public double  StarRating    { get; set; }
        public int     TotalRooms    { get; set; }
        public string? ContactNumber { get; set; }
        public string? ImagePath     { get; set; }
    }
}
