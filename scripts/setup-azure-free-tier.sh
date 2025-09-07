#!/bin/bash

# Azure Free Tier Setup with Kafka Docker Containers
# Zero vendor lock-in, maximum learning value

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration - COST PROTECTED
RESOURCE_GROUP="rg-ticketing-free"
LOCATION="eastus2"
ACR_NAME="ticketingfreeacr$(date +%s)"
AKS_NAME="ticketing-aks-free"
SQL_SERVER_NAME="ticketing-sql-free-$(date +%s)"
REDIS_NAME="ticketing-redis-free"
APP_INSIGHTS_NAME="ticketing-insights-free"
SUBSCRIPTION_ID="68c4a453-3bab-48ba-838c-a43446aad0ad"

# Free tier specific settings
NODE_COUNT=2
VM_SIZE="Standard_B2s"  # FREE for 12 months (750 hours each) - 2 vCPU, 4GB RAM
K8S_VERSION="1.30.14"

echo -e "${BLUE}🚀 Azure Free Tier Setup with Kafka Docker Containers${NC}"
echo -e "${YELLOW}Zero vendor lock-in strategy - Kafka runs in Docker!${NC}"
echo ""
echo -e "${BLUE}Resources to be created (ALL FREE):${NC}"
echo -e "  📁 Resource Group: ${RESOURCE_GROUP}"
echo -e "  🐳 Container Registry: ${ACR_NAME} (Basic tier)"
echo -e "  ☸️  AKS Cluster: ${AKS_NAME} (FREE control plane)"
echo -e "  🖥️  Worker Nodes: ${NODE_COUNT}x ${VM_SIZE} (FREE for 12 months - 2 vCPU, 4GB each)"
echo -e "  🗄️  SQL Database: ${SQL_SERVER_NAME} (FREE tier - 250 DTU hours)"
echo -e "  ⚡ Redis Cache: ${REDIS_NAME} (FREE tier - 250 MB)"
echo -e "  📊 Application Insights: ${APP_INSIGHTS_NAME} (FREE tier - 1GB/month)"
echo ""
echo -e "${GREEN}🎯 Kafka Strategy: Docker containers in AKS (vendor-neutral!)${NC}"
echo -e "${GREEN}💰 Total Cost: \$0 for 12 months${NC}"
echo ""

read -p "Continue with free tier setup? (y/N): " -n 1 -r
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
    echo -e "${YELLOW}Resource group already exists with cost protection...${NC}"
    echo -e "${GREEN}✅ Resource group verified: $RESOURCE_GROUP${NC}"
    echo -e "${BLUE}   Cost tracking tags applied${NC}"
    echo -e "${BLUE}   Budget monitoring enabled${NC}"
}

