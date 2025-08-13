# Cloud-Native Event Ticketing System

## 🏗️ System Architecture Overview

This is a **microservices-based event ticketing system** built with cloud-native principles, designed for scalability, reliability, and maintainability. The system uses **event-driven architecture** with **Azure cloud services** and follows **Domain-Driven Design (DDD)** principles.

### 🎯 High-Level Architecture Pattern
- **Design Pattern**: Microservices with Event-Driven Architecture
- **Communication**: HTTP/REST (synchronous) + Azure Service Bus (asynchronous)
- **Data Strategy**: Database-per-service with eventual consistency
- **Deployment**: Containerized services on Azure Kubernetes Service (AKS)
- **Frontend**: Angular SPA with Azure CDN distribution

## 🛠️ Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Frontend** | Angular 18+ with Material UI | Single Page Application |
| **Backend** | .NET 9 with ASP.NET Core | Web API services |
| **Databases** | Azure SQL Database, Cosmos DB | Data persistence |
| **Message Broker** | Azure Service Bus | Asynchronous communication |
| **Container Registry** | Azure Container Registry (ACR) | Container image storage |
| **Orchestration** | Azure Kubernetes Service (AKS) | Container orchestration |
| **API Gateway** | Azure API Management | API routing and management |
| **Caching** | Azure Cache for Redis | Performance optimization |
| **CDN** | Azure Front Door | Content delivery |
| **Authentication** | Azure AD B2C | User management |

## 🏢 Service Architecture

### 1. 📅 Event Management Service

**Bounded Context**: Complete ownership of event lifecycle management

#### Responsibilities:
- ✅ CRUD operations for events
- ✅ Event categorization and search functionality
- ✅ Event validation and lifecycle state management
- ✅ Integration with external data sources

#### Database Design:
- **Technology**: Azure SQL Database (Standard S2 for dev)
- **Primary Table**: `Events` with clustered index on `EventId`
- **Indexes**: Non-clustered on `EventDate`, `Category`, `IsActive`
- **Search**: Full-text search on `Name` and `Description`
- **Backup**: Point-in-time restore with geo-redundant backup

#### API Endpoints:
```
GET    /api/v1/events           - Paginated event listing with filtering
GET    /api/v1/events/{id}      - Single event retrieval
POST   /api/v1/events           - Event creation (Admin/Organizer only)
PUT    /api/v1/events/{id}      - Event updates (Admin/Organizer only)
DELETE /api/v1/events/{id}      - Soft delete events
GET    /api/v1/events/search    - Advanced search with full-text capabilities
```

#### Security Features:
- 🔐 Azure AD B2C integration
- 🎟️ JWT Bearer tokens (1-hour expiration, 7-day refresh)
- 👥 Role-based authorization: `Admin`, `Organizer`, `User`
- 🔒 Transparent Data Encryption (TDE)
- 🛡️ Azure Key Vault for secrets

#### Caching Strategy:
- 🚀 Redis cache for frequently accessed events (30-min TTL)
- 🔍 Cache-aside pattern for event details
- 📊 Search results caching (15-min TTL)

---

### 2. 🎫 Ticket Inventory Service

**Bounded Context**: Real-time ticket availability and reservation management

#### Core Responsibilities:
- ✅ Track ticket counts in real-time
- ✅ Handle reservations and prevent overselling
- ✅ Manage high-contention scenarios with optimistic locking

#### Database Design:
- **Technology**: Azure Cosmos DB (SQL API)
- **Consistency**: Session consistency for read-your-writes
- **Partition Strategy**: Partition by `EventId`
- **Throughput**: 1000 RU/s autoscale (starting point)
- **TTL**: Automatic cleanup for expired reservations

#### Concurrency Management:
- 🔄 Optimistic concurrency with `_etag`
- ⏰ Exponential backoff retry (50ms → 800ms)
- 🛡️ Circuit breaker pattern (5-second timeout)
- 🏊 Bulkhead pattern for high-demand events

#### Event Publishing:
- 📢 Azure Service Bus topics: `ticket.reserved`, `ticket.confirmed`, `ticket.released`
- 🔗 Message correlation and deduplication
- ⚰️ Dead letter queue handling

#### Monitoring:
- 📊 Real-time inventory levels dashboard
- ⚠️ Alerts for low availability (< 10% remaining)
- 📈 Concurrency conflict tracking

---

### 3. 💳 Payment Processing Service

**Bounded Context**: Financial transaction processing and payment state management

#### Design Pattern:
- 🔄 Saga pattern for distributed transactions
- 🏭 State machine for payment lifecycle

