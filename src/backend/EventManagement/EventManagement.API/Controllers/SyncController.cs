using EventManagement.API.DTOs;
using EventManagement.API.Interfaces;
using EventManagement.API.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SyncController : ControllerBase
{
    private readonly IEventDtoService _eventDtoService;
    private readonly IEventSearchIntegrationService _searchIntegrationService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IEventDtoService eventDtoService,
        IEventSearchIntegrationService searchIntegrationService,
        ILogger<SyncController> logger)
    {
        _eventDtoService = eventDtoService;
        _searchIntegrationService = searchIntegrationService;
        _logger = logger;
    }

    /// <summary>
    /// Synchronize all events from EventManagement to EventSearch service
    /// </summary>
    /// <returns>Synchronization results</returns>
    [HttpPost("events-to-search")]
    public async Task<ActionResult> SyncEventsToSearch()
    {
        try
        {
            _logger.LogInformation("Starting synchronization of events to search service");

            // Check if search service is healthy
            var isHealthy = await _searchIntegrationService.IsSearchServiceHealthyAsync();
            if (!isHealthy)
            {
                _logger.LogWarning("Search service is not healthy, skipping synchronization");
                return BadRequest(new { message = "Search service is not available" });
            }

            // Get all events from EventManagement
            var events = await _eventDtoService.GetAllEventsAsync();
            var eventList = events.ToList();

            if (!eventList.Any())
            {
                _logger.LogInformation("No events found to synchronize");
                return Ok(new { message = "No events found to synchronize", synchronized = 0 });
            }

            var successful = 0;
            var failed = 0;

            // Index each event individually (could be optimized to bulk later)
            foreach (var eventDto in eventList)
            {
                try
                {
                    var searchDto = eventDto.ToSearchIndexDto();
                    var success = await _searchIntegrationService.IndexEventAsync(searchDto);
                    
                    if (success)
                    {
                        successful++;
                        _logger.LogDebug("Successfully synchronized event {EventId}", eventDto.EventId);
                    }
                    else
                    {
                        failed++;
                        _logger.LogWarning("Failed to synchronize event {EventId}", eventDto.EventId);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Error synchronizing event {EventId}", eventDto.EventId);
                }
            }

            _logger.LogInformation("Synchronization completed. Successful: {Successful}, Failed: {Failed}", 
                successful, failed);

            return Ok(new 
            { 
                message = "Synchronization completed", 
                totalEvents = eventList.Count,
                successful,
                failed 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during event synchronization");
            return Problem("An error occurred during synchronization", statusCode: 500);
        }
    }

    /// <summary>
    /// Check the health of the EventSearch service
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("search-service-health")]
    public async Task<ActionResult> CheckSearchServiceHealth()
    {
        try
        {
            var isHealthy = await _searchIntegrationService.IsSearchServiceHealthyAsync();
            
            return Ok(new 
            { 
                searchServiceHealthy = isHealthy,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking search service health");
            return Problem("An error occurred while checking search service health", statusCode: 500);
        }
    }
}