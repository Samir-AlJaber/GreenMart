using GreenMart.Data;
using GreenMart.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GreenMart.Controllers
{
    public class CheckoutController : Controller
    {

        private readonly ApplicationDbContext _context;


        public CheckoutController(
            ApplicationDbContext context)
        {
            _context = context;
        }



        [HttpGet]
        public IActionResult Index()
        {

            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }



            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );



            var cart =
                _context.Carts
                .Include(x => x.User)
                .Include(x => x.CartItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefault(
                    x => x.UserId == userId
                );



            if (cart == null ||
                cart.CartItems == null ||
                !cart.CartItems.Any())
            {
                return RedirectToAction(
                    "Index",
                    "Cart"
                );
            }



            return View(cart);

        }

        [HttpPost]
        public IActionResult ValidateOrder(
    string PhoneNumber,
    string ShippingAddress,
    decimal? ShippingLatitude,
    decimal? ShippingLongitude)
        {

            string? phoneError = null;
            string? addressError = null;



            if (string.IsNullOrWhiteSpace(PhoneNumber) ||
                PhoneNumber.Length != 11 ||
                !PhoneNumber.All(char.IsDigit))
            {
                phoneError =
                    "Please enter a valid 11 digit phone number.";
            }



            if (string.IsNullOrWhiteSpace(ShippingAddress))
            {
                addressError =
                    "Please provide a delivery address.";
            }

            if (!CoordinatesAreValid(ShippingLatitude, ShippingLongitude))
            {
                addressError = "Please choose a valid delivery location on the map.";
            }



            if (phoneError != null ||
                addressError != null)
            {
                return Json(new
                {
                    success = false,
                    phoneError = phoneError,
                    addressError = addressError
                });
            }



            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );



            var cart =
                _context.Carts
                .Include(x => x.CartItems)
                .FirstOrDefault(
                    x => x.UserId == userId
                );



            if (cart == null ||
                cart.CartItems == null ||
                !cart.CartItems.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Your cart is empty."
                });
            }



            return Json(new
            {
                success = true
            });

        }

        [HttpPost]
                public IActionResult PlaceOrder(
            string PhoneNumber,
            string ShippingAddress,
            decimal? ShippingLatitude,
            decimal? ShippingLongitude)
        {

            string? phoneError = null;
            string? addressError = null;



            if (string.IsNullOrWhiteSpace(PhoneNumber) ||
                PhoneNumber.Length != 11 ||
                !PhoneNumber.All(char.IsDigit))
            {
                phoneError =
                    "Please enter a valid 11 digit phone number.";
            }



            if (string.IsNullOrWhiteSpace(ShippingAddress))
            {
                addressError =
                    "Please provide a delivery address.";
            }

            if (!CoordinatesAreValid(ShippingLatitude, ShippingLongitude))
            {
                addressError = "Please choose a valid delivery location on the map.";
            }



            if (phoneError != null ||
                addressError != null)
            {
                return Json(new
                {
                    success = false,
                    phoneError = phoneError,
                    addressError = addressError
                });
            }



            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );



            var cart =
                _context.Carts
                .Include(x => x.CartItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefault(
                    x => x.UserId == userId
                );



            if (cart == null ||
                cart.CartItems == null ||
                !cart.CartItems.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Your cart is empty."
                });
            }



            decimal totalAmount =
    cart.CartItems.Sum(
        x =>
        x.Product.Price *
        x.Quantity
    );


            var order =
                new Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount,
                    ShippingAddress = ShippingAddress,
                    ShippingLatitude = ShippingLatitude.HasValue ? decimal.Round(ShippingLatitude.Value, 6) : null,
                    ShippingLongitude = ShippingLongitude.HasValue ? decimal.Round(ShippingLongitude.Value, 6) : null,
                    Status = "Pending"
                };


            _context.Orders.Add(order);


            foreach (var item in cart.CartItems)
            {

                item.Product.StockQuantity -= item.Quantity;


                var orderItem =
                    new OrderItem
                    {
                        Order = order,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price
                    };


                _context.OrderItems.Add(orderItem);

            }


            _context.CartItems.RemoveRange(
                cart.CartItems
            );


            _context.SaveChanges();


            return Json(new
            {
                success = true,
                orderId = order.OrderId
            });

        }

        private static bool CoordinatesAreValid(decimal? latitude, decimal? longitude)
        {
            if (!latitude.HasValue && !longitude.HasValue) return true;
            if (!latitude.HasValue || !longitude.HasValue) return false;

            return latitude.Value is >= -90 and <= 90 &&
                   longitude.Value is >= -180 and <= 180;
        }
    }
}
