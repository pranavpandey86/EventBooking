#!/bin/bash

# Complete Azure Setup - Simple Commands
# Avoids quote issues

set -e

echo "🔄 Completing Azure setup with cost protection..."

# Set variables without quotes
SQL_SERVER=ticketing-sql-free-$(date +%s)
REDIS_NAME=ticketing-redis-free
APP_INSIGHTS=ticketing-insights-free

echo "Creating SQL Database server: $SQL_SERVER"
az sql server create \
  --name $SQL_SERVER \
  --resource-group rg-ticketing-free \
  --location eastus2 \
  --admin-user sqladmin \
  --admin-password 'SecurePass123!' \
  --output table

echo "Creating SQL Database..."
az sql db create \
  --resource-group rg-ticketing-free \
  --server $SQL_SERVER \
  --name EventManagementDB \
  --service-objective Basic \
  --max-size 250GB \
  --output table

echo "Creating firewall rule for Azure services..."
az sql server firewall-rule create \
  --resource-group rg-ticketing-free \
  --server $SQL_SERVER \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

echo "Creating Redis Cache (FREE tier)..."
az redis create \
  --location eastus2 \
  --name $REDIS_NAME \
  --resource-group rg-ticketing-free \
  --sku Basic \
  --vm-size c0 \
  --output table

echo "Creating Application Insights (FREE tier)..."
az monitor app-insights component create \
  --app $APP_INSIGHTS \
  --location eastus2 \
  --resource-group rg-ticketing-free \
  --output table

echo "✅ Azure setup completed!"
echo "📄 Saving connection strings..."

# Get connection strings
SQL_CONNECTION="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=EventManagementDB;Persist Security Info=False;User ID=sqladmin;Password=SecurePass123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

REDIS_HOSTNAME=$(az redis show --name $REDIS_NAME --resource-group rg-ticketing-free --query "hostName" --output tsv)
REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group rg-ticketing-free --query "primaryKey" --output tsv)
REDIS_CONNECTION="${REDIS_HOSTNAME}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"

INSTRUMENTATION_KEY=$(az monitor app-insights component show --app $APP_INSIGHTS --resource-group rg-ticketing-free --query "instrumentationKey" --output tsv)

# Save to file
cat > azure-connections.txt << EOF
# Azure FREE TIER Connection Strings
SQL_CONNECTION_STRING='$SQL_CONNECTION'
REDIS_CONNECTION_STRING='$REDIS_CONNECTION'
APPINSIGHTS_INSTRUMENTATION_KEY='$INSTRUMENTATION_KEY'
SQL_SERVER_NAME='$SQL_SERVER'
REDIS_NAME='$REDIS_NAME'
APP_INSIGHTS_NAME='$APP_INSIGHTS'
EOF

echo "✅ Connection strings saved to azure-connections.txt"
echo "🎉 Azure FREE TIER setup completed successfully!"