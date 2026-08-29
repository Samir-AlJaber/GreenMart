using System.ComponentModel.DataAnnotations;

namespace GreenMart.Models
{
    public class Category
    {

        [Key]
        public int CategoryId { get; set; }



        [Required(ErrorMessage = "Category name is required")]
        public string CategoryName { get; set; }



        public string? Description { get; set; }



        public bool IsActive { get; set; } = true;



        public DateTime CreatedAt { get; set; }
            = DateTime.Now;



        public ICollection<Product>? Products { get; set; }

    }
}