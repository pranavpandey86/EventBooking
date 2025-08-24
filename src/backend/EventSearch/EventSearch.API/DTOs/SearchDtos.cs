using EventSearch.Core.Models;

namespace EventSearch.API.DTOs;

public class SearchRequestDto
{
    public string? SearchText { get; set; }
    public string? Category { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "relevance";
    public bool SortDescending { get; set; } = true;

    public SearchQuery ToSearchQuery()
    {
        return new SearchQuery
        {
            SearchText = SearchText,
            Category = Category,
            City = City,
            Country = Country,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            StartDate = StartDate,
            EndDate = EndDate,
            Tags = Tags,
            Page = Math.Max(1, Page),
            PageSize = Math.Min(100, Math.Max(1, PageSize)),
            SortBy = ParseSortOption(SortBy),
            SortDescending = SortDescending
        };
    }

    private static SearchSortOption ParseSortOption(string sortBy)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "date" => SearchSortOption.Date,
            "price" => SearchSortOption.Price,
            "popularity" => SearchSortOption.Popularity,
            "rating" => SearchSortOption.Rating,
            "createdat" => SearchSortOption.CreatedAt,
            _ => SearchSortOption.Relevance
        };
    }
}

public class SearchResponseDto<T>
{
    public List<T> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public double SearchTimeMs { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    public SearchFacetsDto? Facets { get; set; }

    public static SearchResponseDto<T> FromSearchResult<TSource>(SearchResult<TSource> result, Func<TSource, T> mapper)
    {
        return new SearchResponseDto<T>
        {
            Items = result.Items.Select(mapper).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            SearchTimeMs = result.SearchTimeMs,
            Facets = result.Facets != null ? SearchFacetsDto.FromSearchFacets(result.Facets) : null
        };
    }
}

public class SearchFacetsDto
{
    public Dictionary<string, long> Categories { get; set; } = new();
    public Dictionary<string, long> Cities { get; set; } = new();
    public Dictionary<string, long> Countries { get; set; } = new();
    public Dictionary<string, long> Tags { get; set; } = new();
    public PriceRangeFacetDto PriceRanges { get; set; } = new();

    public static SearchFacetsDto FromSearchFacets(SearchFacets facets)
    {
        return new SearchFacetsDto
        {
            Categories = facets.Categories,
            Cities = facets.Cities,
            Countries = facets.Countries,
            Tags = facets.Tags,
            PriceRanges = new PriceRangeFacetDto
            {
                Under25 = facets.PriceRanges.Under25,
                Range25To50 = facets.PriceRanges.Range25To50,
                Range50To100 = facets.PriceRanges.Range50To100,
                Range100To200 = facets.PriceRanges.Range100To200,
                Over200 = facets.PriceRanges.Over200
            }
        };
    }
}

public class PriceRangeFacetDto
{
    public long Under25 { get; set; }
    public long Range25To50 { get; set; }
    public long Range50To100 { get; set; }
    public long Range100To200 { get; set; }
    public long Over200 { get; set; }
}

public class AutocompleteResponseDto
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class EventSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AvailableTickets { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public decimal Popularity { get; set; }
    public int ViewCount { get; set; }
    public int BookingCount { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
}

public class IndexEventRequestDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AvailableTickets { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public decimal Popularity { get; set; }
    public int ViewCount { get; set; }
    public int BookingCount { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
}