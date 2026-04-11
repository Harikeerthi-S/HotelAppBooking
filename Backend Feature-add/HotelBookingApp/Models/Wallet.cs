using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Models
{
    /// <summary>User wallet — stores accumulated refund balance.</summary>
    public class Wallet
    {
        [Key]
        public int WalletId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0m;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
