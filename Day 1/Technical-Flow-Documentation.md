# Technical Flow Documentation - Day 1

## 🔄 System Architecture Flow

### **High-Level System Flow**
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Docker Compose Environment                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐                    ┌─────────────────────────────────┐ │
│  │   Client/API    │                    │        SQL Server 2022         │ │
│  │    Consumer     │                    │    (EventManagementDB)        │ │
│  └─────────┬───────┘                    └─────────────┬───────────────────┘ │
│            │                                          │                     │
│            │ HTTP Requests                            │ TCP Connection      │
│            │ (JSON/REST)                              │ (Entity Framework)  │
│            ▼                                          ▼                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                    EventManagement API (.NET 9.0)                      │ │
│  │                              Port: 8080                                │ │
│  ├─────────────────────────────────────────────────────────────────────────┤ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐ │ │
│  │  │Controllers  │  │  Services   │  │Repository   │  │   Data Access   │ │ │
│  │  │    Layer    │  │    Layer    │  │   Layer     │  │     Layer       │ │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────────┘ │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Data Flow Architecture

### **Request Processing Flow**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│                 │    │                 │    │                 │    │                 │
│   HTTP Request  │───▶│  Controller     │───▶│   Service       │───▶│   Repository    │
│                 │    │                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘
                                ▲                        ▲                        ▲
                                │                        │                        │
                                │                        │                        ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│                 │    │                 │    │                 │    │                 │
│  HTTP Response  │◀───│  DTO Mapping    │◀───│ Business Logic  │◀───│  EF DbContext   │
│                 │    │                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘
                                                                                 ▲
                                                                                 │
                                                                                 ▼
                                                                   ┌─────────────────┐
                                                                   │                 │
                                                                   │   SQL Server    │
                                                                   │   Database      │
                                                                   │                 │
                                                                   └─────────────────┘
```

---

## 🔄 API Request Lifecycle

### **Detailed Flow for Event Creation (POST /api/v1/events)**

```
Step 1: HTTP Request Reception
┌─────────────────────────────────────────────────────────────────┐
│ POST /api/v1/events                                             │
│ Content-Type: application/json                                  │
│ {                                                               │
│   "name": "Tech Conference 2025",                               │
│   "description": "Amazing tech event",                          │
│   "category": "Technology",                                     │
│   "eventDate": "2025-12-15T09:00:00Z",                         │
│   "location": "San Francisco",                                  │
│   "maxCapacity": 500,                                           │
│   "ticketPrice": 299.99,                                       │
│   "organizerUserId": "123e4567-e89b-12d3-a456-426614174000"    │
│ }                                                               │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 2: Controller Processing
┌─────────────────────────────────────────────────────────────────┐
│ EventsController.CreateEvent(EventCreateDto createDto)          │
│ ├─ Model validation (DataAnnotations)                           │
│ ├─ Authorization check (if implemented)                         │
│ └─ Call EventService.CreateEventAsync(createDto)                │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 3: Service Layer Processing
┌─────────────────────────────────────────────────────────────────┐
│ EventService.CreateEventAsync(EventCreateDto createDto)         │
│ ├─ Business rule validation                                     │
│ ├─ DTO to Entity mapping                                        │
│ │  Event entity = new Event                                     │
│ │  {                                                            │
│ │    EventId = Guid.NewGuid(),                                  │
│ │    Name = createDto.Name,                                     │
│ │    Description = createDto.Description,                       │
│ │    Category = createDto.Category,                             │
│ │    EventDate = createDto.EventDate,                           │
│ │    Location = createDto.Location,                             │
│ │    MaxCapacity = createDto.MaxCapacity,                       │
│ │    TicketPrice = createDto.TicketPrice,                       │
│ │    OrganizerUserId = createDto.OrganizerUserId,               │
│ │    IsActive = true,                                           │
│ │    CreatedAt = DateTime.UtcNow,                               │
│ │    UpdatedAt = DateTime.UtcNow                                │
│ │  };                                                           │
│ └─ Call Repository.CreateAsync(entity)                          │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 4: Repository Data Access
┌─────────────────────────────────────────────────────────────────┐
│ EventRepository.CreateAsync(Event eventEntity)                  │
│ ├─ Add entity to DbContext                                      │
│ │  _context.Events.Add(eventEntity);                           │
│ ├─ Save changes to database                                     │
│ │  await _context.SaveChangesAsync();                          │
│ └─ Return created entity                                        │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 5: Database Operations
┌─────────────────────────────────────────────────────────────────┐
│ SQL Server Database Operations                                  │
│ ├─ Begin transaction                                            │
│ ├─ Execute INSERT statement:                                    │
│ │  INSERT INTO [Events] (EventId, Name, Description, ...)      │
│ │  VALUES (NEWID(), 'Tech Conference 2025', ...)               │
│ ├─ Update indexes:                                              │
│ │  - IX_Events_Active                                           │
│ │  - IX_Events_Date_Category                                    │
│ │  - IX_Events_Organizer                                        │
│ └─ Commit transaction                                           │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 6: Response Mapping
┌─────────────────────────────────────────────────────────────────┐
│ Entity to DTO Response Mapping                                  │
│ EventResponseDto response = new EventResponseDto                 │
│ {                                                               │
│   EventId = entity.EventId,                                     │
│   Name = entity.Name,                                           │
│   Description = entity.Description,                             │
│   Category = entity.Category,                                   │
│   EventDate = entity.EventDate,                                 │
│   Location = entity.Location,                                   │
│   MaxCapacity = entity.MaxCapacity,                             │
│   TicketPrice = entity.TicketPrice,                             │
│   OrganizerUserId = entity.OrganizerUserId,                     │
│   IsActive = entity.IsActive,                                   │
│   CreatedAt = entity.CreatedAt,                                 │
│   UpdatedAt = entity.UpdatedAt                                  │
│ };                                                              │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 7: HTTP Response
┌─────────────────────────────────────────────────────────────────┐
│ HTTP/1.1 201 Created                                            │
│ Content-Type: application/json                                  │
│ Location: /api/v1/events/77351021-a134-410a-9104-4dd2fb797bab  │
│                                                                 │
│ {                                                               │
│   "eventId": "77351021-a134-410a-9104-4dd2fb797bab",           │
│   "name": "Tech Conference 2025",                               │
│   "description": "Amazing tech event",                          │
│   "category": "Technology",                                     │
│   "eventDate": "2025-12-15T09:00:00Z",                         │
│   "location": "San Francisco",                                  │
│   "maxCapacity": 500,                                           │
│   "ticketPrice": 299.99,                                       │
│   "organizerUserId": "123e4567-e89b-12d3-a456-426614174000",   │
│   "isActive": true,                                             │
│   "createdAt": "2025-08-13T20:58:29.7291941Z",                 │
│   "updatedAt": "2025-08-13T20:58:29.7292022Z"                  │
│ }                                                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Layered Architecture Pattern

