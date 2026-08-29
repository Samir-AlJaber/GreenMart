using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenMart.Data;
using GreenMart.Models;

namespace GreenMart.Controllers
{
    public class CartController : Controller
    {

        private readonly ApplicationDbContext _context;


        public CartController(ApplicationDbContext context)
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
                .Include(x => x.CartItems)
                    .ThenInclude(x => x.Product)
                        .ThenInclude(x => x.User)
                .FirstOrDefault(
                    x => x.UserId == userId
                );



            return View(cart);

        }








        [HttpGet]
        public IActionResult Add(int id)
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





            var product =
                _context.Products
                .FirstOrDefault(
                    x =>
                    x.ProductId == id &&
                    x.IsActive
                );





            if (product == null)
            {
                return NotFound();
            }






            if (product.UserId == userId)
            {
                return RedirectToAction(
                    "Details",
                    "Product",
                    new
                    {
                        id = id
                    }
                );
            }







            var cart =
                _context.Carts
                .FirstOrDefault(
                    x => x.UserId == userId
                );





            if (cart == null)
            {

                cart = new Cart
                {
                    UserId = userId
                };


                _context.Carts.Add(cart);

                _context.SaveChanges();

            }








            var item =
                _context.CartItems
                .FirstOrDefault(
                    x =>
                    x.CartId == cart.CartId &&
                    x.ProductId == id
                );







            if (item == null)
            {

                item = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = id,
                    Quantity = 1
                };


                _context.CartItems.Add(item);

            }
            else
            {

                if (item.Quantity < product.StockQuantity)
                {
                    item.Quantity++;
                }

            }





            _context.SaveChanges();



            return RedirectToAction("Index");

        }









        [HttpPost]
        public IActionResult UpdateQuantity(
            int id,
            int quantity
        )
        {


            var item =
                _context.CartItems
                .Include(x => x.Product)
                .Include(x => x.Cart)
                .FirstOrDefault(
                    x => x.CartItemId == id
                );




            if (item == null)
            {
                return Json(new
                {
                    success = false
                });
            }






            if (quantity > item.Product.StockQuantity)
            {
                quantity =
                    item.Product.StockQuantity;
            }






            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }





            _context.SaveChanges();





            return Json(
                GetCartData(
                    item.Cart.UserId
                )
            );

        }









        [HttpPost]
        public IActionResult RemoveAjax(int id)
        {

            var item =
                _context.CartItems
                .Include(x => x.Cart)
                .FirstOrDefault(
                    x => x.CartItemId == id
                );




            if (item != null)
            {

                var userId =
                    item.Cart.UserId;



                _context.CartItems.Remove(item);


                _context.SaveChanges();



                return Json(
                    GetCartData(userId)
                );

            }





            return Json(new
            {
                success = false
            });

        }









        [HttpGet]
        public IActionResult ValidateCheckout()
        {


            if (User.Identity == null ||
               !User.Identity.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    message = "Please login first."
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
               !cart.CartItems.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Your cart is empty."
                });
            }








            foreach (var item in cart.CartItems)
            {


                if (item.Product == null)
                {

                    return Json(new
                    {
                        success = false,
                        message =
                        "One product in your cart is no longer available."
                    });

                }







                if (!item.Product.IsActive)
                {

                    return Json(new
                    {
                        success = false,
                        message =
                        $"{item.Product.ProductName} is no longer available. Please remove it from your cart."
                    });

                }








                if (item.Product.StockQuantity <= 0)
                {

                    return Json(new
                    {
                        success = false,
                        message =
                        $"{item.Product.ProductName} is currently out of stock."
                    });

                }








                if (item.Quantity > item.Product.StockQuantity)
                {

                    return Json(new
                    {
                        success = false,
                        message =
                        $"{item.Product.ProductName} has only {item.Product.StockQuantity} pieces available now."
                    });

                }


            }






            return Json(new
            {
                success = true
            });


        }









        private object GetCartData(int userId)
        {


            var items =
                _context.CartItems
                .Include(x => x.Product)
                .Where(
                    x =>
                    x.Cart.UserId == userId
                )
                .ToList();







            decimal total =
                items
                .Where(x => x.Product != null)
                .Sum(
                    x =>
                    x.Product.Price *
                    x.Quantity
                );







            int count =
                items.Sum(
                    x => x.Quantity
                );







            return new
            {

                success = true,

                total = total,

                count = count

            };

        }


    }
}