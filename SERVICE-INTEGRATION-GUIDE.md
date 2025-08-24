# Service Integration Architecture

## 🔗 **How EventManagement and EventSearch Services Are Connected**

### **Current Architecture Overview**
```
┌─────────────────────────────────────────────────────────────────┐
│                     Frontend (Angular)                          │
│                   http://localhost:4200                         │
└─────────────────┬───────────────────────────────────────────────┘
                  │
                  ├─────────────────────────────────────────────┐
                  │                                             │
                  ▼                                             ▼
┌─────────────────────────────┐              ┌─────────────────────────────┐
│    EventManagement API      │              │     EventSearch API         │
│   http://localhost:8080     │◄────────────►│   http://localhost:8081     │
│                             │   HTTP       │                             │
│  ┌─────────────────────┐    │   Client     │  ┌─────────────────────┐    │
│  │   SQL Server        │    │              │  │   Elasticsearch     │    │
│  │   (Event CRUD)      │    │              │  │   (Search Index)    │    │
│  └─────────────────────┘    │              │  │                     │    │
└─────────────────────────────┘              │  │   Redis Cache       │    │
                                             │  │   (Search Cache)    │    │
                                             │  └─────────────────────┘    │
                                             └─────────────────────────────┘
```

## 🔄 **Data Synchronization Flow**

### **1. Event Creation Flow**
```
1. User creates event via EventManagement API
2. Event saved to SQL Server
3. EventManagement automatically calls EventSearch API
4. Event indexed in Elasticsearch
5. Event cached in Redis for fast retrieval
```

### **2. Event Update Flow**
```
1. User updates event via EventManagement API
2. Event updated in SQL Server
3. EventManagement automatically calls EventSearch API
4. Event updated in Elasticsearch
5. Related cache entries invalidated in Redis
```

### **3. Event Delete Flow**
```
1. User deletes event via EventManagement API
2. Event deleted from SQL Server
3. EventManagement automatically calls EventSearch API
4. Event removed from Elasticsearch
5. Related cache entries removed from Redis
```

## 🛠️ **Integration Components**

### **EventManagement Service Integration**
- **File**: `EventManagement.API/Services/EventSearchIntegrationService.cs`
- **Purpose**: HTTP client to communicate with EventSearch API
- **Methods**:
  - `IndexEventAsync()` - Add event to search index
  - `UpdateEventAsync()` - Update event in search index
  - `DeleteEventAsync()` - Remove event from search index
  - `IsSearchServiceHealthyAsync()` - Check search service status

### **Data Mapping Layer**
- **File**: `EventManagement.API/Mappers/EventSearchMapper.cs`
- **Purpose**: Convert EventManagement DTOs to EventSearch DTOs
- **Features**:
  - Extracts city from location string
  - Generates relevant tags automatically
  - Calculates popularity scores
  - Maps price ranges and capacity sizes

### **Automatic Synchronization**
- **Integration Point**: `EventDtoService.cs`
- **Trigger**: Every CRUD operation on events
- **Method**: Fire-and-forget async calls (non-blocking)
- **Error Handling**: Logged but doesn't fail main operation

### **Manual Synchronization**
- **Endpoint**: `POST /api/v1/sync/events-to-search`
- **Purpose**: Sync all existing events to search service
- **Use Case**: Initial setup or data recovery
- **Health Check**: `GET /api/v1/sync/search-service-health`

## 📋 **API Endpoints Overview**

### **EventManagement API (Port 8080)**
```
POST   /api/v1/events              - Create event (auto-indexes)
GET    /api/v1/events              - Get all events
GET    /api/v1/events/{id}         - Get single event
PUT    /api/v1/events/{id}         - Update event (auto-updates index)
DELETE /api/v1/events/{id}         - Delete event (auto-removes from index)
POST   /api/v1/sync/events-to-search - Bulk sync to search service
```

### **EventSearch API (Port 8081)**
```
POST   /api/search/events          - Advanced search with filters
GET    /api/search/autocomplete    - Real-time suggestions
GET    /api/search/similar/{id}    - Find similar events
GET    /api/search/popular         - Get trending events

POST   /api/index/events           - Index single event (internal)
PUT    /api/index/events/{id}      - Update indexed event (internal)
DELETE /api/index/events/{id}      - Remove from index (internal)
```

## 🚀 **How to Use the Connected System**

### **1. Start All Services**
```bash
cd /Users/pranavpandey/TicketBookingSystem
docker-compose up -d
```

### **2. Initial Data Synchronization**
```bash
# Wait for all services to be healthy, then sync existing events
curl -X POST http://localhost:8080/api/v1/sync/events-to-search
```

### **3. Create Events (Auto-Indexed)**
```bash
curl -X POST http://localhost:8080/api/v1/events \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Rock Concert",
    "description": "Amazing rock concert in downtown",
    "category": "Music",
    "eventDate": "2025-09-15T20:00:00Z",
    "location": "Madison Square Garden, New York",
    "maxCapacity": 20000,
    "ticketPrice": 85.00,
    "organizerUserId": "550e8400-e29b-41d4-a716-446655440000"
  }'
```

### **4. Search Events (Lightning Fast)**
```bash
# Full-text search
curl -X POST http://localhost:8081/api/search/events \
  -H "Content-Type: application/json" \
  -d '{
    "searchText": "rock concert",
    "category": "Music",
    "city": "New York",
    "minPrice": 50,
    "maxPrice": 100,
    "page": 1,
    "pageSize": 10
  }'

# Autocomplete
curl "http://localhost:8081/api/search/autocomplete?query=rock"

# Popular events
curl "http://localhost:8081/api/search/popular?category=Music"
```

## ✅ **What's Connected and Working**

### **✅ Connected Components**
- ✅ EventManagement ↔ EventSearch (HTTP integration)
- ✅ SQL Server ↔ Elasticsearch (automatic sync)
- ✅ Elasticsearch ↔ Redis (caching layer)
- ✅ Frontend ↔ EventManagement (existing CRUD)
- ✅ Docker network connectivity (all services)

### **✅ Automatic Workflows**
- ✅ Create event → Auto-index in search
- ✅ Update event → Auto-update search index
- ✅ Delete event → Auto-remove from search
- ✅ Search queries → Cache results in Redis
- ✅ Cache invalidation on data changes

### **✅ Manual Operations**
- ✅ Bulk synchronization endpoint
- ✅ Health check endpoints
- ✅ Direct search API access
- ✅ Index management operations

## 🎯 **Next Steps to Complete Integration**

### **Frontend Integration**
1. **Update Angular app** to call EventSearch API for search functionality
2. **Add search components** with autocomplete and filters
3. **Replace basic search** with advanced Elasticsearch-powered search

### **Production Optimizations**
1. **Add authentication** between services
2. **Implement retry logic** for failed sync operations
3. **Add monitoring** and alerting for sync failures
4. **Configure bulk operations** for better performance

### **Enhanced Features**
1. **Real-time notifications** when events are indexed
2. **Analytics tracking** for search patterns
3. **Machine learning** for better recommendations
4. **Geographic search** with location-based filtering

The system is now **fully connected and functional**! Every event operation in EventManagement automatically updates the search index, providing Amazon-level search capabilities while maintaining data consistency across both services.