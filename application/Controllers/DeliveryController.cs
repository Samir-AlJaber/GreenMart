using GreenMart.Data;
using GreenMart.Models;
using GreenMart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenMart.Controllers
{
    [Authorize]
    public class DeliveryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISellerPayoutService _sellerPayoutService;

        public DeliveryController(
            ApplicationDbContext context,
            ISellerPayoutService sellerPayoutService)
        {
            _context = context;
            _sellerPayoutService = sellerPayoutService;
        }

        [Authorize(Roles = "DeliveryMan")]
        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!TryGetUserId(out var userId)) return RedirectToAction("Login", "Account");

            var application = GetApprovedApplication(userId);
            if (application == null) return Forbid();

            var assignments = _context.DeliveryAssignments
                .Include(x => x.Order).ThenInclude(x => x.User)
                .Include(x => x.Order).ThenInclude(x => x.OrderItems)!.ThenInclude(x => x.Product)
                .Include(x => x.Seller)
                .Include(x => x.Rating)
                .Where(x => x.DeliveryManId == userId)
                .OrderBy(x => x.Status == "Delivered" ? 1 : 0)
                .ThenByDescending(x => x.AssignedAt)
                .ToList();

            return View(new DeliveryDashboardViewModel { Application = application, Assignments = assignments });
        }

        [Authorize(Roles = "DeliveryMan")]
        [HttpGet]
        public IActionResult Profile()
        {
            if (!TryGetUserId(out var userId)) return RedirectToAction("Login", "Account");

            var application = GetApprovedApplication(userId);
            if (application == null) return Forbid();

            var ratings = _context.DeliveryRatings
                .Include(x => x.Customer)
                .Where(x => x.DeliveryManId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(new DeliveryProfileViewModel
            {
                Application = application,
                AverageRating = ratings.Any() ? ratings.Average(x => x.RatingValue) : 0,
                RatingCount = ratings.Count,
                RecentRatings = ratings.Take(8).ToList()
            });
        }

        [Authorize(Roles = "DeliveryMan")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetAvailability(bool isAvailable)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var application = GetApprovedApplication(userId);
            if (application == null) return Forbid();

            var hasActiveDelivery = _context.DeliveryAssignments.Any(x => x.DeliveryManId == userId && x.Status != "Delivered");
            if (isAvailable && hasActiveDelivery)
            {
                TempData["DeliveryMessage"] = "Finish your active delivery before becoming available again.";
                return RedirectToAction(nameof(Profile));
            }

            application.IsAvailable = isAvailable;
            _context.SaveChanges();
            TempData["DeliveryMessage"] = isAvailable
                ? "You are now available for a new delivery."
                : "You are now offline and will not appear in seller assignment lists.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(
            int orderId,
            int deliveryManId,
            string pickupAddress,
            string pickupPhone,
            string? pickupInstructions,
            decimal? pickupLatitude,
            decimal? pickupLongitude)
        {
            if (!TryGetUserId(out var sellerId)) return Unauthorized();

            var order = _context.Orders.Include(x => x.OrderItems)!.ThenInclude(x => x.Product)
                .FirstOrDefault(x => x.OrderId == orderId);

            if (order == null || !order.OrderItems!.Any(x => x.Product.UserId == sellerId)) return Forbid();

            if (order.Status != "Confirmed")
            {
                TempData["OrderMessage"] = "Confirm the order before assigning a delivery partner.";
                return RedirectToAction("OwnerOrders", "Order");
            }

            pickupAddress = pickupAddress?.Trim() ?? string.Empty;
            pickupPhone = pickupPhone?.Trim() ?? string.Empty;
            pickupInstructions = string.IsNullOrWhiteSpace(pickupInstructions)
                ? null
                : pickupInstructions.Trim();

            if (pickupAddress.Length == 0 || pickupAddress.Length > 300 ||
                pickupPhone.Length == 0 || pickupPhone.Length > 20 ||
                pickupInstructions?.Length > 500 ||
                !CoordinatesAreValid(pickupLatitude, pickupLongitude))
            {
                TempData["OrderMessage"] = "Please provide a valid pickup address and phone number.";
                return RedirectToAction("OwnerOrders", "Order");
            }

            var deliveryApplication = _context.DeliveryManApplications.FirstOrDefault(x =>
                x.UserId == deliveryManId && x.Status == "Approved" && x.IsAvailable);

            if (deliveryApplication == null)
            {
                TempData["OrderMessage"] = "That delivery partner is no longer available. Please select another one.";
                return RedirectToAction("OwnerOrders", "Order");
            }

            var assignment = _context.DeliveryAssignments.FirstOrDefault(x => x.OrderId == orderId && x.SellerId == sellerId);
            if (assignment != null && assignment.Status != "Assigned")
            {
                TempData["OrderMessage"] = "This delivery has already started and cannot be reassigned.";
                return RedirectToAction("OwnerOrders", "Order");
            }

            if (assignment == null)
            {
                assignment = new DeliveryAssignment
                {
                    OrderId = orderId, SellerId = sellerId, DeliveryManId = deliveryManId,
                    Status = "Assigned", AssignedAt = DateTime.Now,
                    PickupAddress = pickupAddress,
                    PickupLatitude = pickupLatitude.HasValue ? decimal.Round(pickupLatitude.Value, 6) : null,
                    PickupLongitude = pickupLongitude.HasValue ? decimal.Round(pickupLongitude.Value, 6) : null,
                    PickupPhone = pickupPhone,
                    PickupInstructions = pickupInstructions
                };
                _context.DeliveryAssignments.Add(assignment);
            }
            else
            {
                if (assignment.DeliveryManId != deliveryManId)
                {
                    var previous = _context.DeliveryManApplications.FirstOrDefault(x => x.UserId == assignment.DeliveryManId);
                    if (previous != null) previous.IsAvailable = true;
                }
                assignment.DeliveryManId = deliveryManId;
                assignment.AssignedAt = DateTime.Now;
                assignment.PickupAddress = pickupAddress;
                assignment.PickupLatitude = pickupLatitude.HasValue ? decimal.Round(pickupLatitude.Value, 6) : null;
                assignment.PickupLongitude = pickupLongitude.HasValue ? decimal.Round(pickupLongitude.Value, 6) : null;
                assignment.PickupPhone = pickupPhone;
                assignment.PickupInstructions = pickupInstructions;
            }

            deliveryApplication.IsAvailable = false;
            _context.SaveChanges();
            TempData["OrderMessage"] = "Delivery partner assigned successfully.";
            return RedirectToAction("OwnerOrders", "Order");
        }

        [Authorize(Roles = "DeliveryMan")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int assignmentId,
            string status,
            CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var deliveryManId)) return Unauthorized();

            var assignment = _context.DeliveryAssignments.FirstOrDefault(x =>
                x.DeliveryAssignmentId == assignmentId && x.DeliveryManId == deliveryManId);
            if (assignment == null) return NotFound();

            var nextStatus = assignment.Status switch
            {
                "Assigned" => "GoingToPickup",
                "GoingToPickup" => "ArrivedAtPickup",
                "ArrivedAtPickup" => "PickedUp",
                "PickedUp" => "OnTheWay",
                "OnTheWay" => "Delivered",
                _ => null
            };

            if (nextStatus == null || status != nextStatus)
            {
                TempData["DeliveryMessage"] = "That delivery status change is not allowed.";
                return RedirectToAction(nameof(Dashboard));
            }

            assignment.Status = nextStatus;
            if (nextStatus == "GoingToPickup")
            {
                assignment.PickupStartedAt = DateTime.Now;
            }
            else if (nextStatus == "ArrivedAtPickup")
            {
                assignment.ArrivedAtPickupAt = DateTime.Now;
            }
            else if (nextStatus == "PickedUp")
            {
                assignment.PickedUpAt = DateTime.Now;
            }
            else if (nextStatus == "Delivered")
            {
                assignment.DeliveredAt = DateTime.Now;
                assignment.IsLocationSharing = false;
                var application = _context.DeliveryManApplications.FirstOrDefault(x => x.UserId == deliveryManId);
                if (application != null) application.IsAvailable = true;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (nextStatus == "Delivered")
            {
                await _sellerPayoutService.CreateEarningForDeliveredAssignmentAsync(
                    assignment.DeliveryAssignmentId,
                    cancellationToken);
            }

            TempData["DeliveryMessage"] = $"Delivery marked as {FormatStatus(nextStatus)}.";
            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Roles = "DeliveryMan")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateLocation(int assignmentId, decimal latitude, decimal longitude)
        {
            if (!TryGetUserId(out var deliveryManId)) return Unauthorized();

            var assignment = _context.DeliveryAssignments.FirstOrDefault(x =>
                x.DeliveryAssignmentId == assignmentId &&
                x.DeliveryManId == deliveryManId &&
                x.Status != "Delivered");

            if (assignment == null) return NotFound();
            if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            {
                return BadRequest(new { success = false, message = "Invalid location." });
            }

            assignment.CurrentLatitude = decimal.Round(latitude, 6);
            assignment.CurrentLongitude = decimal.Round(longitude, 6);
            assignment.LastLocationUpdatedAt = DateTime.Now;
            assignment.IsLocationSharing = true;
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                updatedAt = assignment.LastLocationUpdatedAt.Value.ToString("o")
            });
        }

        [Authorize(Roles = "DeliveryMan")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StopLocationSharing(int assignmentId)
        {
            if (!TryGetUserId(out var deliveryManId)) return Unauthorized();

            var assignment = _context.DeliveryAssignments.FirstOrDefault(x =>
                x.DeliveryAssignmentId == assignmentId &&
                x.DeliveryManId == deliveryManId);
            if (assignment == null) return NotFound();

            assignment.IsLocationSharing = false;
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public IActionResult Track(int assignmentId)
        {
            if (!TryGetUserId(out var userId)) return RedirectToAction("Login", "Account");

            var assignment = _context.DeliveryAssignments
                .Include(x => x.DeliveryMan)
                .Include(x => x.Seller)
                .Include(x => x.Order).ThenInclude(x => x.User)
                .FirstOrDefault(x => x.DeliveryAssignmentId == assignmentId);

            if (assignment == null) return NotFound();
            if (assignment.SellerId != userId && assignment.Order.UserId != userId) return Forbid();

            return View(assignment);
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public IActionResult GetTrackingLocation(int assignmentId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var assignment = _context.DeliveryAssignments
                .Include(x => x.DeliveryMan)
                .Include(x => x.Order)
                .FirstOrDefault(x => x.DeliveryAssignmentId == assignmentId);

            if (assignment == null) return NotFound();
            if (assignment.SellerId != userId && assignment.Order.UserId != userId) return Forbid();

            var isFresh = assignment.LastLocationUpdatedAt.HasValue &&
                          assignment.LastLocationUpdatedAt.Value > DateTime.Now.AddSeconds(-45);

            return Json(new
            {
                hasLocation = assignment.CurrentLatitude.HasValue && assignment.CurrentLongitude.HasValue,
                latitude = assignment.CurrentLatitude,
                longitude = assignment.CurrentLongitude,
                isLive = assignment.IsLocationSharing && isFresh && assignment.Status != "Delivered",
                status = FormatStatus(assignment.Status),
                deliveryMan = assignment.DeliveryMan.FullName,
                updatedAt = assignment.LastLocationUpdatedAt?.ToString("o")
            });
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rate(int assignmentId, int ratingValue)
        {
            if (!TryGetUserId(out var customerId)) return Unauthorized();

            var assignment = _context.DeliveryAssignments.Include(x => x.Order).Include(x => x.Rating)
                .FirstOrDefault(x => x.DeliveryAssignmentId == assignmentId);
            if (assignment == null || assignment.Order.UserId != customerId) return Forbid();

            if (assignment.Status != "Delivered" || assignment.Rating != null || ratingValue < 1 || ratingValue > 5)
            {
                TempData["OrderMessage"] = "This delivery cannot be rated right now.";
                return RedirectToAction("MyOrders", "Order");
            }

            _context.DeliveryRatings.Add(new DeliveryRating
            {
                DeliveryAssignmentId = assignmentId, CustomerId = customerId,
                DeliveryManId = assignment.DeliveryManId, RatingValue = ratingValue,
                RatingLabel = RatingLabel(ratingValue), CreatedAt = DateTime.Now
            });
            _context.SaveChanges();
            TempData["OrderMessage"] = "Thank you. Your delivery rating has been saved.";
            return RedirectToAction("MyOrders", "Order");
        }

        private DeliveryManApplication? GetApprovedApplication(int userId) =>
            _context.DeliveryManApplications.Include(x => x.User)
                .FirstOrDefault(x => x.UserId == userId && x.Status == "Approved");

        private static bool CoordinatesAreValid(decimal? latitude, decimal? longitude)
        {
            if (!latitude.HasValue && !longitude.HasValue) return true;
            if (!latitude.HasValue || !longitude.HasValue) return false;

            return latitude.Value is >= -90 and <= 90 &&
                   longitude.Value is >= -180 and <= 180;
        }

        private bool TryGetUserId(out int userId) => int.TryParse(User.FindFirst("UserId")?.Value, out userId);

        private static string RatingLabel(int value) => value switch
        {
            1 => "Poor", 2 => "Fair", 3 => "Good", 4 => "Very Good", 5 => "Excellent", _ => "Not rated"
        };

        private static string FormatStatus(string status) => status switch
        {
            "GoingToPickup" => "going to pickup",
            "ArrivedAtPickup" => "arrived at pickup",
            "PickedUp" => "picked up",
            "OnTheWay" => "on the way",
            _ => status.ToLowerInvariant()
        };
    }
}