### **Separation of Concerns**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Presentation Layer                                │
├─────────────────────────────────────────────────────────────────────────────┤
│ • EventsController                                                          │
│ • HealthController                                                          │
│ • HTTP Request/Response handling                                            │
│ • Model validation                                                          │
│ • Error handling                                                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Business Logic Layer                             │
├─────────────────────────────────────────────────────────────────────────────┤
│ • EventService (IEventService)                                             │
│ • Business rule validation                                                  │
│ • DTO ↔ Entity mapping                                                      │
│ • Cross-cutting concerns                                                    │
│ • Transaction coordination                                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Data Access Layer                               │
├─────────────────────────────────────────────────────────────────────────────┤
│ • EventRepository (IEventRepository)                                       │
│ • CRUD operations                                                           │
│ • Query optimization                                                        │
│ • Data filtering                                                            │
│ • Async operations                                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Persistence Layer                             │
├─────────────────────────────────────────────────────────────────────────────┤
│ • EventDbContext (Entity Framework)                                        │
│ • Database connection management                                            │
│ • SQL generation                                                            │
│ • Change tracking                                                           │
│ • Migration management                                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                               Database Layer                               │
├─────────────────────────────────────────────────────────────────────────────┤
│ • SQL Server 2022                                                          │
│ • Events table with indexes                                                 │
│ • ACID transactions                                                         │
│ • Query optimization                                                        │
│ • Backup and recovery                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Container Orchestration Flow

### **Docker Compose Startup Sequence**

