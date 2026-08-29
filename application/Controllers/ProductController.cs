using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenMart.Data;
using GreenMart.Models;

namespace GreenMart.Controllers
{
    public class ProductController : Controller
    {

        private readonly ApplicationDbContext _context;


        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }



        [HttpGet]
        public IActionResult Index()
        {

            var products =
                _context.Products
                .Include(x => x.Category)
                .Include(x => x.User)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();


            return View(products);

        }





        [HttpGet]
        public IActionResult Details(int id)
        {

            var product =
                _context.Products
                .Include(x => x.Category)
                .Include(x => x.User)
                .FirstOrDefault(
                    x => x.ProductId == id
                );


            if (product == null)
            {
                return NotFound();
            }


            return View(product);

        }





        [HttpGet]
        public IActionResult Create()
        {

            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            LoadCategories();


            return View();

        }





        [HttpPost]
        public IActionResult Create(Product product)
        {

            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }



            if (!ModelState.IsValid)
            {
                LoadCategories();

                return View(product);
            }




            var userId =
                User.FindFirst("UserId")?.Value;



            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }




            product.UserId =
                int.Parse(userId);



            product.IsActive = true;

            product.CreatedAt =
                DateTime.Now;



            _context.Products.Add(product);

            _context.SaveChanges();



            return RedirectToAction(
                "MyProducts"
            );

        }





        [HttpGet]
        public IActionResult MyProducts()
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
                    User.FindFirst("UserId")!.Value
                );




            var products =
            _context.Products
            .Include(x => x.Category)
            .Where(
                x =>
                x.UserId == userId &&
                x.IsActive
            )
            .OrderByDescending(
                x => x.CreatedAt
            )
            .ToList();



            return View(products);

        }







        [HttpGet]
        public IActionResult Edit(int id)
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
                    User.FindFirst("UserId")!.Value
                );





            var product =
                _context.Products
                .FirstOrDefault(
                    x =>
                    x.ProductId == id &&
                    x.UserId == userId
                );



            if (product == null)
            {
                return Unauthorized();
            }



            LoadCategories();



            return View(product);

        }







        [HttpPost]
        public IActionResult Edit(Product product)
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
                    User.FindFirst("UserId")!.Value
                );




            var existing =
                _context.Products
                .FirstOrDefault(
                    x =>
                    x.ProductId == product.ProductId &&
                    x.UserId == userId
                );



            if (existing == null)
            {
                return Unauthorized();
            }




            existing.ProductName =
                product.ProductName;


            existing.Brand =
                product.Brand;


            existing.Description =
                product.Description;


            existing.Price =
                product.Price;


            existing.StockQuantity =
                product.StockQuantity;


            existing.CategoryId =
                product.CategoryId;



            _context.SaveChanges();



            return RedirectToAction(
                "MyProducts"
            );

        }







        [HttpPost]
        public IActionResult Delete(int id)
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
                    User.FindFirst("UserId")!.Value
                );




            var product =
                _context.Products
                .FirstOrDefault(
                    x =>
                    x.ProductId == id &&
                    x.UserId == userId
                );




            if (product == null)
            {
                return Unauthorized();
            }




            product.IsActive = false;



            _context.SaveChanges();



            return RedirectToAction(
                "MyProducts"
            );

        }





        private void LoadCategories()
        {

            ViewBag.Categories =
                _context.Categories
                .Where(x => x.IsActive)
                .ToList();

        }

    }
}