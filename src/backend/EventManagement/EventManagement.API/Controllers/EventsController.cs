using EventManagement.API.DTOs;
using EventManagement.Core.Entities;
using EventManagement.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventRepository eventRepository, ILogger<EventsController> logger)
        {
            _eventRepository = eventRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
        {
            try
            {
                var events = await _eventRepository.GetAllEventsAsync();
                var eventDtos = events.Select(MapToEventDto);
                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return StatusCode(500, "An error occurred while retrieving events");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EventDto>> GetEvent(Guid id)
        {
            try
            {
                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null)
                {
                    return NotFound($"Event with ID {id} not found");
                }

                return Ok(MapToEventDto(eventItem));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event {EventId}", id);
                return StatusCode(500, "An error occurred while retrieving the event");
            }
        }

        [HttpGet("organizer/{organizerId}")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEventsByOrganizer(Guid organizerId)
        {
            try
            {
                var events = await _eventRepository.GetEventsByOrganizerAsync(organizerId);
                var eventDtos = events.Select(MapToEventDto);
                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for organizer {OrganizerId}", organizerId);
                return StatusCode(500, "An error occurred while retrieving events");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EventDto>>> SearchEvents([FromQuery] EventSearchDto searchDto)
        {
            try
            {
                var events = await _eventRepository.SearchEventsAsync(
                    searchDto.SearchTerm,
                    searchDto.Category,
                    searchDto.StartDate,
                    searchDto.EndDate);

                var eventDtos = events.Select(MapToEventDto);
                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                return StatusCode(500, "An error occurred while searching events");
            }
        }

        [HttpPost]
        public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventDto createEventDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var eventItem = MapToEvent(createEventDto);
                var createdEvent = await _eventRepository.CreateEventAsync(eventItem);

                var eventDto = MapToEventDto(createdEvent);
                return CreatedAtAction(nameof(GetEvent), new { id = eventDto.EventId }, eventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, "An error occurred while creating the event");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EventDto>> UpdateEvent(Guid id, [FromBody] UpdateEventDto updateEventDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingEvent = await _eventRepository.GetEventByIdAsync(id);
                if (existingEvent == null)
                {
                    return NotFound($"Event with ID {id} not found");
                }

                // Update properties
                existingEvent.Name = updateEventDto.Name;
                existingEvent.Description = updateEventDto.Description;
                existingEvent.Category = updateEventDto.Category;
                existingEvent.EventDate = updateEventDto.EventDate;
                existingEvent.Location = updateEventDto.Location;
                existingEvent.MaxCapacity = updateEventDto.MaxCapacity;
                existingEvent.TicketPrice = updateEventDto.TicketPrice;

                var updatedEvent = await _eventRepository.UpdateEventAsync(existingEvent);
                return Ok(MapToEventDto(updatedEvent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", id);
                return StatusCode(500, "An error occurred while updating the event");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEvent(Guid id)
        {
            try
            {
                var success = await _eventRepository.DeleteEventAsync(id);
                if (!success)
                {
                    return NotFound($"Event with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                return StatusCode(500, "An error occurred while deleting the event");
            }
        }

        // Helper methods for mapping
        private static EventDto MapToEventDto(Event eventItem)
        {
            return new EventDto
            {
                EventId = eventItem.EventId,
                Name = eventItem.Name,
                Description = eventItem.Description,
                Category = eventItem.Category,
                EventDate = eventItem.EventDate,
                Location = eventItem.Location,
                MaxCapacity = eventItem.MaxCapacity,
                TicketPrice = eventItem.TicketPrice,
                OrganizerUserId = eventItem.OrganizerUserId,
                IsActive = eventItem.IsActive,
                CreatedAt = eventItem.CreatedAt,
                UpdatedAt = eventItem.UpdatedAt
            };
        }

        private static Event MapToEvent(CreateEventDto createEventDto)
        {
            return new Event
            {
                Name = createEventDto.Name,
                Description = createEventDto.Description,
                Category = createEventDto.Category,
                EventDate = createEventDto.EventDate,
                Location = createEventDto.Location,
                MaxCapacity = createEventDto.MaxCapacity,
                TicketPrice = createEventDto.TicketPrice,
                OrganizerUserId = createEventDto.OrganizerUserId,
                IsActive = true
            };
        }
    }
}
