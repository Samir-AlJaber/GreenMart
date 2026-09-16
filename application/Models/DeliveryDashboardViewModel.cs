namespace GreenMart.Models
{
    public class DeliveryDashboardViewModel
    {
        public DeliveryManApplication Application { get; set; } = null!;

        public List<DeliveryAssignment> Assignments { get; set; } = new();
    }
}
