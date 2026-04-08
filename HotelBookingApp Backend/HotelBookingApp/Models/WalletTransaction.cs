using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>Audit trail for every wallet credit/debit.</summary>
    public class WalletTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int WalletId { get; set; }

        /// <summary>Credit | Debit</summary>
        [Required, MaxLength(10)]
        public string Type { get; set; } = "Credit";

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Optional reference — e.g. CancellationId that triggered this credit.</summary>
        public int? ReferenceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(WalletId))]
        public Wallet? Wallet { get; set; }
    }
}
