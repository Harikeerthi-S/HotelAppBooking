using System.ComponentModel.DataAnnotations;

namespace HotelBookingApp.Models.Dtos
{
    public class PagedRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1.")]
        public int PageNumber { get; set; } = 1;

        /// <summary>Max 10000 to support admin/manager “full list” loads; keep reasonable to avoid abuse.</summary>
        [Range(1, 10000, ErrorMessage = "Page size must be between 1 and 10000.")]
        public int PageSize { get; set; } = 10;
    }
}
