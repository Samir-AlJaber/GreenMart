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
    string ShippingAddress)
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
            string ShippingAddress)
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
    }
}