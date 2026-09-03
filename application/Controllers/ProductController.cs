using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenMart.Data;
using GreenMart.Models;
using Microsoft.AspNetCore.Hosting;

namespace GreenMart.Controllers
{
    public class ProductController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private const long MaxProductImageBytes = 10 * 1024 * 1024;
        private const int MaxProductImages = 8;


        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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
        [RequestSizeLimit(85 * 1024 * 1024)]
        public IActionResult Create(
            Product product,
            List<IFormFile>? productImages)
        {

            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }



            ValidateProductImages(productImages);


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


            if (productImages != null &&
                productImages.Count > 0)
            {
                SaveProductImages(
                    productImages,
                    product.ProductId
                );
            }



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
        [RequestSizeLimit(85 * 1024 * 1024)]
        public IActionResult Edit(
            Product product,
            List<IFormFile>? productImages)
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


            ValidateProductImages(productImages);


            if (!ModelState.IsValid)
            {
                LoadCategories();

                return View(product);
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


            if (productImages != null &&
                productImages.Count > 0)
            {
                ReplaceProductImages(
                    productImages,
                    existing.ProductId
                );
            }



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



        private void ValidateProductImages(
            List<IFormFile>? productImages)
        {

            if (productImages == null ||
                productImages.Count == 0)
            {
                return;
            }


            if (productImages.Count > MaxProductImages)
            {
                ModelState.AddModelError(
                    "ProductImages",
                    $"You can add up to {MaxProductImages} product photos."
                );

                return;
            }


            for (var index = 0;
                 index < productImages.Count;
                 index++)
            {
                var productImage =
                    productImages[index];


                if (productImage.Length == 0)
                {
                    ModelState.AddModelError(
                        "ProductImages",
                        $"Photo {index + 1} is empty or invalid."
                    );

                    continue;
                }


                if (productImage.Length > MaxProductImageBytes)
                {
                    ModelState.AddModelError(
                        "ProductImages",
                        $"Photo {index + 1} must be smaller than 10 MB."
                    );

                    continue;
                }


                if (!string.Equals(
                        productImage.ContentType,
                        "image/png",
                        StringComparison.OrdinalIgnoreCase) ||
                    !HasPngSignature(productImage))
                {
                    ModelState.AddModelError(
                        "ProductImages",
                        $"Photo {index + 1} could not be processed. Please choose it again."
                    );
                }
            }

        }



        private static bool HasPngSignature(
            IFormFile productImage)
        {

            byte[] expectedSignature =
            {
                137, 80, 78, 71,
                13, 10, 26, 10
            };

            var actualSignature =
                new byte[expectedSignature.Length];


            using var stream =
                productImage.OpenReadStream();

            var bytesRead =
                stream.Read(
                    actualSignature,
                    0,
                    actualSignature.Length
                );


            return bytesRead == expectedSignature.Length &&
                   actualSignature.SequenceEqual(
                       expectedSignature
                   );

        }



        private void SaveProductImages(
            List<IFormFile> productImages,
            int productId)
        {

            var imageDirectory =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "products"
                );


            Directory.CreateDirectory(
                imageDirectory
            );


            for (var index = 0;
                 index < productImages.Count;
                 index++)
            {
                var imagePath =
                    Path.Combine(
                        imageDirectory,
                        $"product-{productId}-{index + 1}.png"
                    );


                using var fileStream =
                    new FileStream(
                        imagePath,
                        FileMode.Create
                    );


                productImages[index].CopyTo(
                    fileStream
                );
            }

        }


        private void ReplaceProductImages(
            List<IFormFile> productImages,
            int productId)
        {

            var imageDirectory =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "products"
                );


            if (Directory.Exists(imageDirectory))
            {
                var numberedImages =
                    Directory.GetFiles(
                        imageDirectory,
                        $"product-{productId}-*.png"
                    );


                foreach (var imagePath in numberedImages)
                {
                    System.IO.File.Delete(imagePath);
                }


                var legacyImagePath =
                    Path.Combine(
                        imageDirectory,
                        $"product-{productId}.png"
                    );


                if (System.IO.File.Exists(legacyImagePath))
                {
                    System.IO.File.Delete(legacyImagePath);
                }
            }


            SaveProductImages(
                productImages,
                productId
            );

        }

    }
}
