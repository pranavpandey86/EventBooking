# 🏗️ Cloud-Native Event Ticketing System - Architecture Document

## 📋 System Overview

**Architecture Pattern**: Microservices with Event-Driven Architecture  
**Domain**: Event Ticketing and Booking Platform  
**Deployment Model**: Cloud-Native on Microsoft Azure  
**Learning Focus**: Production-grade microservices patterns and Azure cloud services

---

## 🎯 Business Context

### **Domain Model:**
The system manages the complete lifecycle of event ticketing:
- **Events**: Concerts, conferences, sports, entertainment
- **Tickets**: Real-time inventory and reservations  
- **Payments**: Secure financial transactions
- **Notifications**: Multi-channel user communication
- **Users**: Event organizers and ticket buyers

### **Key Business Requirements:**
- ✅ **High Availability**: 99.9% uptime for ticket sales
- ✅ **Scalability**: Handle thousands of concurrent ticket purchases
- ✅ **Consistency**: Prevent overselling tickets (no double-booking)
- ✅ **Security**: PCI DSS compliance for payment processing
- ✅ **Performance**: Sub-second response times for availability checks
- ✅ **Auditability**: Complete transaction history and compliance

---

## 🏢 Service Architecture

### **Service Decomposition Strategy:**

We decompose the system by **business capabilities** following **Domain-Driven Design (DDD)** principles:

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Gateway                             │
│                   (Request Routing & Auth)                     │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
        ▼             ▼             ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│    Event     │ │    Ticket    │ │   Payment    │
│ Management   │ │  Inventory   │ │ Processing   │
│   Service    │ │   Service    │ │   Service    │
└──────────────┘ └──────────────┘ └──────────────┘
        │             │             │
        └─────────────┼─────────────┘
                      │
                      ▼
              ┌──────────────┐
              │ Notification │
              │   Service    │
              └──────────────┘
                      │
              ┌──────────────┐
              │ Azure Service│
              │     Bus      │
              └──────────────┘
```

---

## 🎯 Service Details

### **1. EventManagement Service** ✅ **IMPLEMENTED**

#### **Bounded Context:**
Complete ownership of event lifecycle management from creation to archival.

#### **Business Responsibilities:**
- Event creation, modification, and lifecycle management
- Event categorization and taxonomy
- Event search and discovery
- Event metadata and scheduling
- Organizer relationship management

#### **Technical Architecture:**
```
┌─────────────────────────────────────────────────────┐
│                EventManagement.API                 │ ← REST Controllers
├─────────────────────────────────────────────────────┤
│              EventManagement.Core                  │ ← Domain Layer
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │   Event     │  │IEventRepo   │  │EventService │ │
│  │  (Entity)   │  │(Interface)  │  │ (Domain)    │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────┤
│           EventManagement.Infrastructure           │ ← Data Layer
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │EventDbContext│ │EventRepository│ │Configuration│ │
│  │(EF Core)    │  │(Impl)       │  │             │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────┘
```

#### **Database Schema (Azure SQL Database):**
```sql
-- Events table with optimized indexes
CREATE TABLE Events (
    EventId          uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    Name            nvarchar(200) NOT NULL,
    Description     nvarchar(max),
    Category        nvarchar(100),
    EventDate       datetime2 NOT NULL,
    Location        nvarchar(500),
    MaxCapacity     int NOT NULL,
    TicketPrice     decimal(18,2),
    OrganizerUserId uniqueidentifier,
    IsActive        bit DEFAULT 1,
    CreatedAt       datetime2 DEFAULT GETUTCDATE(),
    UpdatedAt       datetime2 DEFAULT GETUTCDATE()
);

-- Performance indexes
CREATE INDEX IX_Events_EventDate ON Events(EventDate) WHERE IsActive = 1;
CREATE INDEX IX_Events_Category ON Events(Category) WHERE IsActive = 1;
CREATE INDEX IX_Events_Organizer ON Events(OrganizerUserId);
CREATE FULLTEXT INDEX ON Events(Name, Description);
```

#### **API Interface:**
```csharp
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    // GET /api/v1/events - Paginated listing with filters
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
        [FromQuery] int page = 1, 
        [FromQuery] int size = 20,
        [FromQuery] string category = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null);

    // GET /api/v1/events/{id} - Single event retrieval  
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(Guid id);

    // POST /api/v1/events - Event creation (Admin/Organizer)
    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventDto dto);

    // PUT /api/v1/events/{id} - Event updates (Admin/Organizer)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult<EventDto>> UpdateEvent(Guid id, [FromBody] UpdateEventDto dto);

    // DELETE /api/v1/events/{id} - Soft delete
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteEvent(Guid id);

    // GET /api/v1/events/search - Full-text search
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<EventDto>>> SearchEvents([FromQuery] EventSearchDto searchDto);

    // GET /api/v1/events/organizer/{organizerId} - Events by organizer
    [HttpGet("organizer/{organizerId}")]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEventsByOrganizer(Guid organizerId);
}
```

#### **Event Publishing:**
```csharp
// Domain events published to Service Bus
public class EventCreatedEvent : IDomainEvent
{
    public Guid EventId { get; set; }
    public string EventName { get; set; }
    public DateTime EventDate { get; set; }
    public int MaxCapacity { get; set; }
    public decimal TicketPrice { get; set; }
    public Guid OrganizerUserId { get; set; }
    public DateTime OccurredAt { get; set; }
}

