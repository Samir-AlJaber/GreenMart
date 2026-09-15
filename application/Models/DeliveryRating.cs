using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class DeliveryRating
    {
        [Key]
        public int DeliveryRatingId { get; set; }

        public int DeliveryAssignmentId { get; set; }

        [ForeignKey(nameof(DeliveryAssignmentId))]
        public DeliveryAssignment DeliveryAssignment { get; set; } = null!;

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public User Customer { get; set; } = null!;

        public int DeliveryManId { get; set; }

        [ForeignKey(nameof(DeliveryManId))]
        public User DeliveryMan { get; set; } = null!;

        [Range(1, 5)]
        public int RatingValue { get; set; }

        [Required, MaxLength(20)]
        public string RatingLabel { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
