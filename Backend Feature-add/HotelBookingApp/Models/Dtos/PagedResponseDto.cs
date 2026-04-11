namespace HotelBookingApp.Models.Dtos
{
    public class PagedResponseDto<T>
    {
        public List<T> Data         { get; set; } = new();
        public int     PageNumber   { get; set; }
        public int     PageSize     { get; set; }
        public int     TotalRecords { get; set; }
        public int     TotalPages   { get; set; }
    }
}
