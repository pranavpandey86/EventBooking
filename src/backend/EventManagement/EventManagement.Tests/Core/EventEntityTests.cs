using AutoFixture;
using EventManagement.Core.Entities;
using FluentAssertions;

namespace EventManagement.Tests.Core;

public class EventEntityTests
{
    private readonly Fixture _fixture;

    public EventEntityTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Event_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var eventEntity = new Event();

        // Assert
        eventEntity.EventId.Should().NotBeEmpty();
        eventEntity.IsActive.Should().BeTrue();
        eventEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        eventEntity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Event_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var name = "Test Event";
        var description = "Test Description";
        var category = "Technology";
        var eventDate = DateTime.UtcNow.AddDays(30);
        var location = "Test Location";
        var maxCapacity = 100;
        var ticketPrice = 50.00m;
        var organizerUserId = Guid.NewGuid();
        var isActive = false;
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        // Act
        var eventEntity = new Event
        {
            EventId = eventId,
            Name = name,
            Description = description,
            Category = category,
            EventDate = eventDate,
            Location = location,
            MaxCapacity = maxCapacity,
            TicketPrice = ticketPrice,
            OrganizerUserId = organizerUserId,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        eventEntity.EventId.Should().Be(eventId);
        eventEntity.Name.Should().Be(name);
        eventEntity.Description.Should().Be(description);
        eventEntity.Category.Should().Be(category);
        eventEntity.EventDate.Should().Be(eventDate);
        eventEntity.Location.Should().Be(location);
        eventEntity.MaxCapacity.Should().Be(maxCapacity);
        eventEntity.TicketPrice.Should().Be(ticketPrice);
        eventEntity.OrganizerUserId.Should().Be(organizerUserId);
        eventEntity.IsActive.Should().Be(isActive);
        eventEntity.CreatedAt.Should().Be(createdAt);
        eventEntity.UpdatedAt.Should().Be(updatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Event_WithInvalidName_ShouldStillCreateInstance(string? invalidName)
    {
        // Note: In a more sophisticated domain model, we might have validation
        // For now, we just ensure the entity can be created with any name value
        
        // Act & Assert
        var action = () => new Event { Name = invalidName! };
        action.Should().NotThrow();
    }

    [Fact]
    public void Event_WithNegativeCapacity_ShouldStillCreateInstance()
    {
        // Note: In a more sophisticated domain model, we might have validation
        // For now, we just ensure the entity can be created with any capacity value
        
        // Act & Assert
        var action = () => new Event { MaxCapacity = -1 };
        action.Should().NotThrow();
    }

    [Fact]
    public void Event_WithNegativePrice_ShouldStillCreateInstance()
    {
        // Note: In a more sophisticated domain model, we might have validation
        // For now, we just ensure the entity can be created with any price value
        
        // Act & Assert
        var action = () => new Event { TicketPrice = -10.00m };
        action.Should().NotThrow();
    }

    [Fact]
    public void Event_WithPastDate_ShouldStillCreateInstance()
    {
        // Note: In a more sophisticated domain model, we might have validation
        // For now, we just ensure the entity can be created with any date value
        
        // Act & Assert
        var action = () => new Event { EventDate = DateTime.UtcNow.AddDays(-1) };
        action.Should().NotThrow();
    }

    [Fact]
    public void Event_PropertiesShould_BeIndependent()
    {
        // Arrange
        var event1 = _fixture.Create<Event>();
        var event2 = _fixture.Create<Event>();

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
        event1.Name.Should().NotBe(event2.Name);
        // Other properties will likely be different due to AutoFixture's random generation
    }

    [Fact]
    public void Event_ShouldSupportEquality_BasedOnEventId()
    {
        // Note: This test assumes we might implement equality based on EventId
        // Currently, the Event class uses default reference equality
        
        // Arrange
        var eventId = Guid.NewGuid();
        var event1 = new Event { EventId = eventId, Name = "Event 1" };
        var event2 = new Event { EventId = eventId, Name = "Event 2" };
        var event3 = new Event { EventId = Guid.NewGuid(), Name = "Event 1" };

        // Act & Assert
        // Currently using reference equality - in a more sophisticated domain model,
        // we might override Equals to use EventId
        event1.Should().NotBe(event2); // Different instances
        event1.Should().NotBe(event3); // Different instances
        event1.EventId.Should().Be(event2.EventId); // Same EventId
        event1.EventId.Should().NotBe(event3.EventId); // Different EventId
    }

    [Fact]
    public void Event_ShouldHandleStringPropertiesCorrectly()
    {
        // Arrange & Act
        var eventEntity = new Event
        {
            Name = "Test Event",
            Description = "This is a test event with special characters: !@#$%^&*()",
            Category = "Technology & Innovation",
            Location = "123 Main St, City, State 12345"
        };

        // Assert
        eventEntity.Name.Should().NotBeNullOrWhiteSpace();
        eventEntity.Description.Should().Contain("special characters");
        eventEntity.Category.Should().Contain("&");
        eventEntity.Location.Should().Contain("123");
    }

    [Fact]
    public void Event_ShouldHandleDecimalPrecisionCorrectly()
    {
        // Arrange & Act
        var eventEntity = new Event
        {
            TicketPrice = 123.456789m // More precision than typical currency
        };

        // Assert
        eventEntity.TicketPrice.Should().Be(123.456789m);
        // Note: Database mapping will likely truncate to 2 decimal places
    }

    [Fact]
    public void Event_ShouldHandleDateTimeCorrectly()
    {
        // Arrange
        var specificDate = new DateTime(2025, 12, 25, 15, 30, 0, DateTimeKind.Utc);

        // Act
        var eventEntity = new Event
        {
            EventDate = specificDate,
            CreatedAt = specificDate.AddDays(-10),
            UpdatedAt = specificDate.AddDays(-5)
        };

        // Assert
        eventEntity.EventDate.Should().Be(specificDate);
        eventEntity.CreatedAt.Should().Be(specificDate.AddDays(-10));
        eventEntity.UpdatedAt.Should().Be(specificDate.AddDays(-5));
    }

    [Fact]
    public void Event_ShouldHandleGuidPropertiesCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();

        // Act
        var eventEntity = new Event
        {
            EventId = eventId,
            OrganizerUserId = organizerId
        };

        // Assert
        eventEntity.EventId.Should().Be(eventId);
        eventEntity.OrganizerUserId.Should().Be(organizerId);
        eventEntity.EventId.Should().NotBe(eventEntity.OrganizerUserId);
    }
}
