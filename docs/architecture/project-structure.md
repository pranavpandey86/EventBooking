# Project Structure Guide

## 📁 Repository Structure

```
TicketBookingSystem/
├── 📁 src/                           # Source code
│   ├── 📁 backend/                   # .NET Backend Services
│   │   ├── 📁 EventManagement/       # Event Management Service
│   │   │   ├── 📁 EventManagement.API/
│   │   │   ├── 📁 EventManagement.Core/
│   │   │   ├── 📁 EventManagement.Infrastructure/
│   │   │   └── 📁 EventManagement.Tests/
│   │   ├── 📁 TicketInventory/       # Ticket Inventory Service
│   │   │   ├── 📁 TicketInventory.API/
│   │   │   ├── 📁 TicketInventory.Core/
│   │   │   ├── 📁 TicketInventory.Infrastructure/
│   │   │   └── 📁 TicketInventory.Tests/
│   │   ├── 📁 PaymentProcessing/     # Payment Processing Service
│   │   │   ├── 📁 PaymentProcessing.API/
│   │   │   ├── 📁 PaymentProcessing.Core/
│   │   │   ├── 📁 PaymentProcessing.Infrastructure/
│   │   │   └── 📁 PaymentProcessing.Tests/
│   │   ├── 📁 NotificationService/   # Notification Service
│   │   │   ├── 📁 NotificationService.API/
│   │   │   ├── 📁 NotificationService.Core/
│   │   │   ├── 📁 NotificationService.Infrastructure/
│   │   │   └── 📁 NotificationService.Tests/
│   │   └── 📁 Shared/               # Shared Libraries
│   │       ├── 📁 Common/
│   │       ├── 📁 EventBus/
│   │       └── 📁 Authentication/
│   └── 📁 frontend/                  # Angular Frontend
│       ├── 📁 src/
│       │   ├── 📁 app/
│       │   │   ├── 📁 core/         # Singleton services
│       │   │   ├── 📁 shared/       # Shared components
│       │   │   ├── 📁 features/     # Feature modules
│       │   │   │   ├── 📁 events/
│       │   │   │   ├── 📁 tickets/
│       │   │   │   ├── 📁 payments/
│       │   │   │   └── 📁 profile/
│       │   │   └── 📁 layouts/      # App layouts
│       │   ├── 📁 assets/           # Static assets
│       │   └── 📁 environments/     # Environment configs
│       ├── 📄 package.json
│       └── 📄 angular.json
├── 📁 infrastructure/                # Infrastructure as Code
│   ├── 📁 terraform/                # Terraform configurations
│   ├── 📁 kubernetes/               # K8s manifests
│   ├── 📁 helm/                     # Helm charts
│   └── 📁 azure-pipelines/          # CI/CD pipelines
├── 📁 docs/                         # Documentation
│   ├── 📁 architecture/             # Architecture docs
│   ├── 📁 api/                      # API documentation
│   ├── 📁 deployment/               # Deployment guides
│   └── 📁 security/                 # Security guidelines
├── 📁 tests/                        # Integration tests
│   ├── 📁 e2e/                      # End-to-end tests
│   ├── 📁 integration/              # Integration tests
│   └── 📁 performance/              # Performance tests
├── 📄 docker-compose.yml            # Local development
├── 📄 .gitignore
├── 📄 README.md
└── 📄 LICENSE
```

## 🏗️ Service Structure Pattern

Each backend service follows Clean Architecture principles:

