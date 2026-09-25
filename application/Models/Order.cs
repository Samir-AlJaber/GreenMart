using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class Order
    {

        [Key]
        public int OrderId { get; set; }



        public int UserId { get; set; }



        [ForeignKey("UserId")]
        public User User { get; set; }



        [Required]
        public decimal TotalAmount { get; set; }



        [Required]
        public string Status { get; set; } = "Pending";

        [Required]
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = "CashOnDelivery";

        [Required]
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = "CashOnDelivery";



        public string? ShippingAddress { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? ShippingLatitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? ShippingLongitude { get; set; }


        public string? RejectionReason { get; set; }


        public string? RejectionNote { get; set; }


        public DateTime CreatedAt { get; set; }
            = DateTime.Now;



        public ICollection<OrderItem>? OrderItems { get; set; }

        public Payment? Payment { get; set; }

    }
}
