using GreenMart.Data;
using GreenMart.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using GreenMart.Services;

namespace GreenMart.Controllers
{
    public class OrderController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly ProductSearchService _productSearchService;


        public OrderController(
            ApplicationDbContext context,
            ProductSearchService productSearchService)
        {
            _context = context;
            _productSearchService = productSearchService;
        }



        [HttpGet]
        public IActionResult MyOrders()
        {

            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }



            var products =
                GetOrderProducts()
                .OrderByDescending(
                    x => x.OrderDate
                )
                .ToList();

            ViewBag.AvailableStatuses =
                products
                .Select(x => x.OrderStatus)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.Categories =
                _context.Categories
                .Where(x => x.IsActive)
                .OrderBy(x => x.CategoryName)
                .ToList();



            ViewBag.Brands =
            GetOrderProducts()
            .Select(x =>
                string.IsNullOrEmpty(x.Brand)
                ? "Not specified"
                : x.Brand
            )
            .Distinct()
            .OrderBy(x => x)
            .ToList();



            ViewBag.Sellers =
                GetOrderProducts()
                .Select(x => x.SellerName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.HasFilters = false;

            return View(products);

        }

        [HttpGet]
        public IActionResult GetAvailableStatuses(string type)
        {

            List<string> statuses;


            if (type == "owner")
            {

                statuses =
                    GetOwnerOrderProducts()
                    .Select(x => x.OrderStatus)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            }
            else
            {

                statuses =
                    GetOrderProducts()
                    .Select(x => x.OrderStatus)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            }


            return Json(statuses);

        }


