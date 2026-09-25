using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class SellerEarning
    {
        [Key]
        public int SellerEarningId { get; set; }

        public int SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public User Seller { get; set; } = null!;

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        public int DeliveryAssignmentId { get; set; }

        [ForeignKey(nameof(DeliveryAssignmentId))]
        public DeliveryAssignment DeliveryAssignment { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GatewayFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        [Required, MaxLength(30)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string Status { get; set; } = "PendingRelease";

        [MaxLength(300)]
        public string? StatusNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? AvailableAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public SellerPayout? Payout { get; set; }
    }
}
