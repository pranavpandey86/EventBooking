using AutoFixture;
using EventManagement.Core.Entities;
using EventManagement.Core.Interfaces;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Tests.Infrastructure;

public class EventRepositoryTests : IDisposable
{
    private readonly EventDbContext _context;
    private readonly IEventRepository _repository;
    private readonly Fixture _fixture;
    private bool _disposed = false;

    public EventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventDbContext(options);
        _repository = new EventRepository(_context);
        _fixture = new Fixture();
        
        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetAllEventsAsync_ShouldReturnAllEvents()
    {
        // Arrange
        var events = _fixture.CreateMany<Event>(3).ToList();
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllEventsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(events);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ShouldReturnEvent()
    {
        // Arrange
        var eventEntity = _fixture.Create<Event>();
        await _context.Events.AddAsync(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventByIdAsync(eventEntity.EventId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(eventEntity);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.GetEventByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateEventAsync_ShouldAddEventToDatabase()
    {
        // Arrange
        var eventEntity = _fixture.Create<Event>();

        // Act
        var result = await _repository.CreateEventAsync(eventEntity);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be(eventEntity.EventId);
        
        var savedEvent = await _context.Events.FindAsync(eventEntity.EventId);
        savedEvent.Should().NotBeNull();
        savedEvent.Should().BeEquivalentTo(eventEntity);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldModifyExistingEvent()
    {
        // Arrange
        var eventEntity = _fixture.Create<Event>();
        await _context.Events.AddAsync(eventEntity);
        await _context.SaveChangesAsync();

        var updatedName = "Updated Event Name";
        eventEntity.Name = updatedName;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        // Act
        var result = await _repository.UpdateEventAsync(eventEntity);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(updatedName);
        
        var savedEvent = await _context.Events.FindAsync(eventEntity.EventId);
        savedEvent!.Name.Should().Be(updatedName);
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldRemoveEventFromDatabase()
    {
        // Arrange
        var eventEntity = _fixture.Create<Event>();
        await _context.Events.AddAsync(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteEventAsync(eventEntity.EventId);

        // Assert
        result.Should().BeTrue();
        var deletedEvent = await _context.Events.FindAsync(eventEntity.EventId);
        deletedEvent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEventAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteEventAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventsByOrganizerAsync_ShouldReturnEventsForOrganizer()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var organizerEvents = _fixture.Build<Event>()
            .With(x => x.OrganizerUserId, organizerId)
            .CreateMany(2).ToList();
        var otherEvents = _fixture.CreateMany<Event>(2).ToList();

        await _context.Events.AddRangeAsync(organizerEvents);
        await _context.Events.AddRangeAsync(otherEvents);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventsByOrganizerAsync(organizerId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(organizerEvents);
        result.All(e => e.OrganizerUserId == organizerId).Should().BeTrue();
    }

    [Fact]
    public async Task SearchEventsAsync_ByCategoryAndDateRange_ShouldReturnFilteredEvents()
    {
        // Arrange
        var category = "Technology";
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(30);

        var matchingEvents = _fixture.Build<Event>()
            .With(x => x.Category, category)
            .With(x => x.EventDate, startDate.AddDays(5))
            .With(x => x.IsActive, true)
            .CreateMany(2).ToList();

        var nonMatchingEvents = _fixture.Build<Event>()
            .With(x => x.Category, "Music")
            .With(x => x.EventDate, startDate.AddDays(5))
            .With(x => x.IsActive, true)
            .CreateMany(2).ToList();

        await _context.Events.AddRangeAsync(matchingEvents);
        await _context.Events.AddRangeAsync(nonMatchingEvents);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchEventsAsync(null, category, startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(matchingEvents);
        result.All(e => e.Category == category).Should().BeTrue();
    }

    [Fact]
    public async Task EventExistsAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = _fixture.Create<Event>();
        await _context.Events.AddAsync(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EventExistsAsync(eventEntity.EventId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EventExistsAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _repository.EventExistsAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
