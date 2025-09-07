#!/bin/bash

# 🔐 Azure Secrets Retrieval Script
# This script safely retrieves all connection strings from Azure without storing them in git

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔐 Retrieving Azure secrets safely...${NC}"

# Resource configuration
RESOURCE_GROUP="rg-ticketing-free"
SQL_SERVER="ticketing-sql-free-1757240316"
ACR_NAME="ticketingfreeacr1757240088"
AKS_NAME="ticketing-aks-free"
REDIS_NAME="ticketing-redis-free"
APPINSIGHTS_NAME="ticketing-insights-free"

# Create secrets directory (this will be in .gitignore)
mkdir -p .secrets
echo "# This directory contains sensitive data - DO NOT COMMIT" > .secrets/README.md

echo -e "${GREEN}📊 Retrieving SQL Database connection string...${NC}"
SQL_CONNECTION_STRING=$(az sql db show-connection-string \
  --server $SQL_SERVER \
  --name EventManagementDB \
  --client ado.net \
  --auth-type SqlPassword \
  --output tsv)

# Replace placeholders with actual values
SQL_CONNECTION_STRING=${SQL_CONNECTION_STRING//<username>/sqladmin}
SQL_CONNECTION_STRING=${SQL_CONNECTION_STRING//<password>/SecurePass123!}

echo -e "${GREEN}🗂️ Retrieving Container Registry login server...${NC}"
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "loginServer" -o tsv)

echo -e "${GREEN}📈 Retrieving Application Insights instrumentation key...${NC}"
APPINSIGHTS_KEY=$(az monitor app-insights component show \
  --app $APPINSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "instrumentationKey" -o tsv)

echo -e "${GREEN}🚀 Retrieving AKS cluster details...${NC}"
AKS_RESOURCE_GROUP=$RESOURCE_GROUP
AKS_CLUSTER_NAME=$AKS_NAME

# Check if Redis exists (it might be deleted for cost savings)
echo -e "${GREEN}🔄 Checking Redis cache status...${NC}"
REDIS_EXISTS=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query "name" -o tsv 2>/dev/null || echo "NOT_FOUND")

if [ "$REDIS_EXISTS" != "NOT_FOUND" ]; then
    echo -e "${GREEN}📦 Retrieving Redis connection string...${NC}"
    REDIS_PRIMARY_KEY=$(az redis list-keys \
      --name $REDIS_NAME \
      --resource-group $RESOURCE_GROUP \
      --query "primaryKey" -o tsv 2>/dev/null || echo "KEY_NOT_FOUND")
    
    if [ "$REDIS_PRIMARY_KEY" != "KEY_NOT_FOUND" ]; then
        REDIS_HOST=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query "hostName" -o tsv)
        REDIS_PORT=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query "sslPort" -o tsv)
        REDIS_CONNECTION_STRING="${REDIS_HOST}:${REDIS_PORT},password=${REDIS_PRIMARY_KEY},ssl=True,abortConnect=False"
    else
        REDIS_CONNECTION_STRING="REDIS_ACCESS_DENIED"
    fi
else
    echo -e "${YELLOW}⚠️ Redis cache not found (deleted for cost savings)${NC}"
    REDIS_CONNECTION_STRING="REDIS_NOT_AVAILABLE"
fi

# Write to local secrets file (NOT committed to git)
cat > .secrets/azure-config.env << EOF
# 🔐 LIVE Azure Configuration - DO NOT COMMIT TO GIT
# Generated on: $(date)

# SQL Database
export SQL_CONNECTION_STRING="$SQL_CONNECTION_STRING"

# Container Registry
export ACR_LOGIN_SERVER="$ACR_LOGIN_SERVER"

# Application Insights
export APPINSIGHTS_INSTRUMENTATION_KEY="$APPINSIGHTS_KEY"

# AKS Cluster
export AKS_CLUSTER_NAME="$AKS_CLUSTER_NAME"
export AKS_RESOURCE_GROUP="$AKS_RESOURCE_GROUP"

# Redis Cache
export REDIS_CONNECTION_STRING="$REDIS_CONNECTION_STRING"

# Kubernetes Secrets (for deployment)
export SQL_CONNECTION_B64=$(echo -n "$SQL_CONNECTION_STRING" | base64)
export APPINSIGHTS_KEY_B64=$(echo -n "$APPINSIGHTS_KEY" | base64)
export REDIS_CONNECTION_B64=$(echo -n "$REDIS_CONNECTION_STRING" | base64)
EOF

# Create Kubernetes secrets YAML (also not committed)
cat > .secrets/k8s-secrets.yaml << EOF
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
  namespace: ticketing-system
type: Opaque
data:
  sql-connection-string: $(echo -n "$SQL_CONNECTION_STRING" | base64)
  appinsights-key: $(echo -n "$APPINSIGHTS_KEY" | base64)
  redis-connection-string: $(echo -n "$REDIS_CONNECTION_STRING" | base64)
---
apiVersion: v1
kind: Secret
metadata:
  name: acr-secret
  namespace: ticketing-system
type: kubernetes.io/dockerconfigjson
data:
  .dockerconfigjson: $(kubectl create secret docker-registry temp-secret --docker-server=$ACR_LOGIN_SERVER --docker-username=$ACR_NAME --docker-password=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv) --dry-run=client -o jsonpath='{.data.\.dockerconfigjson}')
EOF

echo -e "${GREEN}✅ Secrets retrieved and stored in .secrets/ directory${NC}"
echo -e "${BLUE}📁 Files created:${NC}"
echo -e "  • .secrets/azure-config.env (environment variables)"
echo -e "  • .secrets/k8s-secrets.yaml (Kubernetes secrets)"
echo -e ""
echo -e "${YELLOW}🔒 SECURITY NOTES:${NC}"
echo -e "  • .secrets/ directory is in .gitignore"
echo -e "  • Never commit these files to git"
echo -e "  • Use: source .secrets/azure-config.env to load variables"
echo -e "  • Use: kubectl apply -f .secrets/k8s-secrets.yaml to deploy secrets"
echo -e ""
echo -e "${GREEN}🚀 Ready for deployment!${NC}"