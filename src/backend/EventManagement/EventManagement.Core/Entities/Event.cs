using System.ComponentModel.DataAnnotations;

namespace EventManagement.Core.Entities
{
    public class Event
    {
        public Guid EventId { get; set; }
        
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
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
