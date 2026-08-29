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



        public string? ShippingAddress { get; set; }



        public DateTime CreatedAt { get; set; }
            = DateTime.Now;



        public ICollection<OrderItem>? OrderItems { get; set; }

    }
}