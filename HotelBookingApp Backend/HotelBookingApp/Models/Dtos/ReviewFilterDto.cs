namespace HotelBookingApp.Models.Dtos
{
    public class ReviewFilterDto
    {
        public int? HotelId { get; set; }
        public int? UserId  { get; set; }
        public int? Rating  { get; set; }
    }
}
