using EventManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Infrastructure.Data
{
    public class EventDbContext : DbContext
    {
        public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Event entity
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventId);
                
                entity.Property(e => e.EventId)
                    .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Description)
                    .HasMaxLength(2000);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EventDate)
                    .IsRequired();

                entity.Property(e => e.Location)
                    .HasMaxLength(500);

                entity.Property(e => e.MaxCapacity)
                    .IsRequired();

                entity.Property(e => e.TicketPrice)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.OrganizerUserId)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(e => new { e.EventDate, e.Category })
                    .HasDatabaseName("IX_Events_Date_Category");

                entity.HasIndex(e => e.OrganizerUserId)
                    .HasDatabaseName("IX_Events_Organizer");

                entity.HasIndex(e => new { e.IsActive, e.EventDate })
                    .HasDatabaseName("IX_Events_Active");
            });
        }
    }
}
