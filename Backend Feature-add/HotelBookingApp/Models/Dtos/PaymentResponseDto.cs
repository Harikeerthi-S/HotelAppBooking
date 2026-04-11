namespace HotelBookingApp.Models.Dtos
{
    public class PaymentResponseDto
    {
        public int      PaymentId     { get; set; }
        public int      BookingId     { get; set; }
        public decimal  Amount        { get; set; }
        public string   PaymentMethod { get; set; } = string.Empty;
        public string   PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt     { get; set; }
    }
}
