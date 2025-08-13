using AutoFixture;
using EventManagement.API.DTOs;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.Tests.DTOs;

public class EventDtoTests
{

    [Fact]
    public void CreateEventDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateEventDto_WithInvalidName_ShouldFailValidation(string? invalidName)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = invalidName!,
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(CreateEventDto.Name)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateEventDto_WithInvalidCategory_ShouldFailValidation(string? invalidCategory)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = invalidCategory!,
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(CreateEventDto.Category)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreateEventDto_WithInvalidMaxCapacity_ShouldFailValidation(int invalidCapacity)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = invalidCapacity,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(CreateEventDto.MaxCapacity)));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1.00)]
    [InlineData(-100.00)]
    public void CreateEventDto_WithNegativeTicketPrice_ShouldFailValidation(decimal invalidPrice)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = 100,
            TicketPrice = invalidPrice,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(CreateEventDto.TicketPrice)));
    }

    [Fact]
    public void CreateEventDto_WithPastEventDate_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(-1), // Past date
            Location = "Valid location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(CreateEventDto.EventDate)));
    }

    [Fact]
    public void CreateEventDto_WithEmptyOrganizerUserId_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.Empty
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(CreateEventDto.OrganizerUserId)));
    }

    [Fact]
    public void UpdateEventDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateEventDto
        {
            Name = "Updated Event Name",
            Description = "Updated description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Updated location",
            MaxCapacity = 200,
            TicketPrice = 75.00m
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void EventDto_ShouldHaveAllProperties()
    {
        // Arrange & Act
        var dto = new EventDto
        {
            EventId = Guid.NewGuid(),
            Name = "Test Event",
            Description = "Test Description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Test Location",
            MaxCapacity = 100,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        dto.EventId.Should().NotBeEmpty();
        dto.Name.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.Category.Should().NotBeNullOrWhiteSpace();
        dto.Location.Should().NotBeNullOrWhiteSpace();
        dto.MaxCapacity.Should().BeGreaterThan(0);
        dto.TicketPrice.Should().BeGreaterOrEqualTo(0);
        dto.OrganizerUserId.Should().NotBeEmpty();
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().NotBe(default);
        dto.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void EventSearchDto_ShouldAllowNullableProperties()
    {
        // Arrange & Act
        var dto = new EventSearchDto
        {
            SearchTerm = null,
            Category = null,
            StartDate = null,
            EndDate = null
        };

        // Assert
        dto.SearchTerm.Should().BeNull();
        dto.Category.Should().BeNull();
        dto.StartDate.Should().BeNull();
        dto.EndDate.Should().BeNull();

        // Should pass validation
        var validationResults = ValidateDto(dto);
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void EventSearchDto_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var dto = new EventSearchDto
        {
            SearchTerm = "technology",
            Category = "Technology",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void EventSearchDto_WithEndDateBeforeStartDate_ShouldStillPassValidation()
    {
        // Note: We might want to add custom validation for date range logic
        // For now, individual date properties are valid
        
        // Arrange
        var dto = new EventSearchDto
        {
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow // End before start
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty(); // Currently no cross-field validation
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(999999.99)]
    public void CreateEventDto_WithValidTicketPrice_ShouldPassValidation(decimal validPrice)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = 100,
            TicketPrice = validPrice,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000000)]
    public void CreateEventDto_WithValidMaxCapacity_ShouldPassValidation(int validCapacity)
    {
        // Arrange
        var dto = new CreateEventDto
        {
            Name = "Valid Event Name",
            Description = "Valid description",
            Category = "Technology",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Valid location",
            MaxCapacity = validCapacity,
            TicketPrice = 50.00m,
            OrganizerUserId = Guid.NewGuid()
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateDto(object dto)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        Validator.TryValidateObject(dto, validationContext, validationResults, true);
        return validationResults;
    }
}
