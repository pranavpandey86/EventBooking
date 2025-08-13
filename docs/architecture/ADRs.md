# Architecture Decision Records (ADRs)

## ADR-001: Microservices Architecture Choice

**Status**: Accepted  
**Date**: 2025-08-10  
**Decision**: Use microservices architecture with event-driven communication

### Context
We need to build a scalable event ticketing system that can handle high concurrency for ticket booking and support independent scaling of different components.

### Decision
- **Microservices**: Separate services for Events, Tickets, Payments, and Notifications
- **Event-Driven**: Use Azure Service Bus for asynchronous communication
- **Database per Service**: Each service owns its data

### Consequences
✅ **Pros**:
- Independent scaling and deployment
- Technology diversity per service
- Fault isolation
- Team autonomy

❌ **Cons**:
- Increased complexity
- Network latency
- Data consistency challenges
- Operational overhead

---

## ADR-002: Azure Cosmos DB for Ticket Inventory

**Status**: Accepted  
**Date**: 2025-08-10  
**Decision**: Use Azure Cosmos DB for ticket inventory management

### Context
Ticket inventory requires high availability, low latency, and strong consistency for preventing overselling.

### Decision
- **Azure Cosmos DB** with SQL API
- **Session consistency** level
- **Optimistic concurrency** control with ETags
- **Partition by EventId** for optimal distribution

### Consequences
✅ **Pros**:
- Global distribution capability
- Automatic scaling
- Multi-model support
- Strong SLA guarantees

❌ **Cons**:
- Higher cost than SQL Database
- Learning curve for NoSQL
- Request Unit (RU) management complexity

---

## ADR-003: Azure Service Bus for Messaging

**Status**: Accepted  
**Date**: 2025-08-10  
**Decision**: Use Azure Service Bus for inter-service communication

### Context
Services need reliable, ordered message delivery with support for complex routing scenarios.

### Decision
- **Azure Service Bus** with topics and subscriptions
- **Message sessions** for ordering
- **Dead letter queues** for error handling
- **Duplicate detection** enabled

### Consequences
✅ **Pros**:
- Enterprise-grade messaging
- Built-in retry and error handling
- Message ordering guarantees
- Integration with Azure ecosystem

❌ **Cons**:
- Cost consideration for high-volume scenarios
- Azure vendor lock-in
- Complexity for simple use cases

---

## ADR-004: Angular Frontend with NgRx

**Status**: Accepted  
**Date**: 2025-08-10  
**Decision**: Use Angular with NgRx for state management

### Context
Frontend needs to handle complex state management, real-time updates, and provide excellent user experience.

### Decision
- **Angular 18+** with standalone components
- **NgRx** for state management
- **Angular Material** for UI components
- **SignalR** for real-time updates

### Consequences
✅ **Pros**:
- Predictable state management
- Time-travel debugging
- Excellent TypeScript support
- Rich ecosystem

❌ **Cons**:
- Learning curve for NgRx
- Boilerplate code
- Bundle size considerations

---

## ADR-005: Azure Free Tier Development Strategy

**Status**: Accepted  
**Date**: 2025-08-10  
**Decision**: Phase development approach starting with Azure free tier

### Context
Need to minimize costs during development while maintaining ability to scale for production.

### Decision
- **Phase 1**: Use Azure App Service, SQL Database, and Cosmos DB free tiers
- **Phase 2**: Add Service Bus and Key Vault
- **Phase 3**: Migrate to AKS and API Management for production

### Consequences
✅ **Pros**:
- Zero initial cost
- Gradual learning curve
- Risk mitigation
- Budget predictability

❌ **Cons**:
- Limited scalability in free tier
- Migration complexity between phases
- Some features unavailable initially