```
Step 1: Network Creation
┌─────────────────────────────────────────────────────────────────┐
│ docker compose up -d                                            │
│ ├─ Create network: ticketing-network                            │
│ ├─ Create volumes: sqlserver_data                               │
│ └─ Parse service dependencies                                   │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 2: SQL Server Startup
┌─────────────────────────────────────────────────────────────────┐
│ Start: ticketing-sqlserver container                            │
│ ├─ Pull image: mcr.microsoft.com/mssql/server:2022-latest      │
│ ├─ Set environment variables (SA_PASSWORD, ACCEPT_EULA)        │
│ ├─ Bind port: 1433:1433                                        │
│ ├─ Mount volume: sqlserver_data                                │
│ ├─ Start SQL Server service                                    │
│ └─ Wait for health check to pass                               │
│    └─ sqlcmd -S localhost -U sa -Q "SELECT 1"                  │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 3: API Container Build & Start
┌─────────────────────────────────────────────────────────────────┐
│ Start: eventmanagement-api container                            │
│ ├─ Build from Dockerfile (.NET 9.0)                            │
│ ├─ Copy application files                                       │
│ ├─ Restore NuGet packages                                       │
│ ├─ Build application                                            │
│ ├─ Set environment variables (connection string)               │
│ ├─ Wait for SQL Server dependency (health check)               │
│ ├─ Bind port: 8080:8080                                        │
│ └─ Start application                                            │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 4: Application Initialization
┌─────────────────────────────────────────────────────────────────┐
│ EventManagement API Startup                                     │
│ ├─ Load configuration (appsettings.json)                       │
│ ├─ Configure services (DI container)                           │
│ ├─ Initialize Entity Framework                                 │
│ ├─ Run database migrations                                     │
│ │  └─ Create EventManagementDB                                 │
│ │  └─ Create Events table                                      │
│ │  └─ Create indexes                                           │
│ ├─ Configure middleware pipeline                               │
│ ├─ Start Kestrel web server                                    │
│ └─ Health checks become available                              │
└─────────────────────────────────────────────────────────────────┘
                              ▼
Step 5: Ready State
┌─────────────────────────────────────────────────────────────────┐
│ System Ready for Traffic                                        │
│ ├─ SQL Server: Healthy ✅                                       │
│ ├─ API Container: Healthy ✅                                    │
│ ├─ Database: Migrated ✅                                        │
│ ├─ Health Endpoints: Active ✅                                  │
│ └─ API Endpoints: Accepting requests ✅                         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🛡️ Error Handling Flow

### **Exception Handling Strategy**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│                 │    │                 │    │                 │
│   Validation    │───▶│  HTTP 400       │    │   Not Found     │───▶│  HTTP 404       │
│   Errors        │    │  Bad Request    │    │   Errors        │    │  Not Found      │
│                 │    │                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘

┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│                 │    │                 │    │                 │    │                 │
│  Authorization  │───▶│  HTTP 401/403   │    │  Server Errors  │───▶│  HTTP 500       │
│   Errors        │    │  Unauthorized   │    │                 │    │  Internal Error │
│                 │    │                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘
```

---

## 📊 Performance Optimization Flow

### **Query Optimization Strategy**

```
Request Received
       ▼
┌─────────────────┐
│  Index Check    │ ──── IX_Events_Active (IsActive, EventDate)
│                 │ ──── IX_Events_Date_Category (EventDate, Category)  
│                 │ ──── IX_Events_Organizer (OrganizerUserId)
└─────────────────┘
       ▼
┌─────────────────┐
│  Query Plan     │ ──── Entity Framework query optimization
│  Optimization   │ ──── LINQ to SQL translation
│                 │ ──── Parameter binding
└─────────────────┘
       ▼
┌─────────────────┐
│  Connection     │ ──── Connection pooling
│  Management     │ ──── Async I/O operations
│                 │ ──── Transaction scope
└─────────────────┘
       ▼
┌─────────────────┐
│  Result         │ ──── Efficient data serialization
│  Serialization  │ ──── JSON optimization
│                 │ ──── Response compression
└─────────────────┘
```

---

## 🔍 Health Check Flow

### **Multi-Level Health Monitoring**

```
GET /health
    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Health Check Pipeline                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────────────┐   │
│  │    Self     │   │ SQL Server  │   │    API Health       │   │
│  │   Check     │   │   Check     │   │      Check          │   │
│  │             │   │             │   │                     │   │
│  │ • Process   │   │ • Database  │   │ • Controller        │   │
│  │   Status    │   │   Connection│   │   Endpoints         │   │
│  │ • Memory    │   │ • Query     │   │ • Service Layer     │   │
│  │   Usage     │   │   Execution │   │ • Dependencies      │   │
│  │ • CPU Load  │   │ • Response  │   │ • External APIs     │   │
│  │             │   │   Time      │   │                     │   │
│  └─────────────┘   └─────────────┘   └─────────────────────┘   │
│        ▼                 ▼                       ▼             │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │             Aggregate Health Status                     │   │
│  │                                                         │   │
│  │  if (all checks == Healthy)                             │   │
│  │    return Healthy                                       │   │
│  │  else if (any critical == Unhealthy)                    │   │
│  │    return Unhealthy                                     │   │
│  │  else                                                   │   │
│  │    return Degraded                                      │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
    ▼
┌─────────────────────────────────────────────────────────────────┐
│                     JSON Response                               │
│ {                                                               │
│   "status": "Healthy",                                          │
│   "checks": [                                                   │
│     {                                                           │
│       "name": "self",                                           │
│       "status": "Healthy",                                      │
│       "duration": "00:00:00.0005180"                           │
│     },                                                          │
│     {                                                           │
│       "name": "sql-server",                                     │
│       "status": "Healthy",                                      │
│       "duration": "00:00:00.0112967"                           │
│     },                                                          │
│     {                                                           │
│       "name": "api-health",                                     │
│       "status": "Healthy",                                      │
│       "duration": "00:00:00.0004957"                           │
│     }                                                           │
│   ]                                                             │
│ }                                                               │
└─────────────────────────────────────────────────────────────────┘
```

---

*Generated on: August 13, 2025*  
*Status: ✅ Technical Flow Documentation Complete*
