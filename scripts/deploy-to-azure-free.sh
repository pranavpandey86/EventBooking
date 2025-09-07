#!/bin/bash

# Azure FREE TIER Deployment Script with Kafka Docker Containers
# Zero vendor lock-in, maximum learning value

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Load Azure resources configuration
ACR_LOGIN_SERVER="YOUR-ACR-NAME.azurecr.io"
RESOURCE_GROUP="YOUR-RESOURCE-GROUP"
AKS_NAME="YOUR-AKS-CLUSTER"
SQL_CONNECTION_STRING="Server=tcp:YOUR-SQL-SERVER.database.windows.net,1433;Initial Catalog=EventManagementDB;Persist Security Info=False;User ID=YOUR-USERNAME;Password=YOUR-PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
REDIS_CONNECTION_STRING="YOUR-REDIS-CACHE.redis.cache.windows.net:6380,password=YOUR-REDIS-KEY,ssl=True,abortConnect=False"
APPINSIGHTS_INSTRUMENTATION_KEY="YOUR-APPINSIGHTS-KEY"

echo -e "${BLUE}🚀 Deploying to Azure FREE TIER with Kafka Docker Containers${NC}"
echo -e "${YELLOW}Target: ${AKS_NAME} (2x Standard_B2s - FREE for 12 months)${NC}"
echo -e "${GREEN}Strategy: Kafka in Docker (vendor-neutral!)${NC}"

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

