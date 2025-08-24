using EventSearch.Core.Entities;
using EventSearch.API.DTOs;

namespace EventSearch.API.Mappers;

public static class EventMapper
{
    public static EventSearchResultDto ToDto(this SearchableEvent entity)
    {
        return new EventSearchResultDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Category = entity.Category,
            Location = entity.Location,
            City = entity.City,
            Country = entity.Country,
            Venue = entity.Venue,
            Price = entity.Price,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            AvailableTickets = entity.AvailableTickets,
            Organizer = entity.Organizer,
            Tags = entity.Tags,
            Popularity = entity.Popularity,
            ViewCount = entity.ViewCount,
            BookingCount = entity.BookingCount,
            AverageRating = entity.AverageRating,
            RatingCount = entity.RatingCount
        };
    }

    public static SearchableEvent ToEntity(this IndexEventRequestDto dto)
    {
        return new SearchableEvent
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Location = dto.Location,
            City = dto.City,
            Country = dto.Country,
            Venue = dto.Venue,
            Price = dto.Price,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            AvailableTickets = dto.AvailableTickets,
            Organizer = dto.Organizer,
            Tags = dto.Tags,
            Popularity = dto.Popularity,
            ViewCount = dto.ViewCount,
            BookingCount = dto.BookingCount,
            AverageRating = dto.AverageRating,
            RatingCount = dto.RatingCount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static AutocompleteResponseDto ToDto(this Core.Models.AutocompleteResult result)
    {
        return new AutocompleteResponseDto
        {
            Text = result.Text,
            Type = result.Type,
            Score = result.Score
        };
    }
}