#### Database Design:
- **Technology**: Azure SQL Database
- **Tables**: `Payments`, `PaymentMethods`, `TransactionLogs`
- **Security**: Always Encrypted for sensitive data
- **Audit**: SQL Server Audit for all operations

#### Payment Gateway Integration:
- 🎭 Mock gateway for development/testing
- 🔌 Adapter pattern for multiple providers (Stripe, PayPal, Square)
- 🔐 Azure Key Vault for API keys
- 🆔 Idempotency keys for duplicate prevention

#### Security Features:
- 🏛️ PCI DSS compliance simulation
- 🎭 Payment data masking in logs
- 🔒 TLS 1.2+ for all communications
- 🤖 Fraud detection rules

#### Event-Driven Integration:
- 📤 Payment state change publishing
- 🔗 Correlation IDs for tracing
- ↩️ Compensating actions for failures

---

### 4. 📢 Notification Service

**Bounded Context**: Multi-channel communication and user notification management

#### Communication Channels:
- 📧 **Email**: Azure Communication Services (primary)
- 📱 **SMS**: Azure SMS service (critical notifications)
- 🔔 **Push**: Azure Notification Hubs (mobile)
- 📤 **Backup**: SendGrid integration

#### Template Management:
- 🗄️ Azure Blob Storage with versioning
- 🌍 Multi-language support
- 🎨 Personalization engine
- 🧪 A/B testing framework

#### Message Processing:
- 📥 Service Bus topic subscriptions with filters
- 📦 Batch processing (5-minute windows for non-critical)
- 🚦 Priority queues: critical → normal → marketing
- 🎛️ Rate limiting per channel

#### User Preferences:
- 🌐 Azure Cosmos DB for global distribution
- 🏗️ Preference inheritance (global → category → specific)
- ❌ Opt-out mechanisms with compliance tracking

---

### 5. 🚪 API Gateway Service

**Azure API Management Configuration**

#### Gateway Features:
- 🔗 Custom domain with SSL from Key Vault
- 💾 Request/response caching (5-minute TTL)
- 📈 API versioning with backward compatibility
- ⚖️ Load balancing with health probes

#### Security Implementation:
- 🔐 Azure AD B2C integration
- 🎫 OAuth 2.0 + OpenID Connect
- 🛡️ JWT token validation
- 👥 Role-based access control

#### Rate Limiting:
- 👤 **Authenticated users**: 1000 requests/hour
- 👻 **Anonymous users**: 100 requests/hour
- ⚡ **Burst allowance**: 50 requests/minute
- 🎯 **Tier-based limits**: free, premium, enterprise

---

## 🌐 Frontend Architecture (Angular)

### Project Structure:
```
src/
├── app/
│   ├── core/                 # Singleton services, guards
│   ├── shared/               # Common components, services
│   ├── features/
│   │   ├── events/          # Event management
│   │   ├── tickets/         # Ticket booking
│   │   ├── payments/        # Payment processing
│   │   └── profile/         # User profile
│   └── layouts/             # Application layouts
```

### Key Features:
- 🔄 **State Management**: NgRx for complex state
- 🔐 **Authentication**: MSAL for Azure AD B2C
- ⚡ **Performance**: Lazy loading, OnPush detection
- 🌐 **PWA**: Service worker for offline capabilities
- 🖼️ **Optimization**: WebP images, virtual scrolling

---

## 🔗 Cross-Service Communication

### Synchronous Communication (HTTP/REST):
- 🔍 Kubernetes service discovery
- 🕸️ Service mesh (Istio) for traffic management
- 📋 OpenAPI 3.0 specifications
- 🧪 Contract testing with Pact

### Asynchronous Communication (Service Bus):
- 📢 **Topics**: `events`, `tickets`, `payments`, `notifications`
- 🎯 Subscription filters for targeted delivery
- 🔗 Message correlation for transaction tracking
- 🔄 Saga pattern for cross-service transactions

---

## ☁️ Infrastructure & Deployment

### Azure Kubernetes Service (AKS):
- 🆔 Managed identity authentication
- 👥 Azure AD integration
- 🛡️ Network policies for pod communication
- 📋 Azure Policy for governance

### Monitoring & Observability:
- 📊 **Application Insights** for all services
- 🔍 **Distributed tracing** with correlation IDs
- 📈 **Custom metrics** for business KPIs
- 🚨 **Real-time alerting** with Azure Monitor

