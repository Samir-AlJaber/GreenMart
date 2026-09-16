using GreenMart.Data;
using GreenMart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenMart.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AdminController(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
    }
}
