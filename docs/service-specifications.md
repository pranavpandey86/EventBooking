# Service Specifications

## 🎯 Service Overview

This document provides detailed specifications for each microservice in the Event Ticketing System.

---

## 📅 Event Management Service

### Service Responsibility
- **Primary**: Complete ownership of event lifecycle management
- **Secondary**: Event validation, categorization, search functionality

### Database Schema (Azure SQL Database)

#### Events Table
```sql
CREATE TABLE Events (
    EventId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    Category NVARCHAR(100) NOT NULL,
    EventDate DATETIME2 NOT NULL,
    Location NVARCHAR(500),
    MaxCapacity INT NOT NULL,
    TicketPrice DECIMAL(10,2) NOT NULL,
    OrganizerUserId UNIQUEIDENTIFIER NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    INDEX IX_Events_Date_Category (EventDate, Category),
    INDEX IX_Events_Organizer (OrganizerUserId),
    INDEX IX_Events_Active (IsActive, EventDate)
);

-- Full-text search catalog
CREATE FULLTEXT CATALOG EventSearchCatalog;
CREATE FULLTEXT INDEX ON Events(Name, Description) 
KEY INDEX PK_Events ON EventSearchCatalog;
```

#### EventCategories Table
```sql
CREATE TABLE EventCategories (
    CategoryId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1
);
```

### API Specifications

#### Endpoints
```http
GET    /api/v1/events
GET    /api/v1/events/{id}
POST   /api/v1/events
PUT    /api/v1/events/{id}
DELETE /api/v1/events/{id}
GET    /api/v1/events/search
GET    /api/v1/events/categories
```

#### Data Transfer Objects
```csharp
public class EventDto
{
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; }
    public int MaxCapacity { get; set; }
    public decimal TicketPrice { get; set; }
    public Guid OrganizerUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEventRequest
{
    [Required, MaxLength(255)]
    public string Name { get; set; }
    
    [MaxLength(2000)]
    public string Description { get; set; }
    
    [Required, MaxLength(100)]
    public string Category { get; set; }
    
    [Required]
    public DateTime EventDate { get; set; }
    
    [MaxLength(500)]
    public string Location { get; set; }
    
    [Required, Range(1, 100000)]
    public int MaxCapacity { get; set; }
    
    [Required, Range(0.01, 10000)]
    public decimal TicketPrice { get; set; }
}

public class EventSearchRequest
{
    public string Query { get; set; }
    public string Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Location { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

### Published Events
```csharp
public class EventCreatedEvent
{
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public DateTime EventDate { get; set; }
    public int MaxCapacity { get; set; }
    public decimal TicketPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EventUpdatedEvent
{
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public DateTime EventDate { get; set; }
    public int MaxCapacity { get; set; }
    public decimal TicketPrice { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EventDeletedEvent
{
    public Guid EventId { get; set; }
    public DateTime DeletedAt { get; set; }
}
```

---

## 🎫 Ticket Inventory Service

### Service Responsibility
- **Primary**: Real-time ticket availability and reservation management
- **Secondary**: Prevent overselling, handle high concurrency

### Database Schema (Azure Cosmos DB)

#### Container: TicketInventory
```json
{
  "id": "event-{eventId}",
  "partitionKey": "{eventId}",
  "eventId": "guid",
  "eventName": "string",
  "totalCapacity": 100,
  "availableTickets": 75,
  "reservedTickets": 20,
  "soldTickets": 5,
  "ticketPrice": 50.00,
  "reservations": [
    {
      "reservationId": "guid",
      "userId": "guid",
      "quantity": 2,
      "reservedAt": "2025-08-10T10:00:00Z",
      "expiresAt": "2025-08-10T10:15:00Z",
      "status": "Active"
    }
  ],
  "lastUpdated": "2025-08-10T10:00:00Z",
  "_etag": "cosmos-etag-value",
  "ttl": null
}
```

#### Container: ReservationHistory
```json
{
  "id": "reservation-{reservationId}",
  "partitionKey": "{userId}",
  "reservationId": "guid",
  "eventId": "guid",
  "userId": "guid",
  "quantity": 2,
  "status": "Confirmed",
  "reservedAt": "2025-08-10T10:00:00Z",
  "confirmedAt": "2025-08-10T10:10:00Z",
  "expiredAt": null,
  "ttl": 7776000
}
```

### API Specifications

#### Endpoints
```http
GET    /api/v1/inventory/{eventId}
POST   /api/v1/inventory/{eventId}/reserve
POST   /api/v1/inventory/{eventId}/confirm
POST   /api/v1/inventory/{eventId}/release
GET    /api/v1/inventory/user/{userId}/reservations
```

#### Data Transfer Objects
```csharp
public class TicketInventoryDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; }
    public int TotalCapacity { get; set; }
    public int AvailableTickets { get; set; }
    public int ReservedTickets { get; set; }
    public int SoldTickets { get; set; }
    public decimal TicketPrice { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ReserveTicketsRequest
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required, Range(1, 10)]
    public int Quantity { get; set; }
}

public class ReserveTicketsResponse
{
    public Guid ReservationId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public int Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; }
}
```

### Published Events
```csharp
public class TicketReservedEvent
{
    public Guid ReservationId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class TicketConfirmedEvent
{
    public Guid ReservationId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime ConfirmedAt { get; set; }
}

public class TicketReleasedEvent
{
    public Guid ReservationId { get; set; }
    public Guid EventId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; }
    public DateTime ReleasedAt { get; set; }
}
```

---

## 💳 Payment Processing Service

### Service Responsibility
- **Primary**: Financial transaction processing and payment state management
- **Secondary**: PCI compliance, fraud detection, payment method management

### Database Schema (Azure SQL Database)

#### Payments Table
```sql
CREATE TABLE Payments (
    PaymentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReservationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    EventId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Currency NVARCHAR(3) DEFAULT 'USD',
    PaymentMethodId UNIQUEIDENTIFIER,
    GatewayTransactionId NVARCHAR(255),
    Status NVARCHAR(50) NOT NULL,
    PaymentGateway NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ProcessedAt DATETIME2,
    FailedAt DATETIME2,
    FailureReason NVARCHAR(500),
    
    INDEX IX_Payments_Reservation (ReservationId),
    INDEX IX_Payments_User (UserId),
    INDEX IX_Payments_Status (Status, CreatedAt)
);
```

#### PaymentMethods Table
```sql
CREATE TABLE PaymentMethods (
    PaymentMethodId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- CreditCard, PayPal, etc.
    LastFourDigits NVARCHAR(4),
    ExpiryMonth INT,
    ExpiryYear INT,
    CardType NVARCHAR(50),
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    INDEX IX_PaymentMethods_User (UserId)
);
```

#### TransactionLogs Table
```sql
CREATE TABLE TransactionLogs (
    LogId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PaymentId UNIQUEIDENTIFIER NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    RequestData NVARCHAR(MAX),
    ResponseData NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    INDEX IX_TransactionLogs_Payment (PaymentId, CreatedAt)
);
```

### API Specifications

#### Endpoints
```http
POST   /api/v1/payments/process
GET    /api/v1/payments/{paymentId}
GET    /api/v1/payments/user/{userId}
POST   /api/v1/payments/{paymentId}/refund
GET    /api/v1/payments/methods/user/{userId}
POST   /api/v1/payments/methods
DELETE /api/v1/payments/methods/{methodId}
```

#### Data Transfer Objects
```csharp
public class ProcessPaymentRequest
{
    [Required]
    public Guid ReservationId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid EventId { get; set; }
    
