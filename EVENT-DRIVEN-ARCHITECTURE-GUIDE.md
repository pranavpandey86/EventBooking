# 🚀 Event-Driven Architecture Implementation Guide

## 📋 Current State Analysis

### **Current Problem: Direct Service Communication**
```
EventManagement API → HTTP Client → EventSearch API
                   ↓
              Synchronous, blocking
              Tight coupling
              Cascade failure risk
```

### **Target Solution: Event-Driven Architecture**
```
EventManagement API → Kafka Topic → EventSearch Service
                   ↓                      ↓
              Returns immediately    Processes asynchronously
              Loose coupling         Resilient to failures
```

---

## 🎯 Technology Recommendation: **Apache Kafka**

### **Why Kafka Over Azure Service Bus?**

| Factor | Apache Kafka | Azure Service Bus | Recommendation |
|--------|-------------|-------------------|----------------|
| **Local Development** | ✅ Docker container | ❌ Cloud-only | **Kafka** |
| **Learning Value** | ✅ Industry standard | ✅ Azure-specific | **Kafka** |
| **Performance** | ✅ High throughput | ✅ Good performance | **Kafka** |
| **Cost** | ✅ Free locally | ❌ Pay per message | **Kafka** |
| **Ecosystem** | ✅ Huge ecosystem | ✅ Azure integration | **Kafka** |
| **Portability** | ✅ Cloud agnostic | ❌ Azure locked | **Kafka** |

### **✅ Kafka is the Winner Because:**
1. **Local Development**: Perfect Docker support for testing
2. **Industry Standard**: Used by Netflix, Uber, LinkedIn, Airbnb
3. **Learning Investment**: Skills transfer to any company
4. **Cloud Flexibility**: Can run on Azure, AWS, GCP, or on-premises
5. **Performance**: Handles millions of events per second
6. **Ecosystem**: Rich tooling (Kafka Connect, Streams, Schema Registry)

---

## 🐳 Kafka in Azure: Multiple Options

### **Option 1: Azure Event Hubs (Kafka-Compatible)**
```yaml
# Azure Event Hubs with Kafka protocol
connection_string: "Endpoint=sb://your-namespace.servicebus.windows.net/..."
kafka_bootstrap_servers: "your-namespace.servicebus.windows.net:9093"
```
- ✅ Managed Kafka-compatible service
- ✅ No infrastructure management
- ✅ Built-in scaling and monitoring
- ❌ Azure-specific configuration

### **Option 2: Confluent Cloud on Azure**
```yaml
# Confluent Cloud (Kafka creators)
bootstrap_servers: "pkc-abc123.eastus.azure.confluent.cloud:9092"
```
- ✅ Full Kafka ecosystem
- ✅ Enterprise features
- ✅ Global availability
- ❌ Additional cost

### **Option 3: Self-Managed Kafka on Azure VMs**
```yaml
# Azure Virtual Machines with Kafka
kafka_cluster:
  - kafka-1.eastus.cloudapp.azure.com:9092
  - kafka-2.eastus.cloudapp.azure.com:9092
  - kafka-3.eastus.cloudapp.azure.com:9092
```
- ✅ Full control
- ✅ Cost effective for large scale
- ❌ Operations overhead

### **🎯 Recommended Progression:**
1. **Local Development**: Docker Kafka containers
2. **Azure Deployment**: Azure Event Hubs (Kafka-compatible)
3. **Production Scale**: Confluent Cloud on Azure

---

## 🏗️ Implementation Plan

### **Phase 1: Local Kafka Setup (Docker)**

#### **Docker Compose Addition:**
```yaml
# Add to existing docker-compose.yml
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.4.0
    container_name: ticketing-zookeeper
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    networks:
      - ticketing-network

  kafka:
    image: confluentinc/cp-kafka:7.4.0
    container_name: ticketing-kafka
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
    networks:
      - ticketing-network

  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: ticketing-kafka-ui
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: local
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:29092
    networks:
      - ticketing-network
```

### **Phase 2: Event Schema Design**

#### **Event Definitions:**
```csharp
// EventManagement.Core/Events/EventCreatedEvent.cs
public class EventCreatedEvent
{
    public Guid EventId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Location { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}

public class EventUpdatedEvent
{
    public Guid EventId { get; set; }
    public string Title { get; set; }
    // ... updated fields
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
}

public class EventDeletedEvent
{
    public Guid EventId { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletedBy { get; set; }
}
```

### **Phase 3: Producer Implementation (EventManagement)**

#### **Kafka Producer Service:**
```csharp
// EventManagement.Infrastructure/Messaging/KafkaProducer.cs
public class KafkaEventProducer : IEventProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;
    
    public async Task PublishAsync<T>(string topic, string key, T eventData)
    {
        var json = JsonSerializer.Serialize(eventData);
        var message = new Message<string, string>
        {
            Key = key,
            Value = json,
            Headers = new Headers
            {
                { "EventType", Encoding.UTF8.GetBytes(typeof(T).Name) },
                { "Timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString()) }
            }
        };

        await _producer.ProduceAsync(topic, message);
        _logger.LogInformation("Published {EventType} to topic {Topic}", typeof(T).Name, topic);
    }
}
```

#### **EventManagement Service Update:**
```csharp
// EventManagement.Core/Services/EventService.cs
public async Task<Event> CreateEventAsync(CreateEventRequest request)
{
    // 1. Create event in database
    var newEvent = new Event { /* ... */ };
    await _repository.CreateAsync(newEvent);
    
    // 2. Publish event (fire-and-forget)
    var eventCreated = new EventCreatedEvent
    {
        EventId = newEvent.Id,
        Title = newEvent.Title,
        // ... map properties
    };
    
    await _eventProducer.PublishAsync("events.created", newEvent.Id.ToString(), eventCreated);
    
    // 3. Return immediately
    return newEvent;
}
```