### CI/CD Pipeline:
- 🏗️ **Azure DevOps** multi-stage pipelines
- 🏗️ **Infrastructure as Code** (ARM/Terraform)
- 🔒 **Security scanning** with Azure Security DevOps
- 🔄 **Blue-green deployment** with traffic shifting

---

## 💰 Azure Free Tier Analysis

### ✅ Services Available in Free Tier:

| Service | Free Tier Limit | Suitable for Development |
|---------|----------------|-------------------------|
| **Azure App Service** | 10 web apps, 1GB storage | ✅ Perfect for API hosting |
| **Azure SQL Database** | 250GB for 12 months | ✅ Great for development |
| **Azure Cosmos DB** | 1000 RU/s + 25GB storage | ✅ Sufficient for testing |
| **Azure Service Bus** | 750 hours/month | ✅ Good for development |
| **Azure Functions** | 1M executions/month | ✅ Excellent for serverless |
| **Azure Storage** | 5GB LRS + 20K transactions | ✅ Good for files/logs |
| **Azure Key Vault** | 10K transactions | ✅ Perfect for secrets |
| **Azure Monitor** | 5GB log ingestion | ✅ Basic monitoring |
| **Azure CDN** | 100GB data transfer | ✅ Good for static assets |

### ⚠️ Services Requiring Paid Tier:

| Service | Why Paid Needed | Estimated Monthly Cost |
|---------|----------------|----------------------|
| **Azure Kubernetes Service** | Control plane costs | ~$75-150 |
| **Azure API Management** | No free tier | ~$50-500 |
| **Azure Cache for Redis** | No free tier | ~$20-100 |
| **Azure AD B2C** | 50K users free, then paid | $0-50 |
| **Application Insights** | 1GB free, then $2.30/GB | $0-50 |

### 💡 Free Tier Development Strategy:

#### Phase 1: Core Development (100% Free)
- Use **Azure App Service** for hosting APIs
- **Azure SQL Database** free tier for Event Management
- **Azure Cosmos DB** free tier for Ticket Inventory
- **Azure Functions** for lightweight services
- **Angular** hosted on **Azure Static Web Apps** (free)

#### Phase 2: Enhanced Features (Minimal Cost)
- Add **Azure Service Bus** for messaging
- Implement **Azure Key Vault** for secrets
- Use **Azure Storage** for file handling

#### Phase 3: Learning-Enhanced (Optional: $5-20/month)
- Add **Application Insights** for advanced monitoring
- **Custom Domain** for professional appearance
- **Small Redis instance** for caching concepts (or use in-memory)
- **API Management Consumption tier** for gateway learning

### 💡 **Learning Focus**: Master 90% of production concepts on 100% free tier!

### 🎯 Recommended Learning Architecture:

```
Frontend (Angular) → Azure Static Web Apps (Free)
                  ↓
API Gateway → Azure Functions Proxy (Free) 
           ↓
Microservices → Azure App Service (Free, multiple instances)
             ↓
Databases → Azure SQL (Free) + Cosmos DB (Free)
         ↓
Caching → In-Memory Cache (Free) or SQL-based
       ↓
Messaging → Azure Service Bus (Free tier)
```

**🎓 For Learning**: You can master all microservices, event-driven architecture, and cloud-native patterns without spending a penny! See [Learning-Focused Strategy](./docs/learning-focused-strategy.md) for details.

---

## 🚀 Getting Started

### Prerequisites:
- ✅ Azure Account with free tier access
- ✅ .NET 9 SDK
- ✅ Node.js 18+ and Angular CLI
- ✅ Docker Desktop
- ✅ Azure CLI
- ✅ Visual Studio Code with Azure extensions

### Quick Start Commands:
```bash
# Clone and setup
git clone <repository-url>
cd TicketBookingSystem

# Setup backend services
dotnet restore
dotnet build

# Setup frontend
cd frontend
npm install
ng serve

# Deploy to Azure (using Azure CLI)
az login
az group create --name TicketBookingRG --location eastus
```

---

## 📚 Additional Documentation

- [Architecture Decision Records (ADRs)](./docs/architecture/)
- [API Documentation](./docs/api/)
- [Deployment Guide](./docs/deployment/)
- [Security Guidelines](./docs/security/)
- [Performance Tuning](./docs/performance/)

---

## 🤝 Contributing

Please read our [Contributing Guide](./CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

---

## 📞 Support & Contact

- **Issues**: [GitHub Issues](./issues)
- **Discussions**: [GitHub Discussions](./discussions)
- **Documentation**: [Wiki](./wiki)

---

*Built with ❤️ using Azure Cloud Services and modern development practices*
