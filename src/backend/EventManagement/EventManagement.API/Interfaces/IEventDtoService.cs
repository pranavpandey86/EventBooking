using EventManagement.API.DTOs;
using EventManagement.Core.Entities;

namespace EventManagement.API.Interfaces
{
    public interface IEventDtoService
    {
        Task<IEnumerable<EventDto>> GetAllEventsAsync();
        Task<EventDto?> GetEventByIdAsync(Guid eventId);
        Task<IEnumerable<EventDto>> GetEventsByOrganizerAsync(Guid organizerUserId);
        Task<IEnumerable<EventDto>> SearchEventsAsync(EventSearchDto? searchDto);
        Task<EventDto> CreateEventAsync(CreateEventDto createEventDto);
        Task<EventDto?> UpdateEventAsync(Guid eventId, UpdateEventDto updateEventDto);
        Task<bool> DeleteEventAsync(Guid eventId);
        Task<bool> EventExistsAsync(Guid eventId);
        Task<IEnumerable<EventDto>> GetActiveEventsAsync();
        Task<IEnumerable<EventDto>> GetEventsByCategoryAsync(string category);
    }
}
