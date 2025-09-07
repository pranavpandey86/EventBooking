#!/bin/bash

# Azure Deployment Script for TicketBookingSystem
# Deploys to Azure Kubernetes Service (AKS)

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Load Azure resources configuration
if [ -f "azure-resources.txt" ]; then
    source azure-resources.txt
else
    echo -e "${RED}❌ azure-resources.txt not found. Please run setup-azure-infrastructure.sh first.${NC}"
    exit 1
fi

# Load credentials
if [ -f "azure-credentials.txt" ]; then
    source azure-credentials.txt
else
    echo -e "${RED}❌ azure-credentials.txt not found. Please run setup-azure-infrastructure.sh first.${NC}"
    exit 1
fi

echo -e "${BLUE}🚀 Deploying TicketBookingSystem to Azure AKS${NC}"
echo -e "${YELLOW}Target Cluster: ${AKS_NAME}${NC}"
echo -e "${YELLOW}Namespace: ticketing-system${NC}"

# Function to check prerequisites
check_prerequisites() {
    echo -e "${YELLOW}Checking prerequisites...${NC}"
    
    # Check if kubectl is available
    if ! command -v kubectl &> /dev/null; then
        echo -e "${RED}❌ kubectl not found. Please install kubectl.${NC}"
        exit 1
    fi
    
    # Check if docker is available
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}❌ Docker not found. Please install Docker.${NC}"
        exit 1
    fi
    
    # Check Azure CLI
    if ! command -v az &> /dev/null; then
        echo -e "${RED}❌ Azure CLI not found. Please install Azure CLI.${NC}"
        exit 1
    fi
    
    # Verify AKS connection
    kubectl cluster-info &> /dev/null
    if [ $? -ne 0 ]; then
        echo -e "${YELLOW}Getting AKS credentials...${NC}"
        az aks get-credentials --resource-group $RESOURCE_GROUP --name $AKS_NAME --overwrite-existing
    fi
    
    echo -e "${GREEN}✅ Prerequisites verified${NC}"
}

# Function to build and push images
build_and_push_images() {
    echo -e "${YELLOW}Building and pushing Docker images...${NC}"
    
    # Login to ACR
    az acr login --name $ACR_NAME
    
    # Get current timestamp for tagging
    IMAGE_TAG=$(date +%Y%m%d%H%M%S)
    
    echo -e "${BLUE}Building EventManagement API...${NC}"
    docker build -t $ACR_LOGIN_SERVER/eventmanagement-api:$IMAGE_TAG \
                 -t $ACR_LOGIN_SERVER/eventmanagement-api:latest \
                 -f src/backend/EventManagement/EventManagement.API/Dockerfile \
                 src/backend/EventManagement/
    
    echo -e "${BLUE}Building EventSearch API...${NC}"
    docker build -t $ACR_LOGIN_SERVER/eventsearch-api:$IMAGE_TAG \
                 -t $ACR_LOGIN_SERVER/eventsearch-api:latest \
                 -f src/backend/EventSearch/EventSearch.API/Dockerfile \
                 src/backend/EventSearch/
    
    echo -e "${BLUE}Building Frontend...${NC}"
    docker build -t $ACR_LOGIN_SERVER/frontend:$IMAGE_TAG \
                 -t $ACR_LOGIN_SERVER/frontend:latest \
                 -f src/frontend/ticket-booking-system/Dockerfile \
                 src/frontend/ticket-booking-system/
    
    echo -e "${BLUE}Pushing images to ACR...${NC}"
    docker push $ACR_LOGIN_SERVER/eventmanagement-api:$IMAGE_TAG
    docker push $ACR_LOGIN_SERVER/eventmanagement-api:latest
    docker push $ACR_LOGIN_SERVER/eventsearch-api:$IMAGE_TAG
    docker push $ACR_LOGIN_SERVER/eventsearch-api:latest
    docker push $ACR_LOGIN_SERVER/frontend:$IMAGE_TAG
    docker push $ACR_LOGIN_SERVER/frontend:latest
    
    echo -e "${GREEN}✅ Images built and pushed successfully${NC}"
    echo -e "   Tag: ${IMAGE_TAG}"
}