        [HttpGet]
        public IActionResult OwnerOrders()
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }


            var products =
                GetOwnerOrderProducts()
                .OrderByDescending(
                    x => x.OrderDate
                )
                .ToList();

            ViewBag.AvailableStatuses =
                products
                .Select(x => x.OrderStatus)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.Categories =
                _context.Categories
                .Where(x => x.IsActive)
                .OrderBy(x => x.CategoryName)
                .ToList();



            ViewBag.Brands =
                GetOwnerOrderProducts()
                .Select(x =>
                    string.IsNullOrEmpty(x.Brand)
                    ? "Not specified"
                    : x.Brand
                )
                .Distinct()
                .OrderBy(x => x)
                .ToList();



            ViewBag.Buyers =
                GetOwnerOrderProducts()
                .Select(x => x.BuyerName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();



            ViewBag.HasFilters = false;


            return View(products);
        }


        [HttpGet]
        public IActionResult OrderSearchResults(
            string SearchTerm,
            string SortBy,
            int? CategoryId,
            string Brand,
            string Seller,
            decimal? MinPrice,
            decimal? MaxPrice,
            string Status,
            DateTime? FromDate,
            DateTime? ToDate
        )
        {

            var query =
                GetOrderProducts()
                .AsQueryable();

            if (FromDate.HasValue &&
    ToDate.HasValue &&
    FromDate.Value > ToDate.Value)
            {
                return PartialView(
                    "_OrderGrid",
                    new List<OrderProductSearchResultDto>()
                );
            }


            if (FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.OrderDate >= FromDate.Value
                );
            }


            if (ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.OrderDate <= ToDate.Value
                );
            }

            if (CategoryId.HasValue)
            {
                query =
                    query.Where(
                        x =>
                        x.CategoryName != null &&
                        _context.Categories
                        .Any(
                            c =>
                            c.CategoryId ==
                            CategoryId.Value &&
                            c.CategoryName ==
                            x.CategoryName
                        )
                    );
            }



            if (!string.IsNullOrWhiteSpace(Brand))
            {

                if (Brand == "Not specified")
                {
                    query =
                        query.Where(
                            x =>
                                string.IsNullOrEmpty(x.Brand)
                        );
                }
                else
                {
                    query =
                        query.Where(
                            x =>
                                x.Brand == Brand
                        );
                }

            }



            if (!string.IsNullOrWhiteSpace(Seller))
            {
                query =
                    query.Where(
                        x =>
                        x.SellerName == Seller
                    );
            }



            if (MinPrice.HasValue)
            {
                query =
                    query.Where(
                        x =>
                        x.Price >= MinPrice.Value
                    );
            }



            if (MaxPrice.HasValue)
            {
                query =
                    query.Where(
                        x =>
                        x.Price <= MaxPrice.Value
                    );
            }



            if (!string.IsNullOrWhiteSpace(Status))
            {
                query =
                    query.Where(
                        x =>
                        x.OrderStatus == Status
                    );
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {

                var search =
                    SearchTerm.Trim();



                query =
                    query.Where(
                        x =>
                        x.ProductName.Contains(search) ||

                        x.CategoryName.Contains(search) ||

                        (!string.IsNullOrEmpty(x.Brand) &&
                         x.Brand.Contains(search)) ||

                        x.SellerName.Contains(search) ||

                        x.SellerEmail.Contains(search)
                    );

            }




            var result =
                SortBy switch
                {

                    "price_low" =>
                        query
                        .OrderBy(x => x.Price)
                        .ToList(),


                    "price_high" =>
                        query
                        .OrderByDescending(x => x.Price)
                        .ToList(),


                    "name_az" =>
                        query
                        .OrderBy(x => x.ProductName)
                        .ToList(),


                    "name_za" =>
                        query
                        .OrderByDescending(x => x.ProductName)
                        .ToList(),


                    "seller_az" =>
                        query
                        .OrderBy(x => x.SellerName)
                        .ToList(),


                    _ =>
                        query
                        .OrderByDescending(x => x.OrderDate)
                        .ToList()

                };

            ViewBag.HasFilters =
                !string.IsNullOrWhiteSpace(SearchTerm)
                || CategoryId.HasValue
                || !string.IsNullOrWhiteSpace(Brand)
                || !string.IsNullOrWhiteSpace(Seller)
                || MinPrice.HasValue
                || MaxPrice.HasValue
                || !string.IsNullOrWhiteSpace(Status)
                || FromDate.HasValue
                || ToDate.HasValue
                || !string.IsNullOrWhiteSpace(SortBy);

            return PartialView(
                "_OrderGrid",
                result
            );

        }


        [HttpGet]
        public IActionResult OwnerOrderSearchResults(
            string SearchTerm,
            int? CategoryId,
            string Brand,
            string Buyer,
            decimal? MinPrice,
            decimal? MaxPrice,
            string Status,
            DateTime? FromDate,
            DateTime? ToDate,
            string SortBy
        )
        {
            var query =
                GetOwnerOrderProducts();



            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x =>
                    x.ProductName.StartsWith(SearchTerm)
                    ||
                    x.CategoryName.StartsWith(SearchTerm)
                    ||
                    x.Brand.StartsWith(SearchTerm)
                    ||
                    x.BuyerName.StartsWith(SearchTerm)
                );
            }



            if (CategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.CategoryId == CategoryId.Value
                );
            }



            if (!string.IsNullOrWhiteSpace(Brand))
            {

                if (Brand == "Not specified")
                {
                    query =
                        query.Where(
                            x =>
                                string.IsNullOrEmpty(x.Brand)
                        );
                }
                else
                {
                    query =
                        query.Where(
                            x =>
                                x.Brand == Brand
                        );
                }

            }



            if (!string.IsNullOrWhiteSpace(Buyer))
            {
                query = query.Where(x =>
                    x.BuyerName == Buyer
                );
            }



            if (MinPrice.HasValue)
            {
                query = query.Where(x =>
                    x.Price * x.Quantity >= MinPrice.Value
                );
            }



            if (MaxPrice.HasValue)
            {
                query = query.Where(x =>
                    x.Price * x.Quantity <= MaxPrice.Value
                );
            }



            if (!string.IsNullOrWhiteSpace(Status))
            {
                query = query.Where(x =>
                    x.OrderStatus == Status
                );
            }



            if (FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.OrderDate >= FromDate.Value
                );
            }



            if (ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.OrderDate <= ToDate.Value
                );
            }



            switch (SortBy)
            {

                case "oldest":

                    query =
                        query.OrderBy(x =>
                            x.OrderDate
                        );

                    break;


                case "price_low":

                    query =
                        query.OrderBy(x =>
                            x.Price * x.Quantity
                        );

                    break;


                case "price_high":

                    query =
                        query.OrderByDescending(x =>
                            x.Price * x.Quantity
                        );

                    break;


                case "name_az":

                    query =
                        query.OrderBy(x =>
                            x.ProductName
                        );

                    break;


                case "name_za":

                    query =
                        query.OrderByDescending(x =>
                            x.ProductName
                        );

                    break;


                case "buyer_az":

                    query =
                        query.OrderBy(x =>
                            x.BuyerName
                        );

                    break;


                default:

                    query =
                        query.OrderByDescending(x =>
                            x.OrderDate
                        );

                    break;

            }



            var result =
                query.ToList();



            ViewBag.HasFilters =
                !string.IsNullOrWhiteSpace(SearchTerm)
                || CategoryId.HasValue
                || !string.IsNullOrWhiteSpace(Brand)
                || !string.IsNullOrWhiteSpace(Buyer)
                || MinPrice.HasValue
                || MaxPrice.HasValue
                || !string.IsNullOrWhiteSpace(Status)
                || FromDate.HasValue
                || ToDate.HasValue;



            return PartialView(
                "_OwnerOrderGrid",
                result
            );
        }

        [HttpGet]
        public IActionResult OwnerOrderSuggestions(string searchTerm)
        {

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new List<object>());
            }


            var search = searchTerm.Trim();



            var suggestions =
                GetOwnerOrderProducts()
                .Where(x =>
                    x.ProductName.StartsWith(search)

                    ||

                    x.CategoryName.StartsWith(search)

                    ||

                    (!string.IsNullOrEmpty(x.Brand) &&
                     x.Brand.StartsWith(search))

                    ||

                    x.BuyerName.StartsWith(search)
                )
                .OrderByDescending(x =>
                    x.ProductName.StartsWith(search)
                )
                .ThenByDescending(x =>
                    x.CategoryName.StartsWith(search)
                )
                .ThenByDescending(x =>
                    !string.IsNullOrEmpty(x.Brand) &&
                    x.Brand.StartsWith(search)
                )
                .ThenByDescending(x =>
                    x.BuyerName.StartsWith(search)
                )
                .Take(5)
                .Select(x => new
                {
                    productId = x.ProductId,

                    productName = x.ProductName,

                    categoryName = x.CategoryName,

                    brand = x.Brand,

                    price = x.Price,

                    imagePath = x.ImagePath
                })
                .ToList();



            return Json(suggestions);

        }


        [HttpGet]
        public IActionResult OrderSuggestions(string searchTerm)
        {

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new List<object>());
            }



            var search =
                searchTerm.Trim();



            var suggestions =
                GetOrderProducts()
                .Where(
                    x =>
                        x.ProductName.StartsWith(search)

                        ||

                        x.CategoryName.StartsWith(search)

                        ||

                        (!string.IsNullOrEmpty(x.Brand) &&
                         x.Brand.StartsWith(search))

                        ||

                        x.SellerName.StartsWith(search)
                )
                .OrderByDescending(
                    x =>
                        x.ProductName.StartsWith(search)
                )
                .ThenByDescending(
                    x =>
                        x.CategoryName.StartsWith(search)
                )
                .ThenByDescending(
                    x =>
                        !string.IsNullOrEmpty(x.Brand) &&
                        x.Brand.StartsWith(search)
                )
                .ThenByDescending(
                    x =>
                        x.SellerName.StartsWith(search)
                )
                .Take(5)
                .Select(
                    x =>
                    new
                    {
                        productId =
                            x.ProductId,

                        productName =
                            x.ProductName,

                        categoryName =
                            x.CategoryName,

                        brand =
                            x.Brand,

                        price =
                            x.Price,

                        imagePath =
                            x.ImagePath
                    }
                )
                .ToList();



            return Json(suggestions);

        }


        private IQueryable<OrderProductSearchResultDto> GetOrderProducts()
        {

            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );



            return
                _context.OrderItems
                .Include(x => x.Product)
                    .ThenInclude(x => x.User)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Order)
                .Where(
                    x =>
                    x.Order.UserId == userId
                )
                .Select(
                    x =>
                    new OrderProductSearchResultDto
                    {

                        ProductId =
                        x.Product.ProductId,


                        ProductName =
                        x.Product.ProductName,


                        Brand =
                        x.Product.Brand,


                        Description =
                        x.Product.Description,


                        Price =
                        x.Price,


                        CategoryName =
                        x.Product.Category.CategoryName,


                        SellerName =
                        x.Product.User.FullName,


                        SellerEmail =
                        x.Product.User.Email,


                        Quantity =
                        x.Quantity,


                        OrderId =
                        x.OrderId,


                        OrderStatus =
                        x.Order.Status,

                        RejectionReason =
                        x.Order.RejectionReason,


                        RejectionNote =
                        x.Order.RejectionNote,

                        OrderDate =
                        x.Order.CreatedAt,


                        ImagePath =
                        _productSearchService.GetProductImagePath(
                            x.Product.ProductId
                        )

                    }
                );

        }

        private IQueryable<OrderProductSearchResultDto> GetOwnerOrderProducts()
        {
            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );


            return
                _context.OrderItems
                .Include(x => x.Product)
                    .ThenInclude(x => x.User)
                .Include(x => x.Product)
                    .ThenInclude(x => x.Category)
                .Include(x => x.Order)
                    .ThenInclude(x => x.User)
                .Where(
                    x =>
                    x.Product.UserId == userId
                )
                .Select(
                    x =>
                    new OrderProductSearchResultDto
                    {

                        ProductId =
                            x.Product.ProductId,


                        ProductName =
                            x.Product.ProductName,


                        Brand =
                            x.Product.Brand,


                        Description =
                            x.Product.Description,


                        Price =
                            x.Price,


                        CategoryName =
                            x.Product.Category.CategoryName,

                        CategoryId =
                            x.Product.CategoryId,

                        BuyerName =
                            x.Order.User.FullName,


                        BuyerEmail =
                            x.Order.User.Email,


                        BuyerPhone =
                            x.Order.User.PhoneNumber,


                        BuyerAddress =
                            x.Order.ShippingAddress,


                        Quantity =
                            x.Quantity,


                        OrderId =
                            x.OrderId,


                        OrderStatus =
                            x.Order.Status,


                        RejectionReason =
                            x.Order.RejectionReason,


                        RejectionNote =
                            x.Order.RejectionNote,


                        OrderDate =
                            x.Order.CreatedAt,


                        ImagePath =
                            _productSearchService.GetProductImagePath(
                                x.Product.ProductId
                            )

                    }
                );
        }

        [HttpPost]
        public IActionResult ConfirmOrder(int orderId)
        {
            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );


            var order =
                _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefault(
                    x => x.OrderId == orderId
                );


            if (order == null)
            {
                return Json(new
                {
                    success = false
                });
            }



            bool ownsProduct =
                order.OrderItems
                .Any(
                    x =>
                    x.Product.UserId == userId
                );


            if (!ownsProduct)
            {
                return Json(new
                {
                    success = false
                });
            }



            if (order.Status != "Pending")
            {
                return Json(new
                {
                    success = false
                });
            }



            order.Status = "Confirmed";


            _context.SaveChanges();



            return Json(new
            {
                success = true,
                status = order.Status
            });
        }



        [HttpPost]
        public IActionResult RejectOrder(
    int orderId,
    string rejectionReason,
    string rejectionNote
)
        {
            var userId =
                int.Parse(
                    User.FindFirst("UserId").Value
                );



            var order =
                _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefault(
                    x => x.OrderId == orderId
                );



            if (order == null)
            {
                return Json(new
                {
                    success = false
                });
            }




            bool ownsProduct =
                order.OrderItems
                .Any(
                    x =>
                    x.Product.UserId == userId
                );



            if (!ownsProduct)
            {
                return Json(new
                {
                    success = false
                });
            }





            if (order.Status != "Pending")
            {
                return Json(new
                {
                    success = false
                });
            }




            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                return Json(new
                {
                    success = false
                });
            }




            order.Status = "Rejected";

            order.RejectionReason =
                rejectionReason;


            order.RejectionNote =
                rejectionNote;



            _context.SaveChanges();




            return Json(new
            {
                success = true,
                status = order.Status
            });

        }
    }
}