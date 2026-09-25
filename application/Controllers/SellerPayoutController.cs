using GreenMart.Data;
using GreenMart.Models;
using GreenMart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenMart.Controllers
{
    [Authorize(Roles = "User")]
    public class SellerPayoutController : Controller
    {
        private static readonly HashSet<string> AllowedMethods =
            new(StringComparer.OrdinalIgnoreCase) { "Bkash", "Bank" };

        private readonly ApplicationDbContext _context;
        private readonly ISellerPayoutService _payoutService;

        public SellerPayoutController(
            ApplicationDbContext context,
            ISellerPayoutService payoutService)
        {
            _context = context;
            _payoutService = payoutService;
        }

        [HttpGet]
        public async Task<IActionResult> Wallet(CancellationToken cancellationToken)
        {
            var sellerId = CurrentUserId();
            await _payoutService.ProcessDueEarningsAsync(cancellationToken);

            var account = await _context.SellerPayoutAccounts
                .FirstOrDefaultAsync(x => x.SellerId == sellerId, cancellationToken);
            var earnings = await _context.SellerEarnings
                .Include(x => x.Order)
                .Include(x => x.Payout)
                .Where(x => x.SellerId == sellerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return View(new SellerWalletViewModel
            {
                Account = account,
                Earnings = earnings,
                PendingBalance = earnings
                    .Where(x => x.Status is "PendingRelease" or "AwaitingRemittance" or "AwaitingPayoutAccount")
                    .Sum(x => x.NetAmount),
                AvailableBalance = earnings
                    .Where(x => x.Status is "ReadyForPayout" or "PayoutFailed")
                    .Sum(x => x.NetAmount),
                PaidBalance = earnings
                    .Where(x => x.Status == "Paid")
                    .Sum(x => x.NetAmount)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAccount(
            string method,
            string accountHolderName,
            string accountNumber,
            string? bankName,
            string? branchName,
            string? routingNumber,
            CancellationToken cancellationToken)
        {
            method = method?.Trim() ?? string.Empty;
            accountHolderName = accountHolderName?.Trim() ?? string.Empty;
            accountNumber = accountNumber?.Trim().Replace(" ", string.Empty) ?? string.Empty;

            if (!AllowedMethods.Contains(method) || accountHolderName.Length is < 2 or > 120)
            {
                TempData["PayoutMessage"] = "Please provide a valid payout method and account-holder name.";
                return RedirectToAction(nameof(Wallet));
            }

            if (method.Equals("Bkash", StringComparison.OrdinalIgnoreCase) &&
                (accountNumber.Length != 11 || !accountNumber.All(char.IsDigit)))
            {
                TempData["PayoutMessage"] = "Enter a valid 11-digit bKash account number.";
                return RedirectToAction(nameof(Wallet));
            }

            if (method.Equals("Bank", StringComparison.OrdinalIgnoreCase) &&
                (accountNumber.Length is < 6 or > 40 || string.IsNullOrWhiteSpace(bankName)))
            {
                TempData["PayoutMessage"] = "Bank payouts require a valid account number and bank name.";
                return RedirectToAction(nameof(Wallet));
            }

            var sellerId = CurrentUserId();
            var account = await _context.SellerPayoutAccounts
                .FirstOrDefaultAsync(x => x.SellerId == sellerId, cancellationToken);

            if (account == null)
            {
                account = new SellerPayoutAccount { SellerId = sellerId };
                _context.SellerPayoutAccounts.Add(account);
            }

            account.Method = method.Equals("Bank", StringComparison.OrdinalIgnoreCase) ? "Bank" : "Bkash";
            account.AccountHolderName = accountHolderName;
            account.AccountNumber = accountNumber;
            account.BankName = account.Method == "Bank" ? bankName?.Trim() : null;
            account.BranchName = account.Method == "Bank" ? branchName?.Trim() : null;
            account.RoutingNumber = account.Method == "Bank" ? routingNumber?.Trim() : null;
            account.Status = "PendingVerification";
            account.VerifiedAt = null;
            account.VerifiedByUserId = null;
            account.IsActive = true;
            account.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
            TempData["PayoutMessage"] = "Your payout account was saved and sent to the admin for verification.";
            return RedirectToAction(nameof(Wallet));
        }

        private int CurrentUserId() => int.Parse(User.FindFirst("UserId")!.Value);
    }
}
