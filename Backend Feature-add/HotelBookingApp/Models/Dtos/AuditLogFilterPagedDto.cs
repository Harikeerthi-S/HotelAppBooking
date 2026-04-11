using System.Text.Json.Serialization;

namespace HotelBookingApp.Models.Dtos
{
    /// <summary>Combined filter + paging DTO for POST /api/auditlog/filter/paged</summary>
    public class AuditLogFilterPagedDto
    {
        public int?    UserId     { get; set; }
        public string? Action     { get; set; }
        public string? EntityName { get; set; }
        public int?    EntityId   { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? FromDate { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? ToDate   { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize   { get; set; } = 10;
    }
}
