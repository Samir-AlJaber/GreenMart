using GreenMart.Data;
using GreenMart.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMart.Services
{
    public class ProductSearchService : IProductSearchService
    {

        private readonly ApplicationDbContext _context;


        public ProductSearchService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IEnumerable<ProductSearchResultDto>> SearchProductsAsync(
            ListingQueryRequest request)
        {


            var query =
                _context.Products
                .AsNoTracking()
                .Where(
                    x => x.IsActive
                );



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
                                x.Brand == request.Brand
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

            if (request.MinimumRating.HasValue)
            {

                query =
                    query.Where(
                        x =>
                        x.Reviews.Any()
                        &&
                        x.Reviews.Average(
                            r => r.Rating
                        )
                        >=
                        request.MinimumRating.Value
                    );

            }



            if (
                !string.IsNullOrWhiteSpace(request.SearchTerm)
                &&
                string.IsNullOrWhiteSpace(request.SortBy)
            )
            {

                var search =
                    request.SearchTerm.Trim();


                query =
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
                    );

            }

            query =
                request.SortBy switch
                {

                    "price_low" =>
                    query.OrderBy(
                        x => x.Price
                    ),


                    "price_high" =>
                    query.OrderByDescending(
                        x => x.Price
                    ),


                    "name_az" =>
                    query.OrderBy(
                        x => x.ProductName
                    ),


                    "name_za" =>
                    query.OrderByDescending(
                        x => x.ProductName
                    ),


                    "oldest" =>
                    query.OrderBy(
                        x => x.CreatedAt
                    ),


                    "rating" =>
                    query.OrderByDescending(
                        x =>
                        x.Reviews.Any()
                        ?
                        x.Reviews.Average(
                            r => r.Rating
                        )
                        :
                        0
                    ),


                    _ =>
                    query.OrderByDescending(
                        x => x.CreatedAt
                    )

                };


            var products =
                await query
                .Skip(
                    (request.Page - 1)
                    *
                    request.PageSize
                )
                .Take(
                    request.PageSize
                )
                .Select(
                    x =>
                    new ProductSearchResultDto
                    {

                        ProductId =
                            x.ProductId,


                        ProductName =
                            x.ProductName,


                        Brand =
                            x.Brand,


                        Description =
                            x.Description,


                        Price =
                            x.Price,


                        StockQuantity =
                            x.StockQuantity,


                        CategoryName =
                            x.Category.CategoryName,


                        SellerName =
                            x.User.FullName,


                        AverageRating =
                            x.Reviews.Any()
                            ?
                            x.Reviews.Average(
                                r => r.Rating
                            )
                            :
                            0,


                        ReviewCount =
                            x.Reviews.Count(),


                        CreatedAt =
                            x.CreatedAt,


                        IsActive =
                            x.IsActive,

                    }
                )
                .ToListAsync();


            foreach (var product in products)
            {
                product.ImagePath =
                    GetProductImagePath(product.ProductId);
            }


            return products;

        }



        public async Task<IEnumerable<ProductSuggestionDto>> GetSuggestionsAsync(
            string searchTerm)
        {


            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<ProductSuggestionDto>();
            }




            var search =
                searchTerm.Trim();






            var suggestions =
                await _context.Products
                .AsNoTracking()
                .Where(
                    x =>
                    x.IsActive
                    &&
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
                .ToListAsync();


            foreach (var suggestion in suggestions)
            {
                suggestion.ImagePath =
                    GetProductImagePath(
                        suggestion.ProductId
                    );
            }


            return suggestions;

        }

        public string? GetProductImagePath(int productId)
        {

            var directory =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "products"
                );


            if (!Directory.Exists(directory))
            {
                return null;
            }



            var image =
                Directory.GetFiles(
                    directory,
                    $"product-{productId}-*.png"
                )
                .OrderBy(x => x)
                .FirstOrDefault();

            if (image == null)
            {
                var legacy =
                    Path.Combine(
                        directory,
                        $"product-{productId}.png"
                    );


                if (File.Exists(legacy))
                {
                    image = legacy;
                }
            }



            if (image == null)
            {
                return null;
            }



            return "/uploads/products/"
                + Path.GetFileName(image);

        }
    }

    }