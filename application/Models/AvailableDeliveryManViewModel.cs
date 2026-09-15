namespace GreenMart.Models
{
    public class AvailableDeliveryManViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }
}
