using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class Review
    {

        [Key]
        public int ReviewId { get; set; }



        public int UserId { get; set; }



        [ForeignKey("UserId")]
        public User User { get; set; }



        public int ProductId { get; set; }



        [ForeignKey("ProductId")]
        public Product Product { get; set; }



        [Range(1, 5)]
        public int Rating { get; set; }



        public string? Comment { get; set; }



        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

    }
}