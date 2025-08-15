using AutoFixture;
using EventManagement.API.DTOs;
using EventManagement.Core.Entities;
using EventManagement.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Tests.Integration;

public class EventsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Fixture _fixture;

    public EventsApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EventDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<EventDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
        _fixture = new Fixture();

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetEvents_ShouldReturnEmptyList_WhenNoEventsExist()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnCreated_WithValidData()
    {
        // Arrange
        var createDto = new CreateEventDto
        {
            Name = "Test Event",
            Description = "A test event",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/events", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        createdEvent.Should().NotBeNull();
        createdEvent!.Name.Should().Be(createDto.Name);
        createdEvent.Category.Should().Be(createDto.Category);
        createdEvent.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEvent_ShouldReturnEvent_WhenEventExists()
    {
        // Arrange - First create an event
        var createDto = _fixture.Build<CreateEventDto>()
            .With(x => x.EventDate, DateTime.UtcNow.AddDays(30))
            .Create();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createDto);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();

        // Act
        var response = await _client.GetAsync($"/api/v1/events/{createdEvent!.EventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        retrievedEvent.Should().NotBeNull();
        retrievedEvent!.EventId.Should().Be(createdEvent.EventId);
        retrievedEvent.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task GetEvent_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/events/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEvent_ShouldReturnOk_WithValidData()
    {
        // Arrange - First create an event
        var createDto = _fixture.Build<CreateEventDto>()
            .With(x => x.EventDate, DateTime.UtcNow.AddDays(30))
            .Create();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createDto);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();

        var updateDto = new UpdateEventDto
        {
            Name = "Updated Event Name",
            Description = createdEvent!.Description,
            Category = createdEvent.Category,
            EventDate = createdEvent.EventDate,
            Location = createdEvent.Location,
            MaxCapacity = createdEvent.MaxCapacity,
            TicketPrice = createdEvent.TicketPrice
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/events/{createdEvent.EventId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        updatedEvent.Should().NotBeNull();
        updatedEvent!.Name.Should().Be("Updated Event Name");
        updatedEvent.EventId.Should().Be(createdEvent.EventId);
    }

    [Fact]
    public async Task UpdateEvent_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateDto = _fixture.Create<UpdateEventDto>();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/events/{nonExistentId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_ShouldReturnNoContent_WhenEventExists()
    {
        // Arrange - First create an event
        var createDto = _fixture.Build<CreateEventDto>()
            .With(x => x.EventDate, DateTime.UtcNow.AddDays(30))
            .Create();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createDto);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/events/{createdEvent!.EventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the event is actually deleted
        var getResponse = await _client.GetAsync($"/api/v1/events/{createdEvent.EventId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_ShouldReturnNotFound_WhenEventDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/events/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchEvents_ShouldReturnFilteredResults_WhenCategoryProvided()
    {
        // Arrange - Create events with different categories
        var techEvent = new CreateEventDto
        {
            Name = "Tech Conference",
            Description = "Technology event",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Tech Center",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        var musicEvent = new CreateEventDto
        {
            Name = "Music Festival",
            Description = "Music event",
            Category = "Music",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Music Hall",
            MaxCapacity = 200,
            TicketPrice = 75.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        await _client.PostAsJsonAsync("/api/v1/events", techEvent);
        await _client.PostAsJsonAsync("/api/v1/events", musicEvent);

        // Act
        var response = await _client.GetAsync("/api/v1/events/search?category=Technology");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        events.Should().NotBeEmpty();
        events.Should().HaveCount(1);
        events![0].Category.Should().Be("Technology");
        events[0].Name.Should().Be("Tech Conference");
    }

    [Fact]
    public async Task GetEventsByOrganizer_ShouldReturnOrganizerEvents()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var otherOrganizerId = Guid.NewGuid();

        var organizerEvent = _fixture.Build<CreateEventDto>()
            .With(x => x.OrganizerUserId, organizerId)
            .With(x => x.EventDate, DateTime.UtcNow.AddDays(30))
            .Create();

        var otherEvent = _fixture.Build<CreateEventDto>()
            .With(x => x.OrganizerUserId, otherOrganizerId)
            .With(x => x.EventDate, DateTime.UtcNow.AddDays(30))
            .Create();

        await _client.PostAsJsonAsync("/api/v1/events", organizerEvent);
        await _client.PostAsJsonAsync("/api/v1/events", otherEvent);

        // Act
        var response = await _client.GetAsync($"/api/v1/events/organizer/{organizerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        events.Should().NotBeEmpty();
        events.Should().HaveCount(1);
        events![0].OrganizerUserId.Should().Be(organizerId);
    }

    [Fact]
    public async Task CreateEvent_ShouldReturnBadRequest_WithInvalidData()
    {
        // Arrange
        var invalidCreateDto = new CreateEventDto
        {
            Name = "", // Invalid: empty name
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(-1), // Invalid: past date
            Location = "Valid location",
            MaxCapacity = -1, // Invalid: negative capacity
            TicketPrice = -10.00m, // Invalid: negative price
            OrganizerUserId = Guid.Empty // Invalid: empty GUID
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/events", invalidCreateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
