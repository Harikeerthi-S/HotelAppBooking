namespace HotelBookingApp.Models.Dtos
{
    public class BookingRoomResponseDto
    {
        public int     BookingRoomId { get; set; }
        public int     BookingId     { get; set; }
        public int     RoomId        { get; set; }
        public decimal PricePerNight { get; set; }
        public int     NumberOfRooms { get; set; }
    }
}
