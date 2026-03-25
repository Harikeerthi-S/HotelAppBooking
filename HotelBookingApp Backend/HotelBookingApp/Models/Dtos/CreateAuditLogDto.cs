using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class CreateAuditLogDto
    {
        public int? UserId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string? Changes { get; set; }
    }
}
