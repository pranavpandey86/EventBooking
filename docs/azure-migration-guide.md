# 🚀 Azure Migration Guide for TicketBookingSystem

## 📋 **Overview**

This guide provides a complete step-by-step process to migrate your local Kubernetes setup to Azure with CI/CD pipeline integration.

## 🎯 **Migration Strategy**

### **Current State**: Local Kubernetes with 6 services
### **Target State**: Azure Kubernetes Service (AKS) with CI/CD pipeline

---

## 🔧 **Prerequisites**

### **1. Azure Account Setup**
```bash
# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login to Azure
az login

# Set subscription (if you have multiple)
az account set --subscription "Your Subscription Name"
```

### **2. Required Tools**
- ✅ Azure CLI (latest version)
- ✅ kubectl (for Kubernetes management)
- ✅ Docker (for container building)
- ✅ Git (for version control)

---

## 🏗️ **Step 1: Azure Infrastructure Setup**

### **Run Infrastructure Creation Script**
```bash
# Make script executable
chmod +x scripts/setup-azure-infrastructure.sh

# Run infrastructure setup
./scripts/setup-azure-infrastructure.sh
```

### **What This Creates:**
- 📁 **Resource Group**: `rg-ticketing-system`
- 🐳 **Container Registry**: Azure Container Registry (ACR)
- ☸️ **AKS Cluster**: 3-node Kubernetes cluster
- 🗄️ **SQL Database**: Azure SQL Database
- 🌐 **Cosmos DB**: For future TicketInventory service
- ⚡ **Redis Cache**: Azure Cache for Redis
- 📊 **Application Insights**: Monitoring and logging
- 💾 **Storage Account**: For file storage

### **Cost Estimation (Learning/Development)**
```
Azure SQL Database (Basic): ~$5/month
AKS Cluster (3 B2s nodes): ~$70/month
Redis Cache (Basic): ~$16/month
Cosmos DB (Free tier): $0/month
Container Registry (Basic): ~$5/month
Application Insights: Free tier

Total: ~$96/month (can be reduced to ~$20/month with smaller configurations)
```

---

## 🔄 **Step 2: CI/CD Pipeline Setup**

You have **two options** for CI/CD:

### **Option A: Azure DevOps (Recommended for Azure)**

#### **1. Create Azure DevOps Project**
- Go to https://dev.azure.com
- Create new project: `TicketBookingSystem`
- Import your Git repository

#### **2. Create Service Connections**
```bash
# In Azure DevOps > Project Settings > Service Connections:
1. Azure Resource Manager connection
2. Docker Registry connection (to your ACR)
3. Kubernetes service connection (to your AKS)
```

#### **3. Setup Pipeline**
- Create new pipeline using `azure-pipelines.yml`
- Configure variables:
  - `azureSubscription`: Your Azure service connection
  - `containerRegistry`: Your ACR login server
  - `kubernetesServiceConnection`: Your AKS connection

### **Option B: GitHub Actions (Free Alternative)**

#### **1. Create GitHub Secrets**
```bash
# Required secrets in GitHub repo settings:
AZURE_CREDENTIALS          # Service principal JSON
SQL_CONNECTION_STRING       # From azure-credentials.txt
COSMOS_CONNECTION_STRING    # From azure-credentials.txt
REDIS_CONNECTION_STRING     # From azure-credentials.txt
APPINSIGHTS_INSTRUMENTATION_KEY # From azure-credentials.txt
```

#### **2. Create Azure Service Principal**
```bash
# Create service principal for GitHub Actions
az ad sp create-for-rbac --name "GitHub-Actions-TicketBooking" \
    --role contributor \
    --scopes /subscriptions/{subscription-id}/resourceGroups/rg-ticketing-system \
    --sdk-auth

# Use the output JSON as AZURE_CREDENTIALS secret
```

---

## 🚀 **Step 3: Deploy to Azure**

### **Manual Deployment (First Time)**
```bash
# Make script executable
chmod +x scripts/deploy-to-azure.sh

# Deploy to Azure
./scripts/deploy-to-azure.sh
```

### **Automated Deployment (CI/CD)**
```bash
# Push to develop branch for development deployment
git checkout -b develop
git push origin develop

# Push to main branch for production deployment
git checkout main
git push origin main
```

---

## 📊 **Step 4: Verify Deployment**

### **Check Azure Resources**
```bash
# Check AKS cluster
az aks list --resource-group rg-ticketing-system --output table

# Check container registry
az acr repository list --name ticketingacr --output table

# Check SQL database
az sql db list --server ticketing-sql-* --resource-group rg-ticketing-system --output table
```

### **Check Kubernetes Deployment**
```bash
# Get AKS credentials
az aks get-credentials --resource-group rg-ticketing-system --name ticketing-aks

# Check pods
kubectl get pods -n ticketing-system

# Check services
kubectl get services -n ticketing-system

# Check ingress
kubectl get ingress -n ticketing-system
```

### **Access Your Application**
```bash
# Get external IP
kubectl get service ingress-nginx-controller -n ingress-nginx

# Access via browser:
# Frontend: http://EXTERNAL_IP/
# EventManagement API: http://EXTERNAL_IP/api/events
# EventSearch API: http://EXTERNAL_IP/api/search
```

