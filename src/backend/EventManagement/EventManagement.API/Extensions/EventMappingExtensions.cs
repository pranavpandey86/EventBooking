using EventManagement.API.DTOs;
using EventManagement.API.Models;
using EventManagement.Core.Entities;

namespace EventManagement.API.Extensions;

public static class EventMappingExtensions
{
    public static EventCreatedMessage ToEventCreatedMessage(this Event eventEntity)
    {
        return new EventCreatedMessage
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
            Timestamp = DateTime.UtcNow
        };
    }

    public static EventCreatedMessage ToEventCreatedMessage(this EventDto eventDto)
    {
        return new EventCreatedMessage
        {
            EventId = eventDto.EventId,
            Name = eventDto.Name,
            Description = eventDto.Description,
            Category = eventDto.Category,
            EventDate = eventDto.EventDate,
            Location = eventDto.Location,
            MaxCapacity = eventDto.MaxCapacity,
            TicketPrice = eventDto.TicketPrice,
            OrganizerUserId = eventDto.OrganizerUserId,
            IsActive = eventDto.IsActive,
            CreatedAt = eventDto.CreatedAt,
            Timestamp = DateTime.UtcNow
        };
    }

    public static EventUpdatedMessage ToEventUpdatedMessage(this Event eventEntity)
    {
        return new EventUpdatedMessage
        {
            EventId = eventEntity.EventId,
            Name = eventEntity.Name,
            Description = eventEntity.Description,
            Category = eventEntity.Category,
            EventDate = eventEntity.EventDate,
            Location = eventEntity.Location,
            MaxCapacity = eventEntity.MaxCapacity,
            TicketPrice = eventEntity.TicketPrice,
            IsActive = eventEntity.IsActive,
            UpdatedAt = eventEntity.UpdatedAt,
            Timestamp = DateTime.UtcNow
        };
    }

    public static EventUpdatedMessage ToEventUpdatedMessage(this EventDto eventDto)
    {
        return new EventUpdatedMessage
        {
            EventId = eventDto.EventId,
            Name = eventDto.Name,
            Description = eventDto.Description,
            Category = eventDto.Category,
            EventDate = eventDto.EventDate,
            Location = eventDto.Location,
            MaxCapacity = eventDto.MaxCapacity,
            TicketPrice = eventDto.TicketPrice,
            IsActive = eventDto.IsActive,
            UpdatedAt = eventDto.UpdatedAt,
            Timestamp = DateTime.UtcNow
        };
    }

    public static EventDeletedMessage ToEventDeletedMessage(this Guid eventId)
    {
        return new EventDeletedMessage
        {
            EventId = eventId,
            DeletedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };
    }
}