// Published to: TicketInventory, NotificationService
```

---

### **2. TicketInventory Service** ⏳ **NEXT TO IMPLEMENT**

#### **Bounded Context:**
Real-time ticket availability management and reservation system with high-concurrency support.

#### **Business Responsibilities:**
- Real-time inventory tracking and updates
- Ticket reservation with time-based holds (15 minutes)
- Concurrency control to prevent overselling
- Seat/category management for complex venues
- Inventory alerts and threshold management

#### **Technical Architecture:**
```
┌─────────────────────────────────────────────────────┐
│               TicketInventory.API                   │ ← Controllers + SignalR Hubs
├─────────────────────────────────────────────────────┤
│              TicketInventory.Core                   │ ← Domain Layer
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │ Inventory   │  │IInventoryRepo│ │ReservationSvc│ │
│  │  (Entity)   │  │(Interface)  │  │ (Domain)    │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────┤
│           TicketInventory.Infrastructure            │ ← Data + Messaging
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │Cosmos DB    │  │EventBus     │  │Redis Cache  │ │
│  │Repository   │  │Publisher    │  │Service      │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────┘
```

#### **Database Design (Azure Cosmos DB):**
```json
// Container: Inventories (Partition Key: eventId)
{
  "id": "inventory-550e8400-e29b-41d4-a716-446655440000",
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventName": "Tech Conference 2025",
  "totalCapacity": 1000,
  "availableTickets": 750,
  "reservedTickets": 200,
  "soldTickets": 50,
  "priceCategories": [
    {
      "categoryId": "vip",
      "categoryName": "VIP",
      "price": 299.99,
      "totalSeats": 100,
      "availableSeats": 85,
      "reservedSeats": 10,
      "soldSeats": 5
    },
    {
      "categoryId": "premium",
      "categoryName": "Premium",
      "price": 199.99, 
      "totalSeats": 300,
      "availableSeats": 250,
      "reservedSeats": 40,
      "soldSeats": 10
    },
    {
      "categoryId": "general",
      "categoryName": "General Admission",
      "price": 99.99,
      "totalSeats": 600,
      "availableSeats": 415,
      "reservedSeats": 150,
      "soldSeats": 35
    }
  ],
  "reservations": [
    {
      "reservationId": "res-123e4567-e89b-12d3-a456-426614174000",
      "userId": "user-456e7890-e89b-12d3-a456-426614174001",
      "ticketCount": 2,
      "categoryId": "vip",
      "reservedAt": "2025-08-12T10:00:00Z",
      "expiresAt": "2025-08-12T10:15:00Z",
      "status": "Reserved", // Reserved, Confirmed, Expired, Cancelled
      "sessionId": "session-789",
      "amount": 599.98
    }
  ],
  "inventoryRules": {
    "maxTicketsPerUser": 10,
    "reservationTimeoutMinutes": 15,
    "lowInventoryThreshold": 50,
    "lastMinuteReservationMinutes": 30
  },
  "auditLog": [
    {
      "action": "TicketReserved",
      "userId": "user-456",
      "ticketCount": 2,
      "timestamp": "2025-08-12T10:00:00Z",
      "correlationId": "corr-123"
    }
  ],
  "_etag": "\"0000d400-0000-0000-0000-60b5cc6f0000\"",
  "_ts": 1723456789,
  "ttl": -1 // Never expires
}
```

#### **Concurrency Control Strategy:**
```csharp
// Optimistic Concurrency with ETags
public async Task<ReservationResult> ReserveTicketsAsync(
    Guid eventId, 
    string categoryId, 
    int ticketCount, 
    Guid userId)
{
    const int maxRetries = 5;
    const int baseDelayMs = 50;

    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            // Read current inventory with ETag
            var inventory = await _cosmosRepository.GetInventoryAsync(eventId);
            
            // Business logic validation
            if (!CanReserveTickets(inventory, categoryId, ticketCount))
                return ReservationResult.Failed("Insufficient tickets available");

            // Apply reservation
            var reservation = CreateReservation(userId, categoryId, ticketCount);
            inventory.Reservations.Add(reservation);
            inventory.UpdateAvailability(categoryId, -ticketCount);

            // Attempt atomic update with ETag check
            await _cosmosRepository.UpdateInventoryAsync(inventory);
            
            // Publish event
            await _eventBus.PublishAsync(new TicketReservedEvent
            {
                EventId = eventId,
                UserId = userId,
                ReservationId = reservation.ReservationId,
                TicketCount = ticketCount,
                CategoryId = categoryId,
                ExpiresAt = reservation.ExpiresAt
            });

            return ReservationResult.Success(reservation.ReservationId);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            // ETag mismatch - retry with exponential backoff
            var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt));
            await Task.Delay(delay);
        }
    }

    return ReservationResult.Failed("Unable to complete reservation due to high concurrency");
}
```

#### **API Interface:**
```csharp
[Route("api/v1/[controller]")]
public class InventoryController : ControllerBase
{
    // GET /api/v1/inventory/{eventId} - Current availability
    [HttpGet("{eventId}")]
    public async Task<ActionResult<InventoryDto>> GetInventory(Guid eventId);

