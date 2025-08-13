# Getting Started Guide

## 🚀 Quick Start (5-minute setup)

### Prerequisites Checklist:
- [ ] **Azure Account** with free tier access
- [ ] **.NET 9 SDK** installed
- [ ] **Node.js 18+** and **npm** installed
- [ ] **Angular CLI** installed (`npm install -g @angular/cli`)
- [ ] **Docker Desktop** (optional, for local containers)
- [ ] **Azure CLI** installed
- [ ] **Visual Studio Code** with Azure extensions

### Step 1: Clone and Setup Repository
```bash
# Clone the repository
git clone https://github.com/yourusername/TicketBookingSystem.git
cd TicketBookingSystem

# Create basic folder structure
mkdir -p src/backend src/frontend infrastructure docs tests
mkdir -p src/backend/{EventManagement,TicketInventory,PaymentProcessing,NotificationService,Shared}
mkdir -p docs/{architecture,api,deployment,security}
```

### Step 2: Azure Resource Setup (Free Tier)
```bash
# Login to Azure
az login

# Create resource group
az group create --name TicketBookingRG --location eastus

# Create SQL Database (Free tier)
az sql server create \
  --name ticketbooking-sql-server \
  --resource-group TicketBookingRG \
  --location eastus \
  --admin-user sqladmin \
  --admin-password YourComplexPassword123!

az sql db create \
  --resource-group TicketBookingRG \
  --server ticketbooking-sql-server \
  --name EventsDB \
  --edition Basic

# Create Cosmos DB (Free tier)
az cosmosdb create \
  --name ticketbooking-cosmos \
  --resource-group TicketBookingRG \
  --kind GlobalDocumentDB \
  --enable-free-tier true

# Create Service Bus (Free tier)
az servicebus namespace create \
  --resource-group TicketBookingRG \
  --name ticketbooking-servicebus \
  --location eastus

# Create Storage Account (Free tier)
az storage account create \
  --name ticketbookingstorage \
  --resource-group TicketBookingRG \
  --location eastus \
  --sku Standard_LRS
```

### Step 3: Backend Service Setup
```bash
# Navigate to backend directory
cd src/backend

# Create Event Management Service
dotnet new webapi -n EventManagement.API
cd EventManagement.API

# Add required packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Return to backend root
cd ..
```

### Step 4: Frontend Setup
```bash
# Navigate to frontend directory
cd ../frontend

# Create Angular application
ng new ticket-booking-app --routing --style=scss --strict
cd ticket-booking-app

# Add Angular Material
ng add @angular/material

# Add required packages
npm install @azure/msal-angular @azure/msal-browser
npm install @ngrx/store @ngrx/effects @ngrx/store-devtools
npm install @microsoft/signalr
```

### Step 5: Configuration Files

Create the main configuration files:

#### Backend Configuration (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ticketbooking-sql-server.database.windows.net;Database=EventsDB;User Id=sqladmin;Password=YourComplexPassword123!;",
    "CosmosDb": "AccountEndpoint=https://ticketbooking-cosmos.documents.azure.com:443/;AccountKey=YOUR_COSMOS_KEY;",
    "ServiceBus": "Endpoint=sb://ticketbooking-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_SERVICE_BUS_KEY"
  },
  "AzureAd": {
    "Instance": "https://your-tenant.b2clogin.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### Frontend Configuration (`environment.ts`):
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  azureAd: {
    clientId: 'your-client-id',
    authority: 'https://your-tenant.b2clogin.com/your-tenant.onmicrosoft.com/B2C_1_signin',
    knownAuthorities: ['your-tenant.b2clogin.com'],
    redirectUri: 'http://localhost:4200'
  }
};
```

---

## 🏗️ Development Workflow

### Daily Development Process:

1. **Start Local Development Environment**:
```bash
# Start backend services
cd src/backend/EventManagement.API
dotnet run

# In new terminal - start frontend
cd src/frontend/ticket-booking-app
ng serve

# Optional: Start with Docker Compose
docker-compose up -d
```

2. **Make Changes**:
   - Backend: Edit `.cs` files, Entity Framework migrations
   - Frontend: Edit `.ts`, `.html`, `.scss` files
   - Database: Use Entity Framework migrations

3. **Test Changes**:
```bash
# Backend tests
dotnet test

# Frontend tests
npm test

# Integration tests
npm run e2e
```

4. **Commit and Push**:
```bash
git add .
git commit -m "feat: add user registration feature"
git push origin feature-branch
```

### Database Migrations:
```bash
# Create migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

---

## 🔧 Local Development Setup

### Option 1: Direct Development (Recommended for beginners)

#### Backend Setup:
```bash
cd src/backend/EventManagement.API

# Restore packages
dotnet restore

# Run the service
dotnet run
# Service will be available at: https://localhost:5001
```

#### Frontend Setup:
```bash
cd src/frontend/ticket-booking-app

# Install dependencies
npm install

# Start development server
ng serve
# Application will be available at: http://localhost:4200
```

