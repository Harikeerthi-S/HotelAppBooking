namespace HotelBookingApp.Models.Dtos
{
    public class AuditLogResponseDto
    {
        public int AuditLogId { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }
        public string Changes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}