# Function to build and push images to ACR
build_and_push_images() {
    echo -e "${YELLOW}Building and pushing Docker images to ACR...${NC}"
    
    # Login to ACR
    az acr login --name ticketingfreeacr1757240088
    
    # Get current timestamp for tagging
    IMAGE_TAG=$(date +%Y%m%d%H%M%S)
    
    echo -e "${BLUE}Building EventManagement API (optimized for free tier)...${NC}"
    docker build -t $ACR_LOGIN_SERVER/eventmanagement-api:$IMAGE_TAG \
                 -t $ACR_LOGIN_SERVER/eventmanagement-api:latest \
                 -f src/backend/EventManagement/EventManagement.API/Dockerfile \
                 src/backend/EventManagement/
    
    echo -e "${BLUE}Building EventSearch API (optimized for free tier)...${NC}"
    docker build -t $ACR_LOGIN_SERVER/eventsearch-api:$IMAGE_TAG \
                 -t $ACR_LOGIN_SERVER/eventsearch-api:latest \
                 -f src/backend/EventSearch/EventSearch.API/Dockerfile \
                 src/backend/EventSearch/
    
    echo -e "${BLUE}Building Frontend (optimized for free tier)...${NC}"
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
    echo -e "${YELLOW}Creating Kubernetes secrets for FREE TIER...${NC}"
    
    # Create namespace if it doesn't exist
    kubectl create namespace ticketing-system --dry-run=client -o yaml | kubectl apply -f -
    
    # Create database secret
    kubectl create secret generic database-secret \
        --from-literal=connection-string="$SQL_CONNECTION_STRING" \
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

# Function to update application manifests for free tier
update_manifests_for_free_tier() {
    echo -e "${YELLOW}Creating optimized manifests for FREE TIER...${NC}"
    
    # Create free-tier directory if it doesn't exist
    mkdir -p k8s/free-tier
    
    # Create optimized EventManagement deployment
    cat > k8s/free-tier/eventmanagement-deployment-optimized.yaml << EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: eventmanagement-api
  namespace: ticketing-system
spec:
  replicas: 1  # Single replica for free tier
  selector:
    matchLabels:
      app: eventmanagement-api
  template:
    metadata:
      labels:
        app: eventmanagement-api
    spec:
      containers:
      - name: eventmanagement-api
        image: $ACR_LOGIN_SERVER/eventmanagement-api:latest
        resources:
          requests:
            memory: "200Mi"  # Optimized for B1s VMs
            cpu: "200m"
          limits:
            memory: "300Mi"
            cpu: "300m"
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: database-secret
              key: connection-string
        - name: Kafka__BootstrapServers
          value: "kafka-0.kafka-headless.ticketing-system.svc.cluster.local:9092"
        - name: ApplicationInsights__InstrumentationKey
          valueFrom:
            secretKeyRef:
              name: appinsights-secret
              key: instrumentation-key
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: eventmanagement-service
  namespace: ticketing-system
spec:
  selector:
    app: eventmanagement-api
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
EOF

    # Create optimized EventSearch deployment
    cat > k8s/free-tier/eventsearch-deployment-optimized.yaml << EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: eventsearch-api
  namespace: ticketing-system
spec:
  replicas: 1  # Single replica for free tier
  selector:
    matchLabels:
      app: eventsearch-api
  template:
    metadata:
      labels:
        app: eventsearch-api
    spec:
      containers:
      - name: eventsearch-api
        image: $ACR_LOGIN_SERVER/eventsearch-api:latest
        resources:
          requests:
            memory: "200Mi"  # Optimized for B1s VMs
            cpu: "200m"
          limits:
            memory: "300Mi"
            cpu: "300m"
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        - name: Redis__ConnectionString
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: connection-string
        - name: Kafka__BootstrapServers
          value: "kafka-0.kafka-headless.ticketing-system.svc.cluster.local:9092"
        - name: Kafka__GroupId
          value: "eventsearch-consumer-group-free"
        - name: ApplicationInsights__InstrumentationKey
          valueFrom:
            secretKeyRef:
              name: appinsights-secret
              key: instrumentation-key
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: eventsearch-service
  namespace: ticketing-system
spec:
  selector:
    app: eventsearch-api
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
EOF

    # Create optimized Frontend deployment
    cat > k8s/free-tier/frontend-deployment-optimized.yaml << EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: frontend
  namespace: ticketing-system
spec:
  replicas: 1  # Single replica for free tier
  selector:
    matchLabels:
      app: frontend
  template:
    metadata:
      labels:
        app: frontend
    spec:
      containers:
      - name: frontend
        image: $ACR_LOGIN_SERVER/frontend:latest
        resources:
          requests:
            memory: "100Mi"  # Optimized for B1s VMs
            cpu: "100m"
          limits:
            memory: "200Mi"
            cpu: "200m"
        ports:
        - containerPort: 80
        livenessProbe:
          httpGet:
            path: /
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: frontend-service
  namespace: ticketing-system
spec:
  selector:
    app: frontend
  ports:
  - port: 80
    targetPort: 80
  type: ClusterIP
EOF

    echo -e "${GREEN}✅ Free tier optimized manifests created${NC}"
}

# Function to deploy Kafka (Docker containers)
deploy_kafka() {
    echo -e "${YELLOW}Deploying Kafka Docker containers (vendor-neutral!)...${NC}"
    
    # Deploy optimized Kafka cluster
    kubectl apply -f k8s/free-tier/kafka-cluster-optimized.yaml
    
    # Wait for Kafka infrastructure to be ready
    echo -e "${YELLOW}Waiting for Kafka infrastructure...${NC}"
    kubectl wait --for=condition=ready pod -l app=zookeeper -n ticketing-system --timeout=300s
    kubectl wait --for=condition=ready pod -l app=kafka -n ticketing-system --timeout=300s
    
    echo -e "${GREEN}✅ Kafka cluster deployed successfully${NC}"
    echo -e "${GREEN}   🐳 Running as Docker containers in AKS${NC}"
    echo -e "${GREEN}   🔓 Zero vendor lock-in!${NC}"
}

# Function to deploy applications
deploy_applications() {
    echo -e "${YELLOW}Deploying application services to FREE TIER...${NC}"
    
    # Deploy applications with free tier optimizations
    kubectl apply -f k8s/free-tier/eventmanagement-deployment-optimized.yaml
    kubectl apply -f k8s/free-tier/eventsearch-deployment-optimized.yaml
    kubectl apply -f k8s/free-tier/frontend-deployment-optimized.yaml
    
    # Wait for applications to be ready
    echo -e "${YELLOW}Waiting for applications to be ready...${NC}"
    kubectl wait --for=condition=available deployment/eventmanagement-api -n ticketing-system --timeout=300s
    kubectl wait --for=condition=available deployment/eventsearch-api -n ticketing-system --timeout=300s
    kubectl wait --for=condition=available deployment/frontend -n ticketing-system --timeout=300s
    
    echo -e "${GREEN}✅ Applications deployed successfully${NC}"
}

# Function to setup ingress for external access
setup_ingress() {
    echo -e "${YELLOW}Setting up ingress for external access...${NC}"
    
    # Install NGINX Ingress Controller (lightweight version for free tier)
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml
    
    # Wait for ingress controller to be ready
    kubectl wait --namespace ingress-nginx \
        --for=condition=ready pod \
        --selector=app.kubernetes.io/component=controller \
        --timeout=120s
    
    # Create ingress configuration optimized for free tier
    cat > k8s/free-tier/ingress-optimized.yaml << EOF
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: ticketing-ingress
  namespace: ticketing-system
  annotations:
    kubernetes.io/ingress.class: nginx
    nginx.ingress.kubernetes.io/rewrite-target: /
    nginx.ingress.kubernetes.io/ssl-redirect: "false"  # HTTP only for free tier
spec:
  rules:
  - http:
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
    
    kubectl apply -f k8s/free-tier/ingress-optimized.yaml
    
    echo -e "${GREEN}✅ Ingress configured for FREE TIER${NC}"
}

# Function to verify deployment
verify_deployment() {
    echo -e "${YELLOW}Verifying FREE TIER deployment...${NC}"
    
    echo -e "${BLUE}📊 Deployment Status:${NC}"
    kubectl get pods -n ticketing-system
    echo ""
    kubectl get services -n ticketing-system
    echo ""
    kubectl get ingress -n ticketing-system
    
    # Get external IP
    EXTERNAL_IP=$(kubectl get service ingress-nginx-controller -n ingress-nginx -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "pending")
    
    echo -e "${BLUE}📈 Resource Usage:${NC}"
    kubectl top pods -n ticketing-system 2>/dev/null || echo "Metrics not ready yet"
    
    if [ "$EXTERNAL_IP" != "pending" ] && [ "$EXTERNAL_IP" != "" ]; then
        echo -e "${GREEN}🌐 Your FREE TIER application is accessible at:${NC}"
        echo -e "   Frontend: http://${EXTERNAL_IP}/"
        echo -e "   EventManagement API: http://${EXTERNAL_IP}/api/events"
        echo -e "   EventSearch API: http://${EXTERNAL_IP}/api/search"
    else
        echo -e "${YELLOW}⏳ External IP is being assigned. Check again in a few minutes with:${NC}"
        echo -e "   kubectl get service ingress-nginx-controller -n ingress-nginx"
    fi
    
    echo -e "${GREEN}✅ FREE TIER deployment verification completed${NC}"
}

# Function to display cost information
show_cost_info() {
    echo -e "${GREEN}💰 Cost Breakdown - FREE TIER:${NC}"
    echo -e "  ✅ AKS Control Plane: \$0 (always free)"
    echo -e "  ✅ ${NODE_COUNT}x ${VM_SIZE} VMs: \$0 (750 hours each x 12 months)"
    echo -e "  ✅ Azure SQL Database: \$0 (250 DTU-hours/month)"
    echo -e "  ✅ Azure Cache for Redis: \$0 (250 MB C0 instance)"
    echo -e "  ✅ Application Insights: \$0 (1 GB data/month)"
    echo -e "  ✅ Container Registry: ~\$5/month (Basic tier)"
    echo -e "  ✅ Load Balancer: \$0 (Basic tier)"
    echo ""
    echo -e "${GREEN}Total Monthly Cost: ~\$5 (only ACR)"
    echo -e "Free for 12 months: Everything except ACR"
    echo -e "Kafka Strategy: Docker containers (vendor-neutral!)"
    echo ""
}

# Main execution
echo -e "${BLUE}Starting FREE TIER deployment...${NC}"

check_prerequisites
build_and_push_images
create_secrets
update_manifests_for_free_tier
deploy_kafka
deploy_applications
setup_ingress
verify_deployment
show_cost_info

echo ""
echo -e "${GREEN}🎉 Azure FREE TIER deployment completed successfully!${NC}"
echo ""
echo -e "${BLUE}📋 Deployment Summary:${NC}"
echo -e "  ✅ Kafka running as Docker containers (vendor-neutral)"
echo -e "  ✅ Applications optimized for ${VM_SIZE} VMs"
echo -e "  ✅ Azure managed services (SQL, Redis) integrated"
echo -e "  ✅ Ingress configured for external access"
echo -e "  ✅ All services running on FREE TIER"
echo ""
echo -e "${YELLOW}💡 Useful commands for FREE TIER:${NC}"
echo -e "  📊 Check status: kubectl get pods -n ticketing-system"
echo -e "  📋 View logs: kubectl logs -f deployment/eventmanagement-api -n ticketing-system"
echo -e "  🔍 Monitor Kafka: kubectl logs -f statefulset/kafka -n ticketing-system"
echo -e "  💾 Check resources: kubectl top pods -n ticketing-system"
echo ""
echo -e "${GREEN}Your vendor-neutral application is running on Azure FREE TIER! 🚀${NC}"
echo -e "${GREEN}Kafka in Docker = Zero vendor lock-in! 🔓${NC}"