### Service Template Structure:
```
ServiceName/
├── 📁 ServiceName.API/              # Web API Layer
│   ├── 📁 Controllers/              # API Controllers
│   ├── 📁 Middleware/               # Custom middleware
│   ├── 📁 Extensions/               # Service extensions
│   ├── 📄 Program.cs                # Application entry point
│   └── 📄 appsettings.json          # Configuration
├── 📁 ServiceName.Core/             # Domain Layer
│   ├── 📁 Entities/                 # Domain entities
│   ├── 📁 Interfaces/               # Repository interfaces
│   ├── 📁 Services/                 # Domain services
│   ├── 📁 Events/                   # Domain events
│   └── 📁 DTOs/                     # Data transfer objects
├── 📁 ServiceName.Infrastructure/   # Infrastructure Layer
│   ├── 📁 Data/                     # Data access
│   │   ├── 📁 Repositories/         # Repository implementations
│   │   ├── 📁 Configurations/       # Entity configurations
│   │   └── 📄 DbContext.cs          # Database context
│   ├── 📁 ExternalServices/         # External integrations
│   ├── 📁 Messaging/                # Message handling
│   └── 📁 Caching/                  # Caching implementations
└── 📁 ServiceName.Tests/            # Unit Tests
    ├── 📁 Controllers/              # Controller tests
    ├── 📁 Services/                 # Service tests
    ├── 📁 Repositories/             # Repository tests
    └── 📁 Integration/              # Integration tests
```

## 📱 Frontend Structure Details

### Angular Feature Module Structure:
```
features/
├── 📁 events/                       # Event Management Feature
│   ├── 📁 components/               # Feature components
│   │   ├── 📁 event-list/
│   │   ├── 📁 event-detail/
│   │   └── 📁 event-search/
│   ├── 📁 services/                 # Feature services
│   ├── 📁 models/                   # TypeScript models
│   ├── 📁 store/                    # NgRx store files
│   │   ├── 📄 events.actions.ts
│   │   ├── 📄 events.effects.ts
│   │   ├── 📄 events.reducer.ts
│   │   └── 📄 events.selectors.ts
│   └── 📄 events.module.ts          # Feature module
```

### Shared Module Structure:
```
shared/
├── 📁 components/                   # Reusable components
│   ├── 📁 loading-spinner/
│   ├── 📁 error-display/
│   └── 📁 pagination/
├── 📁 directives/                   # Custom directives
├── 📁 pipes/                       # Custom pipes
├── 📁 guards/                       # Route guards
├── 📁 interceptors/                 # HTTP interceptors
└── 📁 models/                       # Shared models
```

## 🚀 Development Workflow

### 1. Local Development Setup:
```bash
# Start all services locally
docker-compose up -d

# Backend development
cd src/backend/EventManagement
dotnet run

# Frontend development
cd src/frontend
ng serve
```

### 2. Feature Development Process:
1. **Create feature branch**: `feature/ticket-booking`
2. **Backend first**: Implement API endpoints
3. **Frontend integration**: Create Angular components
4. **Testing**: Unit tests + integration tests
5. **Documentation**: Update API docs
6. **Pull request**: Code review process

### 3. Deployment Process:
1. **Build**: Azure DevOps pipeline builds all services
2. **Test**: Automated testing suite runs
3. **Security**: Security scans and compliance checks
4. **Deploy**: Blue-green deployment to AKS
5. **Monitor**: Application insights and alerts

## 📦 Package Management

### Backend (.NET):
- **Package management**: NuGet packages
- **Configuration**: `Directory.Build.props` for common settings
- **Versioning**: Semantic versioning with GitVersion

### Frontend (Angular):
- **Package management**: npm with package-lock.json
- **Configuration**: Workspace configuration in `angular.json`
- **Versioning**: Aligned with backend versioning

## 🔧 Configuration Management

### Environment-Specific Configs:
```
Configuration Sources (Priority Order):
1. Azure Key Vault (Production secrets)
2. Environment Variables (Docker/K8s)
3. appsettings.{Environment}.json
4. appsettings.json (Base configuration)
5. User secrets (Development only)
```

### Configuration Categories:
- **Database**: Connection strings, retry policies
- **Azure Services**: Service Bus, Storage, Key Vault
- **Authentication**: Azure AD B2C settings
- **Logging**: Application Insights, Serilog
- **Feature Flags**: Azure App Configuration

This structure ensures maintainability, scalability, and follows industry best practices for enterprise applications.
