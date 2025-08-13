# Azure Setup Guide for Event Ticketing System

This guide will help you set up the required Azure resources for your Event Ticketing System using the **Azure Free Tier** for learning purposes.

## Prerequisites

1. **Azure Account**: Create a free Azure account at [https://azure.microsoft.com/free/](https://azure.microsoft.com/free/)
2. **Azure CLI**: Install Azure CLI from [https://docs.microsoft.com/en-us/cli/azure/install-azure-cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

## Phase 1: Core Services Setup (Free Tier)

### 1. Login to Azure CLI

```bash
# Login to your Azure account
az login

# Set your subscription (if you have multiple)
az account set --subscription "your-subscription-id"
```

### 2. Create Resource Group

```bash
# Create a resource group in East US region (good free tier availability)
az group create \
  --name "rg-eventtickets-dev" \
  --location "eastus"
```

### 3. Create Azure SQL Database (Free Tier)

```bash
# Create SQL Server
az sql server create \
  --name "sql-eventtickets-dev" \
  --resource-group "rg-eventtickets-dev" \
  --location "eastus" \
  --admin-user "sqladmin" \
  --admin-password "YourSecurePassword123!"

# Configure firewall to allow Azure services
az sql server firewall-rule create \
  --resource-group "rg-eventtickets-dev" \
  --server "sql-eventtickets-dev" \
  --name "AllowAzureServices" \
  --start-ip-address "0.0.0.0" \
  --end-ip-address "0.0.0.0"

# Allow your local IP (replace with your actual IP)
az sql server firewall-rule create \
  --resource-group "rg-eventtickets-dev" \
  --server "sql-eventtickets-dev" \
  --name "AllowLocalIP" \
  --start-ip-address "YOUR_IP_ADDRESS" \
  --end-ip-address "YOUR_IP_ADDRESS"

# Create EventManagement database (Free tier: Basic, 5 DTU, 2GB)
az sql db create \
  --resource-group "rg-eventtickets-dev" \
  --server "sql-eventtickets-dev" \
  --name "EventManagementDB" \
  --service-objective "Basic" \
  --max-size "2GB"

# Create additional databases for other services
az sql db create \
  --resource-group "rg-eventtickets-dev" \
  --server "sql-eventtickets-dev" \
  --name "TicketInventoryDB" \
  --service-objective "Basic" \
  --max-size "2GB"

az sql db create \
  --resource-group "rg-eventtickets-dev" \
  --server "sql-eventtickets-dev" \
  --name "PaymentProcessingDB" \
  --service-objective "Basic" \
  --max-size "2GB"
```

### 4. Create Azure Service Bus (Free Tier)

```bash
# Create Service Bus namespace (Basic tier - 100 connections)
az servicebus namespace create \
  --resource-group "rg-eventtickets-dev" \
  --name "sb-eventtickets-dev" \
  --location "eastus" \
  --sku "Basic"

# Create queues for microservices communication
az servicebus queue create \
  --resource-group "rg-eventtickets-dev" \
  --namespace-name "sb-eventtickets-dev" \
  --name "event-created"

az servicebus queue create \
  --resource-group "rg-eventtickets-dev" \
  --namespace-name "sb-eventtickets-dev" \
  --name "ticket-reserved"

az servicebus queue create \
  --resource-group "rg-eventtickets-dev" \
  --namespace-name "sb-eventtickets-dev" \
  --name "payment-processed"

az servicebus queue create \
  --resource-group "rg-eventtickets-dev" \
  --namespace-name "sb-eventtickets-dev" \
  --name "notification-requests"
```

### 5. Create Azure Cosmos DB (Free Tier)

```bash
# Create Cosmos DB account (Free tier: 1000 RU/s, 25GB)
az cosmosdb create \
  --name "cosmos-eventtickets-dev" \
  --resource-group "rg-eventtickets-dev" \
  --locations regionName="East US" \
  --enable-free-tier true

# Create database for notifications
az cosmosdb sql database create \
  --account-name "cosmos-eventtickets-dev" \
  --resource-group "rg-eventtickets-dev" \
  --name "NotificationDB"

# Create container for notification templates
az cosmosdb sql container create \
  --account-name "cosmos-eventtickets-dev" \
  --resource-group "rg-eventtickets-dev" \
  --database-name "NotificationDB" \
  --name "NotificationTemplates" \
  --partition-key-path "/type" \
  --throughput 400
```

### 6. Create Azure Storage Account (Free Tier)

```bash
# Create storage account for file uploads and static content
az storage account create \
  --name "steventicketsdev" \
  --resource-group "rg-eventtickets-dev" \
  --location "eastus" \
  --sku "Standard_LRS" \
  --kind "StorageV2"

# Create blob containers
az storage container create \
  --name "event-images" \
  --account-name "steventicketsdev" \
  --public-access "blob"

az storage container create \
  --name "ticket-files" \
  --account-name "steventicketsdev" \
  --public-access "off"
```

### 7. Create App Service Plan and Web Apps (Free Tier)

```bash
# Create App Service Plan (Free tier: F1)
az appservice plan create \
  --name "plan-eventtickets-dev" \
  --resource-group "rg-eventtickets-dev" \
  --location "eastus" \
  --sku "F1" \
  --is-linux

# Create web apps for each microservice
az webapp create \
  --resource-group "rg-eventtickets-dev" \
  --plan "plan-eventtickets-dev" \
  --name "app-eventmanagement-dev" \
  --runtime "DOTNETCORE:8.0"

az webapp create \
  --resource-group "rg-eventtickets-dev" \
  --plan "plan-eventtickets-dev" \
  --name "app-ticketinventory-dev" \
  --runtime "DOTNETCORE:8.0"

az webapp create \
  --resource-group "rg-eventtickets-dev" \
  --plan "plan-eventtickets-dev" \
  --name "app-paymentprocessing-dev" \
  --runtime "DOTNETCORE:8.0"

az webapp create \
  --resource-group "rg-eventtickets-dev" \
  --plan "plan-eventtickets-dev" \
  --name "app-notifications-dev" \
  --runtime "DOTNETCORE:8.0"

az webapp create \
  --resource-group "rg-eventtickets-dev" \
  --plan "plan-eventtickets-dev" \
  --name "app-apigateway-dev" \
  --runtime "DOTNETCORE:8.0"

# Create web app for Angular frontend
az webapp create \
  --resource-group "rg-eventtickets-dev" \
  --plan "plan-eventtickets-dev" \
  --name "app-frontend-dev" \
  --runtime "NODE:18-lts"
```

## Connection Strings and Secrets

After creating the resources, you'll need to get the connection strings and configure them in your applications.

### Get SQL Connection String

```bash
# Get SQL connection string
az sql db show-connection-string \
  --name "EventManagementDB" \
  --server "sql-eventtickets-dev" \
  --client "ado.net"
```

### Get Service Bus Connection String

```bash
# Get Service Bus connection string
az servicebus namespace authorization-rule keys list \
  --resource-group "rg-eventtickets-dev" \
  --namespace-name "sb-eventtickets-dev" \
  --name "RootManageSharedAccessKey"
```

### Get Cosmos DB Connection String

```bash
# Get Cosmos DB connection string
az cosmosdb keys list \
  --name "cosmos-eventtickets-dev" \
  --resource-group "rg-eventtickets-dev" \
  --type "connection-strings"
```

### Get Storage Account Connection String

```bash
# Get Storage account connection string
az storage account show-connection-string \
  --name "steventicketsdev" \
  --resource-group "rg-eventtickets-dev"
```

## Local Development Configuration

Update your `appsettings.Development.json` files with the Azure connection strings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql-eventtickets-dev.database.windows.net;Database=EventManagementDB;User ID=sqladmin;Password=YourSecurePassword123!;Encrypt=true;TrustServerCertificate=false;",
    "ServiceBus": "Endpoint=sb://sb-eventtickets-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY",
    "CosmosDB": "AccountEndpoint=https://cosmos-eventtickets-dev.documents.azure.com:443/;AccountKey=YOUR_KEY;",
    "StorageAccount": "DefaultEndpointsProtocol=https;AccountName=steventicketsdev;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
  }
}
```

## Next Steps

1. **Update Connection Strings**: Replace the connection strings in your application configuration files
2. **Deploy First Service**: We'll start by deploying the EventManagement API
3. **Test Connectivity**: Verify the service can connect to Azure SQL Database
4. **Expand Services**: Add the remaining microservices one by one

## Free Tier Limitations to Keep in Mind

- **SQL Database**: Basic tier (5 DTU, 2GB storage) - sufficient for development/learning
- **Service Bus**: Basic tier (100 concurrent connections) - good for low-volume testing
- **Cosmos DB**: 1000 RU/s shared across containers - plan queries efficiently
- **App Service**: F1 tier (1GB RAM, 1GB storage, 60 CPU minutes/day) - suitable for development
- **Storage**: 5GB locally redundant storage with pay-as-you-go pricing

## Cost Monitoring

Set up budget alerts to monitor costs:

```bash
# This requires additional setup in the Azure portal for budget alerts
# Navigate to Cost Management + Billing > Budgets to set up alerts
```

Let me know when you've completed these steps, and I'll help you deploy the EventManagement service to Azure!
