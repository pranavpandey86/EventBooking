using System.ComponentModel.DataAnnotations;

namespace EventManagement.API.DTOs
{
    public class EventDto
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string? Location { get; set; }
        public int MaxCapacity { get; set; }
        public decimal TicketPrice { get; set; }
        public Guid OrganizerUserId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateEventDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        [Required]
        [Range(1, 100000)]
        public int MaxCapacity { get; set; }

        [Required]
        [Range(0.01, 10000)]
        public decimal TicketPrice { get; set; }

        [Required]
        public Guid OrganizerUserId { get; set; }
    }

    public class UpdateEventDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        [Required]
        [Range(1, 100000)]
        public int MaxCapacity { get; set; }

        [Required]
        [Range(0.01, 10000)]
        public decimal TicketPrice { get; set; }
    }

    public class EventSearchDto
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
