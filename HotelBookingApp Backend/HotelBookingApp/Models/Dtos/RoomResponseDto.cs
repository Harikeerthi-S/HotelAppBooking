namespace HotelBookingApp.Models.Dtos
{
    public class RoomResponseDto
    {
        public int     RoomId        { get; set; }
        public int     HotelId       { get; set; }
        public int     RoomNumber    { get; set; }
        public string  RoomType      { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public string? ImageUrl { get; set; } = string.Empty;
        public int     Capacity      { get; set; }
        public bool    IsAvailable   { get; set; }
    }
}
