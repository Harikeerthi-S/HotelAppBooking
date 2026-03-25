namespace HotelBookingApp.Models.Dtos
{
    public class CancellationResponseDto
    {
        public int      CancellationId   { get; set; }
        public int      BookingId        { get; set; }
        public string   Reason           { get; set; } = string.Empty;
        public decimal  RefundAmount     { get; set; }
        public string   Status           { get; set; } = string.Empty;
        public DateTime CancellationDate { get; set; }
    }
}
