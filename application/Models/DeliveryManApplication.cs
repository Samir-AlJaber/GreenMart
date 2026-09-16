using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class DeliveryManApplication
    {
        [Key]
        public int DeliveryManApplicationId { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required, MaxLength(30)]
        public string NidNumber { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string VehicleType { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByUserId { get; set; }

        public bool IsAvailable { get; set; } = false;
    }
}
