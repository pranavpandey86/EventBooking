using EventSearch.API.DTOs;

namespace EventManagement.API.Interfaces;

public interface IEventSearchIntegrationService
{
    Task<bool> IndexEventAsync(IndexEventRequestDto eventData, CancellationToken cancellationToken = default);
    Task<bool> UpdateEventAsync(IndexEventRequestDto eventData, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> IsSearchServiceHealthyAsync(CancellationToken cancellationToken = default);
}