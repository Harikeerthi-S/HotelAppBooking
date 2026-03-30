namespace HotelBookingApp.Models.Dtos
{
    public class ChatHistoryDto
    {
        public int      ChatMessageId { get; set; }
        public int?     UserId        { get; set; }
        public string   SessionId     { get; set; } = string.Empty;
        public string   Sender        { get; set; } = string.Empty;
        public string   Message       { get; set; } = string.Empty;
        public string?  Intent        { get; set; }
        public DateTime CreatedAt     { get; set; }
    }
}
