using GreenMart.Data;
using GreenMart.Models;
using GreenMart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GreenMart.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISslCommerzService _sslCommerzService;

        public PaymentController(ApplicationDbContext context, ISslCommerzService sslCommerzService)
        {
            _context = context;
            _sslCommerzService = sslCommerzService;
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Success(CancellationToken cancellationToken)
        {
            var values = await ReadCallbackValues(cancellationToken);
            var outcome = await VerifyAndCompletePayment(
                values.GetValueOrDefault("tran_id"),
                values.GetValueOrDefault("val_id"),
                cancellationToken);

            return RedirectToAction(nameof(Result), new { orderId = outcome.OrderId, callbackStatus = outcome.Status });
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Fail(CancellationToken cancellationToken)
        {
            var values = await ReadCallbackValues(cancellationToken);
            var orderId = await ReleaseReservedOrder(
                values.GetValueOrDefault("tran_id"), "Failed", cancellationToken);

            return RedirectToAction(nameof(Result), new { orderId, callbackStatus = "Failed" });
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
        {
            var values = await ReadCallbackValues(cancellationToken);
            var orderId = await ReleaseReservedOrder(
                values.GetValueOrDefault("tran_id"), "Cancelled", cancellationToken);

            return RedirectToAction(nameof(Result), new { orderId, callbackStatus = "Cancelled" });
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Ipn(CancellationToken cancellationToken)
        {
            var values = await ReadCallbackValues(cancellationToken);
            var status = values.GetValueOrDefault("status");

            if (status?.Equals("VALID", StringComparison.OrdinalIgnoreCase) == true)
            {
                var outcome = await VerifyAndCompletePayment(
                    values.GetValueOrDefault("tran_id"),
                    values.GetValueOrDefault("val_id"),
                    cancellationToken);

                return outcome.Status == "Paid" ? Ok() : BadRequest();
            }

            await ReleaseReservedOrder(
                values.GetValueOrDefault("tran_id"),
                status?.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) == true ? "Cancelled" : "Failed",
                cancellationToken);

            return Ok();
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> Result(int? orderId, string? callbackStatus, CancellationToken cancellationToken)
        {
            if (!orderId.HasValue || !int.TryParse(User.FindFirst("UserId")?.Value, out var userId))
            {
                return RedirectToAction("MyOrders", "Order");
            }

            var payment = await _context.Payments
                .Include(x => x.Order)
                .FirstOrDefaultAsync(
                    x => x.OrderId == orderId.Value && x.Order.UserId == userId,
                    cancellationToken);

            if (payment == null) return NotFound();

            var effectiveStatus = payment.Status == "Paid" ? "Paid" : callbackStatus ?? payment.Status;
            var message = effectiveStatus switch
            {
                "Paid" => "Your payment was verified and your order is confirmed.",
                "Cancelled" => "You cancelled the payment. Your cart items are still available.",
                "Failed" => "The payment was not completed. No online payment was recorded.",
                _ => "We could not verify this payment. Please check My Orders before trying again."
            };

            return View(new PaymentResultViewModel
            {
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Status = effectiveStatus,
                PaymentMethod = payment.SelectedChannel ?? payment.Method,
                Message = message,
                CanDownloadReceipt = payment.Status == "Paid"
            });
        }

        private async Task<(int? OrderId, string Status)> VerifyAndCompletePayment(
            string? transactionId,
            string? validationId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(validationId))
                return (null, "Invalid");

            var validation = await _sslCommerzService.ValidatePaymentAsync(validationId, cancellationToken);
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var payment = await _context.Payments
                .Include(x => x.Order).ThenInclude(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);

            if (payment == null) return (null, "Invalid");

            var verified = validation.IsValid &&
                           string.Equals(validation.TransactionId, payment.TransactionId, StringComparison.Ordinal) &&
                           validation.Amount == payment.Amount &&
                           string.Equals(validation.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase) &&
                           validation.RiskLevel == 0;

            if (!verified) return (payment.OrderId, "Invalid");
            if (payment.Status == "Paid") return (payment.OrderId, "Paid");
            if (payment.Status != "Pending") return (payment.OrderId, payment.Status);

            payment.Status = "Paid";
            payment.ValidationId = validationId;
            payment.GatewayTransactionId = validation.GatewayTransactionId;
            payment.BankTransactionId = validation.BankTransactionId;
            payment.CardType = validation.CardType;
            payment.PaidAt = DateTime.Now;
            payment.Order.PaymentStatus = "Paid";
            payment.Order.Status = "Pending";

            var orderedProductIds = payment.Order.OrderItems?.Select(x => x.ProductId).ToList() ?? new List<int>();
            var cartItems = await _context.CartItems
                .Include(x => x.Cart)
                .Where(x => x.Cart.UserId == payment.Order.UserId && orderedProductIds.Contains(x.ProductId))
                .ToListAsync(cancellationToken);

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (payment.OrderId, "Paid");
        }

        private async Task<int?> ReleaseReservedOrder(
            string? transactionId,
            string status,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return null;

            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var payment = await _context.Payments
                .Include(x => x.Order).ThenInclude(x => x.OrderItems)!.ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);

            if (payment == null) return null;

            if (payment.Status == "Pending")
            {
                foreach (var item in payment.Order.OrderItems ?? Array.Empty<OrderItem>())
                    item.Product.StockQuantity += item.Quantity;

                payment.Status = status;
                payment.Order.PaymentStatus = status;
                payment.Order.Status = status == "Cancelled" ? "PaymentCancelled" : "PaymentFailed";
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return payment.OrderId;
        }

        private async Task<Dictionary<string, string>> ReadCallbackValues(CancellationToken cancellationToken)
        {
            var values = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);

            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                foreach (var item in form) values[item.Key] = item.Value.ToString();
            }

            return values;
        }
    }
}