    // POST /api/v1/inventory/{eventId}/reserve - Reserve tickets
    [HttpPost("{eventId}/reserve")]
    [Authorize]
    public async Task<ActionResult<ReservationDto>> ReserveTickets(
        Guid eventId, [FromBody] ReserveTicketsDto dto);

    // POST /api/v1/inventory/{eventId}/confirm - Confirm reservation
    [HttpPost("{eventId}/confirm")]
    [Authorize]
    public async Task<ActionResult> ConfirmReservation(
        Guid eventId, [FromBody] ConfirmReservationDto dto);

    // POST /api/v1/inventory/{eventId}/release - Release reservation
    [HttpPost("{eventId}/release")]
    [Authorize]
    public async Task<ActionResult> ReleaseReservation(
        Guid eventId, [FromBody] ReleaseReservationDto dto);

    // GET /api/v1/inventory/{eventId}/status - Real-time status via SignalR
    [HttpGet("{eventId}/status")]
    public async Task<ActionResult<InventoryStatusDto>> GetRealTimeStatus(Guid eventId);

    // PUT /api/v1/inventory/{eventId}/capacity - Update capacity (Admin)
    [HttpPut("{eventId}/capacity")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<ActionResult> UpdateCapacity(
        Guid eventId, [FromBody] UpdateCapacityDto dto);
}
```

#### **Real-Time Updates (SignalR):**
```csharp
[Authorize]
public class InventoryHub : Hub
{
    // Clients join event-specific groups for real-time updates
    public async Task JoinEventGroup(string eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
    }

    public async Task LeaveEventGroup(string eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
    }
}

// Background service pushes updates to connected clients
public class InventoryUpdateService : BackgroundService
{
    public async Task NotifyInventoryChanged(Guid eventId, InventoryStatusDto status)
    {
        await _hubContext.Clients.Group($"event-{eventId}")
            .SendAsync("InventoryUpdated", status);
    }
}
```

---

### **3. PaymentProcessing Service** ⏳ **PLANNED**

#### **Bounded Context:**
Secure financial transaction processing with distributed transaction coordination.

#### **Business Responsibilities:**
- Payment method validation and processing
- Multi-gateway support (Stripe, PayPal, bank transfers)
- Refund and chargeback management
- Fraud detection and prevention
- Financial reporting and reconciliation
- PCI DSS compliance (simulated)

#### **Technical Architecture:**
```
┌─────────────────────────────────────────────────────┐
│              PaymentProcessing.API                  │ ← Controllers + Webhooks
├─────────────────────────────────────────────────────┤
│             PaymentProcessing.Core                  │ ← Domain + Saga Orchestrator
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │   Payment   │  │  PaymentSaga│  │GatewayAdapter│ │
│  │  (Entity)   │  │Orchestrator │  │  (Interface)│ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────┤
│           PaymentProcessing.Infrastructure          │ ← Data + Gateways
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │SQL Database │  │StripeAdapter│  │EventBus     │ │
│  │(ACID Trans) │  │PayPalAdapter│  │Publisher    │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────┘
```

#### **Saga Pattern Implementation:**
```csharp
// Payment Processing Saga - Orchestrator Pattern
public class PaymentProcessingSaga : ISaga<PaymentSagaData>
{
    public Guid CorrelationId { get; set; }
    public PaymentSagaData Data { get; set; }

    // Saga Steps
    private readonly ISagaStep[] _steps = new ISagaStep[]
    {
        new ValidateInventoryStep(),    // Check ticket availability
        new ProcessPaymentStep(),       // Charge payment method
        new ConfirmInventoryStep(),     // Confirm ticket reservation
        new SendConfirmationStep(),     // Send success notification
        new CompleteTransactionStep()   // Finalize transaction
    };

