using EventManagement.API.Interfaces;
using EventManagement.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.API.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<DebugController> _logger;

        public DebugController(IEventPublisher eventPublisher, ILogger<DebugController> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        [HttpPost("test-kafka")]
        public async Task<IActionResult> TestKafka()
        {
            try
            {
                _logger.LogInformation("Testing Kafka publisher directly");
                
                var testMessage = new EventCreatedMessage
                {
                    EventId = Guid.NewGuid(),
                    Name = "Direct Debug Test Event",
                    Description = "Testing Kafka publisher directly from debug controller",
                    Category = "Debug",
                    EventDate = DateTime.UtcNow.AddDays(7),
                    Location = "Debug Location",
                    MaxCapacity = 100,
                    TicketPrice = 25.00m,
                    OrganizerUserId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
                    CreatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("About to call PublishEventCreatedAsync for event {EventId}", testMessage.EventId);
                var success = await _eventPublisher.PublishEventCreatedAsync(testMessage);
                _logger.LogInformation("PublishEventCreatedAsync completed with result: {Success}", success);

                return Ok(new { 
                    success = success, 
                    eventId = testMessage.EventId,
                    message = success ? "Kafka message published successfully" : "Failed to publish Kafka message"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Kafka publisher");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}