# Function to create Kubernetes secrets
create_secrets() {
    echo -e "${YELLOW}Creating Kubernetes secrets...${NC}"
    
    # Create namespace if it doesn't exist
    kubectl create namespace ticketing-system --dry-run=client -o yaml | kubectl apply -f -
    
    # Create database secret
    kubectl create secret generic database-secret \
        --from-literal=connection-string="$SQL_CONNECTION_STRING" \
        --namespace=ticketing-system \
        --dry-run=client -o yaml | kubectl apply -f -
    
    # Create Cosmos DB secret
    kubectl create secret generic cosmos-secret \
        --from-literal=connection-string="$COSMOS_CONNECTION_STRING" \
        --namespace=ticketing-system \
        --dry-run=client -o yaml | kubectl apply -f -
    
    # Create Redis secret
    kubectl create secret generic redis-secret \
        --from-literal=connection-string="$REDIS_CONNECTION_STRING" \
        --namespace=ticketing-system \
        --dry-run=client -o yaml | kubectl apply -f -
    
    # Create Application Insights secret
    kubectl create secret generic appinsights-secret \
        --from-literal=instrumentation-key="$APPINSIGHTS_INSTRUMENTATION_KEY" \
        --namespace=ticketing-system \
        --dry-run=client -o yaml | kubectl apply -f -
    
    echo -e "${GREEN}✅ Secrets created successfully${NC}"
}