### **Phase 4: Consumer Implementation (EventSearch)**

#### **Kafka Consumer Service:**
```csharp
// EventSearch.Infrastructure/Messaging/KafkaConsumer.cs
public class EventSearchConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IEventIndexService _indexService;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new[] { "events.created", "events.updated", "events.deleted" });
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = _consumer.Consume(stoppingToken);
            
            await ProcessMessage(consumeResult);
            _consumer.Commit(consumeResult);
        }
    }
    
    private async Task ProcessMessage(ConsumeResult<string, string> result)
    {
        var eventType = GetHeader(result.Message.Headers, "EventType");
        
        switch (eventType)
        {
            case "EventCreatedEvent":
                var createdEvent = JsonSerializer.Deserialize<EventCreatedEvent>(result.Message.Value);
                await HandleEventCreated(createdEvent);
                break;
            case "EventUpdatedEvent":
                var updatedEvent = JsonSerializer.Deserialize<EventUpdatedEvent>(result.Message.Value);
                await HandleEventUpdated(updatedEvent);
                break;
            case "EventDeletedEvent":
                var deletedEvent = JsonSerializer.Deserialize<EventDeletedEvent>(result.Message.Value);
                await HandleEventDeleted(deletedEvent);
                break;
        }
    }
}
```

---

## 📦 Required NuGet Packages

### **EventManagement Service:**
```xml
<PackageReference Include="Confluent.Kafka" Version="2.2.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="7.0.1" />
```

### **EventSearch Service:**
```xml
<PackageReference Include="Confluent.Kafka" Version="2.2.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="7.0.1" />
```

---

## 🧪 Testing Strategy

### **Local Testing with Docker:**
```bash
# 1. Start all services including Kafka
docker compose up -d

# 2. Verify Kafka is running
docker exec ticketing-kafka kafka-topics --bootstrap-server localhost:9092 --list

# 3. Create test topics
docker exec ticketing-kafka kafka-topics --bootstrap-server localhost:9092 --create --topic events.created
docker exec ticketing-kafka kafka-topics --bootstrap-server localhost:9092 --create --topic events.updated
docker exec ticketing-kafka kafka-topics --bootstrap-server localhost:9092 --create --topic events.deleted

# 4. Test event flow
curl -X POST http://localhost:8080/api/v1/events -H "Content-Type: application/json" -d '{
  "title": "Async Test Event",
  "category": "Technology"
}'

# 5. Check Kafka UI for messages
open http://localhost:8080  # Kafka UI interface

# 6. Verify EventSearch got the message
curl "http://localhost:8081/api/search/events" | jq
```

### **Performance Testing:**
```bash
# Test high-throughput event creation
for i in {1..1000}; do
  curl -X POST http://localhost:8080/api/v1/events -H "Content-Type: application/json" -d "{
    \"title\": \"Load Test Event $i\",
    \"category\": \"Technology\"
  }"
done

# Monitor Kafka consumer lag
docker exec ticketing-kafka kafka-consumer-groups --bootstrap-server localhost:9092 --describe --group eventsearch-group
```

---

## 🎯 Benefits of This Approach

### **Immediate Benefits:**
1. **Resilience**: EventSearch downtime doesn't affect EventManagement
2. **Performance**: Event creation returns immediately (non-blocking)
3. **Scalability**: Can process thousands of events per second
4. **Loose Coupling**: Services only depend on event schemas

### **Advanced Benefits:**
1. **Event Replay**: Can reprocess all events if needed
2. **Multiple Consumers**: Add more services that react to events
3. **Event Sourcing**: Complete audit trail of all changes
4. **CQRS**: Separate read/write models optimized for their use cases

---

## 🚀 Migration Strategy

### **Phase 1: Parallel Implementation (Safe)**
1. Keep existing HTTP integration working
2. Add Kafka publishing alongside HTTP calls
3. Add Kafka consumer in EventSearch
4. Verify both paths work correctly

### **Phase 2: Switch to Kafka Primary**
1. Make Kafka the primary integration path
2. Keep HTTP as fallback for critical operations
3. Monitor for any issues

### **Phase 3: Remove HTTP Integration**
1. Remove HTTP client code from EventManagement
2. Remove sync endpoints from EventSearch
3. Pure event-driven architecture

---

## 📊 Expected Performance Improvements

### **Current (HTTP):**
- Event creation: ~100ms (includes search indexing)
- Tight coupling: failures cascade
- Limited throughput: ~100 events/second

### **With Kafka:**
- Event creation: ~25ms (just database write)
- Loose coupling: independent failure domains
- High throughput: ~10,000+ events/second
- Asynchronous processing: better user experience

---

## 🎓 Learning Outcomes

By implementing this, you'll master:

1. **Event-Driven Architecture**: Industry-standard pattern
2. **Apache Kafka**: Most popular event streaming platform
3. **Microservice Communication**: Async, resilient patterns
4. **Event Sourcing**: Advanced data architecture pattern
5. **Container Orchestration**: Complex multi-service setups
6. **Performance Optimization**: High-throughput system design

This will give you production-ready skills that apply to virtually any large-scale system!

---

*Next Steps: Add Kafka containers to docker-compose.yml and implement event producers/consumers*