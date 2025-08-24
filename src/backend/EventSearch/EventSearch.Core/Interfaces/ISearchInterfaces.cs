using EventSearch.Core.Entities;
using EventSearch.Core.Models;

namespace EventSearch.Core.Interfaces;

public interface ISearchService
{
    Task<SearchResult<SearchableEvent>> SearchEventsAsync(SearchQuery query, CancellationToken cancellationToken = default);
    Task<List<AutocompleteResult>> GetAutocompleteSuggestionsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<SearchResult<SearchableEvent>> GetSimilarEventsAsync(Guid eventId, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<SearchResult<SearchableEvent>> GetPopularEventsAsync(string? category = null, string? city = null, int maxResults = 20, CancellationToken cancellationToken = default);
}

public interface IEventIndexService
{
    Task IndexEventAsync(SearchableEvent eventData, CancellationToken cancellationToken = default);
    Task IndexEventsAsync(IEnumerable<SearchableEvent> events, CancellationToken cancellationToken = default);
    Task UpdateEventAsync(SearchableEvent eventData, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public interface IElasticsearchRepository
{
    Task<SearchResult<SearchableEvent>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
    Task<List<AutocompleteResult>> SuggestAsync(string query, int maxResults, CancellationToken cancellationToken = default);
    Task<SearchResult<SearchableEvent>> MoreLikeThisAsync(Guid eventId, int maxResults, CancellationToken cancellationToken = default);
    Task<bool> IndexDocumentAsync(SearchableEvent document, CancellationToken cancellationToken = default);
    Task<bool> IndexDocumentsAsync(IEnumerable<SearchableEvent> documents, CancellationToken cancellationToken = default);
    Task<bool> UpdateDocumentAsync(SearchableEvent document, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<bool> CreateIndexIfNotExistsAsync(CancellationToken cancellationToken = default);
}