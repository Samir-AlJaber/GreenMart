namespace GreenMart.Models
{
    public class DeliveryProfileViewModel
    {
        public DeliveryManApplication Application { get; set; } = null!;
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public List<DeliveryRating> RecentRatings { get; set; } = new();
    }
}
