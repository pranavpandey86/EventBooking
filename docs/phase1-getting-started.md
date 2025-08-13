# Phase 1: Getting Started Guide - First Steps

## 🎯 Phase 1 Overview
**Goal**: Set up your development environment and create the foundation with 100% free Azure services.

**Timeline**: 2-4 weeks  
**Cost**: $0 (100% free tier)

---

## 📋 Step 1: Azure Account & Initial Setup (Day 1)

### 1.1 Create Azure Free Account
```bash
# Go to: https://azure.microsoft.com/en-us/free/
# Sign up with your Microsoft account
# Verify your identity (credit card required but won't be charged)
# You get $200 credit + free services for 12 months
```

### 1.2 Install Required Tools
```bash
# Install Azure CLI
brew install azure-cli

# Install .NET 9 SDK
brew install --cask dotnet-sdk

# Install Node.js and Angular CLI
brew install node
npm install -g @angular/cli

# Install Docker Desktop (optional for local development)
brew install --cask docker

# Verify installations
az --version
dotnet --version
node --version
ng version
```

### 1.3 Login and Setup Azure CLI
```bash
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account list --output table
az account set --subscription "Your-Subscription-ID"

# Verify you're logged in
az account show
```

---

## 🏗️ Step 2: Create Azure Resources (Day 1-2)

### 2.1 Create Resource Group
```bash
# Create main resource group
az group create \
  --name TicketBookingRG \
  --location eastus

# Verify creation
az group show --name TicketBookingRG
```

### 2.2 Create SQL Database (Free Tier)
```bash
# Create SQL Server
az sql server create \
  --name ticketbooking-sql-server \
  --resource-group TicketBookingRG \
  --location eastus \
  --admin-user sqladmin \
  --admin-password "YourComplexPassword123!"

# Create SQL Database (Free tier)
az sql db create \
  --resource-group TicketBookingRG \
  --server ticketbooking-sql-server \
  --name EventsDB \
  --edition Basic \
  --service-objective Basic

# Configure firewall (allow your IP)
az sql server firewall-rule create \
  --resource-group TicketBookingRG \
  --server ticketbooking-sql-server \
  --name AllowYourIP \
  --start-ip-address "YOUR_IP" \
  --end-ip-address "YOUR_IP"

# Allow Azure services
az sql server firewall-rule create \
  --resource-group TicketBookingRG \
  --server ticketbooking-sql-server \
  --name AllowAzureIps \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 2.3 Create Cosmos DB (Free Tier)
```bash
# Create Cosmos DB account (Free tier)
az cosmosdb create \
  --name ticketbooking-cosmos \
  --resource-group TicketBookingRG \
  --kind GlobalDocumentDB \
  --enable-free-tier true \
  --locations regionName=eastus

# Create database
az cosmosdb sql database create \
  --account-name ticketbooking-cosmos \
  --resource-group TicketBookingRG \
  --name TicketInventoryDB

# Create container
az cosmosdb sql container create \
  --account-name ticketbooking-cosmos \
  --resource-group TicketBookingRG \
  --database-name TicketInventoryDB \
  --name TicketInventory \
  --partition-key-path "/eventId" \
  --throughput 1000
```

### 2.4 Create Service Bus (Free Tier)
```bash
# Create Service Bus namespace
az servicebus namespace create \
  --resource-group TicketBookingRG \
  --name ticketbooking-servicebus \
  --location eastus \
  --sku Basic

# Create topics
az servicebus topic create \
  --resource-group TicketBookingRG \
  --namespace-name ticketbooking-servicebus \
  --name events

az servicebus topic create \
  --resource-group TicketBookingRG \
  --namespace-name ticketbooking-servicebus \
  --name tickets

az servicebus topic create \
  --resource-group TicketBookingRG \
  --namespace-name ticketbooking-servicebus \
  --name payments
