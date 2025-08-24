using EventSearch.Core.Entities;
using EventSearch.Core.Interfaces;

namespace EventSearch.Core.Services;

public class EventIndexService : IEventIndexService
{
    private readonly IElasticsearchRepository _elasticsearchRepository;
    private readonly ICacheService _cacheService;

    public EventIndexService(IElasticsearchRepository elasticsearchRepository, ICacheService cacheService)
    {
        _elasticsearchRepository = elasticsearchRepository;
        _cacheService = cacheService;
    }

    public async Task IndexEventAsync(SearchableEvent eventData, CancellationToken cancellationToken = default)
    {
        eventData.UpdatedAt = DateTime.UtcNow;
        
        var success = await _elasticsearchRepository.IndexDocumentAsync(eventData, cancellationToken);
        
        if (success)
        {
            // Invalidate related cache entries
            await InvalidateRelatedCacheAsync(eventData, cancellationToken);
        }
    }

    public async Task IndexEventsAsync(IEnumerable<SearchableEvent> events, CancellationToken cancellationToken = default)
    {
        var eventList = events.ToList();
        
        // Update timestamps
        foreach (var eventData in eventList)
        {
            eventData.UpdatedAt = DateTime.UtcNow;
        }
        
        var success = await _elasticsearchRepository.IndexDocumentsAsync(eventList, cancellationToken);
        
        if (success)
        {
            // Invalidate cache patterns that might be affected
            await _cacheService.RemoveByPatternAsync("search:*", cancellationToken);
            await _cacheService.RemoveByPatternAsync("popular:*", cancellationToken);
            await _cacheService.RemoveByPatternAsync("autocomplete:*", cancellationToken);
        }
    }

    public async Task UpdateEventAsync(SearchableEvent eventData, CancellationToken cancellationToken = default)
    {
        eventData.UpdatedAt = DateTime.UtcNow;
        
        var success = await _elasticsearchRepository.UpdateDocumentAsync(eventData, cancellationToken);
        
        if (success)
        {
            await InvalidateRelatedCacheAsync(eventData, cancellationToken);
        }
    }

    public async Task DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var success = await _elasticsearchRepository.DeleteDocumentAsync(eventId, cancellationToken);
        
        if (success)
        {
            // Invalidate cache entries related to this event
            await _cacheService.RemoveByPatternAsync($"similar:{eventId}:*", cancellationToken);
            await _cacheService.RemoveByPatternAsync("search:*", cancellationToken);
            await _cacheService.RemoveByPatternAsync("popular:*", cancellationToken);
        }
    }

    public async Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _elasticsearchRepository.DocumentExistsAsync(eventId, cancellationToken);
    }

    private async Task InvalidateRelatedCacheAsync(SearchableEvent eventData, CancellationToken cancellationToken)
    {
        // Invalidate specific cache patterns that might be affected by this event
        var cachePatterns = new[]
        {
            "search:*",
            "popular:*",
            $"popular:{eventData.Category}:*",
            $"popular:*:{eventData.City}:*",
            $"popular:{eventData.Category}:{eventData.City}:*",
            $"similar:{eventData.Id}:*",
            "autocomplete:*"
        };

        foreach (var pattern in cachePatterns)
        {
            await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
        }
    }
}