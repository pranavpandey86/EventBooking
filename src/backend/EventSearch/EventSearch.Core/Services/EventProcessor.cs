using EventSearch.Core.Entities;
using EventSearch.Core.Interfaces;
using EventSearch.Core.Models;
using Microsoft.Extensions.Logging;

namespace EventSearch.Core.Services;

public class EventProcessor : IEventProcessor
{
    private readonly IEventIndexService _eventIndexService;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(IEventIndexService eventIndexService, ILogger<EventProcessor> logger)
    {
        _eventIndexService = eventIndexService ?? throw new ArgumentNullException(nameof(eventIndexService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessEventCreatedAsync(EventCreatedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing event-created message for event {EventId}", message.EventId);

            var searchableEvent = MapToSearchableEvent(message);
            await _eventIndexService.IndexEventAsync(searchableEvent, cancellationToken);

            _logger.LogInformation("Successfully processed event-created message for event {EventId}", message.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event-created message for event {EventId}", message.EventId);
            throw;
        }
    }

    public async Task ProcessEventUpdatedAsync(EventUpdatedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing event-updated message for event {EventId}", message.EventId);

            var searchableEvent = MapToSearchableEvent(message);
            await _eventIndexService.UpdateEventAsync(searchableEvent, cancellationToken);

            _logger.LogInformation("Successfully processed event-updated message for event {EventId}", message.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event-updated message for event {EventId}", message.EventId);
            throw;
        }
    }

    public async Task ProcessEventDeletedAsync(EventDeletedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing event-deleted message for event {EventId}", message.EventId);

            await _eventIndexService.DeleteEventAsync(message.EventId, cancellationToken);

            _logger.LogInformation("Successfully processed event-deleted message for event {EventId}", message.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event-deleted message for event {EventId}", message.EventId);
            throw;
        }
    }

    private SearchableEvent MapToSearchableEvent(EventCreatedMessage message)
    {
        // Extract city from location (simple implementation - could be enhanced)
        var city = ExtractCityFromLocation(message.Location);
        
        return new SearchableEvent
        {
            Id = message.EventId,
            Title = message.Name,
            Description = message.Description,
            Category = message.Category,
            StartDate = message.EventDate,
            EndDate = message.EventDate, // Assuming single-day events for now
            Location = message.Location,
            City = city,
            AvailableTickets = message.MaxCapacity,
            Price = message.TicketPrice,
            Organizer = message.OrganizerUserId.ToString(), // Convert to string for now
            IsActive = message.IsActive,
            CreatedAt = message.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private SearchableEvent MapToSearchableEvent(EventUpdatedMessage message)
    {
        // Extract city from location (simple implementation - could be enhanced)
        var city = ExtractCityFromLocation(message.Location);
        
        return new SearchableEvent
        {
            Id = message.EventId,
            Title = message.Name,
            Description = message.Description,
            Category = message.Category,
            StartDate = message.EventDate,
            EndDate = message.EventDate, // Assuming single-day events for now
            Location = message.Location,
            City = city,
            AvailableTickets = message.MaxCapacity,
            Price = message.TicketPrice,
            IsActive = message.IsActive,
            UpdatedAt = message.UpdatedAt
        };
    }

    private static string ExtractCityFromLocation(string location)
    {
        // Simple city extraction logic - split by comma and take the last part
        // This could be enhanced with proper address parsing or geocoding
        if (string.IsNullOrWhiteSpace(location))
            return "Unknown";

        var parts = location.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1].Trim() : "Unknown";
    }
}