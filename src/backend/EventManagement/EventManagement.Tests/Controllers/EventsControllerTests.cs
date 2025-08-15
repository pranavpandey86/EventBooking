using AutoFixture;
using EventManagement.API.Controllers;
using EventManagement.API.DTOs;
using EventManagement.API.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Tests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventRepository> _mockRepository;
    private readonly Mock<ILogger<EventsController>> _mockLogger;
    private readonly EventsController _controller;
    private readonly Fixture _fixture;

    public EventsControllerTests()
    {
        _mockRepository = new Mock<IEventRepository>();
        _mockLogger = new Mock<ILogger<EventsController>>();
        _controller = new EventsController(_mockRepository.Object, _mockLogger.Object);
        _fixture = new Fixture();

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetEvents_ShouldReturnOkWithEvents()
    {
        // Arrange
        var events = _fixture.CreateMany<Event>(3).ToList();
        _mockRepository.Setup(x => x.GetAllEventsAsync())
            .ReturnsAsync(events);

        // Act
        var result = await _controller.GetEvents();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvents = okResult.Value.Should().BeAssignableTo<IEnumerable<EventDto>>().Subject;
        returnedEvents.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetEvents_WhenRepositoryThrows_ShouldReturnInternalServerError()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetAllEventsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetEvents();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetEvent_WithValidId_ShouldReturnOkWithEvent()
    {
        // Arrange
        var eventEntity = _fixture.Create<Event>();
        _mockRepository.Setup(x => x.GetEventByIdAsync(eventEntity.EventId))
            .ReturnsAsync(eventEntity);

        // Act
        var result = await _controller.GetEvent(eventEntity.EventId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvent = okResult.Value.Should().BeOfType<EventDto>().Subject;
        returnedEvent.EventId.Should().Be(eventEntity.EventId);
        returnedEvent.Name.Should().Be(eventEntity.Name);
    }

    [Fact]
    public async Task GetEvent_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetEventByIdAsync(invalidId))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _controller.GetEvent(invalidId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Event with ID {invalidId} not found");
    }

    [Fact]
    public async Task GetEventsByOrganizer_ShouldReturnOkWithEvents()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var events = _fixture.Build<Event>()
            .With(x => x.OrganizerUserId, organizerId)
            .CreateMany(2).ToList();

        _mockRepository.Setup(x => x.GetEventsByOrganizerAsync(organizerId))
            .ReturnsAsync(events);

        // Act
        var result = await _controller.GetEventsByOrganizer(organizerId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvents = okResult.Value.Should().BeAssignableTo<IEnumerable<EventDto>>().Subject;
        returnedEvents.Should().HaveCount(2);
        returnedEvents.All(e => e.OrganizerUserId == organizerId).Should().BeTrue();
    }

    [Fact]
    public async Task SearchEvents_ShouldReturnOkWithFilteredEvents()
    {
        // Arrange
        var searchDto = new EventSearchDto
        {
            Category = "Technology",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var events = _fixture.Build<Event>()
            .With(x => x.Category, "Technology")
            .CreateMany(2).ToList();

        _mockRepository.Setup(x => x.SearchEventsAsync(
                searchDto.SearchTerm,
                searchDto.Category,
                searchDto.StartDate,
                searchDto.EndDate))
            .ReturnsAsync(events);

        // Act
        var result = await _controller.SearchEvents(searchDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvents = okResult.Value.Should().BeAssignableTo<IEnumerable<EventDto>>().Subject;
        returnedEvents.Should().HaveCount(2);
        returnedEvents.All(e => e.Category == "Technology").Should().BeTrue();
    }

    [Fact]
    public async Task CreateEvent_WithValidDto_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var createDto = _fixture.Create<CreateEventDto>();
        var createdEvent = _fixture.Build<Event>()
            .With(x => x.Name, createDto.Name)
            .With(x => x.Description, createDto.Description)
            .With(x => x.Category, createDto.Category)
            .Create();

        _mockRepository.Setup(x => x.CreateEventAsync(It.IsAny<Event>()))
            .ReturnsAsync(createdEvent);

        // Act
        var result = await _controller.CreateEvent(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(EventsController.GetEvent));
        var returnedEvent = createdResult.Value.Should().BeOfType<EventDto>().Subject;
        returnedEvent.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task CreateEvent_WhenRepositoryThrows_ShouldReturnInternalServerError()
    {
        // Arrange
        var createDto = _fixture.Create<CreateEventDto>();
        _mockRepository.Setup(x => x.CreateEventAsync(It.IsAny<Event>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateEvent(createDto);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateEvent_WithValidData_ShouldReturnOkWithUpdatedEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = _fixture.Create<UpdateEventDto>();
        var existingEvent = _fixture.Build<Event>()
            .With(x => x.EventId, eventId)
            .Create();

        var updatedEvent = _fixture.Build<Event>()
            .With(x => x.EventId, eventId)
            .With(x => x.Name, updateDto.Name)
            .With(x => x.Description, updateDto.Description)
            .Create();

        _mockRepository.Setup(x => x.GetEventByIdAsync(eventId))
            .ReturnsAsync(existingEvent);
        _mockRepository.Setup(x => x.UpdateEventAsync(It.IsAny<Event>()))
            .ReturnsAsync(updatedEvent);

        // Act
        var result = await _controller.UpdateEvent(eventId, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvent = okResult.Value.Should().BeOfType<EventDto>().Subject;
        returnedEvent.Name.Should().Be(updateDto.Name);
        returnedEvent.Description.Should().Be(updateDto.Description);
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var updateDto = _fixture.Create<UpdateEventDto>();

        _mockRepository.Setup(x => x.GetEventByIdAsync(invalidId))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _controller.UpdateEvent(invalidId, updateDto);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Event with ID {invalidId} not found");
    }

    [Fact]
    public async Task DeleteEvent_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mockRepository.Setup(x => x.DeleteEventAsync(eventId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteEvent_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        _mockRepository.Setup(x => x.DeleteEventAsync(invalidId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteEvent(invalidId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Event with ID {invalidId} not found");
    }

    [Fact]
    public async Task DeleteEvent_WhenRepositoryThrows_ShouldReturnInternalServerError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mockRepository.Setup(x => x.DeleteEventAsync(eventId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }
}
