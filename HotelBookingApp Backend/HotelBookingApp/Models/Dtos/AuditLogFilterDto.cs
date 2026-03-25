namespace HotelBookingApp.Models.Dtos
{
    public class AuditLogFilterDto
    {
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public int? EntityId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