    public async Task<SagaResult> ExecuteAsync()
    {
        foreach (var step in _steps)
        {
            try
            {
                var result = await step.ExecuteAsync(Data);
                if (!result.IsSuccess)
                {
                    // Execute compensation actions in reverse order
                    await ExecuteCompensationAsync(step);
                    return SagaResult.Failed(result.ErrorMessage);
                }
                
                Data.CompletedSteps.Add(step.GetType().Name);
            }
            catch (Exception ex)
            {
                await ExecuteCompensationAsync(step);
                return SagaResult.Failed($"Saga failed at step {step.GetType().Name}: {ex.Message}");
            }
        }

        return SagaResult.Success();
    }

    private async Task ExecuteCompensationAsync(ISagaStep failedStep)
    {
        // Execute compensation actions for completed steps in reverse order
        var completedSteps = _steps.Take(Array.IndexOf(_steps, failedStep)).Reverse();
        
        foreach (var step in completedSteps)
        {
            try
            {
                await step.CompensateAsync(Data);
            }
            catch (Exception ex)
            {
                // Log compensation failure - requires manual intervention
                _logger.LogCritical(ex, "Compensation failed for saga {SagaId} step {StepName}", 
                    CorrelationId, step.GetType().Name);
            }
        }
    }
}
```

#### **Database Schema (Azure SQL Database):**
```sql
-- Payments table with encryption for sensitive data
CREATE TABLE Payments (
    PaymentId               uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    EventId                 uniqueidentifier NOT NULL,
    UserId                  uniqueidentifier NOT NULL,
    ReservationId          uniqueidentifier NOT NULL,
    Amount                  decimal(18,2) NOT NULL,
    Currency               nvarchar(3) DEFAULT 'USD',
    PaymentMethodType      nvarchar(50) NOT NULL, -- CreditCard, PayPal, BankTransfer
    PaymentMethodDetails   nvarchar(max), -- Encrypted JSON
    GatewayTransactionId   nvarchar(255),
    GatewayReference       nvarchar(255),
    Status                 nvarchar(50) NOT NULL, -- Pending, Processing, Completed, Failed, Refunded
    FailureReason          nvarchar(max),
    ProcessedAt            datetime2,
    CreatedAt              datetime2 DEFAULT GETUTCDATE(),
    UpdatedAt              datetime2 DEFAULT GETUTCDATE(),
    
    -- Audit and compliance
    IpAddress              nvarchar(45),
    UserAgent              nvarchar(500),
    FraudScore             decimal(5,2),
    ComplianceFlags        nvarchar(max)
);

-- Payment Saga State table for transaction coordination
CREATE TABLE PaymentSagaState (
    SagaId                 uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    PaymentId              uniqueidentifier NOT NULL,
    EventId                uniqueidentifier NOT NULL,
    UserId                 uniqueidentifier NOT NULL,
    CurrentStep            nvarchar(100) NOT NULL,
    SagaData               nvarchar(max) NOT NULL, -- JSON state
    Status                 nvarchar(50) NOT NULL, -- Started, InProgress, Completed, Failed, Compensating
    CreatedAt              datetime2 DEFAULT GETUTCDATE(),
    UpdatedAt              datetime2 DEFAULT GETUTCDATE(),
    CompletedAt            datetime2,
    
    FOREIGN KEY (PaymentId) REFERENCES Payments(PaymentId)
);

-- Indexes for performance and compliance
CREATE INDEX IX_Payments_EventId ON Payments(EventId);
CREATE INDEX IX_Payments_UserId ON Payments(UserId);
CREATE INDEX IX_Payments_Status_CreatedAt ON Payments(Status, CreatedAt);
CREATE INDEX IX_PaymentSagaState_Status ON PaymentSagaState(Status);
```

---

### **4. NotificationService** ⏳ **PLANNED**

#### **Bounded Context:**
Multi-channel user communication and notification delivery system.

#### **Business Responsibilities:**
- Multi-channel notification delivery (Email, SMS, Push, In-App)
- Template management with personalization
- User preference management and opt-out handling
- Delivery tracking and analytics
- A/B testing for notification effectiveness
- Compliance with communication regulations

#### **Technical Architecture:**
```
┌─────────────────────────────────────────────────────┐
│               NotificationService.API               │ ← Controllers + Webhooks
├─────────────────────────────────────────────────────┤
│              NotificationService.Core               │ ← Domain + Template Engine
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │Notification │  │TemplateEngine│ │ChannelMgr  │ │
│  │  (Entity)   │  │             │  │ (Abstraction)│ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────┤
│            NotificationService.Infrastructure       │ ← Data + Channels
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │Cosmos DB    │  │Email Channel│  │SMS Channel  │ │
│  │Repository   │  │(Azure Comm) │  │(Twilio)     │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────┘
```

#### **Database Design (Azure Cosmos DB):**
```json
// Container: NotificationTemplates (Partition Key: templateType)
{
  "id": "template-ticket-confirmation-v2",
  "templateType": "TicketConfirmation",
  "templateName": "Ticket Purchase Confirmation",
  "version": "2.0",
  "isActive": true,
  "channels": ["email", "sms", "push"],
  "languages": ["en", "es", "fr"],
  "templates": {
    "email": {
      "en": {
        "subject": "🎫 Your tickets for {{eventName}} are confirmed!",
        "htmlBody": "<!DOCTYPE html><html>...",
        "textBody": "Hi {{userName}}, your tickets for {{eventName}} are confirmed..."
      },
      "es": {
        "subject": "🎫 ¡Tus boletos para {{eventName}} están confirmados!",
        "htmlBody": "<!DOCTYPE html><html>...",
        "textBody": "Hola {{userName}}, tus boletos para {{eventName}} están confirmados..."
      }
    },
    "sms": {
      "en": {
        "body": "Tickets confirmed for {{eventName}} on {{eventDate}}. Total: ${{amount}}. Check email for details."
      }
    },
    "push": {
      "en": {
        "title": "Tickets Confirmed!",
        "body": "Your tickets for {{eventName}} are ready",
        "data": {
          "eventId": "{{eventId}}",
          "action": "view_tickets"
        }
      }
    }
  },
  "variables": [
    {"name": "userName", "type": "string", "required": true},
    {"name": "eventName", "type": "string", "required": true},
    {"name": "eventDate", "type": "datetime", "format": "MMM dd, yyyy"},
    {"name": "amount", "type": "decimal", "format": "currency"}
  ],
  "abTestVariants": [
    {
      "variantId": "A",
      "percentage": 50,
      "subject": "🎫 Your tickets for {{eventName}} are confirmed!"
    },
    {
      "variantId": "B", 
      "percentage": 50,
      "subject": "✅ {{eventName}} - Ticket confirmation"
    }
  ],
  "_ts": 1723456789
}

// Container: UserPreferences (Partition Key: userId)
{
  "id": "prefs-user-123e4567-e89b-12d3-a456-426614174000",
  "userId": "user-123e4567-e89b-12d3-a456-426614174000",
  "preferences": {
    "channels": {
      "email": {
        "enabled": true,
        "address": "user@example.com",
        "verified": true,
        "categories": {
          "transactional": true,
          "marketing": false,
          "reminders": true
        }
      },
      "sms": {
        "enabled": false,
        "phoneNumber": "+1234567890",
        "verified": false,
        "categories": {
          "transactional": true,
          "marketing": false,
          "reminders": false
        }
      },
      "push": {
        "enabled": true,
        "deviceTokens": ["token1", "token2"],
        "categories": {
          "transactional": true,
          "marketing": true,
          "reminders": true
        }
      }
    },
    "language": "en",
    "timezone": "America/New_York",
    "frequency": {
      "immediate": ["TicketConfirmation", "PaymentFailed"],
      "batched": ["EventReminder", "MarketingPromo"],
      "digest": ["WeeklyUpdates"]
    }
  },
  "optOutTokens": {
    "email": "opt-out-token-email-123",
    "sms": "opt-out-token-sms-456"
  },
  "compliance": {
    "gdprConsent": true,
    "canSpamCompliance": true,
    "lastConsentDate": "2025-08-12T10:00:00Z"
  },
  "_ts": 1723456789
}
```

---

### **5. API Gateway Service** ⏳ **PLANNED**

#### **Bounded Context:**
Centralized request routing, authentication, and cross-cutting concerns management.

#### **Business Responsibilities:**
- Request routing and load balancing across microservices
- Authentication and authorization enforcement
- Rate limiting and throttling
- Request/response transformation
- API versioning and deprecation management
- Monitoring and analytics

#### **Technical Architecture Options:**

##### **Option A: Azure API Management (Recommended for Learning)**
```yaml
# API Management Configuration
apiVersion: apimanagement.azure.com/v1
kind: ApiManagementService
metadata:
  name: ticketing-apim
spec:
  policies:
    - authentication: jwt-validation
    - rate-limiting: per-user-limits
    - transformation: request-response-mapping
    - cors: cross-origin-support
  
