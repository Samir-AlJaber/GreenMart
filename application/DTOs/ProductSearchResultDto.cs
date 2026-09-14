using System;

namespace GreenMart.DTOs
{
    public class ProductSearchResultDto
    {

        public int ProductId { get; set; }


        public string ProductName { get; set; }


        public string? Brand { get; set; }


        public string? Description { get; set; }


        public decimal Price { get; set; }


        public int StockQuantity { get; set; }


        public string CategoryName { get; set; }


        public string SellerName { get; set; }


        public double AverageRating { get; set; }


        public int ReviewCount { get; set; }


        public DateTime CreatedAt { get; set; }


        public bool IsActive { get; set; }


        public string? ImagePath { get; set; }

    }
}