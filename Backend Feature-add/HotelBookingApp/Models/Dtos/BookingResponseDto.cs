namespace HotelBookingApp.Models.Dtos
{
    public class BookingResponseDto
    {
        public int      BookingId     { get; set; }
        public int      UserId        { get; set; }
        public int      HotelId       { get; set; }
        public string   HotelName     { get; set; } = string.Empty;
        public int      RoomId        { get; set; }
        public int      NumberOfRooms { get; set; }
        public DateTime CheckIn       { get; set; }
        public DateTime CheckOut      { get; set; }
        public decimal  TotalAmount   { get; set; }
        public string   Status        { get; set; } = string.Empty;
    }
}
