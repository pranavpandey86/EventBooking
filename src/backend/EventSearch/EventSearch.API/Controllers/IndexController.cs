using EventSearch.API.DTOs;
using EventSearch.API.Mappers;
using EventSearch.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventSearch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IndexController : ControllerBase
{
    private readonly IEventIndexService _eventIndexService;
    private readonly ILogger<IndexController> _logger;

    public IndexController(IEventIndexService eventIndexService, ILogger<IndexController> logger)
    {
        _eventIndexService = eventIndexService;
        _logger = logger;
    }

    /// <summary>
    /// Index a single event for search
    /// </summary>
    /// <param name="request">Event data to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> IndexEvent(
        [FromBody] IndexEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Indexing event {EventId}: {EventTitle}", request.Id, request.Title);

            var searchableEvent = request.ToEntity();
            await _eventIndexService.IndexEventAsync(searchableEvent, cancellationToken);

            _logger.LogInformation("Successfully indexed event {EventId}", request.Id);
            return Ok(new { message = "Event indexed successfully", eventId = request.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing event {EventId}", request.Id);
            return Problem("An error occurred while indexing the event", statusCode: 500);
        }
    }

    /// <summary>
    /// Index multiple events in bulk
    /// </summary>
    /// <param name="requests">List of events to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status with count</returns>
    [HttpPost("events/bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> IndexEventsBulk(
        [FromBody] List<IndexEventRequestDto> requests,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!requests.Any())
            {
                return BadRequest("No events provided for indexing");
            }

            _logger.LogInformation("Bulk indexing {EventCount} events", requests.Count);

            var searchableEvents = requests.Select(r => r.ToEntity()).ToList();
            await _eventIndexService.IndexEventsAsync(searchableEvents, cancellationToken);

            _logger.LogInformation("Successfully bulk indexed {EventCount} events", requests.Count);
            return Ok(new { message = "Events indexed successfully", count = requests.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing {EventCount} events", requests.Count);
            return Problem("An error occurred while bulk indexing events", statusCode: 500);
        }
    }

    /// <summary>
    /// Update an existing event in the search index
    /// </summary>
    /// <param name="eventId">ID of the event to update</param>
    /// <param name="request">Updated event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPut("events/{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateEvent(
        [FromRoute] Guid eventId,
        [FromBody] IndexEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (eventId != request.Id)
            {
                return BadRequest("Event ID in URL does not match event ID in request body");
            }

            _logger.LogInformation("Updating event {EventId}: {EventTitle}", request.Id, request.Title);

            var searchableEvent = request.ToEntity();
            await _eventIndexService.UpdateEventAsync(searchableEvent, cancellationToken);

            _logger.LogInformation("Successfully updated event {EventId}", request.Id);
            return Ok(new { message = "Event updated successfully", eventId = request.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", eventId);
            return Problem("An error occurred while updating the event", statusCode: 500);
        }
    }

    /// <summary>
    /// Remove an event from the search index
    /// </summary>
    /// <param name="eventId">ID of the event to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("events/{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteEvent(
        [FromRoute] Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting event {EventId} from search index", eventId);

            // Check if event exists first
            var exists = await _eventIndexService.EventExistsAsync(eventId, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Event {EventId} not found in search index", eventId);
                return NotFound(new { message = "Event not found in search index", eventId });
            }

            await _eventIndexService.DeleteEventAsync(eventId, cancellationToken);

            _logger.LogInformation("Successfully deleted event {EventId} from search index", eventId);
            return Ok(new { message = "Event deleted successfully", eventId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId} from search index", eventId);
            return Problem("An error occurred while deleting the event", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if an event exists in the search index
    /// </summary>
    /// <param name="eventId">ID of the event to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existence status</returns>
    [HttpHead("events/{eventId:guid}")]
    [HttpGet("events/{eventId:guid}/exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> EventExists(
        [FromRoute] Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _eventIndexService.EventExistsAsync(eventId, cancellationToken);
            
            if (exists)
            {
                return Ok(new { exists = true, eventId });
            }
            else
            {
                return NotFound(new { exists = false, eventId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if event {EventId} exists in search index", eventId);
            return Problem("An error occurred while checking event existence", statusCode: 500);
        }
    }
}