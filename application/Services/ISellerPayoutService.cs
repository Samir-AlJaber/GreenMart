namespace GreenMart.Services
{
    public interface ISellerPayoutService
    {
        Task CreateEarningForDeliveredAssignmentAsync(
            int deliveryAssignmentId,
            CancellationToken cancellationToken = default);

        Task ProcessDueEarningsAsync(CancellationToken cancellationToken = default);
    }
}
