using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class CartItem
    {

        [Key]
        public int CartItemId { get; set; }



        public int CartId { get; set; }



        [ForeignKey("CartId")]
        public Cart Cart { get; set; }



        public int ProductId { get; set; }



        [ForeignKey("ProductId")]
        public Product Product { get; set; }



        public int Quantity { get; set; } = 1;



        public DateTime AddedAt { get; set; }
            = DateTime.Now;

    }
}