# Function to update manifests with Azure-specific configurations
update_manifests() {
    echo -e "${YELLOW}Updating Kubernetes manifests for Azure...${NC}"
    
    # Create temporary directory for modified manifests
    mkdir -p k8s/azure-temp
    
    # Copy all manifests to temp directory
    cp k8s/*.yaml k8s/azure-temp/
    
    # Update image references in manifests
    sed -i.bak "s|ticketbookingsystem-eventmanagement-api:latest|$ACR_LOGIN_SERVER/eventmanagement-api:latest|g" k8s/azure-temp/eventmanagement-deployment.yaml
    sed -i.bak "s|ticketbookingsystem-eventsearch-api:latest|$ACR_LOGIN_SERVER/eventsearch-api:latest|g" k8s/azure-temp/eventsearch-deployment.yaml
    sed -i.bak "s|ticketbookingsystem-frontend:latest|$ACR_LOGIN_SERVER/frontend:latest|g" k8s/azure-temp/frontend-deployment.yaml
    
    echo -e "${GREEN}✅ Manifests updated for Azure${NC}"
}

# Function to deploy infrastructure
deploy_infrastructure() {
    echo -e "${YELLOW}Deploying infrastructure services...${NC}"
    
    # Deploy namespace
    kubectl apply -f k8s/azure-temp/namespace.yaml
    
    # Note: For Azure, we'll use managed services instead of pods for some components
    echo -e "${BLUE}Using Azure managed services:${NC}"
    echo -e "  🗄️  SQL Database: Azure SQL Database"
    echo -e "  🌐 Cosmos DB: Azure Cosmos DB"
    echo -e "  ⚡ Redis: Azure Cache for Redis"
    
    # Deploy only the services that need to run in Kubernetes
    echo -e "${YELLOW}Deploying Kafka and supporting services...${NC}"
    kubectl apply -f k8s/azure-temp/kafka-cluster.yaml
    kubectl apply -f k8s/azure-temp/elasticsearch.yaml
    
    # Wait for infrastructure to be ready
    echo -e "${YELLOW}Waiting for infrastructure to be ready...${NC}"
    kubectl wait --for=condition=ready pod -l app=kafka -n ticketing-system --timeout=300s || true
    kubectl wait --for=condition=ready pod -l app=elasticsearch -n ticketing-system --timeout=300s || true
    
    echo -e "${GREEN}✅ Infrastructure deployed${NC}"
}

# Function to deploy applications
deploy_applications() {
    echo -e "${YELLOW}Deploying application services...${NC}"
    
    # Deploy applications
    kubectl apply -f k8s/azure-temp/eventmanagement-deployment.yaml
    kubectl apply -f k8s/azure-temp/eventsearch-deployment.yaml
    kubectl apply -f k8s/azure-temp/frontend-deployment.yaml
    
    # Wait for applications to be ready
    echo -e "${YELLOW}Waiting for applications to be ready...${NC}"
    kubectl wait --for=condition=available deployment/eventmanagement-api -n ticketing-system --timeout=300s
    kubectl wait --for=condition=available deployment/eventsearch-api -n ticketing-system --timeout=300s
    kubectl wait --for=condition=available deployment/frontend -n ticketing-system --timeout=300s
    
    echo -e "${GREEN}✅ Applications deployed${NC}"
}

# Function to setup ingress (for external access)
setup_ingress() {
    echo -e "${YELLOW}Setting up ingress for external access...${NC}"
    
    # Install NGINX Ingress Controller
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml
    
    # Wait for ingress controller to be ready
    kubectl wait --namespace ingress-nginx \
        --for=condition=ready pod \
        --selector=app.kubernetes.io/component=controller \
        --timeout=120s
    
    # Create ingress configuration
    cat > k8s/azure-temp/ingress.yaml << EOF
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: ticketing-ingress
  namespace: ticketing-system
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
  - host: ticketing.${LOCATION}.cloudapp.azure.com
    http:
      paths:
      - path: /api/events
        pathType: Prefix
        backend:
          service:
            name: eventmanagement-service
            port:
              number: 80
      - path: /api/search
        pathType: Prefix
        backend:
          service:
            name: eventsearch-service
            port:
              number: 80
      - path: /
        pathType: Prefix
        backend:
          service:
            name: frontend-service
            port:
              number: 80
EOF
    
    kubectl apply -f k8s/azure-temp/ingress.yaml
    
    echo -e "${GREEN}✅ Ingress configured${NC}"
}

# Function to verify deployment
verify_deployment() {
    echo -e "${YELLOW}Verifying deployment...${NC}"
    
    echo -e "${BLUE}📊 Deployment Status:${NC}"
    kubectl get pods -n ticketing-system
    echo ""
    kubectl get services -n ticketing-system
    echo ""
    kubectl get ingress -n ticketing-system
    
    # Get external IP
    EXTERNAL_IP=$(kubectl get service ingress-nginx-controller -n ingress-nginx -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
    
    if [ ! -z "$EXTERNAL_IP" ]; then
        echo -e "${GREEN}🌐 Your application is accessible at:${NC}"
        echo -e "   Frontend: http://${EXTERNAL_IP}/"
        echo -e "   EventManagement API: http://${EXTERNAL_IP}/api/events"
        echo -e "   EventSearch API: http://${EXTERNAL_IP}/api/search"
    else
        echo -e "${YELLOW}⏳ External IP is being assigned. Check again in a few minutes with:${NC}"
        echo -e "   kubectl get service ingress-nginx-controller -n ingress-nginx"
    fi
    
    echo -e "${GREEN}✅ Deployment verification completed${NC}"
}

# Function to cleanup temp files
cleanup() {
    echo -e "${YELLOW}Cleaning up temporary files...${NC}"
    rm -rf k8s/azure-temp
    echo -e "${GREEN}✅ Cleanup completed${NC}"
}

# Main execution
echo -e "${BLUE}Starting Azure deployment...${NC}"

check_prerequisites
build_and_push_images
create_secrets
update_manifests
deploy_infrastructure
deploy_applications
setup_ingress
verify_deployment
cleanup

echo ""
echo -e "${GREEN}🎉 Azure deployment completed successfully!${NC}"
echo ""
echo -e "${BLUE}📋 Deployment Summary:${NC}"
echo -e "  ✅ Images built and pushed to ACR"
echo -e "  ✅ Secrets created in Kubernetes"
echo -e "  ✅ Infrastructure services deployed"
echo -e "  ✅ Application services deployed"
echo -e "  ✅ Ingress configured for external access"
echo ""
echo -e "${YELLOW}💡 Useful commands:${NC}"
echo -e "  📊 Check status: kubectl get pods -n ticketing-system"
echo -e "  📋 View logs: kubectl logs -f deployment/eventmanagement-api -n ticketing-system"
echo -e "  🔄 Port forward: kubectl port-forward svc/eventmanagement-service 8080:80 -n ticketing-system"
echo ""
echo -e "${GREEN}Your application is now running on Azure! 🚀${NC}"