using GreenMart.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenMart.Services
{
    public interface IProductSearchService
    {

        Task<IEnumerable<ProductSearchResultDto>> SearchProductsAsync(
            ListingQueryRequest request);


        Task<IEnumerable<ProductSuggestionDto>> GetSuggestionsAsync(
            string searchTerm
        );

    }
}