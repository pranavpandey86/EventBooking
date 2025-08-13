using EventManagement.API.DTOs;
using EventManagement.API.Interfaces;
using EventManagement.API.Services;
using EventManagement.Core.Interfaces;
using EventManagement.Core.Services;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Tests.Integration;

public class ServiceLayerIntegrationTests
{
    private readonly EventDbContext _context;
    private readonly IEventRepository _eventRepository;
    private readonly IEventService _eventService;
    private readonly IEventDtoService _eventDtoService;
    private readonly Mock<ILogger<EventService>> _eventServiceLoggerMock;
    private readonly Mock<ILogger<EventDtoService>> _eventDtoServiceLoggerMock;

    public ServiceLayerIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventDbContext(options);
        _eventRepository = new EventRepository(_context);
        
        // Setup loggers
        _eventServiceLoggerMock = new Mock<ILogger<EventService>>();
        _eventDtoServiceLoggerMock = new Mock<ILogger<EventDtoService>>();
        
        // Setup service layer
        _eventService = new EventService(_eventRepository, _eventServiceLoggerMock.Object);
        _eventDtoService = new EventDtoService(_eventService, _eventDtoServiceLoggerMock.Object);
    }

    [Fact]
    public async Task CreateEvent_ThroughServiceLayer_ShouldPersistToDatabase()
    {
        // Arrange
        var createDto = new CreateEventDto
        {
            Name = "Tech Conference 2024",
            Description = "Annual technology conference",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Convention Center",
            MaxCapacity = 500,
            TicketPrice = 99.99m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var result = await _eventDtoService.CreateEventAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(createDto.Name);
        result.Category.Should().Be(createDto.Category);
        result.TicketPrice.Should().Be(createDto.TicketPrice);
        result.IsActive.Should().BeTrue();
        result.EventId.Should().NotBeEmpty();

        // Verify persistence
        var eventFromDb = await _context.Events.FindAsync(result.EventId);
        eventFromDb.Should().NotBeNull();
        eventFromDb!.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task GetAllEvents_ThroughServiceLayer_ShouldReturnAllEvents()
    {
        // Arrange - Create some test events directly in database
        var events = new[]
        {
            new EventManagement.Core.Entities.Event
            {
                EventId = Guid.NewGuid(),
                Name = "Event 1",
                Category = "Technology",
                EventDate = DateTime.UtcNow.AddDays(10),
                Location = "Location 1",
                MaxCapacity = 100,
                TicketPrice = 50.00m,
                OrganizerUserId = Guid.NewGuid(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new EventManagement.Core.Entities.Event
            {
                EventId = Guid.NewGuid(),
                Name = "Event 2",
                Category = "Business",
                EventDate = DateTime.UtcNow.AddDays(20),
                Location = "Location 2",
                MaxCapacity = 200,
                TicketPrice = 75.00m,
                OrganizerUserId = Guid.NewGuid(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventDtoService.GetAllEventsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.IsActive);
        
        var resultList = result.ToList();
        resultList.Should().Contain(e => e.Name == "Event 1");
        resultList.Should().Contain(e => e.Name == "Event 2");
    }

    [Fact]
    public async Task UpdateEvent_ThroughServiceLayer_ShouldUpdateInDatabase()
    {
        // Arrange - Create an event first
        var originalEvent = new EventManagement.Core.Entities.Event
        {
            EventId = Guid.NewGuid(),
            Name = "Original Event",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Original Location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(originalEvent);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateEventDto
        {
            Name = "Updated Event",
            Description = "Updated description",
            Category = "Business",
            EventDate = DateTime.UtcNow.AddDays(45),
            Location = "Updated Location",
            MaxCapacity = 150,
            TicketPrice = 75.00m
        };

        // Act
        var result = await _eventDtoService.UpdateEventAsync(originalEvent.EventId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(updateDto.Name);
        result.Category.Should().Be(updateDto.Category);
        result.MaxCapacity.Should().Be(updateDto.MaxCapacity);
        result.EventId.Should().Be(originalEvent.EventId);

        // Verify persistence
        var eventFromDb = await _context.Events.FindAsync(originalEvent.EventId);
        eventFromDb.Should().NotBeNull();
        eventFromDb!.Name.Should().Be(updateDto.Name);
        eventFromDb.Category.Should().Be(updateDto.Category);
    }

    [Fact]
    public async Task DeleteEvent_ThroughServiceLayer_ShouldRemoveFromDatabase()
    {
        // Arrange
        var eventToDelete = new EventManagement.Core.Entities.Event
        {
            EventId = Guid.NewGuid(),
            Name = "Event to Delete",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventToDelete);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventDtoService.DeleteEventAsync(eventToDelete.EventId);

        // Assert
        result.Should().BeTrue();

        // Verify soft deletion (event should still exist but be inactive)
        var eventFromDb = await _context.Events.FindAsync(eventToDelete.EventId);
        eventFromDb.Should().NotBeNull();
        eventFromDb!.IsActive.Should().BeFalse();
        eventFromDb.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task BusinessValidation_ShouldPreventInvalidEvents()
    {
        // Arrange - Try to create event with past date
        var invalidCreateDto = new CreateEventDto
        {
            Name = "Invalid Event",
            Description = "Event with past date",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(-1), // Past date
            Location = "Location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act & Assert
        var action = async () => await _eventDtoService.CreateEventAsync(invalidCreateDto);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Event date must be in the future*");
    }

}