    [Required, Range(0.01, 10000)]
    public decimal Amount { get; set; }
    
    [Required]
    public PaymentMethodDto PaymentMethod { get; set; }
    
    public string Currency { get; set; } = "USD";
}

public class PaymentMethodDto
{
    public Guid? PaymentMethodId { get; set; }
    public string Type { get; set; }
    public string CardNumber { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string CVV { get; set; }
    public string CardholderName { get; set; }
    public bool SaveForFuture { get; set; }
}

public class PaymentDto
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Status { get; set; }
    public string PaymentGateway { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string FailureReason { get; set; }
}
```

### Payment State Machine
```csharp
public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

public class PaymentStateMachine
{
    private static readonly Dictionary<PaymentStatus, List<PaymentStatus>> ValidTransitions = new()
    {
        { PaymentStatus.Pending, new() { PaymentStatus.Processing, PaymentStatus.Cancelled } },
        { PaymentStatus.Processing, new() { PaymentStatus.Completed, PaymentStatus.Failed } },
        { PaymentStatus.Completed, new() { PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded } },
        { PaymentStatus.Failed, new() { PaymentStatus.Processing } },
        { PaymentStatus.Cancelled, new() { } },
        { PaymentStatus.Refunded, new() { } },
        { PaymentStatus.PartiallyRefunded, new() { PaymentStatus.Refunded } }
    };
}
```

### Published Events
```csharp
public class PaymentProcessedEvent
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class PaymentFailedEvent
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid UserId { get; set; }
    public string FailureReason { get; set; }
    public DateTime FailedAt { get; set; }
}
```

---

## 📢 Notification Service

### Service Responsibility
- **Primary**: Multi-channel communication and user notification management
- **Secondary**: Template management, user preferences, delivery tracking

### Database Schema (Azure Cosmos DB)

#### Container: NotificationTemplates
```json
{
  "id": "template-{templateId}",
  "partitionKey": "{templateType}",
  "templateType": "TicketConfirmation",
  "channel": "Email",
  "language": "en-US",
  "subject": "Your tickets for {{EventName}}",
  "body": "HTML template content",
  "version": "1.0",
  "isActive": true,
  "createdAt": "2025-08-10T10:00:00Z"
}
```

#### Container: UserPreferences
```json
{
  "id": "user-{userId}",
  "partitionKey": "{userId}",
  "userId": "guid",
  "emailEnabled": true,
  "smsEnabled": false,
  "pushEnabled": true,
  "preferences": {
    "TicketConfirmation": ["Email", "Push"],
    "EventReminder": ["Email"],
    "EventCancellation": ["Email", "SMS", "Push"],
    "PaymentConfirmation": ["Email"]
  },
  "language": "en-US",
  "timezone": "UTC",
  "lastUpdated": "2025-08-10T10:00:00Z"
}
```

### API Specifications

#### Endpoints
```http
POST   /api/v1/notifications/send
GET    /api/v1/notifications/user/{userId}/preferences
PUT    /api/v1/notifications/user/{userId}/preferences
GET    /api/v1/notifications/templates
POST   /api/v1/notifications/templates
PUT    /api/v1/notifications/templates/{templateId}
```

#### Data Transfer Objects
```csharp
public class SendNotificationRequest
{
    [Required]
    public string TemplateName { get; set; }
    
