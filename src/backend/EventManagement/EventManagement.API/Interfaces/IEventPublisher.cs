using EventManagement.API.Models;

namespace EventManagement.API.Interfaces;

public interface IEventPublisher
{
    Task<bool> PublishEventCreatedAsync(EventCreatedMessage message, CancellationToken cancellationToken = default);
    Task<bool> PublishEventUpdatedAsync(EventUpdatedMessage message, CancellationToken cancellationToken = default);
    Task<bool> PublishEventDeletedAsync(EventDeletedMessage message, CancellationToken cancellationToken = default);
}