using EventManagement.Core.Entities;
using EventManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventManagement.Core.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<EventService> _logger;

        public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            _logger.LogInformation("Retrieving all events");
            
            try
            {
                var events = await _eventRepository.GetAllEventsAsync();
                _logger.LogInformation("Successfully retrieved {EventCount} events", events.Count());
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all events");
                throw;
            }
        }

        public async Task<Event?> GetEventByIdAsync(Guid eventId)
        {
            _logger.LogInformation("Retrieving event with ID: {EventId}", eventId);
            
            if (eventId == Guid.Empty)
            {
                _logger.LogWarning("Invalid event ID provided: {EventId}", eventId);
                return null;
            }

            try
            {
                var eventItem = await _eventRepository.GetEventByIdAsync(eventId);
                
                if (eventItem == null)
                {
                    _logger.LogInformation("Event with ID {EventId} not found", eventId);
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved event with ID: {EventId}", eventId);
                }
                
                return eventItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving event with ID: {EventId}", eventId);
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(Guid organizerUserId)
        {
            _logger.LogInformation("Retrieving events for organizer: {OrganizerId}", organizerUserId);
            
            if (organizerUserId == Guid.Empty)
            {
                _logger.LogWarning("Invalid organizer ID provided: {OrganizerId}", organizerUserId);
                return Enumerable.Empty<Event>();
            }

            try
            {
                var events = await _eventRepository.GetEventsByOrganizerAsync(organizerUserId);
                _logger.LogInformation("Successfully retrieved {EventCount} events for organizer: {OrganizerId}", 
                    events.Count(), organizerUserId);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving events for organizer: {OrganizerId}", organizerUserId);
                throw;
            }
        }

        public async Task<IEnumerable<Event>> SearchEventsAsync(string? searchTerm, string? category, DateTime? startDate, DateTime? endDate)
        {
            _logger.LogInformation("Searching events with criteria - SearchTerm: {SearchTerm}, Category: {Category}, StartDate: {StartDate}, EndDate: {EndDate}",
                searchTerm, category, startDate, endDate);

            try
            {
                var events = await _eventRepository.SearchEventsAsync(searchTerm, category, startDate, endDate);
                _logger.LogInformation("Search returned {EventCount} events", events.Count());
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching events");
                throw;
            }
        }

        public async Task<Event> CreateEventAsync(Event eventItem)
        {
            _logger.LogInformation("Creating new event: {EventName}", eventItem.Name);
            
            if (eventItem == null)
            {
                throw new ArgumentNullException(nameof(eventItem));
            }

            // Business logic validation
            await ValidateEventAsync(eventItem);

            try
            {
                // Set creation timestamps
                eventItem.EventId = Guid.NewGuid();
                eventItem.CreatedAt = DateTime.UtcNow;
                eventItem.UpdatedAt = DateTime.UtcNow;
                eventItem.IsActive = true;

                var createdEvent = await _eventRepository.CreateEventAsync(eventItem);
                _logger.LogInformation("Successfully created event with ID: {EventId}", createdEvent.EventId);
                return createdEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating event: {EventName}", eventItem.Name);
                throw;
            }
        }

        public async Task<Event?> UpdateEventAsync(Guid eventId, Event updatedEvent)
        {
            _logger.LogInformation("Updating event with ID: {EventId}", eventId);
            
            if (eventId == Guid.Empty)
            {
                _logger.LogWarning("Invalid event ID provided for update: {EventId}", eventId);
                return null;
            }

            if (updatedEvent == null)
            {
                throw new ArgumentNullException(nameof(updatedEvent));
            }

            try
            {
                var existingEvent = await _eventRepository.GetEventByIdAsync(eventId);
                if (existingEvent == null)
                {
                    _logger.LogInformation("Event with ID {EventId} not found for update", eventId);
                    return null;
                }

                // Preserve important fields from existing event BEFORE validation
                updatedEvent.EventId = eventId;
                updatedEvent.CreatedAt = existingEvent.CreatedAt;
                updatedEvent.UpdatedAt = DateTime.UtcNow;
                
                // Preserve OrganizerUserId if not provided in update (allow empty Guid to use existing)
                if (updatedEvent.OrganizerUserId == Guid.Empty)
                {
                    updatedEvent.OrganizerUserId = existingEvent.OrganizerUserId;
                }

                // Business logic validation
                await ValidateEventAsync(updatedEvent);

                var result = await _eventRepository.UpdateEventAsync(updatedEvent);
                _logger.LogInformation("Successfully updated event with ID: {EventId}", eventId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating event with ID: {EventId}", eventId);
                throw;
            }
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            _logger.LogInformation("Deleting event with ID: {EventId}", eventId);
            
            if (eventId == Guid.Empty)
            {
                _logger.LogWarning("Invalid event ID provided for deletion: {EventId}", eventId);
                return false;
            }

            try
            {
                var existingEvent = await _eventRepository.GetEventByIdAsync(eventId);
                if (existingEvent == null)
                {
                    _logger.LogInformation("Event with ID {EventId} not found for deletion", eventId);
                    return false;
                }

                var result = await _eventRepository.DeleteEventAsync(eventId);
                if (result)
                {
                    _logger.LogInformation("Successfully deleted event with ID: {EventId}", eventId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete event with ID: {EventId}", eventId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting event with ID: {EventId}", eventId);
                throw;
            }
        }

        public async Task<bool> EventExistsAsync(Guid eventId)
        {
            _logger.LogDebug("Checking if event exists with ID: {EventId}", eventId);
            
            if (eventId == Guid.Empty)
            {
                return false;
            }

            try
            {
                return await _eventRepository.EventExistsAsync(eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if event exists with ID: {EventId}", eventId);
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetActiveEventsAsync()
        {
            _logger.LogInformation("Retrieving all active events");
            
            try
            {
                var events = await GetAllEventsAsync();
                var activeEvents = events.Where(e => e.IsActive && e.EventDate > DateTime.UtcNow);
                
                _logger.LogInformation("Successfully retrieved {ActiveEventCount} active events", activeEvents.Count());
                return activeEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving active events");
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetEventsByCategoryAsync(string category)
        {
            _logger.LogInformation("Retrieving events for category: {Category}", category);
            
            if (string.IsNullOrWhiteSpace(category))
            {
                _logger.LogWarning("Invalid category provided");
                return Enumerable.Empty<Event>();
            }

            try
            {
                var events = await GetAllEventsAsync();
                var categoryEvents = events.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                
                _logger.LogInformation("Successfully retrieved {EventCount} events for category: {Category}", 
                    categoryEvents.Count(), category);
                return categoryEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving events for category: {Category}", category);
                throw;
            }
        }

        private async Task ValidateEventAsync(Event eventItem)
        {
            var validationErrors = new List<string>();

            // Business rule validations
            if (string.IsNullOrWhiteSpace(eventItem.Name))
            {
                validationErrors.Add("Event name is required");
            }

            if (string.IsNullOrWhiteSpace(eventItem.Category))
            {
                validationErrors.Add("Event category is required");
            }

            if (string.IsNullOrWhiteSpace(eventItem.Location))
            {
                validationErrors.Add("Event location is required");
            }

            if (eventItem.EventDate <= DateTime.UtcNow)
            {
                validationErrors.Add("Event date must be in the future");
            }

            if (eventItem.MaxCapacity <= 0)
            {
                validationErrors.Add("Event capacity must be greater than zero");
            }

            if (eventItem.TicketPrice < 0)
            {
                validationErrors.Add("Ticket price cannot be negative");
            }

            if (eventItem.OrganizerUserId == Guid.Empty)
            {
                validationErrors.Add("Organizer user ID is required");
            }

            // Check for duplicate events (same name, date, and organizer)
            if (eventItem.EventId == Guid.Empty) // Only for new events
            {
                var existingEvents = await _eventRepository.GetEventsByOrganizerAsync(eventItem.OrganizerUserId);
                var duplicateEvent = existingEvents.FirstOrDefault(e => 
                    e.Name.Equals(eventItem.Name, StringComparison.OrdinalIgnoreCase) &&
                    e.EventDate.Date == eventItem.EventDate.Date);
                
                if (duplicateEvent != null)
                {
                    validationErrors.Add("An event with the same name and date already exists for this organizer");
                }
            }

            if (validationErrors.Any())
            {
                var errorMessage = string.Join("; ", validationErrors);
                _logger.LogWarning("Event validation failed: {ValidationErrors}", errorMessage);
                throw new ArgumentException($"Event validation failed: {errorMessage}");
            }
        }
    }
}
