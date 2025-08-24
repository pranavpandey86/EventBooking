namespace EventSearch.Core.Entities;

public class SearchableEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int AvailableTickets { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Search-specific fields
    public decimal Popularity { get; set; } // For boosting search results
    public int ViewCount { get; set; }
    public int BookingCount { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
}