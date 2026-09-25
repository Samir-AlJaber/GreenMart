using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class SellerPayoutAccount
    {
        [Key]
        public int SellerPayoutAccountId { get; set; }

        public int SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public User Seller { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Method { get; set; } = "Bkash";

        [Required, MaxLength(120)]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string AccountNumber { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? BankName { get; set; }

        [MaxLength(120)]
        public string? BranchName { get; set; }

        [MaxLength(30)]
        public string? RoutingNumber { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "PendingVerification";

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public DateTime? VerifiedAt { get; set; }

        public int? VerifiedByUserId { get; set; }
    }
}
