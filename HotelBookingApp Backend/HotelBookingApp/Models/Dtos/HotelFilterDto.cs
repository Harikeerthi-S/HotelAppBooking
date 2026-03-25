namespace HotelBookingApp.Models.Dtos
{
    public class HotelFilterDto
    {
        public int?     HotelId   { get; set; }
        public string?  Location  { get; set; }
        public double?  MinRating { get; set; }
        public decimal? MinPrice  { get; set; }
        public decimal? MaxPrice  { get; set; }
        public int?     AmenityId { get; set; }
    }
}
