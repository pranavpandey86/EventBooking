using EventManagement.API.DTOs;
using EventManagement.API.Extensions;
using EventManagement.API.Interfaces;
using EventManagement.API.Mappers;
using EventManagement.Core.Entities;
using EventManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventManagement.API.Services
{
    public class EventDtoService : IEventDtoService
    {
        private readonly IEventService _eventService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<EventDtoService> _logger;

        public EventDtoService(
            IEventService eventService, 
            IEventPublisher eventPublisher,
            ILogger<EventDtoService> logger)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EventDto>> GetAllEventsAsync()
        {
            _logger.LogInformation("Getting all events through DTO service");
            
            var events = await _eventService.GetAllEventsAsync();
            return events.Select(MapToEventDto);
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
        {
            _logger.LogInformation("Getting event by ID {EventId} through DTO service", eventId);
            
            var eventItem = await _eventService.GetEventByIdAsync(eventId);
            return eventItem != null ? MapToEventDto(eventItem) : null;
        }

        public async Task<IEnumerable<EventDto>> GetEventsByOrganizerAsync(Guid organizerUserId)
        {
            _logger.LogInformation("Getting events by organizer {OrganizerId} through DTO service", organizerUserId);
            
            var events = await _eventService.GetEventsByOrganizerAsync(organizerUserId);
            return events.Select(MapToEventDto);
        }

        public async Task<IEnumerable<EventDto>> SearchEventsAsync(EventSearchDto? searchDto)
        {
            _logger.LogInformation("Searching events through DTO service");
            
            var events = await _eventService.SearchEventsAsync(
                searchDto?.SearchTerm,
                searchDto?.Category,
                searchDto?.StartDate,
                searchDto?.EndDate);
            
            return events.Select(MapToEventDto);
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto)
        {
            _logger.LogInformation("Creating event {EventName} through DTO service", createEventDto.Name);
            
            if (createEventDto == null)
            {
                throw new ArgumentNullException(nameof(createEventDto));
            }

            var eventEntity = MapToEntity(createEventDto);
            var createdEvent = await _eventService.CreateEventAsync(eventEntity);
            var eventDto = MapToEventDto(createdEvent);

            // Publish event created message to Kafka (synchronous with proper error handling)
            try
            {
                _logger.LogInformation("Publishing event-created message for event {EventId} to Kafka", createdEvent.EventId);
                var eventMessage = eventDto.ToEventCreatedMessage();
                var success = await _eventPublisher.PublishEventCreatedAsync(eventMessage);
                if (success)
                {
                    _logger.LogInformation("Successfully published event-created message for event {EventId}", createdEvent.EventId);
                }
                else
                {
                    _logger.LogError("Failed to publish event-created message for event {EventId} - Kafka publisher returned false", createdEvent.EventId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while publishing event-created message for event {EventId}", createdEvent.EventId);
            }

            // PURE KAFKA IMPLEMENTATION - No HTTP fallback
            _logger.LogInformation("Event {EventId} processing complete - Kafka-only architecture", createdEvent.EventId);
            
            return eventDto;
        }

        public async Task<EventDto?> UpdateEventAsync(Guid eventId, UpdateEventDto updateEventDto)
        {
            _logger.LogInformation("Updating event {EventId} through DTO service", eventId);
            
            if (updateEventDto == null)
            {
                throw new ArgumentNullException(nameof(updateEventDto));
            }

            var eventEntity = MapToEntity(updateEventDto);
            eventEntity.EventId = eventId;  // Ensure the ID is set correctly
            
            var updatedEvent = await _eventService.UpdateEventAsync(eventId, eventEntity);
            
            if (updatedEvent != null)
            {
                var eventDto = MapToEventDto(updatedEvent);

                // Publish event updated message to Kafka (synchronous with proper error handling)
                try
                {
                    _logger.LogInformation("Publishing event-updated message for event {EventId} to Kafka", eventId);
                    var eventMessage = eventDto.ToEventUpdatedMessage();
                    var success = await _eventPublisher.PublishEventUpdatedAsync(eventMessage);
                    if (success)
                    {
                        _logger.LogInformation("Successfully published event-updated message for event {EventId}", eventId);
                    }
                    else
                    {
                        _logger.LogError("Failed to publish event-updated message for event {EventId} - Kafka publisher returned false", eventId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while publishing event-updated message for event {EventId}", eventId);
                }

                // PURE KAFKA IMPLEMENTATION - No HTTP fallback
                _logger.LogInformation("Event {EventId} update processing complete - Kafka-only architecture", eventId);

                return eventDto;
            }
            
            return null;
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            _logger.LogInformation("Deleting event {EventId} through DTO service", eventId);
            
            var success = await _eventService.DeleteEventAsync(eventId);

            if (success)
            {
                // Publish event deleted message to Kafka (synchronous with proper error handling)
                try
                {
                    _logger.LogInformation("Publishing event-deleted message for event {EventId} to Kafka", eventId);
                    var eventMessage = eventId.ToEventDeletedMessage();
                    var kafkaSuccess = await _eventPublisher.PublishEventDeletedAsync(eventMessage);
                    if (kafkaSuccess)
                    {
                        _logger.LogInformation("Successfully published event-deleted message for event {EventId}", eventId);
                    }
                    else
                    {
                        _logger.LogError("Failed to publish event-deleted message for event {EventId} - Kafka publisher returned false", eventId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while publishing event-deleted message for event {EventId}", eventId);
                }

                // PURE KAFKA IMPLEMENTATION - No HTTP fallback
                _logger.LogInformation("Event {EventId} deletion processing complete - Kafka-only architecture", eventId);
            }

            return success;
        }

        public async Task<bool> EventExistsAsync(Guid eventId)
        {
            return await _eventService.EventExistsAsync(eventId);
        }

        public async Task<IEnumerable<EventDto>> GetActiveEventsAsync()
        {
            _logger.LogInformation("Getting active events through DTO service");
            
            var events = await _eventService.GetActiveEventsAsync();
            return events.Select(MapToEventDto);
        }

        public async Task<IEnumerable<EventDto>> GetEventsByCategoryAsync(string category)
        {
            _logger.LogInformation("Getting events by category {Category} through DTO service", category);
            
            var events = await _eventService.GetEventsByCategoryAsync(category);
            return events.Select(MapToEventDto);
        }

        #region Mapping Methods

        private static EventDto MapToEventDto(Event eventEntity)
        {
            return new EventDto
            {
                EventId = eventEntity.EventId,
                Name = eventEntity.Name,
                Description = eventEntity.Description,
                Category = eventEntity.Category,
                EventDate = eventEntity.EventDate,
                Location = eventEntity.Location,
                MaxCapacity = eventEntity.MaxCapacity,
                TicketPrice = eventEntity.TicketPrice,
                OrganizerUserId = eventEntity.OrganizerUserId,
                IsActive = eventEntity.IsActive,
                CreatedAt = eventEntity.CreatedAt,
                UpdatedAt = eventEntity.UpdatedAt
            };
        }

        private static Event MapToEntity(CreateEventDto createEventDto)
        {
            return new Event
            {
                Name = createEventDto.Name,
                Description = createEventDto.Description ?? string.Empty,
                Category = createEventDto.Category,
                EventDate = createEventDto.EventDate,
                Location = createEventDto.Location,
                MaxCapacity = createEventDto.MaxCapacity,
                TicketPrice = createEventDto.TicketPrice,
                OrganizerUserId = createEventDto.OrganizerUserId
            };
        }

        private static Event MapToEntity(UpdateEventDto updateEventDto)
        {
            return new Event
            {
                Name = updateEventDto.Name,
                Description = updateEventDto.Description ?? string.Empty,
                Category = updateEventDto.Category,
                EventDate = updateEventDto.EventDate,
                Location = updateEventDto.Location,
                MaxCapacity = updateEventDto.MaxCapacity,
                TicketPrice = updateEventDto.TicketPrice
            };
        }

        #endregion
    }
}
