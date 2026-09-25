namespace GreenMart.Services
{
    public class SellerPayoutWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SellerPayoutWorker> _logger;

        public SellerPayoutWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SellerPayoutWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISellerPayoutService>();
                    await service.ProcessDueEarningsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "The seller payout worker could not process due earnings.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