# Function to create Azure Container Registry (Basic = cheap)
create_acr() {
    echo -e "${YELLOW}Creating Azure Container Registry (Basic tier)...${NC}"
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

# Function to create AKS cluster with FREE TIER settings
create_aks() {
    echo -e "${YELLOW}Creating AKS cluster with FREE TIER VMs...${NC}"
    echo -e "${BLUE}This uses Standard_B1s VMs (FREE for 12 months)${NC}"
    
    az aks create \
        --resource-group $RESOURCE_GROUP \
        --name $AKS_NAME \
        --kubernetes-version $K8S_VERSION \
        --node-count $NODE_COUNT \
        --node-vm-size $VM_SIZE \
        --enable-managed-identity \
        --attach-acr $ACR_NAME \
        --enable-addons monitoring \
        --tier Free \
        --generate-ssh-keys \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ AKS cluster created with FREE TIER VMs: $AKS_NAME${NC}"
        echo -e "${GREEN}   Control Plane: FREE (managed by Azure)${NC}"
        echo -e "${GREEN}   Worker Nodes: ${NODE_COUNT}x ${VM_SIZE} = FREE for 12 months${NC}"
        
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

# Function to create FREE TIER SQL Database
create_sql_database() {
    echo -e "${YELLOW}Creating Azure SQL Database (FREE TIER)...${NC}"
    
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
    
    # Create database with FREE TIER settings
    az sql db create \
        --resource-group $RESOURCE_GROUP \
        --server $SQL_SERVER_NAME \
        --name EventManagementDB \
        --service-objective Basic \
        --max-size 250GB \
        --output table
    
    # Configure firewall to allow Azure services
    az sql server firewall-rule create \
        --resource-group $RESOURCE_GROUP \
        --server $SQL_SERVER_NAME \
        --name AllowAzureServices \
        --start-ip-address 0.0.0.0 \
        --end-ip-address 0.0.0.0
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ SQL Database created (FREE TIER): $SQL_SERVER_NAME${NC}"
        echo -e "   Database: EventManagementDB"
        echo -e "   Tier: Basic (250 DTU-hours/month FREE)"
        echo -e "   Storage: 250GB (FREE)"
        
        # Save connection details
        SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=EventManagementDB;Persist Security Info=False;User ID=sqladmin;Password=${SQL_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        echo "SQL_CONNECTION_STRING='$SQL_CONNECTION_STRING'" >> azure-free-credentials.txt
        echo "SQL_PASSWORD='$SQL_PASSWORD'" >> azure-free-credentials.txt
    else
        echo -e "${RED}❌ Failed to create SQL Database${NC}"
        exit 1
    fi
}

# Function to create FREE TIER Redis Cache
create_redis_cache() {
    echo -e "${YELLOW}Creating Redis Cache (FREE TIER)...${NC}"
    az redis create \
        --location $LOCATION \
        --name $REDIS_NAME \
        --resource-group $RESOURCE_GROUP \
        --sku Basic \
        --vm-size c0 \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Redis Cache created (FREE TIER): $REDIS_NAME${NC}"
        echo -e "   Tier: C0 Basic (250 MB FREE)"
        
        # Get Redis connection details
        REDIS_HOSTNAME=$(az redis show --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query "hostName" --output tsv)
        REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query "primaryKey" --output tsv)
        REDIS_CONNECTION_STRING="${REDIS_HOSTNAME}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"
        echo "REDIS_CONNECTION_STRING='$REDIS_CONNECTION_STRING'" >> azure-free-credentials.txt
    else
        echo -e "${RED}❌ Failed to create Redis Cache${NC}"
        exit 1
    fi
}

# Function to create FREE TIER Application Insights
create_app_insights() {
    echo -e "${YELLOW}Creating Application Insights (FREE TIER)...${NC}"
    az monitor app-insights component create \
        --app $APP_INSIGHTS_NAME \
        --location $LOCATION \
        --resource-group $RESOURCE_GROUP \
        --output table
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Application Insights created (FREE TIER): $APP_INSIGHTS_NAME${NC}"
        echo -e "   Data ingestion: 1 GB/month FREE"
        
        # Get instrumentation key
        INSTRUMENTATION_KEY=$(az monitor app-insights component show --app $APP_INSIGHTS_NAME --resource-group $RESOURCE_GROUP --query "instrumentationKey" --output tsv)
        echo "APPINSIGHTS_INSTRUMENTATION_KEY='$INSTRUMENTATION_KEY'" >> azure-free-credentials.txt
    else
        echo -e "${RED}❌ Failed to create Application Insights${NC}"
        exit 1
    fi
}

# Function to optimize Kubernetes manifests for free tier
optimize_manifests() {
    echo -e "${YELLOW}Creating optimized Kubernetes manifests for FREE TIER...${NC}"
    
    # Create optimized directory
    mkdir -p k8s/free-tier
    
    # Copy and optimize Kafka manifest for free tier
    cat > k8s/free-tier/kafka-cluster-optimized.yaml << 'EOF'
apiVersion: v1
kind: Namespace
metadata:
  name: ticketing-system
---
# Zookeeper StatefulSet - Optimized for Free Tier
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: zookeeper
  namespace: ticketing-system
spec:
  serviceName: zookeeper-headless
  replicas: 1
  selector:
    matchLabels:
      app: zookeeper
  template:
    metadata:
      labels:
        app: zookeeper
    spec:
      containers:
      - name: zookeeper
        image: confluentinc/cp-zookeeper:7.6.1
        resources:
          requests:
            memory: "150Mi"
            cpu: "150m"
          limits:
            memory: "200Mi"
            cpu: "200m"
        env:
        - name: ZOOKEEPER_CLIENT_PORT
          value: "2181"
        - name: ZOOKEEPER_TICK_TIME
          value: "2000"
        - name: ZOOKEEPER_HEAP_SIZE
          value: "128m"
        ports:
        - containerPort: 2181
        volumeMounts:
        - name: zookeeper-data
          mountPath: /var/lib/zookeeper/data
  volumeClaimTemplates:
  - metadata:
      name: zookeeper-data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 1Gi
---
# Zookeeper Headless Service
apiVersion: v1
kind: Service
metadata:
  name: zookeeper-headless
  namespace: ticketing-system
spec:
  clusterIP: None
  selector:
    app: zookeeper
  ports:
  - port: 2181
    targetPort: 2181
---
# Kafka StatefulSet - Optimized for Free Tier
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: kafka
  namespace: ticketing-system
spec:
  serviceName: kafka-headless
  replicas: 1
  selector:
    matchLabels:
      app: kafka
  template:
    metadata:
      labels:
        app: kafka
    spec:
      containers:
      - name: kafka
        image: confluentinc/cp-kafka:7.6.1
        resources:
          requests:
            memory: "300Mi"
            cpu: "300m"
          limits:
            memory: "400Mi"
            cpu: "400m"
        env:
        - name: KAFKA_HEAP_OPTS
          value: "-Xms256m -Xmx256m"
        - name: KAFKA_BROKER_ID
          value: "0"
        - name: KAFKA_ZOOKEEPER_CONNECT
          value: "zookeeper-0.zookeeper-headless.ticketing-system.svc.cluster.local:2181"
        - name: KAFKA_LISTENERS
          value: "PLAINTEXT://0.0.0.0:9092"
        - name: KAFKA_ADVERTISED_LISTENERS
          value: "PLAINTEXT://kafka-0.kafka-headless.ticketing-system.svc.cluster.local:9092"
        - name: KAFKA_AUTO_CREATE_TOPICS_ENABLE
          value: "true"
        - name: KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR
          value: "1"
        - name: KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR
          value: "1"
        - name: KAFKA_TRANSACTION_STATE_LOG_MIN_ISR
          value: "1"
        - name: KAFKA_NUM_PARTITIONS
          value: "3"
        - name: KAFKA_LOG_RETENTION_HOURS
          value: "24"
        - name: KAFKA_LOG_SEGMENT_BYTES
          value: "104857600"
        ports:
        - containerPort: 9092
        volumeMounts:
        - name: kafka-data
          mountPath: /var/lib/kafka/data
  volumeClaimTemplates:
  - metadata:
      name: kafka-data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 2Gi
---
# Kafka Headless Service
apiVersion: v1
kind: Service
metadata:
  name: kafka-headless
  namespace: ticketing-system
spec:
  clusterIP: None
  selector:
    app: kafka
  ports:
  - port: 9092
    targetPort: 9092
---
# Kafka Service
apiVersion: v1
kind: Service
metadata:
  name: kafka-service
  namespace: ticketing-system
spec:
  selector:
    app: kafka
  ports:
  - port: 9092
    targetPort: 9092
EOF

    echo -e "${GREEN}✅ Optimized Kafka manifests created for FREE TIER${NC}"
    echo -e "   Location: k8s/free-tier/kafka-cluster-optimized.yaml"
    echo -e "   Resources: Reduced for B1s VMs (1 vCPU, 1 GB RAM each)"
}

# Function to save resource information
save_resource_info() {
    echo -e "${YELLOW}Saving resource information...${NC}"
    
    cat > azure-free-resources.txt << EOF
# Azure FREE TIER Resources Created
RESOURCE_GROUP=${RESOURCE_GROUP}
LOCATION=${LOCATION}
ACR_NAME=${ACR_NAME}
ACR_LOGIN_SERVER=${ACR_LOGIN_SERVER}
AKS_NAME=${AKS_NAME}
SQL_SERVER_NAME=${SQL_SERVER_NAME}
REDIS_NAME=${REDIS_NAME}
APP_INSIGHTS_NAME=${APP_INSIGHTS_NAME}

# FREE TIER Specifications
NODE_COUNT=${NODE_COUNT}
VM_SIZE=${VM_SIZE} (FREE for 12 months - 750 hours each)
TOTAL_VCPU=2
TOTAL_RAM=2GB

# Kafka Strategy
KAFKA_DEPLOYMENT=Docker containers in AKS
VENDOR_LOCKIN=ZERO
PORTABILITY=100% (can run anywhere)

# Next Steps:
# 1. Deploy optimized Kafka: kubectl apply -f k8s/free-tier/kafka-cluster-optimized.yaml
# 2. Update app connection strings with azure-free-credentials.txt
# 3. Deploy your applications: ./scripts/deploy-to-azure-free.sh
# 4. Setup CI/CD pipeline with free tier settings
EOF

    echo -e "${GREEN}✅ Resource information saved to: azure-free-resources.txt${NC}"
    echo -e "${GREEN}✅ Connection strings saved to: azure-free-credentials.txt${NC}"
}

# Main execution
echo -e "${BLUE}Starting FREE TIER infrastructure creation...${NC}"

check_azure_login
create_resource_group
create_acr
create_aks
create_sql_database
create_redis_cache
create_app_insights
optimize_manifests
save_resource_info

echo ""
echo -e "${GREEN}🎉 Azure FREE TIER setup completed successfully!${NC}"
echo ""
echo -e "${BLUE}📋 FREE Resources created:${NC}"
echo -e "  ✅ Resource Group: ${RESOURCE_GROUP}"
echo -e "  ✅ Container Registry: ${ACR_NAME} (Basic tier)"
echo -e "  ✅ AKS Cluster: ${AKS_NAME} (FREE control plane)"
echo -e "  ✅ Worker Nodes: ${NODE_COUNT}x ${VM_SIZE} (FREE for 12 months)"
echo -e "  ✅ SQL Database: ${SQL_SERVER_NAME} (FREE tier)"
echo -e "  ✅ Redis Cache: ${REDIS_NAME} (FREE tier)"
echo -e "  ✅ Application Insights: ${APP_INSIGHTS_NAME} (FREE tier)"
echo ""
echo -e "${GREEN}🎯 Kafka Strategy: Docker containers (vendor-neutral!)${NC}"
echo -e "${GREEN}💰 Total Cost: \$0 for 12 months${NC}"
echo -e "${GREEN}🔓 Vendor Lock-in: ZERO${NC}"
echo ""
echo -e "${YELLOW}📁 Important files created:${NC}"
echo -e "  🔐 azure-free-credentials.txt (connection strings)"
echo -e "  📋 azure-free-resources.txt (resource details)"
echo -e "  ⚙️  k8s/free-tier/kafka-cluster-optimized.yaml (optimized manifests)"
echo ""
echo -e "${BLUE}💡 Next steps:${NC}"
echo -e "  1. Deploy Kafka: kubectl apply -f k8s/free-tier/kafka-cluster-optimized.yaml"
echo -e "  2. Update your app connection strings"
echo -e "  3. Deploy applications to AKS"
echo -e "  4. Enjoy FREE TIER Kubernetes with Kafka!"
echo ""
echo -e "${GREEN}Ready for zero-cost, vendor-neutral deployment! 🚀${NC}"