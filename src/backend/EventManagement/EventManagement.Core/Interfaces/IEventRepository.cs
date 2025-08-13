using EventManagement.Core.Entities;

namespace EventManagement.Core.Interfaces
{
    public interface IEventRepository
    {
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<Event?> GetEventByIdAsync(Guid eventId);
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(Guid organizerUserId);
        Task<IEnumerable<Event>> SearchEventsAsync(string? searchTerm, string? category, DateTime? startDate, DateTime? endDate);
        Task<Event> CreateEventAsync(Event eventItem);
        Task<Event> UpdateEventAsync(Event eventItem);
        Task<bool> DeleteEventAsync(Guid eventId);
        Task<bool> EventExistsAsync(Guid eventId);
    }
}