### Option 2: Docker Development (Advanced)

#### Docker Compose Configuration:
```yaml
version: '3.8'
services:
  eventmanagement-api:
    build: ./src/backend/EventManagement.API
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=EventsDB;User Id=sa;Password=YourStrong!Passw0rd;
    depends_on:
      - sql-server

  frontend:
    build: ./src/frontend
    ports:
      - "4200:80"
    depends_on:
      - eventmanagement-api

  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: YourStrong!Passw0rd
      ACCEPT_EULA: Y
    ports:
      - "1433:1433"
```

#### Run with Docker:
```bash
# Build and start all services
docker-compose up --build

# Stop all services
docker-compose down
```

---

## 🚀 Deployment Guide

### Phase 1: Deploy to Azure Free Tier

#### 1. Frontend Deployment (Azure Static Web Apps):
```bash
# Build for production
ng build --prod

# Deploy using Azure CLI
az staticwebapp create \
  --name ticket-booking-frontend \
  --resource-group TicketBookingRG \
  --source https://github.com/yourusername/TicketBookingSystem \
  --location "East US 2" \
  --branch main \
  --app-location "/src/frontend/ticket-booking-app" \
  --output-location "dist"
```

#### 2. Backend Deployment (Azure App Service):
```bash
# Create App Service Plan (Free tier)
az appservice plan create \
  --name TicketBookingPlan \
  --resource-group TicketBookingRG \
  --sku F1 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group TicketBookingRG \
  --plan TicketBookingPlan \
  --name ticketbooking-eventmanagement \
  --runtime "DOTNETCORE:8.0"

# Deploy from local folder
dotnet publish -c Release
az webapp deployment source config-zip \
  --resource-group TicketBookingRG \
  --name ticketbooking-eventmanagement \
  --src ./bin/Release/net8.0/publish.zip
```

### Phase 2: CI/CD with Azure DevOps

#### Azure Pipeline Configuration (`azure-pipelines.yml`):
```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: BuildBackend
    steps:
    - task: UseDotNet@2
      inputs:
        version: '8.x'
    - script: dotnet build --configuration $(buildConfiguration)
      displayName: 'Build Backend'
    - script: dotnet test --configuration $(buildConfiguration)
      displayName: 'Run Tests'

  - job: BuildFrontend
    steps:
    - task: NodeTool@0
      inputs:
        versionSpec: '18.x'
    - script: npm install
      workingDirectory: 'src/frontend/ticket-booking-app'
    - script: npm run build --prod
      workingDirectory: 'src/frontend/ticket-booking-app'

- stage: Deploy
  dependsOn: Build
  jobs:
  - deployment: DeployToAzure
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'your-subscription'
              appType: 'webApp'
              appName: 'ticketbooking-eventmanagement'
              package: '$(Pipeline.Workspace)/**/*.zip'
```

---

## 📊 Monitoring and Troubleshooting

### Application Insights Setup:
```bash
# Add Application Insights to your app
az monitor app-insights component create \
  --app ticketbooking-insights \
  --location eastus \
  --resource-group TicketBookingRG
```

### Common Issues and Solutions:

#### 1. CORS Issues:
```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://your-frontend-url.azurestaticapps.net")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

app.UseCors("AllowAngularApp");
```

#### 2. Database Connection Issues:
```bash
# Test connection
sqlcmd -S ticketbooking-sql-server.database.windows.net -U sqladmin -P YourComplexPassword123! -d EventsDB -Q "SELECT 1"
```

#### 3. Service Bus Connection Issues:
```csharp
// Add retry policy
services.AddServiceBusClient(connectionString)
    .WithOptions(options =>
    {
        options.RetryOptions.MaxRetries = 3;
        options.RetryOptions.Delay = TimeSpan.FromSeconds(1);
    });
```

### Useful Commands:

#### Debugging:
```bash
# Check app logs
az webapp log tail --name ticketbooking-eventmanagement --resource-group TicketBookingRG

# Check resource usage
az monitor metrics list --resource ticketbooking-eventmanagement --metric "CpuPercentage"

# Database queries
az sql db show-usage --name EventsDB --server ticketbooking-sql-server --resource-group TicketBookingRG
```

---

## 📚 Next Steps

Once your basic setup is complete:

1. **Implement Authentication**: Set up Azure AD B2C
2. **Add Real-time Features**: Implement SignalR for live updates
3. **Performance Optimization**: Add caching with Redis
4. **Security Hardening**: Implement security best practices
5. **Monitoring**: Set up comprehensive logging and monitoring
6. **Testing**: Add comprehensive test suites
7. **Documentation**: Create API documentation with Swagger

### Learning Resources:
- [Azure Free Account](https://azure.microsoft.com/en-us/free/)
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Angular Documentation](https://angular.io/docs)
- [Azure Service Bus](https://docs.microsoft.com/en-us/azure/service-bus-messaging/)
- [Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/)

This guide provides everything you need to get started with your cloud-native event ticketing system using Azure's free tier!
