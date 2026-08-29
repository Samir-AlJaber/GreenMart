using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class Product
    {

        [Key]
        public int ProductId { get; set; }



        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; }



        public string? Brand { get; set; }



        public string? Description { get; set; }



        [Required(ErrorMessage = "Price is required")]
        public decimal Price { get; set; }



        public int StockQuantity { get; set; }



        public bool IsActive { get; set; } = true;



        public DateTime CreatedAt { get; set; }
            = DateTime.Now;





        public int UserId { get; set; }



        [ForeignKey("UserId")]
        public User? User { get; set; }





        public int CategoryId { get; set; }



        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }





        public ICollection<OrderItem>? OrderItems { get; set; }



        public ICollection<Review>? Reviews { get; set; }

    }
}