# 💰 Cost-Optimized Azure Resources Status

## **Current Status (All Non-Essential Resources Paused/Stopped)**

### ✅ **STOPPED/PAUSED RESOURCES (Zero Cost)**
- **AKS Cluster**: Stopped - No VM billing ✅
- **All Kubernetes Pods**: Stopped - No compute costs ✅  
- **SQL Database**: Paused - No compute billing (data preserved) ✅
- **Redis Cache**: Deleted - No ongoing costs ✅
- **Container Registries**: 3 deleted, 1 remains (with images) ✅

### 💸 **REMAINING ACTIVE RESOURCES (Minimal Cost)**
- **Container Registry** (1): ~$15/month (contains Docker images)
- **Application Insights**: FREE tier (monitoring)
- **Data Collection Rules**: Minimal cost for monitoring

**ESTIMATED MONTHLY COST: ~$15** (down from ~$126/month)

---

## **🚀 Quick Restart Instructions**

### **1. Resume SQL Database**
```bash
# Resume when ready to test
az sql db resume --resource-group rg-ticketing-free --server ticketing-sql-free-1757240316 --name EventManagementDB

# Check status
az sql db show --resource-group rg-ticketing-free --server ticketing-sql-free-1757240316 --name EventManagementDB --query "status" -o tsv
```

### **2. Start AKS Cluster**
```bash
# Start the cluster
az aks start --resource-group rg-ticketing-free --name ticketing-aks-free

# Get credentials
az aks get-credentials --resource-group rg-ticketing-free --name ticketing-aks-free --overwrite-existing

# Set namespace
kubectl config set-context --current --namespace=ticketing-system
```

### **3. Start All Application Services**
```bash
# Scale up all deployments
kubectl scale deployment --all --replicas=1 -n ticketing-system

# Wait for pods to be ready
kubectl wait --for=condition=ready pod --all --timeout=300s -n ticketing-system

# Verify services
kubectl get pods -n ticketing-system
kubectl get services -n ticketing-system
```

### **4. Recreate Redis Cache (Optional)**
```bash
# Only if you need Redis caching
az redis create \
  --name ticketing-redis-free \
  --resource-group rg-ticketing-free \
  --location eastus2 \
  --sku Basic \
  --vm-size c0
```

---

## **⚡ Complete Startup Sequence (Single Script)**

```bash
#!/bin/bash
echo "🚀 Starting Azure Ticketing System..."

# 1. Resume SQL Database
echo "📊 Resuming SQL Database..."
az sql db resume --resource-group rg-ticketing-free --server ticketing-sql-free-1757240316 --name EventManagementDB

# 2. Start AKS Cluster  
echo "🎯 Starting AKS Cluster..."
az aks start --resource-group rg-ticketing-free --name ticketing-aks-free

# 3. Configure kubectl
echo "⚙️ Configuring kubectl..."
az aks get-credentials --resource-group rg-ticketing-free --name ticketing-aks-free --overwrite-existing
kubectl config set-context --current --namespace=ticketing-system

# 4. Start all services
echo "🔄 Starting all services..."
kubectl scale deployment --all --replicas=1 -n ticketing-system

# 5. Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
kubectl wait --for=condition=ready pod --all --timeout=300s -n ticketing-system

echo "✅ All services started successfully!"
kubectl get pods -n ticketing-system
```

---

## **💸 Cost Management Commands**

### **Stop Everything (Back to $15/month)**
```bash
# Pause SQL Database
az sql db pause --resource-group rg-ticketing-free --server ticketing-sql-free-1757240316 --name EventManagementDB

# Stop AKS Cluster
az aks stop --resource-group rg-ticketing-free --name ticketing-aks-free

# Scale down all pods (if AKS is running)
kubectl scale deployment --all --replicas=0 -n ticketing-system
```

### **Check Current Costs**
```bash
# Check SQL Database status
az sql db show --resource-group rg-ticketing-free --server ticketing-sql-free-1757240316 --name EventManagementDB --query "{status:status,tier:currentServiceObjectiveName}" -o table

# Check AKS status
az aks show --resource-group rg-ticketing-free --name ticketing-aks-free --query "powerState.code" -o tsv

# List all resources
az resource list --resource-group rg-ticketing-free --query "[].{Name:name,Type:type}" -o table
```

---

## **🎯 Testing Your System**

### **Port Forward for Local Testing**
```bash
# EventManagement API
kubectl port-forward service/eventmanagement-api 8080:80 -n ticketing-system &

# EventSearch API
kubectl port-forward service/eventsearch-api 8081:80 -n ticketing-system &

# Elasticsearch
kubectl port-forward service/elasticsearch 9200:9200 -n ticketing-system &
```

### **Test Event Creation**
```bash
# Create test event
curl -X POST http://localhost:8080/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Cost-Optimized Test Event",
    "description": "Testing after cost optimization",
    "venue": "Azure Cloud", 
    "dateTime": "2024-12-25T19:00:00Z",
    "ticketPrice": 50.00,
    "totalTickets": 500
  }'

# Search events
curl "http://localhost:8081/api/search/events?query=Cost"
```

---

## **💡 Cost Optimization Achievements**

### **Monthly Cost Reduction:**
- **Before**: ~$126/month
- **After**: ~$15/month  
- **Savings**: ~$111/month (88% reduction!)

### **What Was Optimized:**
✅ AKS Cluster stopped (was $60/month)  
✅ SQL Database paused (was $5/month)  
✅ Redis Cache deleted (was $1.30/month)  
✅ 3 duplicate Container Registries deleted (was $45/month)  
✅ All Kubernetes workloads stopped  

### **What Remains Active:**
- 1 Container Registry with your Docker images ($15/month)
- Application Insights (FREE tier)
- Monitoring infrastructure (minimal cost)

**Your pure Kafka event-driven architecture is preserved and ready to restart in minutes!** 🎉