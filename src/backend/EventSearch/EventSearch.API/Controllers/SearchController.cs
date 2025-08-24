using EventSearch.API.DTOs;
using EventSearch.API.Mappers;
using EventSearch.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventSearch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Search for events based on various criteria
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with pagination and facets</returns>
    [HttpPost("events")]
    [ProducesResponseType(typeof(SearchResponseDto<EventSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SearchResponseDto<EventSearchResultDto>>> SearchEvents(
        [FromBody] SearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching events with query: {SearchText}, Page: {Page}", 
                request.SearchText, request.Page);

            var query = request.ToSearchQuery();
            var result = await _searchService.SearchEventsAsync(query, cancellationToken);
            
            var response = SearchResponseDto<EventSearchResultDto>.FromSearchResult(result, e => e.ToDto());
            
            _logger.LogInformation("Search completed in {SearchTimeMs}ms, found {TotalCount} results", 
                result.SearchTimeMs, result.TotalCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events");
            return Problem("An error occurred while searching events", statusCode: 500);
        }
    }

    /// <summary>
    /// Get autocomplete suggestions for search queries
    /// </summary>
    /// <param name="query">Search query text</param>
    /// <param name="maxResults">Maximum number of suggestions to return (default: 10, max: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of autocomplete suggestions</returns>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(List<AutocompleteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AutocompleteResponseDto>>> GetAutocompleteSuggestions(
        [FromQuery, Required] string query,
        [FromQuery, Range(1, 20)] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return BadRequest("Query must be at least 2 characters long");
            }

            _logger.LogDebug("Getting autocomplete suggestions for query: {Query}", query);

            var suggestions = await _searchService.GetAutocompleteSuggestionsAsync(query, maxResults, cancellationToken);
            var response = suggestions.Select(s => s.ToDto()).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete suggestions for query: {Query}", query);
            return Problem("An error occurred while getting autocomplete suggestions", statusCode: 500);
        }
    }

    /// <summary>
    /// Get events similar to a specific event
    /// </summary>
    /// <param name="eventId">ID of the event to find similar events for</param>
    /// <param name="maxResults">Maximum number of similar events to return (default: 10, max: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of similar events</returns>
    [HttpGet("similar/{eventId:guid}")]
    [ProducesResponseType(typeof(SearchResponseDto<EventSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SearchResponseDto<EventSearchResultDto>>> GetSimilarEvents(
        [FromRoute] Guid eventId,
        [FromQuery, Range(1, 20)] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting similar events for event: {EventId}", eventId);

            var result = await _searchService.GetSimilarEventsAsync(eventId, maxResults, cancellationToken);
            
            if (result.TotalCount == 0)
            {
                _logger.LogInformation("No similar events found for event: {EventId}", eventId);
            }

            var response = SearchResponseDto<EventSearchResultDto>.FromSearchResult(result, e => e.ToDto());
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar events for event: {EventId}", eventId);
            return Problem("An error occurred while getting similar events", statusCode: 500);
        }
    }

    /// <summary>
    /// Get popular events, optionally filtered by category and/or city
    /// </summary>
    /// <param name="category">Filter by event category</param>
    /// <param name="city">Filter by city</param>
    /// <param name="maxResults">Maximum number of events to return (default: 20, max: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of popular events</returns>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(SearchResponseDto<EventSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SearchResponseDto<EventSearchResultDto>>> GetPopularEvents(
        [FromQuery] string? category = null,
        [FromQuery] string? city = null,
        [FromQuery, Range(1, 50)] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting popular events - Category: {Category}, City: {City}", 
                category ?? "All", city ?? "All");

            var result = await _searchService.GetPopularEventsAsync(category, city, maxResults, cancellationToken);
            var response = SearchResponseDto<EventSearchResultDto>.FromSearchResult(result, e => e.ToDto());

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular events");
            return Problem("An error occurred while getting popular events", statusCode: 500);
        }
    }
}