using GreenMart.Data;
using GreenMart.Models;
using GreenMart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GreenMart.Controllers
{
    [Authorize(Roles = "User")]
    public class CheckoutController : Controller
    {
        private static readonly HashSet<string> OnlineChannels =
            new(StringComparer.OrdinalIgnoreCase) { "bkash", "nagad", "rocket", "visa", "mastercard" };

        private readonly ApplicationDbContext _context;
        private readonly ISslCommerzService _sslCommerzService;

        public CheckoutController(ApplicationDbContext context, ISslCommerzService sslCommerzService)
        {
            _context = context;
            _sslCommerzService = sslCommerzService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = _context.Carts
                .Include(x => x.User)
                .Include(x => x.CartItems).ThenInclude(x => x.Product)
                .FirstOrDefault(x => x.UserId == CurrentUserId());

            if (cart?.CartItems == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        [HttpPost]
        public IActionResult ValidateOrder(
            string PhoneNumber,
            string ShippingAddress,
            decimal? ShippingLatitude,
            decimal? ShippingLongitude,
            string PaymentMethod,
            string? PaymentChannel)
        {
            var validation = ValidateCheckout(
                PhoneNumber, ShippingAddress, ShippingLatitude, ShippingLongitude,
                PaymentMethod, PaymentChannel);

            if (validation != null) return Json(validation);

            var hasCartItems = _context.Carts
                .Include(x => x.CartItems)
                .Any(x => x.UserId == CurrentUserId() && x.CartItems.Any());

            return hasCartItems
                ? Json(new { success = true })
                : Json(new { success = false, message = "Your cart is empty." });
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(
            string PhoneNumber,
            string ShippingAddress,
            decimal? ShippingLatitude,
            decimal? ShippingLongitude,
            string PaymentMethod,
            string? PaymentChannel,
            CancellationToken cancellationToken)
        {
            var validation = ValidateCheckout(
                PhoneNumber, ShippingAddress, ShippingLatitude, ShippingLongitude,
                PaymentMethod, PaymentChannel);

            if (validation != null) return Json(validation);

            var isCashOnDelivery = PaymentMethod.Equals("CashOnDelivery", StringComparison.OrdinalIgnoreCase);
            var userId = CurrentUserId();

            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var cart = await _context.Carts
                .Include(x => x.User)
                .Include(x => x.CartItems).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (cart?.CartItems == null || !cart.CartItems.Any())
            {
                return Json(new { success = false, message = "Your cart is empty." });
            }

            var unavailableItem = cart.CartItems.FirstOrDefault(x =>
                !x.Product.IsActive || x.Quantity <= 0 || x.Product.StockQuantity < x.Quantity);

            if (unavailableItem != null)
            {
                return Json(new
                {
                    success = false,
                    message = $"{unavailableItem.Product.ProductName} no longer has enough stock. Please update your cart."
                });
            }

            var totalAmount = cart.CartItems.Sum(x => x.Product.Price * x.Quantity);
            var order = new Order
            {
                UserId = userId,
                TotalAmount = totalAmount,
                ShippingAddress = ShippingAddress.Trim(),
                ShippingLatitude = ShippingLatitude.HasValue ? decimal.Round(ShippingLatitude.Value, 6) : null,
                ShippingLongitude = ShippingLongitude.HasValue ? decimal.Round(ShippingLongitude.Value, 6) : null,
                Status = isCashOnDelivery ? "Pending" : "PaymentPending",
                PaymentMethod = isCashOnDelivery ? "CashOnDelivery" : "Online",
                PaymentStatus = isCashOnDelivery ? "CashOnDelivery" : "Pending"
            };

            var payment = new Payment
            {
                Order = order,
                TransactionId = $"GM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..38],
                Method = order.PaymentMethod,
                SelectedChannel = isCashOnDelivery ? null : PaymentChannel!.ToLowerInvariant(),
                Amount = totalAmount,
                Currency = "BDT",
                Status = order.PaymentStatus
            };

            _context.Orders.Add(order);
            _context.Payments.Add(payment);

            foreach (var item in cart.CartItems)
            {
                item.Product.StockQuantity -= item.Quantity;
                _context.OrderItems.Add(new OrderItem
                {
                    Order = order,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                });
            }

            if (isCashOnDelivery) _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (isCashOnDelivery)
            {
                return Json(new { success = true, orderId = order.OrderId });
            }

            order.OrderItems = await _context.OrderItems
                .Include(x => x.Product)
                .Where(x => x.OrderId == order.OrderId)
                .ToListAsync(cancellationToken);

            var session = await _sslCommerzService.CreateSessionAsync(
                order, payment, cart.User,
                $"{Request.Scheme}://{Request.Host}{Request.PathBase}",
                cancellationToken);

            if (!session.Success || string.IsNullOrWhiteSpace(session.GatewayUrl))
            {
                await MarkPaymentFailedAndReleaseStock(order.OrderId, cancellationToken);
                return Json(new { success = false, message = session.Error });
            }

            payment.SessionKey = session.SessionKey;
            await _context.SaveChangesAsync(cancellationToken);

            return Json(new { success = true, orderId = order.OrderId, redirectUrl = session.GatewayUrl });
        }

        private object? ValidateCheckout(
            string phoneNumber,
            string shippingAddress,
            decimal? shippingLatitude,
            decimal? shippingLongitude,
            string paymentMethod,
            string? paymentChannel)
        {
            string? phoneError = null;
            string? addressError = null;
            string? paymentError = null;

            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 11 || !phoneNumber.All(char.IsDigit))
                phoneError = "Please enter a valid 11 digit phone number.";

            if (string.IsNullOrWhiteSpace(shippingAddress))
                addressError = "Please provide a delivery address.";

            if (!CoordinatesAreValid(shippingLatitude, shippingLongitude))
                addressError = "Please choose a valid delivery location on the map.";

            var isCashOnDelivery = paymentMethod?.Equals("CashOnDelivery", StringComparison.OrdinalIgnoreCase) == true;
            var isOnline = paymentMethod?.Equals("Online", StringComparison.OrdinalIgnoreCase) == true;

            if (!isCashOnDelivery && !isOnline)
                paymentError = "Please select a payment method.";
            else if (isOnline && (string.IsNullOrWhiteSpace(paymentChannel) || !OnlineChannels.Contains(paymentChannel)))
                paymentError = "Please select an online payment option.";
            else if (isOnline && !_sslCommerzService.IsConfigured)
                paymentError = "Online payment is not configured yet. You can use Cash on Delivery for now.";

            return phoneError != null || addressError != null || paymentError != null
                ? new { success = false, phoneError, addressError, paymentError }
                : null;
        }

        private async Task MarkPaymentFailedAndReleaseStock(int orderId, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var payment = await _context.Payments
                .Include(x => x.Order).ThenInclude(x => x.OrderItems)!.ThenInclude(x => x.Product)
                .FirstAsync(x => x.OrderId == orderId, cancellationToken);

            if (payment.Status == "Pending")
            {
                foreach (var item in payment.Order.OrderItems ?? Array.Empty<OrderItem>())
                    item.Product.StockQuantity += item.Quantity;

                payment.Status = "Failed";
                payment.Order.PaymentStatus = "Failed";
                payment.Order.Status = "PaymentFailed";
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        private int CurrentUserId() => int.Parse(User.FindFirst("UserId")!.Value);

        private static bool CoordinatesAreValid(decimal? latitude, decimal? longitude)
        {
            if (!latitude.HasValue && !longitude.HasValue) return true;
            if (!latitude.HasValue || !longitude.HasValue) return false;
            return latitude.Value is >= -90 and <= 90 && longitude.Value is >= -180 and <= 180;
        }
    }
}