  apis:
    - name: events-api
      backend: https://eventmanagement-api.azurewebsites.net
      policies:
        - rate-limit: 1000/hour
        - caching: 5-minutes
    
    - name: inventory-api  
      backend: https://ticketinventory-api.azurewebsites.net
      policies:
        - rate-limit: 2000/hour
        - real-time: signalr-proxy
```

##### **Option B: Custom .NET Gateway (Learning Implementation Patterns)**
```csharp
// Custom API Gateway using YARP (Yet Another Reverse Proxy)
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddReverseProxy()
            .LoadFromConfig(Configuration.GetSection("ReverseProxy"));
            
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options => { /* JWT config */ });
            
        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiAccess", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "api"));
        });

        services.AddSingleton<IRateLimitingService, RateLimitingService>();
        services.AddSingleton<IApiAnalyticsService, ApiAnalyticsService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseMiddleware<ApiAnalyticsMiddleware>();
        app.UseReverseProxy();
    }
}
```

#### **Routing Configuration:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "events-route": {
        "ClusterId": "events-cluster",
        "Match": {
          "Path": "/api/v1/events/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/v1/events/{**catch-all}" },
          { "RequestHeader": "X-Forwarded-For", "Append": "{RemoteIpAddress}" }
        ]
      },
      "inventory-route": {
        "ClusterId": "inventory-cluster", 
        "Match": {
          "Path": "/api/v1/inventory/{**catch-all}"
        }
      },
      "payments-route": {
        "ClusterId": "payments-cluster",
        "Match": {
          "Path": "/api/v1/payments/{**catch-all}"
        },
        "AuthorizationPolicy": "AdminOnly"
      }
    },
    "Clusters": {
      "events-cluster": {
        "Destinations": {
          "events-api": {
            "Address": "https://eventmanagement-api.azurewebsites.net/"
          }
        }
      },
      "inventory-cluster": {
        "Destinations": {
          "inventory-api": {
            "Address": "https://ticketinventory-api.azurewebsites.net/"
          }
        }
      }
    }
  }
}
```

