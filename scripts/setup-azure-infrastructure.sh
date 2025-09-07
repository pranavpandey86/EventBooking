#!/bin/bash

# Azure Infrastructure Setup Script for TicketBookingSystem
# This script creates all Azure resources needed for the application

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="rg-ticketing-system"
LOCATION="eastus"
ACR_NAME="ticketingacr$(date +%s)"  # Add timestamp to ensure uniqueness
AKS_NAME="ticketing-aks"
SQL_SERVER_NAME="ticketing-sql-$(date +%s)"
COSMOS_ACCOUNT_NAME="ticketing-cosmos-$(date +%s)"
REDIS_NAME="ticketing-redis"
APP_INSIGHTS_NAME="ticketing-insights"
STORAGE_ACCOUNT_NAME="ticketingstorage$(date +%s)"

# Kubernetes configuration
K8S_VERSION="1.28.9"
NODE_COUNT=3
VM_SIZE="Standard_B2s"  # Cost-effective for learning

echo -e "${BLUE}🚀 Azure Infrastructure Setup for Ticketing System${NC}"
echo -e "${YELLOW}This will create all Azure resources needed for your application${NC}"
echo ""
echo -e "${BLUE}Resources to be created:${NC}"
echo -e "  📁 Resource Group: ${RESOURCE_GROUP}"
echo -e "  🐳 Container Registry: ${ACR_NAME}"
echo -e "  ☸️  AKS Cluster: ${AKS_NAME}"
echo -e "  🗄️  SQL Database: ${SQL_SERVER_NAME}"
echo -e "  🌐 Cosmos DB: ${COSMOS_ACCOUNT_NAME}"
echo -e "  ⚡ Redis Cache: ${REDIS_NAME}"
echo -e "  📊 Application Insights: ${APP_INSIGHTS_NAME}"
echo ""

read -p "Continue with infrastructure creation? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Setup cancelled${NC}"
    exit 0
fi

# Function to check if user is logged in
check_azure_login() {
    echo -e "${YELLOW}Checking Azure login status...${NC}"
    az account show > /dev/null 2>&1
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Not logged into Azure. Please run 'az login' first.${NC}"
        exit 1
    fi
    
    SUBSCRIPTION_ID=$(az account show --query id --output tsv)
    SUBSCRIPTION_NAME=$(az account show --query name --output tsv)
    echo -e "${GREEN}✅ Logged into Azure${NC}"
    echo -e "   Subscription: ${SUBSCRIPTION_NAME} (${SUBSCRIPTION_ID})"
}

# Function to create resource group
create_resource_group() {
    echo -e "${YELLOW}Creating resource group...${NC}"
    az group create \
        --name $RESOURCE_GROUP \
        --location $LOCATION \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Resource group created: $RESOURCE_GROUP${NC}"
    else
        echo -e "${RED}❌ Failed to create resource group${NC}"
        exit 1
    fi
}

# Function to create Azure Container Registry
create_acr() {
    echo -e "${YELLOW}Creating Azure Container Registry...${NC}"
    az acr create \
        --resource-group $RESOURCE_GROUP \
        --name $ACR_NAME \
        --sku Basic \
        --admin-enabled true \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Azure Container Registry created: $ACR_NAME${NC}"
        
        # Get login server
        ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "loginServer" --output tsv)
        echo -e "   Login Server: ${ACR_LOGIN_SERVER}"
    else
        echo -e "${RED}❌ Failed to create Azure Container Registry${NC}"
        exit 1
    fi
}

# Function to create AKS cluster
create_aks() {
    echo -e "${YELLOW}Creating AKS cluster (this may take 10-15 minutes)...${NC}"
    az aks create \
        --resource-group $RESOURCE_GROUP \
        --name $AKS_NAME \
        --kubernetes-version $K8S_VERSION \
        --node-count $NODE_COUNT \
        --node-vm-size $VM_SIZE \
        --enable-managed-identity \
        --attach-acr $ACR_NAME \
        --enable-addons monitoring \
        --generate-ssh-keys \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ AKS cluster created: $AKS_NAME${NC}"
        
        # Get AKS credentials
        echo -e "${YELLOW}Getting AKS credentials...${NC}"
        az aks get-credentials --resource-group $RESOURCE_GROUP --name $AKS_NAME --overwrite-existing
        
        # Verify connection
        kubectl get nodes
        echo -e "${GREEN}✅ kubectl configured for AKS${NC}"
    else
        echo -e "${RED}❌ Failed to create AKS cluster${NC}"
        exit 1
    fi
}

