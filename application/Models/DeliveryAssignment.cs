using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMart.Models
{
    public class DeliveryAssignment
    {
        [Key]
        public int DeliveryAssignmentId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        public int DeliveryManId { get; set; }

        [ForeignKey(nameof(DeliveryManId))]
        public User DeliveryMan { get; set; } = null!;

        public int SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public User Seller { get; set; } = null!;

        [Required, MaxLength(300)]
        public string PickupAddress { get; set; } = string.Empty;

        [Column(TypeName = "decimal(9,6)")]
        public decimal? PickupLatitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? PickupLongitude { get; set; }

        [Required, MaxLength(20)]
        public string PickupPhone { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? PickupInstructions { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Assigned";

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? PickupStartedAt { get; set; }

        public DateTime? ArrivedAtPickupAt { get; set; }

        public DateTime? PickedUpAt { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? CurrentLatitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? CurrentLongitude { get; set; }

        public DateTime? LastLocationUpdatedAt { get; set; }

        public bool IsLocationSharing { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DeliveryRating? Rating { get; set; }
    }
}
