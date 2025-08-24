namespace EventSearch.Core.Models;

public class SearchQuery
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
    public SearchSortOption SortBy { get; set; } = SearchSortOption.Relevance;
    public bool SortDescending { get; set; } = true;
}

public enum SearchSortOption
{
    Relevance,
    Date,
    Price,
    Popularity,
    Rating,
    CreatedAt
}

public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public double SearchTimeMs { get; set; }
    public SearchFacets? Facets { get; set; }
}

public class SearchFacets
{
    public Dictionary<string, long> Categories { get; set; } = new();
    public Dictionary<string, long> Cities { get; set; } = new();
    public Dictionary<string, long> Countries { get; set; } = new();
    public Dictionary<string, long> Tags { get; set; } = new();
    public PriceRangeFacet PriceRanges { get; set; } = new();
}

public class PriceRangeFacet
{
    public long Under25 { get; set; }
    public long Range25To50 { get; set; }
    public long Range50To100 { get; set; }
    public long Range100To200 { get; set; }
    public long Over200 { get; set; }
}

public class AutocompleteResult
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "event", "category", "location"
    public int Score { get; set; }
}