    [Required]
    public List<string> Recipients { get; set; }
    
    public Dictionary<string, object> TemplateData { get; set; }
    
    public List<string> Channels { get; set; }
    
    public string Priority { get; set; } = "Normal";
    
    public DateTime? ScheduledFor { get; set; }
}

public class UserPreferencesDto
{
    public Guid UserId { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public Dictionary<string, List<string>> NotificationPreferences { get; set; }
    public string Language { get; set; }
    public string Timezone { get; set; }
}
```

### Message Processing
```csharp
public class NotificationMessage
{
    public Guid MessageId { get; set; }
    public string TemplateName { get; set; }
    public string Channel { get; set; }
    public string Recipient { get; set; }
    public Dictionary<string, object> TemplateData { get; set; }
    public string Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; }
}

public enum NotificationPriority
{
    Critical = 1,    // Immediate delivery
    High = 2,        // 1-minute batch
    Normal = 3,      // 5-minute batch
    Low = 4          // 30-minute batch
}
```

---

## 🚪 API Gateway Specifications

### Azure API Management Configuration

#### API Policies
```xml
<policies>
    <inbound>
        <cors allow-credentials="true">
            <allowed-origins>
                <origin>https://your-frontend-domain.com</origin>
                <origin>http://localhost:4200</origin>
            </allowed-origins>
            <allowed-methods>
                <method>GET</method>
                <method>POST</method>
                <method>PUT</method>
                <method>DELETE</method>
                <method>OPTIONS</method>
            </allowed-methods>
            <allowed-headers>
                <header>*</header>
            </allowed-headers>
        </cors>
        
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401">
            <openid-config url="https://your-tenant.b2clogin.com/.well-known/openid_configuration" />
            <audiences>
                <audience>your-api-client-id</audience>
            </audiences>
        </validate-jwt>
        
        <rate-limit-by-key calls="100" renewal-period="60" counter-key="@(context.Request.IpAddress)" />
        
        <set-header name="X-Correlation-ID" exists-action="skip">
            <value>@(Guid.NewGuid().ToString())</value>
        </set-header>
    </inbound>
    
    <outbound>
        <set-header name="X-Response-Time" exists-action="override">
            <value>@(context.Elapsed.TotalMilliseconds.ToString())</value>
        </set-header>
    </outbound>
    
    <on-error>
        <return-response>
            <set-status code="500" />
            <set-header name="Content-Type" exists-action="override">
                <value>application/json</value>
            </set-header>
            <set-body>
                {
                    "error": "Internal Server Error",
                    "correlationId": "@(context.Request.Headers.GetValueOrDefault("X-Correlation-ID", ""))"
                }
            </set-body>
        </return-response>
    </on-error>
</policies>
```

#### Rate Limiting Configuration
```json
{
  "rateLimits": {
    "anonymous": {
      "requestsPerHour": 100,
      "requestsPerMinute": 10
    },
    "authenticated": {
      "requestsPerHour": 1000,
      "requestsPerMinute": 50
    },
    "premium": {
      "requestsPerHour": 10000,
      "requestsPerMinute": 500
    }
  }
}
```

This comprehensive service specification provides the detailed technical requirements for implementing each microservice in the Event Ticketing System.
