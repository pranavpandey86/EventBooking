using EventSearch.API.DTOs;
using EventManagement.API.DTOs;

namespace EventManagement.API.Mappers;

public static class EventSearchMapper
{
    public static IndexEventRequestDto ToSearchIndexDto(this EventDto eventDto)
    {
        return new IndexEventRequestDto
        {
            Id = eventDto.EventId,
            Title = eventDto.Name,
            Description = eventDto.Description ?? string.Empty,
            Category = eventDto.Category,
            Location = eventDto.Location ?? string.Empty,
            City = ExtractCityFromLocation(eventDto.Location),
            Country = "USA", // Default for now, can be extracted/configured later
            Venue = eventDto.Location ?? string.Empty,
            Price = eventDto.TicketPrice,
            StartDate = eventDto.EventDate,
            EndDate = eventDto.EventDate.AddHours(3), // Assume 3-hour events by default
            AvailableTickets = eventDto.MaxCapacity,
            Organizer = eventDto.OrganizerUserId.ToString(),
            Tags = GenerateTagsFromEvent(eventDto),
            Popularity = CalculatePopularity(eventDto),
            ViewCount = 0, // Initialize to 0, can be updated later
            BookingCount = 0, // Initialize to 0, can be updated later
            AverageRating = 0, // Initialize to 0, can be updated later
            RatingCount = 0 // Initialize to 0, can be updated later
        };
    }

    public static IndexEventRequestDto ToSearchIndexDto(this CreateEventDto createDto, Guid eventId, Guid organizerUserId)
    {
        return new IndexEventRequestDto
        {
            Id = eventId,
            Title = createDto.Name,
            Description = createDto.Description ?? string.Empty,
            Category = createDto.Category,
            Location = createDto.Location ?? string.Empty,
            City = ExtractCityFromLocation(createDto.Location),
            Country = "USA", // Default for now
            Venue = createDto.Location ?? string.Empty,
            Price = createDto.TicketPrice,
            StartDate = createDto.EventDate,
            EndDate = createDto.EventDate.AddHours(3),
            AvailableTickets = createDto.MaxCapacity,
            Organizer = organizerUserId.ToString(),
            Tags = GenerateTagsFromCreateEvent(createDto),
            Popularity = 1.0m, // Default popularity for new events
            ViewCount = 0,
            BookingCount = 0,
            AverageRating = 0,
            RatingCount = 0
        };
    }

    private static string ExtractCityFromLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return string.Empty;

        // Simple city extraction - can be enhanced with more sophisticated parsing
        var parts = location.Split(',');
        return parts.Length > 0 ? parts[0].Trim() : location.Trim();
    }

    private static List<string> GenerateTagsFromEvent(EventDto eventDto)
    {
        var tags = new List<string>();
        
        // Add category as tag
        if (!string.IsNullOrWhiteSpace(eventDto.Category))
            tags.Add(eventDto.Category.ToLowerInvariant());

        // Add city as tag if available
        var city = ExtractCityFromLocation(eventDto.Location);
        if (!string.IsNullOrWhiteSpace(city))
            tags.Add(city.ToLowerInvariant());

        // Add price range tags
        if (eventDto.TicketPrice <= 25)
            tags.Add("budget");
        else if (eventDto.TicketPrice <= 100)
            tags.Add("affordable");
        else
            tags.Add("premium");

        // Add capacity tags
        if (eventDto.MaxCapacity <= 50)
            tags.Add("intimate");
        else if (eventDto.MaxCapacity <= 500)
            tags.Add("medium");
        else
            tags.Add("large");

        return tags;
    }

    private static List<string> GenerateTagsFromCreateEvent(CreateEventDto createDto)
    {
        var tags = new List<string>();
        
        // Add category as tag
        if (!string.IsNullOrWhiteSpace(createDto.Category))
            tags.Add(createDto.Category.ToLowerInvariant());

        // Add city as tag if available
        var city = ExtractCityFromLocation(createDto.Location);
        if (!string.IsNullOrWhiteSpace(city))
            tags.Add(city.ToLowerInvariant());

        // Add price range tags
        if (createDto.TicketPrice <= 25)
            tags.Add("budget");
        else if (createDto.TicketPrice <= 100)
            tags.Add("affordable");
        else
            tags.Add("premium");

        // Add capacity tags
        if (createDto.MaxCapacity <= 50)
            tags.Add("intimate");
        else if (createDto.MaxCapacity <= 500)
            tags.Add("medium");
        else
            tags.Add("large");

        return tags;
    }

    private static decimal CalculatePopularity(EventDto eventDto)
    {
        // Simple popularity calculation - can be enhanced with more factors
        var basePopularity = 1.0m;
        
        // Recent events get higher popularity
        var daysSinceCreation = (DateTime.UtcNow - eventDto.CreatedAt).Days;
        if (daysSinceCreation <= 7)
            basePopularity += 2.0m;
        else if (daysSinceCreation <= 30)
            basePopularity += 1.0m;

        // Active events get higher popularity
        if (eventDto.IsActive)
            basePopularity += 1.0m;

        // Lower price events might be more popular
        if (eventDto.TicketPrice <= 50)
            basePopularity += 0.5m;

        return Math.Min(basePopularity, 5.0m); // Cap at 5.0
    }
}