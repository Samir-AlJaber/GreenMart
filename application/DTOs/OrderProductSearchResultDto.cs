using System;

namespace GreenMart.DTOs
{
    public class OrderProductSearchResultDto
    {

        public int ProductId { get; set; }


        public string ProductName { get; set; }


        public string? Brand { get; set; }


        public string? Description { get; set; }


        public decimal Price { get; set; }


        public string CategoryName { get; set; }


        public string SellerName { get; set; }


        public string SellerEmail { get; set; }

        public int SellerId { get; set; }


        public int Quantity { get; set; }


        public int OrderId { get; set; }


        public string OrderStatus { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }


        public DateTime OrderDate { get; set; }


        public string? ImagePath { get; set; }

        public string BuyerName { get; set; }

        public string BuyerEmail { get; set; }

        public string BuyerPhone { get; set; }

        public string? BuyerAddress { get; set; }


        public string? RejectionReason { get; set; }

        public string? RejectionNote { get; set; }

        public int CategoryId { get; set; }

    }
}
