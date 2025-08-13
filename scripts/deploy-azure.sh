# Azure deployment script
#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="rg-ticketing-system"
LOCATION="eastus"
ACR_NAME="ticketingacr"
AKS_NAME="ticketing-aks"
PLAN_NAME="plan-ticketing-system"

echo -e "${BLUE}🚀 Azure Deployment Script for Ticketing System${NC}"
echo -e "${YELLOW}This script will deploy your Docker containers to Azure${NC}"

# Function to check if user is logged in
check_azure_login() {
    echo -e "${YELLOW}Checking Azure login status...${NC}"
    az account show > /dev/null 2>&1
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Not logged into Azure. Please run 'az login' first.${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ Azure login verified${NC}"
}

# Function to create Azure Container Registry
create_acr() {
    echo -e "${YELLOW}Creating Azure Container Registry...${NC}"
    az acr create \
        --resource-group $RESOURCE_GROUP \
        --name $ACR_NAME \
        --sku Basic \
        --admin-enabled true
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Azure Container Registry created: $ACR_NAME${NC}"
    else
        echo -e "${RED}❌ Failed to create Azure Container Registry${NC}"
        exit 1
    fi
}

# Function to build and push Docker image
build_and_push() {
    echo -e "${YELLOW}Building and pushing Docker image...${NC}"
    
    # Get ACR login server
    ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "loginServer" --output tsv)
    
    # Login to ACR
    az acr login --name $ACR_NAME
    
    # Build and tag image
    docker build -t $ACR_LOGIN_SERVER/eventmanagement-api:latest ./src/backend/EventManagement
    
    # Push image
    docker push $ACR_LOGIN_SERVER/eventmanagement-api:latest
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Image pushed successfully${NC}"
    else
        echo -e "${RED}❌ Failed to push image${NC}"
        exit 1
    fi
}

# Function to deploy to Azure Container Instances (simple option)
deploy_to_aci() {
    echo -e "${YELLOW}Deploying to Azure Container Instances...${NC}"
    
    ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "loginServer" --output tsv)
    ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "passwords[0].value" --output tsv)
    
    az container create \
        --resource-group $RESOURCE_GROUP \
        --name eventmanagement-api \
        --image $ACR_LOGIN_SERVER/eventmanagement-api:latest \
        --cpu 1 \
        --memory 1 \
        --registry-login-server $ACR_LOGIN_SERVER \
        --registry-username $ACR_NAME \
        --registry-password $ACR_PASSWORD \
        --dns-name-label ticketing-eventapi \
        --ports 80 \
        --environment-variables \
            ASPNETCORE_ENVIRONMENT=Production \
            ConnectionStrings__DefaultConnection="$SQL_CONNECTION_STRING"
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Container deployed to Azure Container Instances${NC}"
        echo -e "${BLUE}🌐 Your API is available at: http://ticketing-eventapi.eastus.azurecontainer.io${NC}"
    else
        echo -e "${RED}❌ Failed to deploy container${NC}"
        exit 1
    fi
}

# Function to deploy to Azure App Service (recommended for learning)
deploy_to_app_service() {
    echo -e "${YELLOW}Deploying to Azure App Service...${NC}"
    
    # Create App Service Plan
    az appservice plan create \
        --name $PLAN_NAME \
        --resource-group $RESOURCE_GROUP \
        --is-linux \
        --sku B1
    
    # Create Web App
    az webapp create \
        --resource-group $RESOURCE_GROUP \
        --plan $PLAN_NAME \
        --name eventmanagement-api \
        --deployment-container-image-name $ACR_LOGIN_SERVER/eventmanagement-api:latest
    
    # Configure container registry credentials
    az webapp config container set \
        --name eventmanagement-api \
        --resource-group $RESOURCE_GROUP \
        --docker-custom-image-name $ACR_LOGIN_SERVER/eventmanagement-api:latest \
        --docker-registry-server-url https://$ACR_LOGIN_SERVER \
        --docker-registry-server-user $ACR_NAME \
        --docker-registry-server-password $(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "passwords[0].value" --output tsv)
    
    # Set application settings
    az webapp config appsettings set \
        --resource-group $RESOURCE_GROUP \
        --name eventmanagement-api \
        --settings \
            ASPNETCORE_ENVIRONMENT=Production \
            ConnectionStrings__DefaultConnection="$SQL_CONNECTION_STRING" \
            WEBSITES_PORT=8080
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ App deployed to Azure App Service${NC}"
        echo -e "${BLUE}🌐 Your API is available at: https://eventmanagement-api.azurewebsites.net${NC}"
    else
        echo -e "${RED}❌ Failed to deploy to App Service${NC}"
        exit 1
    fi
}

# Main execution
echo -e "${BLUE}Choose deployment option:${NC}"
echo -e "${YELLOW}1) Azure Container Instances (simple, good for learning)${NC}"
echo -e "${YELLOW}2) Azure App Service (recommended, more features)${NC}"
echo -e "${YELLOW}3) Azure Kubernetes Service (advanced, production-ready)${NC}"

read -p "Enter your choice (1-3): " choice

check_azure_login

case $choice in
    1)
        echo -e "${GREEN}Deploying to Azure Container Instances...${NC}"
        create_acr
        build_and_push
        deploy_to_aci
        ;;
    2)
        echo -e "${GREEN}Deploying to Azure App Service...${NC}"
        create_acr
        build_and_push
        deploy_to_app_service
        ;;
    3)
        echo -e "${YELLOW}AKS deployment requires additional setup. Please refer to k8s/ directory.${NC}"
        create_acr
        build_and_push
        echo -e "${BLUE}💡 Next: Configure kubectl and apply k8s manifests${NC}"
        ;;
    *)
        echo -e "${RED}Invalid choice. Exiting.${NC}"
        exit 1
        ;;
esac

echo -e "${GREEN}🎉 Deployment completed!${NC}"
