using System.Text.Json.Serialization;

namespace EventManagement.API.Models;

public abstract class BaseEventMessage
{
    [JsonPropertyName("eventId")]
    public required Guid EventId { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
}

public class EventCreatedMessage : BaseEventMessage
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public required string Category { get; set; }
    
    [JsonPropertyName("eventDate")]
    public required DateTime EventDate { get; set; }
    
    [JsonPropertyName("location")]
    public required string Location { get; set; }
    
    [JsonPropertyName("maxCapacity")]
    public required int MaxCapacity { get; set; }
    
    [JsonPropertyName("ticketPrice")]
    public required decimal TicketPrice { get; set; }
    
    [JsonPropertyName("organizerUserId")]
    public required Guid OrganizerUserId { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
    
    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; set; }
}

public class EventUpdatedMessage : BaseEventMessage
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public required string Category { get; set; }
    
    [JsonPropertyName("eventDate")]
    public required DateTime EventDate { get; set; }
    
    [JsonPropertyName("location")]
    public required string Location { get; set; }
    
    [JsonPropertyName("maxCapacity")]
    public required int MaxCapacity { get; set; }
    
    [JsonPropertyName("ticketPrice")]
    public required decimal TicketPrice { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public required DateTime UpdatedAt { get; set; }
}

public class EventDeletedMessage : BaseEventMessage
{
    [JsonPropertyName("deletedAt")]
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}