# Function to create SQL Database
create_sql_database() {
    echo -e "${YELLOW}Creating Azure SQL Database...${NC}"
    
    # Generate random password
    SQL_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
    
    # Create SQL Server
    az sql server create \
        --name $SQL_SERVER_NAME \
        --resource-group $RESOURCE_GROUP \
        --location $LOCATION \
        --admin-user sqladmin \
        --admin-password $SQL_PASSWORD \
        --output table
    
    # Create database
    az sql db create \
        --resource-group $RESOURCE_GROUP \
        --server $SQL_SERVER_NAME \
        --name EventManagementDB \
        --service-objective Basic \
        --output table
    
    # Configure firewall to allow Azure services
    az sql server firewall-rule create \
        --resource-group $RESOURCE_GROUP \
        --server $SQL_SERVER_NAME \
        --name AllowAzureServices \
        --start-ip-address 0.0.0.0 \
        --end-ip-address 0.0.0.0
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ SQL Database created: $SQL_SERVER_NAME${NC}"
        echo -e "   Database: EventManagementDB"
        echo -e "   Admin User: sqladmin"
        echo -e "   🔐 Password saved to: ./azure-credentials.txt"
        
        # Save connection details
        SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=EventManagementDB;Persist Security Info=False;User ID=sqladmin;Password=${SQL_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        echo "SQL_CONNECTION_STRING='$SQL_CONNECTION_STRING'" >> azure-credentials.txt
    else
        echo -e "${RED}❌ Failed to create SQL Database${NC}"
        exit 1
    fi
}

# Function to create Cosmos DB
create_cosmos_db() {
    echo -e "${YELLOW}Creating Cosmos DB account...${NC}"
    az cosmosdb create \
        --name $COSMOS_ACCOUNT_NAME \
        --resource-group $RESOURCE_GROUP \
        --kind GlobalDocumentDB \
        --locations regionName=$LOCATION failoverPriority=0 isZoneRedundant=False \
        --default-consistency-level Session \
        --enable-free-tier true \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Cosmos DB created: $COSMOS_ACCOUNT_NAME${NC}"
        
        # Get connection string
        COSMOS_CONNECTION_STRING=$(az cosmosdb keys list --name $COSMOS_ACCOUNT_NAME --resource-group $RESOURCE_GROUP --type connection-strings --query "connectionStrings[0].connectionString" --output tsv)
        echo "COSMOS_CONNECTION_STRING='$COSMOS_CONNECTION_STRING'" >> azure-credentials.txt
    else
        echo -e "${RED}❌ Failed to create Cosmos DB${NC}"
        exit 1
    fi
}

# Function to create Redis Cache
create_redis_cache() {
    echo -e "${YELLOW}Creating Redis Cache...${NC}"
    az redis create \
        --location $LOCATION \
        --name $REDIS_NAME \
        --resource-group $RESOURCE_GROUP \
        --sku Basic \
        --vm-size c0 \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Redis Cache created: $REDIS_NAME${NC}"
        
        # Get Redis connection details
        REDIS_HOSTNAME=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query "hostName" --output tsv)
        REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query "primaryKey" --output tsv)
        REDIS_CONNECTION_STRING="${REDIS_HOSTNAME}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"
        echo "REDIS_CONNECTION_STRING='$REDIS_CONNECTION_STRING'" >> azure-credentials.txt
    else
        echo -e "${RED}❌ Failed to create Redis Cache${NC}"
        exit 1
    fi
}

# Function to create Application Insights
create_app_insights() {
    echo -e "${YELLOW}Creating Application Insights...${NC}"
    az monitor app-insights component create \
        --app $APP_INSIGHTS_NAME \
        --location $LOCATION \
        --resource-group $RESOURCE_GROUP \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Application Insights created: $APP_INSIGHTS_NAME${NC}"
        
        # Get instrumentation key
        INSTRUMENTATION_KEY=$(az monitor app-insights component show --app $APP_INSIGHTS_NAME --resource-group $RESOURCE_GROUP --query "instrumentationKey" --output tsv)
        echo "APPINSIGHTS_INSTRUMENTATION_KEY='$INSTRUMENTATION_KEY'" >> azure-credentials.txt
    else
        echo -e "${RED}❌ Failed to create Application Insights${NC}"
        exit 1
    fi
}