---

## 🔗 Cross-Service Communication

### **Communication Patterns:**

#### **Synchronous Communication (HTTP/REST):**
- **API Gateway → Microservices**: Request routing and aggregation
- **Frontend → API Gateway**: User interface interactions  
- **Admin Operations**: Direct service-to-service calls for administrative functions

#### **Asynchronous Communication (Azure Service Bus):**
- **Event Publishing**: Domain events across service boundaries
- **Command Processing**: Background operations and workflows
- **Integration Events**: Cross-bounded context communication

### **Service Bus Topic Architecture:**
```
Azure Service Bus Topics:
├── events-topic
│   ├── inventory-subscription      (TicketInventory Service)
│   ├── notifications-subscription  (NotificationService)
│   └── analytics-subscription      (Future: Analytics Service)
├── tickets-topic  
│   ├── payments-subscription       (PaymentProcessing Service)
│   ├── notifications-subscription  (NotificationService)
│   └── inventory-subscription      (TicketInventory Service)
├── payments-topic
│   ├── inventory-subscription      (TicketInventory Service)
│   ├── notifications-subscription  (NotificationService)
│   └── audit-subscription          (Future: Audit Service)
└── notifications-topic
    ├── analytics-subscription      (Future: Analytics Service)
    └── preferences-subscription     (NotificationService - internal)
```

