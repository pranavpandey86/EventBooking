using AutoFixture;
using EventManagement.API.DTOs;
using EventManagement.API.Services;
using EventManagement.Core.Entities;
using EventManagement.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Tests.Services;

public class EventDtoServiceTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<ILogger<EventDtoService>> _loggerMock;
    private readonly EventDtoService _eventDtoService;
    private readonly Fixture _fixture;

    public EventDtoServiceTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _loggerMock = new Mock<ILogger<EventDtoService>>();
        _eventDtoService = new EventDtoService(_eventServiceMock.Object, _loggerMock.Object);
        _fixture = new Fixture();
        
        // Configure AutoFixture to generate valid events
        _fixture.Customize<Event>(composer => composer
            .With(e => e.EventDate, DateTime.UtcNow.AddDays(_fixture.Create<int>() % 365 + 1))
            .With(e => e.MaxCapacity, _fixture.Create<int>() % 10000 + 1)
            .With(e => e.TicketPrice, Math.Abs(_fixture.Create<decimal>()) % 1000)
            .With(e => e.IsActive, true));
    }

    [Fact]
    public async Task GetAllEventsAsync_ShouldReturnEventDtos()
    {
        // Arrange
        var events = _fixture.CreateMany<Event>(3).ToList();
        _eventServiceMock.Setup(s => s.GetAllEventsAsync())
            .ReturnsAsync(events);

        // Act
        var result = await _eventDtoService.GetAllEventsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().AllBeAssignableTo<EventDto>();
        
        var resultList = result.ToList();
        for (int i = 0; i < events.Count; i++)
        {
            resultList[i].EventId.Should().Be(events[i].EventId);
            resultList[i].Name.Should().Be(events[i].Name);
            resultList[i].Category.Should().Be(events[i].Category);
        }

        _eventServiceMock.Verify(s => s.GetAllEventsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ShouldReturnEventDto()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventEntity = _fixture.Build<Event>()
            .With(e => e.EventId, eventId)
            .Create();
        
        _eventServiceMock.Setup(s => s.GetEventByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _eventDtoService.GetEventByIdAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result!.EventId.Should().Be(eventId);
        result.Name.Should().Be(eventEntity.Name);
        result.Category.Should().Be(eventEntity.Category);
        
        _eventServiceMock.Verify(s => s.GetEventByIdAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventServiceMock.Setup(s => s.GetEventByIdAsync(eventId))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _eventDtoService.GetEventByIdAsync(eventId);

        // Assert
        result.Should().BeNull();
        _eventServiceMock.Verify(s => s.GetEventByIdAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_WithValidDto_ShouldReturnCreatedEventDto()
    {
        // Arrange
        var createDto = _fixture.Build<CreateEventDto>()
            .With(dto => dto.EventDate, DateTime.UtcNow.AddDays(30))
            .With(dto => dto.MaxCapacity, 100)
            .With(dto => dto.TicketPrice, 50.00m)
            .Create();

        var createdEvent = _fixture.Build<Event>()
            .With(e => e.Name, createDto.Name)
            .With(e => e.Category, createDto.Category)
            .With(e => e.EventDate, createDto.EventDate)
            .Create();

        _eventServiceMock.Setup(s => s.CreateEventAsync(It.IsAny<Event>()))
            .ReturnsAsync(createdEvent);

        // Act
        var result = await _eventDtoService.CreateEventAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(createDto.Name);
        result.Category.Should().Be(createDto.Category);
        result.EventDate.Should().Be(createDto.EventDate);
        
        _eventServiceMock.Verify(s => s.CreateEventAsync(It.Is<Event>(e =>
            e.Name == createDto.Name &&
            e.Category == createDto.Category &&
            e.EventDate == createDto.EventDate
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_WithValidData_ShouldReturnUpdatedEventDto()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = _fixture.Build<UpdateEventDto>()
            .With(dto => dto.EventDate, DateTime.UtcNow.AddDays(30))
            .Create();

        var updatedEvent = _fixture.Build<Event>()
            .With(e => e.EventId, eventId)
            .With(e => e.Name, updateDto.Name)
            .With(e => e.Category, updateDto.Category)
            .Create();

        _eventServiceMock.Setup(s => s.UpdateEventAsync(eventId, It.IsAny<Event>()))
            .ReturnsAsync(updatedEvent);

        // Act
        var result = await _eventDtoService.UpdateEventAsync(eventId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.EventId.Should().Be(eventId);
        result.Name.Should().Be(updateDto.Name);
        result.Category.Should().Be(updateDto.Category);
        
        _eventServiceMock.Verify(s => s.UpdateEventAsync(eventId, It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldCallServiceAndReturnResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventServiceMock.Setup(s => s.DeleteEventAsync(eventId))
            .ReturnsAsync(true);

        // Act
        var result = await _eventDtoService.DeleteEventAsync(eventId);

        // Assert
        result.Should().BeTrue();
        _eventServiceMock.Verify(s => s.DeleteEventAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task SearchEventsAsync_ShouldReturnFilteredEventDtos()
    {
        // Arrange
        var searchDto = new EventSearchDto
        {
            SearchTerm = "technology",
            Category = "Technology"
        };

        var events = _fixture.CreateMany<Event>(2).ToList();
        _eventServiceMock.Setup(s => s.SearchEventsAsync(
                searchDto.SearchTerm,
                searchDto.Category,
                searchDto.StartDate,
                searchDto.EndDate))
            .ReturnsAsync(events);

        // Act
        var result = await _eventDtoService.SearchEventsAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllBeAssignableTo<EventDto>();
        
        _eventServiceMock.Verify(s => s.SearchEventsAsync(
            searchDto.SearchTerm,
            searchDto.Category,
            searchDto.StartDate,
            searchDto.EndDate), Times.Once);
    }
}
