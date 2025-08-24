using EventSearch.Core.Entities;
using EventSearch.Core.Interfaces;
using EventSearch.Core.Models;

namespace EventSearch.Core.Services;

public class SearchService : ISearchService
{
    private readonly IElasticsearchRepository _elasticsearchRepository;
    private readonly ICacheService _cacheService;
    private const int CacheExpirationMinutes = 5;

    public SearchService(IElasticsearchRepository elasticsearchRepository, ICacheService cacheService)
    {
        _elasticsearchRepository = elasticsearchRepository;
        _cacheService = cacheService;
    }

    public async Task<SearchResult<SearchableEvent>> SearchEventsAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        // Generate cache key based on query parameters
        var cacheKey = GenerateSearchCacheKey(query);
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<SearchResult<SearchableEvent>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // If not in cache, search from Elasticsearch
        var result = await _elasticsearchRepository.SearchAsync(query, cancellationToken);
        
        // Cache the result for future requests
        if (result.Items.Any())
        {
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes), cancellationToken);
        }

        return result;
    }

    public async Task<List<AutocompleteResult>> GetAutocompleteSuggestionsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new List<AutocompleteResult>();
        }

        var cacheKey = $"autocomplete:{query.ToLowerInvariant()}:{maxResults}";
        
        // Try cache first
        var cachedSuggestions = await _cacheService.GetAsync<List<AutocompleteResult>>(cacheKey, cancellationToken);
        if (cachedSuggestions != null)
        {
            return cachedSuggestions;
        }

        // Get from Elasticsearch
        var suggestions = await _elasticsearchRepository.SuggestAsync(query, maxResults, cancellationToken);
        
        // Cache for shorter time since autocomplete should be more real-time
        await _cacheService.SetAsync(cacheKey, suggestions, TimeSpan.FromMinutes(2), cancellationToken);

        return suggestions;
    }

    public async Task<SearchResult<SearchableEvent>> GetSimilarEventsAsync(Guid eventId, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"similar:{eventId}:{maxResults}";
        
        var cachedResult = await _cacheService.GetAsync<SearchResult<SearchableEvent>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var result = await _elasticsearchRepository.MoreLikeThisAsync(eventId, maxResults, cancellationToken);
        
        // Cache similar events for longer since they don't change often
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), cancellationToken);

        return result;
    }

    public async Task<SearchResult<SearchableEvent>> GetPopularEventsAsync(string? category = null, string? city = null, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"popular:{category ?? "all"}:{city ?? "all"}:{maxResults}";
        
        var cachedResult = await _cacheService.GetAsync<SearchResult<SearchableEvent>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var query = new SearchQuery
        {
            Category = category,
            City = city,
            PageSize = maxResults,
            SortBy = SearchSortOption.Popularity,
            SortDescending = true
        };

        var result = await _elasticsearchRepository.SearchAsync(query, cancellationToken);
        
        // Cache popular events for longer since they change less frequently
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), cancellationToken);

        return result;
    }

    private static string GenerateSearchCacheKey(SearchQuery query)
    {
        var keyParts = new List<string>
        {
            "search",
            query.SearchText ?? "null",
            query.Category ?? "null",
            query.City ?? "null",
            query.Country ?? "null",
            query.MinPrice?.ToString() ?? "null",
            query.MaxPrice?.ToString() ?? "null",
            query.StartDate?.ToString("yyyy-MM-dd") ?? "null",
            query.EndDate?.ToString("yyyy-MM-dd") ?? "null",
            string.Join(",", query.Tags.OrderBy(t => t)),
            query.Page.ToString(),
            query.PageSize.ToString(),
            query.SortBy.ToString(),
            query.SortDescending.ToString()
        };

        return string.Join(":", keyParts);
    }
}