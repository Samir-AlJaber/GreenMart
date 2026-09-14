namespace GreenMart.DTOs
{
    public class ListingQueryRequest
    {

        public string? SearchTerm { get; set; }

        public int? CategoryId { get; set; }

        public string? Brand { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public bool? InStock { get; set; }

        public int? MinimumRating { get; set; }

        public string? SortBy { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

    }
}