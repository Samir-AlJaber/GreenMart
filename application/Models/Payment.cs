using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [Required, MaxLength(80)]
        public string TransactionId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? GatewayTransactionId { get; set; }

        [MaxLength(100)]
        public string? SessionKey { get; set; }

        [Required, MaxLength(30)]
        public string Method { get; set; } = "CashOnDelivery";

        [MaxLength(30)]
        public string? SelectedChannel { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "BDT";

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";

        [MaxLength(100)]
        public string? ValidationId { get; set; }

        [MaxLength(80)]
        public string? BankTransactionId { get; set; }

        [MaxLength(80)]
        public string? CardType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? PaidAt { get; set; }
    }
}
