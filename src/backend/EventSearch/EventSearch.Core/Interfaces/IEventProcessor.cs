using EventSearch.Core.Models;

namespace EventSearch.Core.Interfaces;

public interface IEventProcessor
{
    Task ProcessEventCreatedAsync(EventCreatedMessage message, CancellationToken cancellationToken = default);
    Task ProcessEventUpdatedAsync(EventUpdatedMessage message, CancellationToken cancellationToken = default);
    Task ProcessEventDeletedAsync(EventDeletedMessage message, CancellationToken cancellationToken = default);
}