# Function to create storage account
create_storage_account() {
    echo -e "${YELLOW}Creating Storage Account...${NC}"
    az storage account create \
        --name $STORAGE_ACCOUNT_NAME \
        --resource-group $RESOURCE_GROUP \
        --location $LOCATION \
        --sku Standard_LRS \
        --kind StorageV2 \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Storage Account created: $STORAGE_ACCOUNT_NAME${NC}"
        
        # Get connection string
        STORAGE_CONNECTION_STRING=$(az storage account show-connection-string --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP --query "connectionString" --output tsv)
        echo "STORAGE_CONNECTION_STRING='$STORAGE_CONNECTION_STRING'" >> azure-credentials.txt
    else
        echo -e "${RED}❌ Failed to create Storage Account${NC}"
        exit 1
    fi
}

# Function to save resource information
save_resource_info() {
    echo -e "${YELLOW}Saving resource information...${NC}"
    
    cat > azure-resources.txt << EOF
# Azure Resources Created
RESOURCE_GROUP=${RESOURCE_GROUP}
LOCATION=${LOCATION}
ACR_NAME=${ACR_NAME}
ACR_LOGIN_SERVER=${ACR_LOGIN_SERVER}
AKS_NAME=${AKS_NAME}
SQL_SERVER_NAME=${SQL_SERVER_NAME}
COSMOS_ACCOUNT_NAME=${COSMOS_ACCOUNT_NAME}
REDIS_NAME=${REDIS_NAME}
APP_INSIGHTS_NAME=${APP_INSIGHTS_NAME}
STORAGE_ACCOUNT_NAME=${STORAGE_ACCOUNT_NAME}

# Next Steps:
# 1. Update your Kubernetes manifests with the new connection strings
# 2. Set up Azure DevOps pipeline with these resource names
# 3. Deploy your application using: ./scripts/deploy-to-azure.sh
EOF

    echo -e "${GREEN}✅ Resource information saved to: azure-resources.txt${NC}"
    echo -e "${GREEN}✅ Connection strings saved to: azure-credentials.txt${NC}"
}

# Main execution
echo -e "${BLUE}Starting infrastructure creation...${NC}"

check_azure_login
create_resource_group
create_acr
create_aks
create_sql_database
create_cosmos_db
create_redis_cache
create_app_insights
create_storage_account
save_resource_info

echo ""
echo -e "${GREEN}🎉 Infrastructure setup completed successfully!${NC}"
echo ""
echo -e "${BLUE}📋 Resources created:${NC}"
echo -e "  ✅ Resource Group: ${RESOURCE_GROUP}"
echo -e "  ✅ Container Registry: ${ACR_NAME}"
echo -e "  ✅ AKS Cluster: ${AKS_NAME} (${NODE_COUNT} nodes)"
echo -e "  ✅ SQL Database: ${SQL_SERVER_NAME}"
echo -e "  ✅ Cosmos DB: ${COSMOS_ACCOUNT_NAME}"
echo -e "  ✅ Redis Cache: ${REDIS_NAME}"
echo -e "  ✅ Application Insights: ${APP_INSIGHTS_NAME}"
echo -e "  ✅ Storage Account: ${STORAGE_ACCOUNT_NAME}"
echo ""
echo -e "${YELLOW}📁 Important files created:${NC}"
echo -e "  🔐 azure-credentials.txt (connection strings)"
echo -e "  📋 azure-resources.txt (resource names)"
echo ""
echo -e "${BLUE}💡 Next steps:${NC}"
echo -e "  1. Review the credentials file (keep it secure!)"
echo -e "  2. Update your K8s manifests with new connection strings"
echo -e "  3. Set up Azure DevOps pipeline"
echo -e "  4. Deploy your application"
echo ""
echo -e "${GREEN}Ready for Azure deployment! 🚀${NC}"