namespace HotelBookingApp.Models.Dtos
{
    public class RoomResponseDto
    {
        public int RoomId { get; set; }
        public int HotelId { get; set; }
        public int RoomNumber { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }

        public int Capacity { get; set; }

        // ? INVENTORY
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }

        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }
}