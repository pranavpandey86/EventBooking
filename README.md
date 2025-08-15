# Cloud-Native Event Ticketing System

## 📈 Latest Development Update (August 15, 2025)

### 🎉 Major Milestone Achieved: Complete Containerization

**✅ Successfully transitioned from local development to full containerized deployment:**

#### 🚀 What's New:
- **🌐 Angular 18 Frontend**: Production-ready Material UI application with event management
- **🐳 Full Container Stack**: All services now run in containers (no local development dependencies)
- **🔄 Nginx Integration**: Reverse proxy with SPA routing and API v1 proxy configuration
- **📊 Health Monitoring**: Container health checks and status monitoring
- **🔗 Service Integration**: Frontend successfully connecting to .NET API and SQL Server

#### 🏗️ Architecture Completed:
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Angular Frontend │────│  .NET 9 API     │────│  SQL Server DB  │
│  (Port 8080)     │    │  (Port 5000)    │    │  (Port 1433)    │
│  ticket-booking-  │    │  eventmanagement │    │  ticketing-     │
│  frontend        │    │  -api           │    │  sqlserver      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                        │                        │
        └────────── Docker Compose Network ──────────────┘
```

#### 🎯 Development Status:
- ✅ **Database Layer**: SQL Server with event management schema
- ✅ **API Layer**: .NET 9 with comprehensive event endpoints and health checks  
- ✅ **Frontend Layer**: Angular 18 with Material UI displaying live event data
- ✅ **Container Orchestration**: Docker Compose with proper networking and dependencies
- ✅ **Production Configuration**: Multi-stage builds and nginx optimization

#### 📱 Live Application Features:
- **Event Browsing**: Material Design cards displaying events from database
- **Event Details**: Dedicated pages with comprehensive event information
- **Responsive Design**: Mobile-first layout that works across all devices
- **API Integration**: Real-time data loading from containerized backend
- **Production Ready**: Optimized builds with compression and caching

Access the application at: **http://localhost:8080**

---

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

### 🎯 Current Implementation Status:
**✅ Phase 1 Complete**: Full containerized deployment with Angular 18 frontend

### Project Structure:
```
src/frontend/ticket-booking-system/
├── src/app/
│   ├── components/
│   │   ├── event-list/          # ✅ Material UI event grid
│   │   └── event-detail/        # ✅ Event details with routing
│   ├── services/
│   │   └── event.service.ts     # ✅ HTTP client with API integration
│   ├── models/
│   │   └── event.model.ts       # ✅ TypeScript interfaces
│   └── app.component.ts         # ✅ Standalone components
├── Dockerfile                   # ✅ Multi-stage production build
└── nginx.conf                   # ✅ SPA routing + API proxy
```

### ✅ Implemented Features:
- 🎨 **Angular Material UI**: Complete Material Design implementation
- 📱 **Responsive Design**: Mobile-first grid layout with Angular Flex
- 🔗 **API Integration**: HTTP client connecting to .NET Event Management API
- 🧭 **SPA Routing**: Client-side navigation with nginx fallback
- 🐳 **Docker Containerization**: Multi-stage builds with nginx production server
- 🔄 **API Proxy**: Nginx reverse proxy for seamless API integration
- ⚡ **Performance**: Production optimized builds with compression
- 🏥 **Health Checks**: Container health monitoring

### 🎨 UI Implementation:
- **Event Grid**: Material cards with event information
- **Event Details**: Dedicated route with comprehensive event data
- **Navigation**: Material toolbar with responsive design
- **Loading States**: Angular Material progress indicators
- **Error Handling**: User-friendly error messages and fallbacks

### 🔧 Technical Architecture:
- **TypeScript Strict Mode**: Enhanced type safety
- **Standalone Components**: Modern Angular 18 architecture
- **HTTP Interceptors**: Centralized API communication
- **Environment Configuration**: Development and production configs
- **Build Optimization**: Tree-shaking, minification, and compression

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

### 🐳 Containerized Development (Recommended)

The entire application stack now runs in containers for consistent development and deployment.

#### Quick Start with Docker Compose:
```bash
# Clone repository
git clone <repository-url>
cd TicketBookingSystem

# Start the entire application stack
docker-compose up -d

# Access the application
# Frontend: http://localhost:8080
# API: http://localhost:8080/api/v1/events
# Database: SQL Server on port 1433
```

#### Container Services:
- **📊 SQL Server Database**: `ticketing-sqlserver` - Event data storage
- **🔧 .NET API**: `eventmanagement-api` - Backend services with health checks
- **🌐 Angular Frontend**: `ticket-booking-frontend` - Material UI interface with Nginx

#### Development Commands:
```bash
# View container status
docker-compose ps

# Check container logs
docker-compose logs frontend
docker-compose logs api
docker-compose logs db

# Rebuild specific service
docker-compose build frontend
docker-compose up -d frontend

# Stop all services
docker-compose down

# Reset with fresh database
docker-compose down -v
docker-compose up -d
```

### 🏗️ Local Development (Alternative)

For traditional local development without containers:

```bash
# Setup backend services
dotnet restore
dotnet build

# Setup frontend
cd src/frontend/ticket-booking-system
npm install
ng serve

# Database setup
# Use SQL Server or update connection string in appsettings.json
```

### 🌟 Frontend Features Implemented

The Angular 18 frontend includes:
- ✅ **Event Management**: Complete CRUD operations with Material UI
- ✅ **Responsive Design**: Material Design with modern Angular architecture
- ✅ **API Integration**: HTTP client with proper error handling
- ✅ **Component Architecture**: Standalone components with TypeScript strict mode
- ✅ **Production Build**: Multi-stage Docker builds with Nginx optimization
- ✅ **Routing**: SPA routing with nginx fallback configuration

### 📱 Application URL Structure:
```
http://localhost:8080/           # Event listing page
http://localhost:8080/event/{id} # Event details page
http://localhost:8080/api/v1/*   # API proxy through nginx
```

### 🔧 Development Workflow:

#### Frontend Development:
```bash
# Rebuild and restart frontend container
docker-compose build frontend
docker-compose up -d frontend

# Watch frontend logs
docker-compose logs -f frontend
```

#### API Development:
```bash
# Rebuild API container
docker-compose build api
docker-compose up -d api

# Execute commands in API container
docker exec -it eventmanagement-api bash
```

#### Database Management:
```bash
# Connect to SQL Server
docker exec -it ticketing-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd"

# Backup/restore database
docker exec ticketing-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "BACKUP DATABASE..."
```

### 🚀 Deploy to Azure (using Azure CLI)
```bash
az login
az group create --name TicketBookingRG --location eastus
# Deploy containerized services to Azure Container Instances or AKS
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
