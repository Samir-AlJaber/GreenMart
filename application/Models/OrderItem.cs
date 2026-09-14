using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class OrderItem
    {

        [Key]
        public int OrderItemId { get; set; }



        public int OrderId { get; set; }



        [ForeignKey("OrderId")]
        public Order Order { get; set; }



        public int ProductId { get; set; }



        [ForeignKey("ProductId")]
        public Product Product { get; set; }



        public int Quantity { get; set; }



        public decimal Price { get; set; }



        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

    }
}