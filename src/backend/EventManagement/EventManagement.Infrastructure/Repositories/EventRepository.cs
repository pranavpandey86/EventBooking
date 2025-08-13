using EventManagement.Core.Entities;
using EventManagement.Core.Interfaces;
using EventManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly EventDbContext _context;

        public EventRepository(EventDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _context.Events
                .Where(e => e.IsActive)
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<Event?> GetEventByIdAsync(Guid eventId)
        {
            return await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.IsActive);
        }

        public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(Guid organizerUserId)
        {
            return await _context.Events
                .Where(e => e.OrganizerUserId == organizerUserId && e.IsActive)
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> SearchEventsAsync(string? searchTerm, string? category, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Events.Where(e => e.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(e => e.Name.Contains(searchTerm) || 
                                       (e.Description != null && e.Description.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            if (startDate.HasValue)
            {
                query = query.Where(e => e.EventDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.EventDate <= endDate.Value);
            }

            return await query
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<Event> CreateEventAsync(Event eventItem)
        {
            eventItem.EventId = Guid.NewGuid();
            eventItem.CreatedAt = DateTime.UtcNow;
            eventItem.UpdatedAt = DateTime.UtcNow;

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();
            return eventItem;
        }

        public async Task<Event> UpdateEventAsync(Event eventItem)
        {
            eventItem.UpdatedAt = DateTime.UtcNow;
            _context.Events.Update(eventItem);
            await _context.SaveChangesAsync();
            return eventItem;
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
                return false;

            // Soft delete
            eventItem.IsActive = false;
            eventItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EventExistsAsync(Guid eventId)
        {
            return await _context.Events
                .AnyAsync(e => e.EventId == eventId && e.IsActive);
        }
    }
}