```

### 2.5 Create Storage Account (Free Tier)
```bash
# Create storage account
az storage account create \
  --name ticketbookingstorage \
  --resource-group TicketBookingRG \
  --location eastus \
  --sku Standard_LRS \
  --kind StorageV2

# Create blob container
az storage container create \
  --name documents \
  --account-name ticketbookingstorage
```

---

## 💻 Step 3: Project Structure Setup (Day 2-3)

### 3.1 Create Project Folders
```bash
# Navigate to your workspace
cd /Users/pranavpandey/TicketBookingSystem

# Create complete folder structure
mkdir -p src/backend/{EventManagement,TicketInventory,PaymentProcessing,NotificationService,Shared}
mkdir -p src/frontend
mkdir -p infrastructure/{terraform,kubernetes,docker}
mkdir -p tests/{integration,e2e}

# Verify structure
tree -L 3
```

### 3.2 How Many Solutions You'll Create

You'll create **5 separate .NET solutions** (start with first 2):

#### **Priority 1 - Start Here (Week 1-2):**
1. **EventManagement.sln** (First to build)
   - EventManagement.API
   - EventManagement.Core  
   - EventManagement.Infrastructure
   - EventManagement.Tests

2. **Frontend Angular App** (Build alongside #1)
   - Single Angular application
   - Will consume EventManagement API

#### **Priority 2 - Next Phase (Week 3-4):**
3. **TicketInventory.sln**
   - TicketInventory.API
   - TicketInventory.Core
   - TicketInventory.Infrastructure  
   - TicketInventory.Tests

4. **PaymentProcessing.sln**
   - PaymentProcessing.API
   - PaymentProcessing.Core
   - PaymentProcessing.Infrastructure
   - PaymentProcessing.Tests

#### **Priority 3 - Final Services (Week 5-6):**
5. **NotificationService.sln**
   - NotificationService.API
   - NotificationService.Core
   - NotificationService.Infrastructure
   - NotificationService.Tests

6. **Shared.sln** (Supporting libraries)
   - Shared.Common
   - Shared.EventBus
   - Shared.Authentication

---

## 🚀 Step 4: Start With Event Management Service (Day 3-7)

### 4.1 Create Event Management Solution
```bash
# Navigate to backend folder
cd src/backend/EventManagement

# Create solution and projects
dotnet new sln -n EventManagement

# Create API project
dotnet new webapi -n EventManagement.API
dotnet sln add EventManagement.API

# Create Class Library projects
dotnet new classlib -n EventManagement.Core
dotnet new classlib -n EventManagement.Infrastructure  
dotnet new xunit -n EventManagement.Tests

# Add all projects to solution
dotnet sln add EventManagement.Core
dotnet sln add EventManagement.Infrastructure
dotnet sln add EventManagement.Tests

# Add project references
cd EventManagement.API
dotnet add reference ../EventManagement.Core
dotnet add reference ../EventManagement.Infrastructure

cd ../EventManagement.Infrastructure
dotnet add reference ../EventManagement.Core

cd ../EventManagement.Tests
dotnet add reference ../EventManagement.API
dotnet add reference ../EventManagement.Core
dotnet add reference ../EventManagement.Infrastructure
```

### 4.2 Add Required NuGet Packages
```bash
# In EventManagement.API
cd ../EventManagement.API
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

# In EventManagement.Infrastructure  
cd ../EventManagement.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Azure.Messaging.ServiceBus

# Restore all packages
cd ..
dotnet restore
```

---

## 🌐 Step 5: Create Angular Frontend (Day 4-7)

### 5.1 Create Angular Application
```bash
# Navigate to frontend folder
cd ../../frontend

# Create Angular app
ng new ticket-booking-app \
  --routing=true \
  --style=scss \
  --strict=true \
  --standalone=false

cd ticket-booking-app

# Add Angular Material
ng add @angular/material

