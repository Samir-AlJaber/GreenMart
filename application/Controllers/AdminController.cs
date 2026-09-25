using GreenMart.Data;
using GreenMart.Services;
using GreenMart.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GreenMart.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISellerPayoutService _sellerPayoutService;
        private readonly PayoutSettings _payoutSettings;

        public AdminController(
            ApplicationDbContext context,
            IEmailService emailService,
            ISellerPayoutService sellerPayoutService,
            IOptions<PayoutSettings> payoutSettings)
        {
            _context = context;
            _emailService = emailService;
            _sellerPayoutService = sellerPayoutService;
            _payoutSettings = payoutSettings.Value;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var applications = _context.DeliveryManApplications
                .Include(x => x.User)
                .OrderBy(x => x.Status == "Pending" ? 0 : 1)
                .ThenByDescending(x => x.SubmittedAt)
                .ToList();

            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Admins(CancellationToken cancellationToken)
        {
            return View(await BuildAdminManagementViewModelAsync(cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(
            [Bind(Prefix = "NewAdmin")] CreateAdminViewModel model,
            CancellationToken cancellationToken)
        {
            var currentAdmin = await GetCurrentAdminAsync(cancellationToken);
            if (currentAdmin == null)
            {
                return Forbid();
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword ?? string.Empty, currentAdmin.PasswordHash))
            {
                ModelState.AddModelError(
                    "NewAdmin.CurrentPassword",
                    "Your current admin password is incorrect.");
            }

            var normalizedEmail = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            if (await _context.Users.AnyAsync(
                    x => x.Email.ToLower() == normalizedEmail,
                    cancellationToken))
            {
                ModelState.AddModelError("NewAdmin.Email", "An account already uses this email address.");
            }

            if (!ModelState.IsValid)
            {
                var viewModel = await BuildAdminManagementViewModelAsync(cancellationToken);
                viewModel.NewAdmin = model;
                viewModel.NewAdmin.Password = string.Empty;
                viewModel.NewAdmin.ConfirmPassword = string.Empty;
                viewModel.NewAdmin.CurrentPassword = string.Empty;
                return View("Admins", viewModel);
            }

            _context.Users.Add(new User
            {
                FullName = model.FullName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = model.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12),
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync(cancellationToken);

            TempData["AdminManagementMessage"] = $"{normalizedEmail} was added as an administrator.";
            return RedirectToAction(nameof(Admins));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(
            int userId,
            string currentPassword,
            CancellationToken cancellationToken)
        {
            var currentAdmin = await GetCurrentAdminAsync(cancellationToken);
            if (currentAdmin == null)
            {
                return Forbid();
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword ?? string.Empty, currentAdmin.PasswordHash))
            {
                TempData["AdminManagementError"] = "Your current admin password is incorrect.";
                return RedirectToAction(nameof(Admins));
            }

            if (userId == currentAdmin.UserId)
            {
                TempData["AdminManagementError"] = "You cannot remove your own administrator access.";
                return RedirectToAction(nameof(Admins));
            }

            var admin = await _context.Users.FirstOrDefaultAsync(
                x => x.UserId == userId && x.Role == "Admin" && x.IsActive,
                cancellationToken);
            if (admin == null)
            {
                return NotFound();
            }

            if (await _context.Users.CountAsync(
                    x => x.Role == "Admin" && x.IsActive,
                    cancellationToken) <= 1)
            {
                TempData["AdminManagementError"] = "The final active administrator cannot be removed.";
                return RedirectToAction(nameof(Admins));
            }

            // Preserve the account and its related records while removing privileged access.
            admin.Role = "User";
            await _context.SaveChangesAsync(cancellationToken);

            TempData["AdminManagementMessage"] = $"Administrator access was removed from {admin.Email}.";
            return RedirectToAction(nameof(Admins));
        }

        private async Task<User?> GetCurrentAdminAsync(CancellationToken cancellationToken)
        {
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out var userId))
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(
                x => x.UserId == userId && x.Role == "Admin" && x.IsActive,
                cancellationToken);
        }

        private async Task<AdminManagementViewModel> BuildAdminManagementViewModelAsync(
            CancellationToken cancellationToken)
        {
            _ = int.TryParse(User.FindFirst("UserId")?.Value, out var currentAdminId);

            return new AdminManagementViewModel
            {
                CurrentAdminId = currentAdminId,
                Administrators = await _context.Users
                    .AsNoTracking()
                    .Where(x => x.Role == "Admin" && x.IsActive)
                    .OrderBy(x => x.FullName)
                    .ToListAsync(cancellationToken)
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDeliveryMan(int applicationId)
        {
            var application = _context.DeliveryManApplications
                .Include(x => x.User)
                .FirstOrDefault(x =>
                    x.DeliveryManApplicationId == applicationId
                );

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != "Pending")
            {
                TempData["AdminMessage"] = "This request has already been reviewed.";
                return RedirectToAction(nameof(Dashboard));
            }

            var adminIdClaim = User.FindFirst("UserId")?.Value;

            application.Status = "Approved";
            application.ReviewedAt = DateTime.Now;
            application.ReviewedByUserId = int.TryParse(adminIdClaim, out var adminId)
                ? adminId
                : null;
            application.User.Role = "DeliveryMan";
            application.IsAvailable = true;

            _context.SaveChanges();

            var emailSent = await _emailService.SendDeliveryApprovalAsync(
                application.User.Email,
                application.User.FullName
            );

            TempData["AdminMessage"] = emailSent
                ? $"{application.User.FullName} is now an approved delivery partner. An email was sent."
                : $"{application.User.FullName} is now an approved delivery partner. Configure email settings to send approval emails.";

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Payouts(CancellationToken cancellationToken)
        {
            await _sellerPayoutService.ProcessDueEarningsAsync(cancellationToken);

            return View(new AdminPayoutDashboardViewModel
            {
                PendingAccounts = await _context.SellerPayoutAccounts
                    .Include(x => x.Seller)
                    .Where(x => x.Status == "PendingVerification")
                    .OrderBy(x => x.UpdatedAt)
                    .ToListAsync(cancellationToken),
                CodEarningsAwaitingRemittance = await _context.SellerEarnings
                    .Include(x => x.Seller)
                    .Include(x => x.Order)
                    .Include(x => x.DeliveryAssignment)
                    .Where(x => x.Status == "AwaitingRemittance")
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync(cancellationToken),
                Payouts = await _context.SellerPayouts
                    .Include(x => x.Seller)
                    .Include(x => x.Earning).ThenInclude(x => x.Order)
                    .OrderByDescending(x => x.RequestedAt)
                    .Take(100)
                    .ToListAsync(cancellationToken)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayoutAccount(
            int accountId,
            CancellationToken cancellationToken)
        {
            var account = await _context.SellerPayoutAccounts
                .FirstOrDefaultAsync(x => x.SellerPayoutAccountId == accountId, cancellationToken);
            if (account == null) return NotFound();

            account.Status = "Verified";
            account.VerifiedAt = DateTime.Now;
            account.VerifiedByUserId = int.TryParse(User.FindFirst("UserId")?.Value, out var adminId)
                ? adminId
                : null;
            await _context.SaveChangesAsync(cancellationToken);
            await _sellerPayoutService.ProcessDueEarningsAsync(cancellationToken);

            TempData["PayoutAdminMessage"] = "The seller payout account is verified.";
            return RedirectToAction(nameof(Payouts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCodRemittance(
            int earningId,
            CancellationToken cancellationToken)
        {
            var earning = await _context.SellerEarnings
                .Include(x => x.DeliveryAssignment)
                .FirstOrDefaultAsync(
                    x => x.SellerEarningId == earningId && x.Status == "AwaitingRemittance",
                    cancellationToken);
            if (earning == null) return NotFound();

            var deliveredAt = earning.DeliveryAssignment.DeliveredAt ?? earning.CreatedAt;
            earning.Status = "PendingRelease";
            earning.AvailableAt = deliveredAt.AddHours(Math.Max(0, _payoutSettings.ReleaseDelayHours));
            earning.StatusNote = "Cash on Delivery funds were received by GreenMart.";
            await _context.SaveChangesAsync(cancellationToken);
            await _sellerPayoutService.ProcessDueEarningsAsync(cancellationToken);

            TempData["PayoutAdminMessage"] = "Cash on Delivery remittance was confirmed.";
            return RedirectToAction(nameof(Payouts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteManualPayout(
            int payoutId,
            string externalReference,
            CancellationToken cancellationToken)
        {
            externalReference = externalReference?.Trim() ?? string.Empty;
            if (externalReference.Length is < 4 or > 100)
            {
                TempData["PayoutAdminMessage"] = "Enter the bank or bKash transaction reference before confirming payment.";
                return RedirectToAction(nameof(Payouts));
            }

            var payout = await _context.SellerPayouts
                .Include(x => x.Earning)
                .FirstOrDefaultAsync(x => x.SellerPayoutId == payoutId, cancellationToken);
            if (payout == null) return NotFound();
            if (payout.Status == "Completed")
            {
                TempData["PayoutAdminMessage"] = "This payout was already completed.";
                return RedirectToAction(nameof(Payouts));
            }

            if (await _context.SellerPayouts.AnyAsync(
                    x => x.SellerPayoutId != payoutId && x.ExternalReference == externalReference,
                    cancellationToken))
            {
                TempData["PayoutAdminMessage"] = "That transaction reference is already attached to another payout.";
                return RedirectToAction(nameof(Payouts));
            }

            payout.Status = "Completed";
            payout.ExternalReference = externalReference;
            payout.FailureReason = null;
            payout.ProcessedAt = DateTime.Now;
            payout.Earning.Status = "Paid";
            payout.Earning.StatusNote = "Seller payout completed.";
            payout.Earning.PaidAt = payout.ProcessedAt;
            await _context.SaveChangesAsync(cancellationToken);

            TempData["PayoutAdminMessage"] = "The seller payout was recorded as completed.";
            return RedirectToAction(nameof(Payouts));
        }
    }
}
