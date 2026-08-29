using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class Cart
    {

        [Key]
        public int CartId { get; set; }



        public int UserId { get; set; }



        [ForeignKey("UserId")]
        public User User { get; set; }



        public DateTime CreatedAt { get; set; }
            = DateTime.Now;



        public ICollection<CartItem>? CartItems { get; set; }

    }
}