### **Event Schema Design:**
```csharp
// Base event interface
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
    int Version { get; }
    string CorrelationId { get; }
    string CausationId { get; }
}

// Example: Cross-service integration event
public class TicketReservedEvent : IDomainEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType => "TicketReserved";
    public int Version => 1;
    public string CorrelationId { get; set; }
    public string CausationId { get; set; }
    
    // Business data
    public Guid TicketEventId { get; set; }
    public Guid UserId { get; set; }
    public Guid ReservationId { get; set; }
    public int TicketCount { get; set; }
    public string CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

---

## 📊 Data Architecture

### **Database-per-Service Pattern:**

#### **Data Consistency Strategy:**
- **Strong Consistency**: Within service boundaries (ACID transactions)
- **Eventual Consistency**: Across service boundaries (Event-driven updates)
- **Compensation**: Saga pattern for distributed transaction management

#### **Database Technology Choices:**

| Service | Database | Rationale |
|---------|----------|-----------|
| **EventManagement** | Azure SQL Database | ✅ ACID transactions, complex queries, reporting |
| **TicketInventory** | Azure Cosmos DB | ✅ High throughput, global distribution, optimistic concurrency |
| **PaymentProcessing** | Azure SQL Database | ✅ Financial ACID compliance, audit requirements |
| **NotificationService** | Azure Cosmos DB | ✅ Global distribution, flexible schema, high availability |
| **API Gateway** | In-Memory/Redis | ✅ High-speed caching, session management |

#### **Data Synchronization Patterns:**
```csharp
// Event-driven data synchronization
public class EventCreatedEventHandler : IEventHandler<EventCreatedEvent>
{
    public async Task HandleAsync(EventCreatedEvent @event)
    {
        // Create inventory record in TicketInventory service
        var inventoryCommand = new CreateInventoryCommand
        {
            EventId = @event.EventId,
            TotalCapacity = @event.MaxCapacity,
            PriceCategories = new[]
            {
                new PriceCategory { Name = "General", Price = @event.TicketPrice }
            }
        };
        
        await _inventoryService.CreateInventoryAsync(inventoryCommand);
        
        // Create notification templates for the event
        var templateCommand = new CreateEventTemplatesCommand
        {
            EventId = @event.EventId,
            EventName = @event.EventName,
            OrganizerUserId = @event.OrganizerUserId
        };
        
        await _notificationService.CreateTemplatesAsync(templateCommand);
    }
}
```

---

## 🔐 Security Architecture

### **Authentication & Authorization:**

#### **Azure AD B2C Integration:**
```csharp
// JWT Configuration
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = "https://ticketingb2c.b2clogin.com/ticketingb2c.onmicrosoft.com/v2.0/";
            options.Audience = "api://ticketing-api";
            options.RequireHttpsMetadata = true;
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        });

    services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireClaim("role", "Admin"));
            
        options.AddPolicy("OrganizerOrAdmin", policy =>
            policy.RequireClaim("role", "Admin", "Organizer"));
            
        options.AddPolicy("AuthenticatedUser", policy =>
            policy.RequireAuthenticatedUser());
    });
}
```

#### **Role-Based Access Control (RBAC):**
```json
{
  "roles": {
    "Admin": {
      "permissions": [
        "events:create", "events:update", "events:delete",
        "inventory:manage", "payments:refund", 
        "notifications:send", "users:manage"
      ]
    },
    "Organizer": {
      "permissions": [
        "events:create", "events:update", "events:view-own",
        "inventory:view-own", "payments:view-own",
        "notifications:send-own"
      ]
    },
    "User": {
      "permissions": [
        "events:view", "tickets:purchase", 
        "payments:view-own", "profile:manage"
      ]
    }
  }
}
```

### **Data Protection:**

#### **Encryption Strategy:**
- **In Transit**: TLS 1.2+ for all communications
- **At Rest**: Transparent Data Encryption (TDE) for SQL databases
- **Application Level**: Always Encrypted for PII and financial data
- **Key Management**: Azure Key Vault for all secrets and certificates

#### **PCI DSS Compliance Simulation:**
```csharp
// Sensitive data encryption
public class PaymentMethod
{
    public Guid Id { get; set; }
    
    [EncryptedPersonalData] // Custom attribute for encryption
    public string CardNumber { get; set; }
    
    [EncryptedPersonalData]
    public string ExpirationDate { get; set; }
    
    // Only store last 4 digits in plain text
    public string LastFourDigits { get; set; }
    
    public string CardType { get; set; } // Visa, MasterCard, etc.
    
    // Never store CVV - validate only
}
```

---

## 📈 Monitoring & Observability

### **Distributed Tracing:**
```csharp
// Application Insights integration
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry();
    
    services.AddOpenTelemetry()
        .WithTracing(builder => builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddConsoleExporter()
            .AddApplicationInsightsTraceExporter());
}

// Custom telemetry
public class InventoryService
{
    private readonly TelemetryClient _telemetryClient;
    
    public async Task ReserveTicketsAsync(ReserveTicketsCommand command)
    {
        using var activity = Activity.StartActivity("ReserveTickets");
        activity?.SetTag("event.id", command.EventId.ToString());
        activity?.SetTag("user.id", command.UserId.ToString());
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _repository.ReserveTicketsAsync(command);
            
            _telemetryClient.TrackMetric("TicketsReserved", command.TicketCount, new Dictionary<string, string>
            {
                ["EventId"] = command.EventId.ToString(),
                ["Category"] = command.CategoryId
            });
            
            return result;
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _telemetryClient.TrackDependency("CosmosDB", "ReserveTickets", 
                DateTime.UtcNow.Subtract(stopwatch.Elapsed), stopwatch.Elapsed, true);
        }
    }
}
```

### **Business Metrics Dashboard:**
```csharp
// Custom business metrics
public class BusinessMetricsService
{
    public void TrackEventCreated(Guid eventId, string category, decimal ticketPrice)
    {
        _telemetryClient.TrackEvent("EventCreated", new Dictionary<string, string>
        {
            ["EventId"] = eventId.ToString(),
            ["Category"] = category,
            ["TicketPrice"] = ticketPrice.ToString("F2")
        });
    }
    
