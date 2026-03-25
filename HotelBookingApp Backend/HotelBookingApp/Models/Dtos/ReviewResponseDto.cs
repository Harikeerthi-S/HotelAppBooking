namespace HotelBookingApp.Models.Dtos
{
    public class ReviewResponseDto
    {
        public int      ReviewId  { get; set; }
        public int      HotelId   { get; set; }
        public int      UserId    { get; set; }
        public int      Rating    { get; set; }
        public string   Comment   { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
