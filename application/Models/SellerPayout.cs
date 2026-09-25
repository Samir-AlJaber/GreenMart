using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class SellerPayout
    {
        [Key]
        public int SellerPayoutId { get; set; }

        public int SellerEarningId { get; set; }

        [ForeignKey(nameof(SellerEarningId))]
        public SellerEarning Earning { get; set; } = null!;

        public int SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public User Seller { get; set; } = null!;

        [Required, MaxLength(30)]
        public string Provider { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string AccountNumberSnapshot { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(40)]
        public string Status { get; set; } = "AwaitingProviderSetup";

        [MaxLength(100)]
        public string? ExternalReference { get; set; }

        [MaxLength(300)]
        public string? FailureReason { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public DateTime? ProcessedAt { get; set; }
    }
}