    public void TrackTicketSales(Guid eventId, int ticketsSold, decimal revenue)
    {
        _telemetryClient.TrackMetric("TicketsSold", ticketsSold);
        _telemetryClient.TrackMetric("Revenue", revenue);
        
        // Custom business KPIs
        _telemetryClient.TrackMetric("AverageTicketPrice", revenue / ticketsSold);
    }
}
```

---

## 🚀 Deployment Architecture

### **Container Strategy:**
```dockerfile
# Multi-stage Dockerfile for .NET services
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EventManagement.API/EventManagement.API.csproj", "EventManagement.API/"]
COPY ["EventManagement.Core/EventManagement.Core.csproj", "EventManagement.Core/"]
COPY ["EventManagement.Infrastructure/EventManagement.Infrastructure.csproj", "EventManagement.Infrastructure/"]
RUN dotnet restore "EventManagement.API/EventManagement.API.csproj"

COPY . .
WORKDIR "/src/EventManagement.API"
RUN dotnet build "EventManagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventManagement.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventManagement.API.dll"]
```

### **Azure Deployment Options:**

#### **Phase 1: Azure App Service (Free Tier)**
```yaml
# Azure App Service deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: eventmanagement-api
spec:
  replicas: 1
  selector:
    matchLabels:
      app: eventmanagement-api
  template:
    metadata:
      labels:
        app: eventmanagement-api
    spec:
      containers:
      - name: api
        image: ticketingacr.azurecr.io/eventmanagement-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: sql-connection
              key: connectionstring
```

#### **Phase 2: Azure Kubernetes Service (Production)**
```yaml
# AKS deployment with advanced features
apiVersion: v1
kind: ConfigMap
metadata:
  name: eventmanagement-config
data:
  appsettings.json: |
    {
      "ConnectionStrings": {
        "DefaultConnection": "$(SQL_CONNECTION_STRING)"
      },
      "ServiceBus": {
        "ConnectionString": "$(SERVICEBUS_CONNECTION_STRING)"
      },
      "ApplicationInsights": {
        "ConnectionString": "$(APPINSIGHTS_CONNECTION_STRING)"
      }
    }
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: eventmanagement-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: eventmanagement-api
  template:
    metadata:
      labels:
        app: eventmanagement-api
    spec:
      containers:
      - name: api
        image: ticketingacr.azurecr.io/eventmanagement-api:latest
        ports:
        - containerPort: 80
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 10
        envFrom:
        - configMapRef:
            name: eventmanagement-config
        - secretRef:
            name: eventmanagement-secrets
```

---

## 💰 Cost Optimization Strategy

### **Azure Free Tier Maximization:**

| Service | Free Tier Allocation | Usage Strategy | Monthly Cost |
|---------|---------------------|----------------|--------------|
| **App Service** | 10 web apps, 1GB storage | Host all 5 APIs | $0 |
| **SQL Database** | 250GB, 5 DTU | Event + Payment data | $0 |
| **Cosmos DB** | 1000 RU/s, 25GB | Inventory + Notifications | $0 |
| **Service Bus** | 750 hours messaging | Event-driven communication | $0 |
| **Functions** | 1M executions | Background processing | $0 |
| **Storage** | 5GB + 20K transactions | File uploads, logs | $0 |
| **Key Vault** | 10K transactions | Secrets management | $0 |
| **Application Insights** | 5GB log ingestion | Basic monitoring | $0 |
| ****Total Phase 1** | | **Complete learning platform** | **$0/month** |

### **Scaling Cost Strategy:**

#### **Phase 2: Enhanced Features ($5-20/month)**
- Custom domain with SSL certificate
- Enhanced Application Insights monitoring  
- Small Redis cache instance
- Premium storage for better performance

#### **Phase 3: Production-Ready ($50-150/month)**
- Azure Kubernetes Service cluster
- Premium database tiers
- API Management standard tier
- Advanced security features
- Global content delivery network

---

## 🎯 Implementation Success Criteria

### **Technical Milestones:**
- ✅ **Service Independence**: Each service can be deployed and scaled independently
- ✅ **Data Isolation**: Database-per-service with clear boundaries
- ✅ **Event-Driven Communication**: Loose coupling via messaging
- ✅ **Resilience**: Circuit breakers, retries, and graceful degradation
- ✅ **Observability**: Distributed tracing and comprehensive monitoring
- ✅ **Security**: Authentication, authorization, and data protection

### **Learning Objectives Achieved:**
- ✅ **Microservices Patterns**: Practical implementation of key patterns
- ✅ **Cloud-Native Development**: Azure services integration
- ✅ **Event-Driven Architecture**: Asynchronous communication mastery
- ✅ **DevOps Practices**: CI/CD, Infrastructure as Code, monitoring
- ✅ **Security Best Practices**: Authentication, encryption, compliance
- ✅ **Performance Optimization**: Caching, scaling, concurrency handling

**This architecture provides a comprehensive learning platform for modern, cloud-native application development using production-grade patterns and practices.**
