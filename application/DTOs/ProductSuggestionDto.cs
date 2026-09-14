namespace GreenMart.DTOs
{
    public class ProductSuggestionDto
    {

        public int ProductId { get; set; }


        public string ProductName { get; set; }


        public string? Brand { get; set; }


        public decimal Price { get; set; }


        public string CategoryName { get; set; }


        public string? ImagePath { get; set; }


        public bool IsAvailable { get; set; }

    }
}