# Add required packages
npm install @azure/msal-angular @azure/msal-browser
npm install @ngrx/store @ngrx/effects @ngrx/store-devtools
npm install @angular/material @angular/cdk
```

### 5.2 Generate Feature Modules
```bash
# Generate feature modules
ng generate module features/events --routing
ng generate module features/shared
ng generate module core

# Generate components for events
ng generate component features/events/event-list
ng generate component features/events/event-detail
ng generate component features/shared/header
ng generate component features/shared/footer
```

---

## 🔧 Step 6: Get Connection Strings (Day 1-2)

### 6.1 Get SQL Database Connection String
```bash
# Get SQL connection string
az sql db show-connection-string \
  --client ado.net \
  --name EventsDB \
  --server ticketbooking-sql-server
  
# Result will be like:
# Server=tcp:ticketbooking-sql-server.database.windows.net,1433;Database=EventsDB;User ID=<username>;Password=<password>;Encrypt=true;Connection Timeout=30;
```

### 6.2 Get Cosmos DB Connection String
```bash
# Get Cosmos DB connection string
az cosmosdb keys list \
  --resource-group TicketBookingRG \
  --name ticketbooking-cosmos \
  --type connection-strings

# Get the primary connection string
```

### 6.3 Get Service Bus Connection String
```bash
# Get Service Bus connection string
az servicebus namespace authorization-rule keys list \
  --resource-group TicketBookingRG \
  --namespace-name ticketbooking-servicebus \
  --name RootManageSharedAccessKey
```

---

## ⚙️ Step 7: Configuration Setup (Day 7)

### 7.1 Update appsettings.json (EventManagement.API)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-SQL-Connection-String-Here",
    "ServiceBus": "Your-ServiceBus-Connection-String-Here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 7.2 Update Angular Environment
```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  apiUrlEventManagement: 'https://localhost:5001/api'
};
```

---

## 🧪 Step 8: First Test & Validation (Day 7)

### 8.1 Test Backend
```bash
# Run Event Management API
cd src/backend/EventManagement/EventManagement.API
dotnet run

# Should start on https://localhost:5001
# Visit https://localhost:5001/swagger to see API docs
```

### 8.2 Test Frontend
```bash
# Run Angular app (in new terminal)
cd src/frontend/ticket-booking-app
ng serve

# Should start on http://localhost:4200
# Visit http://localhost:4200 to see the app
```

---

## 📝 Week 1 Checklist

### Day 1-2: Azure Setup
- [ ] Azure free account created
- [ ] Azure CLI installed and configured
- [ ] Resource group created
- [ ] SQL Database created (free tier)
- [ ] Cosmos DB created (free tier)
- [ ] Service Bus created (free tier)
- [ ] Storage account created (free tier)

### Day 3-4: Project Structure
- [ ] Folder structure created
- [ ] EventManagement solution created
- [ ] All projects added with references
- [ ] NuGet packages installed
- [ ] Angular app created and configured

### Day 5-7: Basic Implementation
- [ ] Basic Event entity created
- [ ] Simple CRUD API endpoints
- [ ] Entity Framework DbContext configured
- [ ] Angular service for API calls
- [ ] Basic event list component
- [ ] Both applications running locally

---

## 🎯 Success Criteria for Week 1

✅ **Azure Resources**: All free tier services provisioned and accessible  
✅ **Development Environment**: Both .NET and Angular apps running locally  
✅ **Basic Functionality**: Can create, read, update, delete events via API  
✅ **Frontend Integration**: Angular app can display list of events from API  
✅ **Database Connection**: Entity Framework successfully connecting to Azure SQL  

## 📚 Next Steps After Week 1

Once you complete Week 1:
1. **Week 2**: Add authentication with Azure AD B2C
2. **Week 3**: Create Ticket Inventory service with Cosmos DB
3. **Week 4**: Add Service Bus messaging between services
4. **Week 5**: Create Payment Processing service
5. **Week 6**: Add Notification service and complete integration

**Start with just the Event Management service and Angular frontend - this gives you a solid foundation to build upon!** 🚀
