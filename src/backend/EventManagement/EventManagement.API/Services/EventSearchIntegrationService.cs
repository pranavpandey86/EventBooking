using EventManagement.API.Interfaces;
using EventSearch.API.DTOs;
using System.Text;
using System.Text.Json;

namespace EventManagement.API.Services;

public class EventSearchIntegrationService : IEventSearchIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventSearchIntegrationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventSearchIntegrationService(HttpClient httpClient, ILogger<EventSearchIntegrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<bool> IndexEventAsync(IndexEventRequestDto eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Indexing event {EventId} in search service", eventData.Id);

            var json = JsonSerializer.Serialize(eventData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/index/events", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully indexed event {EventId} in search service", eventData.Id);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to index event {EventId}. Status: {StatusCode}, Error: {Error}", 
                    eventData.Id, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while indexing event {EventId} in search service", eventData.Id);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while indexing event {EventId} in search service", eventData.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while indexing event {EventId} in search service", eventData.Id);
            return false;
        }
    }

    public async Task<bool> UpdateEventAsync(IndexEventRequestDto eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating event {EventId} in search service", eventData.Id);

            var json = JsonSerializer.Serialize(eventData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/api/index/events/{eventData.Id}", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated event {EventId} in search service", eventData.Id);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to update event {EventId}. Status: {StatusCode}, Error: {Error}", 
                    eventData.Id, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while updating event {EventId} in search service", eventData.Id);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while updating event {EventId} in search service", eventData.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating event {EventId} in search service", eventData.Id);
            return false;
        }
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting event {EventId} from search service", eventId);

            var response = await _httpClient.DeleteAsync($"/api/index/events/{eventId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted event {EventId} from search service", eventId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to delete event {EventId}. Status: {StatusCode}, Error: {Error}", 
                    eventId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while deleting event {EventId} from search service", eventId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while deleting event {EventId} from search service", eventId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting event {EventId} from search service", eventId);
            return false;
        }
    }

    public async Task<bool> IsSearchServiceHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking search service health");
            return false;
        }
    }
}