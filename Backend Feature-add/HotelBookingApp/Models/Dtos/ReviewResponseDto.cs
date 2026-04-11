namespace HotelBookingApp.Models.Dtos
{
    public class ReviewResponseDto
    {
        public int      ReviewId  { get; set; }
        public int      HotelId   { get; set; }
        public int      UserId    { get; set; }
        public int      Rating    { get; set; }
        public string   Comment   { get; set; } = string.Empty;
        public string?  PhotoUrl  { get; set; }
        public int      CoinsEarned { get; set; }   // populated only on photo upload
        public DateTime CreatedAt { get; set; }
    }
}
