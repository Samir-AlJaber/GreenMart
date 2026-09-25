using GreenMart.Data;
using GreenMart.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GreenMart.Services
{
    public class SellerPayoutService : ISellerPayoutService
    {
        private readonly ApplicationDbContext _context;
        private readonly PayoutSettings _settings;

        public SellerPayoutService(
            ApplicationDbContext context,
            IOptions<PayoutSettings> settings)
        {
            _context = context;
            _settings = settings.Value;
        }

        public async Task CreateEarningForDeliveredAssignmentAsync(
            int deliveryAssignmentId,
            CancellationToken cancellationToken = default)
        {
            if (await _context.SellerEarnings.AnyAsync(
                    x => x.DeliveryAssignmentId == deliveryAssignmentId,
                    cancellationToken))
            {
                return;
            }

            var assignment = await _context.DeliveryAssignments
                .Include(x => x.Order).ThenInclude(x => x.OrderItems)!.ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(
                    x => x.DeliveryAssignmentId == deliveryAssignmentId && x.Status == "Delivered",
                    cancellationToken);

            if (assignment == null) return;

            var grossAmount = assignment.Order.OrderItems?
                .Where(x => x.Product.UserId == assignment.SellerId)
                .Sum(x => x.Price * x.Quantity) ?? 0;

            if (grossAmount <= 0) return;

            var isVerifiedOnlinePayment =
                assignment.Order.PaymentMethod == "Online" &&
                assignment.Order.PaymentStatus == "Paid";
            var isCashOnDelivery = assignment.Order.PaymentMethod == "CashOnDelivery";

            if (!isVerifiedOnlinePayment && !isCashOnDelivery) return;

            var deliveredAt = assignment.DeliveredAt ?? DateTime.Now;
            _context.SellerEarnings.Add(new SellerEarning
            {
                SellerId = assignment.SellerId,
                OrderId = assignment.OrderId,
                DeliveryAssignmentId = assignment.DeliveryAssignmentId,
                GrossAmount = grossAmount,
                GatewayFee = 0,
                CommissionAmount = 0,
                NetAmount = grossAmount,
                PaymentMethod = assignment.Order.PaymentMethod,
                Status = isCashOnDelivery ? "AwaitingRemittance" : "PendingRelease",
                StatusNote = isCashOnDelivery
                    ? "Waiting for the delivery partner's Cash on Delivery remittance."
                    : "Held during the post-delivery release window.",
                AvailableAt = isCashOnDelivery
                    ? null
                    : deliveredAt.AddHours(Math.Max(0, _settings.ReleaseDelayHours))
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ProcessDueEarningsAsync(CancellationToken cancellationToken = default)
        {
            var deliveredWithoutEarnings = await _context.DeliveryAssignments
                .Where(x => x.Status == "Delivered" &&
                            !_context.SellerEarnings.Any(e => e.DeliveryAssignmentId == x.DeliveryAssignmentId))
                .Select(x => x.DeliveryAssignmentId)
                .ToListAsync(cancellationToken);

            foreach (var assignmentId in deliveredWithoutEarnings)
            {
                await CreateEarningForDeliveredAssignmentAsync(assignmentId, cancellationToken);
            }

            var now = DateTime.Now;
            var earnings = await _context.SellerEarnings
                .Include(x => x.Payout)
                .Where(x =>
                    (x.Status == "PendingRelease" || x.Status == "AwaitingPayoutAccount") &&
                    x.AvailableAt.HasValue && x.AvailableAt <= now)
                .ToListAsync(cancellationToken);

            if (earnings.Count == 0) return;

            var sellerIds = earnings.Select(x => x.SellerId).Distinct().ToList();
            var accounts = await _context.SellerPayoutAccounts
                .Where(x => sellerIds.Contains(x.SellerId) && x.IsActive && x.Status == "Verified")
                .ToDictionaryAsync(x => x.SellerId, cancellationToken);

            foreach (var earning in earnings)
            {
                if (!accounts.TryGetValue(earning.SellerId, out var account))
                {
                    earning.Status = "AwaitingPayoutAccount";
                    earning.StatusNote = "Add a payout account and wait for admin verification.";
                    continue;
                }

                if (earning.Payout == null)
                {
                    _context.SellerPayouts.Add(new SellerPayout
                    {
                        SellerEarningId = earning.SellerEarningId,
                        SellerId = earning.SellerId,
                        Provider = account.Method,
                        AccountNumberSnapshot = account.AccountNumber,
                        Amount = earning.NetAmount,
                        Status = "AwaitingProviderSetup",
                        FailureReason = "Approved payout-provider credentials are not connected. Admin may complete this payout manually."
                    });
                }

                earning.Status = "ReadyForPayout";
                earning.StatusNote = "Ready for an admin-recorded transfer while the payout provider is not connected.";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