---

## 🔧 **Step 5: Configuration Updates Needed**

### **1. Update Connection Strings**

Your Kubernetes secrets will be created automatically, but you may need to update your application configuration:

#### **EventManagement API - appsettings.Production.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "#{DATABASE_CONNECTION_STRING}#"
  },
  "Kafka": {
    "BootstrapServers": "kafka-0.kafka-headless.ticketing-system.svc.cluster.local:9092"
  },
  "ApplicationInsights": {
    "InstrumentationKey": "#{APPINSIGHTS_INSTRUMENTATION_KEY}#"
  }
}
```

#### **EventSearch API - appsettings.Production.json**
```json
{
  "Elasticsearch": {
    "Url": "http://elasticsearch-service.ticketing-system.svc.cluster.local:9200"
  },
  "Redis": {
    "ConnectionString": "#{REDIS_CONNECTION_STRING}#"
  },
  "Kafka": {
    "BootstrapServers": "kafka-0.kafka-headless.ticketing-system.svc.cluster.local:9092"
  }
}
```

### **2. Update Kubernetes Manifests**

The deployment scripts will automatically update image references, but ensure your manifests use the correct patterns:

```yaml
# Example: eventmanagement-deployment.yaml
containers:
- name: eventmanagement-api
  image: ticketbookingsystem-eventmanagement-api:latest  # This gets replaced
  env:
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: database-secret
        key: connection-string
```

---

## 🔍 **Step 6: Monitoring and Logging**

### **Application Insights Setup**
```bash
# View logs in Azure Portal
# Go to Application Insights > ticketing-insights
# Check Live Metrics, Failures, Performance
```

### **Kubernetes Monitoring**
```bash
# View pod logs
kubectl logs -f deployment/eventmanagement-api -n ticketing-system

# View events
kubectl get events -n ticketing-system --sort-by='.lastTimestamp'

# Monitor resource usage
kubectl top pods -n ticketing-system
kubectl top nodes
```

---

## 🛡️ **Security Considerations**

### **1. Network Security**
- ✅ AKS cluster uses private IPs
- ✅ SQL Database firewall configured
- ✅ Redis requires authentication
- ✅ Container Registry requires authentication

### **2. Secrets Management**
- ✅ Connection strings stored in Kubernetes secrets
- ✅ ACR credentials managed by AKS
- ✅ No hardcoded credentials in code

### **3. Access Control**
- ✅ Azure RBAC for resource access
- ✅ Kubernetes RBAC for cluster access
- ✅ Service principal with minimal permissions

---

## 💰 **Cost Optimization Tips**

### **Development Environment**
```bash
# Scale down when not in use
kubectl scale deployment --all --replicas=0 -n ticketing-system

# Scale up when needed
kubectl scale deployment --all --replicas=1 -n ticketing-system

# Use Azure DevTest subscription (if available)
# Use B-series VMs for cost savings
```

### **Production Environment**
```bash
# Enable autoscaling
kubectl autoscale deployment eventmanagement-api --cpu-percent=70 --min=2 --max=10 -n ticketing-system

# Use reserved instances for long-term workloads
# Monitor and optimize resource requests/limits
```

---

## 🚨 **Troubleshooting Guide**

### **Common Issues**

#### **1. Image Pull Errors**
```bash
# Check ACR authentication
az acr login --name ticketingacr

# Verify image exists
az acr repository show-tags --name ticketingacr --repository eventmanagement-api
```

#### **2. Pod Startup Issues**
```bash
# Check pod logs
kubectl describe pod POD_NAME -n ticketing-system
kubectl logs POD_NAME -n ticketing-system

# Check secrets
kubectl get secrets -n ticketing-system
kubectl describe secret database-secret -n ticketing-system
```

#### **3. Network Connectivity**
```bash
# Test internal DNS
kubectl run test-pod --image=busybox -it --rm -- nslookup kafka-0.kafka-headless.ticketing-system.svc.cluster.local

# Check service endpoints
kubectl get endpoints -n ticketing-system
```

---

## 📈 **Next Steps After Migration**

### **1. Phase 6: TicketInventory Service**
- Deploy with Cosmos DB integration
- Real-time inventory management
- SignalR for live updates

### **2. Performance Optimization**
- Add horizontal pod autoscaling
- Implement caching strategies
- Optimize database queries

### **3. Advanced Features**
- API Gateway (Azure Application Gateway)
- Service mesh (Istio)
- Advanced monitoring (Prometheus/Grafana)

---

## 🎯 **Success Criteria**

✅ **Infrastructure**: All Azure resources created successfully  
✅ **Deployment**: Application running on AKS  
✅ **CI/CD**: Pipeline building and deploying automatically  
✅ **Networking**: External access working via ingress  
✅ **Monitoring**: Logs and metrics visible in Application Insights  
✅ **Event Flow**: Kafka message flow working end-to-end  

**Your TicketBookingSystem is now cloud-native and production-ready! 🚀**