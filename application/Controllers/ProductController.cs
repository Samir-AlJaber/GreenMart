using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GreenMart.Data;
using GreenMart.Models;
using GreenMart.Services;
using GreenMart.DTOs;
using Microsoft.AspNetCore.Hosting;

namespace GreenMart.Controllers
{
    public class ProductController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IProductSearchService _productSearchService;


        private const long MaxProductImageBytes = 10 * 1024 * 1024;
        private const int MaxProductImages = 8;



        public ProductController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IProductSearchService productSearchService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _productSearchService = productSearchService;
        }


        [HttpGet]
        public async Task<IActionResult> Index(
            ListingQueryRequest request)
        {

            LoadCategories();

            LoadBrands();

            var products =
                await _productSearchService
                .SearchProductsAsync(request);


            return View(products);
        }



        [HttpGet]
        public async Task<IActionResult> Search(
            ListingQueryRequest request)
        {


            var products =
                await _productSearchService
                .SearchProductsAsync(request);



            return Json(products);

        }



        [HttpGet]
        public async Task<IActionResult> SearchResults(
            ListingQueryRequest request)
        {

                var products =
                    await _productSearchService
                    .SearchProductsAsync(request);


                return PartialView(
                    "_ProductGrid",
                    products
                );

            }




        [HttpGet]
        public async Task<IActionResult> Suggestions(
            string searchTerm)
        {


            var suggestions =
                await _productSearchService
                .GetSuggestionsAsync(searchTerm);



            return Json(suggestions);

        }



        [HttpGet]
        public IActionResult MyProductSuggestions(
    string searchTerm)
        {

            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }



            var userId =
                int.Parse(
                    User.FindFirst("UserId")!.Value
                );



            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new List<ProductSuggestionDto>());
            }



            var search =
                searchTerm.Trim();



            var suggestions =
                _context.Products
                .Include(x => x.Category)
                .Where(
                    x =>
                        x.UserId == userId &&
                        x.IsActive &&
                        (
                            EF.Functions.Like(
                                x.ProductName,
                                $"{search}%"
                            )

                            ||

                            (x.Brand != null &&
                            EF.Functions.Like(
                                x.Brand,
                                $"{search}%"
                            ))

                            ||

                            EF.Functions.Like(
                                x.Category.CategoryName,
                                $"{search}%"
                            )
                        )
                )
                .OrderByDescending(
                    x =>
                    x.ProductName.StartsWith(search)
                )
                .ThenByDescending(
                    x =>
                    x.Category.CategoryName.StartsWith(search)
                )
                .ThenByDescending(
                    x =>
                    x.Brand != null &&
                    x.Brand.StartsWith(search)
                )
                .Take(8)
                .Select(
                    x =>
                    new ProductSuggestionDto
                    {
                        ProductId =
                            x.ProductId,

                        ProductName =
                            x.ProductName,

                        Brand =
                            x.Brand,

                        Price =
                            x.Price,

                        CategoryName =
                            x.Category.CategoryName,

                        ImagePath =
                            "/uploads/products/product-"
                            +
                            x.ProductId
                            +
                            "-1.png",

                        IsAvailable =
                            x.StockQuantity > 0
                    }
                )
                .ToList();


            return Json(suggestions);

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

            LoadCategories();

            LoadBrands();

            return View(products);

        }

        [HttpGet]
        public IActionResult MyProductSearchResults(
    ListingQueryRequest request)
        {

            if (
                User.Identity == null ||
                !User.Identity.IsAuthenticated
            )
            {
                return Unauthorized();
            }


            var userId =
                int.Parse(
                    User.FindFirst("UserId")!.Value
                );


            var query =
                _context.Products
                .Include(x => x.Category)
                .Where(
                    x =>
                        x.UserId == userId &&
                        x.IsActive
                );

            if (request.CategoryId.HasValue)
            {

                query =
                    query.Where(
                        x =>
                            x.CategoryId ==
                            request.CategoryId.Value
                    );

            }

            if (!string.IsNullOrWhiteSpace(request.Brand))
            {

                if (request.Brand == "__NONE__")
                {

                    query =
                        query.Where(
                            x =>
                                x.Brand == null ||
                                x.Brand.Trim() == ""
                        );

                }
                else
                {

                    query =
                        query.Where(
                            x =>
                                x.Brand ==
                                request.Brand
                        );

                }

            }

            if (request.MinPrice.HasValue)
            {

                query =
                    query.Where(
                        x =>
                            x.Price >=
                            request.MinPrice.Value
                    );

            }



            if (request.MaxPrice.HasValue)
            {

                query =
                    query.Where(
                        x =>
                            x.Price <=
                            request.MaxPrice.Value
                    );

            }

            if (request.InStock.HasValue)
            {

                if (request.InStock.Value)
                {

                    query =
                        query.Where(
                            x =>
                                x.StockQuantity > 0
                        );

                }
                else
                {

                    query =
                        query.Where(
                            x =>
                                x.StockQuantity <= 0
                        );

                }

            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {

                var search =
                    request.SearchTerm.Trim();


                query =
                    query.Where(
                        x =>
                            EF.Functions.Like(
                                x.ProductName,
                                $"{search}%"
                            )

                            ||

                            (x.Brand != null &&
                            EF.Functions.Like(
                                x.Brand,
                                $"{search}%"
                            ))

                            ||

                            EF.Functions.Like(
                                x.Category.CategoryName,
                                $"{search}%"
                            )
                    );

            }



            List<Product> products;



            if (!string.IsNullOrWhiteSpace(request.SearchTerm) &&
                string.IsNullOrWhiteSpace(request.SortBy))
            {

                var search =
                    request.SearchTerm.Trim();


                products =
                    query
                    .OrderByDescending(
                        x =>
                        x.ProductName.StartsWith(search)
                    )
                    .ThenByDescending(
                        x =>
                        x.ProductName.Contains(search)
                    )
                    .ThenByDescending(
                        x =>
                        x.Category.CategoryName.StartsWith(search)
                    )
                    .ThenByDescending(
                        x =>
                        x.Brand != null &&
                        x.Brand.StartsWith(search)
                    )
                    .ThenBy(
                        x =>
                        x.ProductName
                    )
                    .ToList();

            }
            else
            {

                products =
                    request.SortBy switch
                    {

                        "price_low" =>
                            query
                            .OrderBy(
                                x => x.Price
                            )
                            .ToList(),


                        "price_high" =>
                            query
                            .OrderByDescending(
                                x => x.Price
                            )
                            .ToList(),


                        "name_az" =>
                            query
                            .OrderBy(
                                x => x.ProductName
                            )
                            .ToList(),


                        "name_za" =>
                            query
                            .OrderByDescending(
                                x => x.ProductName
                            )
                            .ToList(),


                        "oldest" =>
                            query
                            .OrderBy(
                                x => x.CreatedAt
                            )
                            .ToList(),


                        _ =>
                            query
                            .OrderByDescending(
                                x => x.CreatedAt
                            )
                            .ToList()

                    };

            }



            return PartialView(
                "_MyProductGrid",
                products
            );

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
                .Include(x => x.OrderItems)
                .FirstOrDefault(
                    x =>
                    x.ProductId == id &&
                    x.UserId == userId
                );






            if (product == null)
            {
                return Unauthorized();
            }



            bool hasOrderHistory =
                product.OrderItems != null &&
                product.OrderItems.Any();


            product.IsActive = false;





            if (!hasOrderHistory)
            {

                DeleteProductImages(
                    product.ProductId
                );

            }



            _context.SaveChanges();

            return RedirectToAction(
                "MyProducts"
            );

        }



        private void LoadCategories()
        {

            ViewBag.Categories =
                _context.Categories
                .Where(
                    x => x.IsActive
                )
                .ToList();

        }



        private void LoadBrands()
        {

            var brands =
                _context.Products
                .Where(
                    x =>
                        x.Brand != null &&
                        x.Brand.Trim() != ""
                )
                .Select(
                    x => x.Brand
                )
                .Distinct()
                .OrderBy(
                    x => x
                )
                .ToList();



            brands.Insert(
                0,
                "Not specified"
            );


            ViewBag.Brands = brands;

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


                if (productImage.Length >
                    MaxProductImageBytes)
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
                        StringComparison.OrdinalIgnoreCase)
                    ||
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
                137,80,78,71,
                13,10,26,10
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




            return bytesRead ==
                   expectedSignature.Length
                   &&
                   actualSignature.SequenceEqual(
                       expectedSignature
                   );

        }

        private void SaveProductImages(
    List<IFormFile> productImages,
    int productId)
        {

            var uploadPath =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "products"
                );



            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }






            int imageNumber = 1;




            foreach (var image in productImages)
            {

                var fileName =
                    $"product-{productId}-{imageNumber}.png";



                var filePath =
                    Path.Combine(
                        uploadPath,
                        fileName
                    );





                using var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    );



                image.CopyTo(stream);



                imageNumber++;

            }

        }








        private void ReplaceProductImages(
            List<IFormFile> productImages,
            int productId)
        {

            DeleteProductImages(productId);



            SaveProductImages(
                productImages,
                productId
            );

        }








        private void DeleteProductImages(
            int productId)
        {

            var uploadPath =
                Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "products"
                );



            if (!Directory.Exists(uploadPath))
            {
                return;
            }





            var files =
                Directory.GetFiles(
                    uploadPath,
                    $"product-{productId}-*.png"
                );






            foreach (var file in files)
            {

                if (System.IO.File.Exists(file))
                {

                    System.IO.File.Delete(file);

                }

            }

        }



    }
}