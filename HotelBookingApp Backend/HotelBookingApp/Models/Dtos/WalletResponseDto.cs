namespace HotelBookingApp.Models.Dtos
{
    public class WalletResponseDto
    {
        public int     WalletId  { get; set; }
        public int     UserId    { get; set; }
        public decimal Balance   { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletTransactionDto
    {
        public int      TransactionId { get; set; }
        public string   Type          { get; set; } = string.Empty;
        public decimal  Amount        { get; set; }
        public string?  Description   { get; set; }
        public int?     ReferenceId   { get; set; }
        public DateTime CreatedAt     { get; set; }
    }
}
