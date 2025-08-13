using EventManagement.API.DTOs;
using EventManagement.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventDtoService _eventDtoService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventDtoService eventDtoService, ILogger<EventsController> logger)
        {
            _eventDtoService = eventDtoService ?? throw new ArgumentNullException(nameof(eventDtoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
        {
            try
            {
                _logger.LogInformation("Getting all events");
                var events = await _eventDtoService.GetAllEventsAsync();
                return Ok(events);
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
                _logger.LogInformation("Getting event with ID: {EventId}", id);
                var eventDto = await _eventDtoService.GetEventByIdAsync(id);
                
                if (eventDto == null)
                {
                    return NotFound($"Event with ID {id} not found");
                }

                return Ok(eventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event with ID: {EventId}", id);
                return StatusCode(500, "An error occurred while retrieving the event");
            }
        }

        [HttpGet("organizer/{organizerId}")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEventsByOrganizer(Guid organizerId)
        {
            try
            {
                _logger.LogInformation("Getting events for organizer: {OrganizerId}", organizerId);
                var events = await _eventDtoService.GetEventsByOrganizerAsync(organizerId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for organizer: {OrganizerId}", organizerId);
                return StatusCode(500, "An error occurred while retrieving events for the organizer");
            }
        }

        [HttpPost("search")]
        public async Task<ActionResult<IEnumerable<EventDto>>> SearchEvents([FromBody] EventSearchDto? searchDto)
        {
            try
            {
                _logger.LogInformation("Searching events");
                var events = await _eventDtoService.SearchEventsAsync(searchDto);
                return Ok(events);
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

                _logger.LogInformation("Creating new event: {EventName}", createEventDto.Name);
                var createdEvent = await _eventDtoService.CreateEventAsync(createEventDto);
                return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.EventId }, createdEvent);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid event data provided");
                return BadRequest(ex.Message);
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

                _logger.LogInformation("Updating event with ID: {EventId}", id);
                var updatedEvent = await _eventDtoService.UpdateEventAsync(id, updateEventDto);
                
                if (updatedEvent == null)
                {
                    return NotFound($"Event with ID {id} not found");
                }

                return Ok(updatedEvent);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid event data provided for update");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event with ID: {EventId}", id);
                return StatusCode(500, "An error occurred while updating the event");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEvent(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting event with ID: {EventId}", id);
                var success = await _eventDtoService.DeleteEventAsync(id);
                
                if (!success)
                {
                    return NotFound($"Event with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with ID: {EventId}", id);
                return StatusCode(500, "An error occurred while deleting the event");
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetActiveEvents()
        {
            try
            {
                _logger.LogInformation("Getting active events");
                var events = await _eventDtoService.GetActiveEventsAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active events");
                return StatusCode(500, "An error occurred while retrieving active events");
            }
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEventsByCategory(string category)
        {
            try
            {
                _logger.LogInformation("Getting events by category: {Category}", category);
                var events = await _eventDtoService.GetEventsByCategoryAsync(category);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events by category: {Category}", category);
                return StatusCode(500, "An error occurred while retrieving events by category");
            }
        }

        [HttpHead("{id}")]
        public async Task<ActionResult> EventExists(Guid id)
        {
            try
            {
                var exists = await _eventDtoService.EventExistsAsync(id);
                return exists ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if event exists with ID: {EventId}", id);
                return StatusCode(500, "An error occurred while checking event existence");
            }
        }
    }
}
