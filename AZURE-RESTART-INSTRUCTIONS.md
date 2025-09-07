# Azure Services Restart Instructions

## **Current Status (All Services Stopped)**
✅ **All Kubernetes pods stopped** - No active services consuming resources  
✅ **AKS cluster still running** - 2 nodes active in `rg-ticketing-free` resource group  
⚠️ **Azure SQL Database** - Still active and incurring charges  

---

## **Quick Restart Commands**

### **1. Connect to AKS Cluster**
```bash
az aks get-credentials --resource-group rg-ticketing-free --name ticketing-aks-free --overwrite-existing
kubectl config set-context --current --namespace=ticketing-system
```

### **2. Restart All Services (Single Command)**
```bash
# Scale up all deployments to 1 replica
kubectl scale deployment --all --replicas=1 -n ticketing-system

# Wait for services to be ready
kubectl wait --for=condition=ready pod --all --timeout=300s -n ticketing-system
```

### **3. Verify Services are Running**
```bash
kubectl get pods -n ticketing-system
kubectl get services -n ticketing-system
```

---

## **Detailed Service Information**

### **Deployment Names**
- `eventmanagement-api` - Main API service
- `eventsearch-api` - Search API service  
- `kafka` - Message broker (KRaft mode)
- `elasticsearch` - Search engine

### **Service Ports**
- **EventManagement API**: `http://localhost:8080` (via port-forward)
- **EventSearch API**: `http://localhost:8081` (via port-forward)
- **Kafka**: `kafka:9092` (internal)
- **Elasticsearch**: `http://localhost:9200` (via port-forward)

### **Port Forward Commands (for testing)**
```bash
# EventManagement API
kubectl port-forward service/eventmanagement-api 8080:80 -n ticketing-system &

# EventSearch API  
kubectl port-forward service/eventsearch-api 8081:80 -n ticketing-system &

# Elasticsearch
kubectl port-forward service/elasticsearch 9200:9200 -n ticketing-system &
```

---

## **Architecture Configuration**

### **Pure Kafka Event-Driven Architecture**
✅ **NO HTTP fallback mechanisms** - All inter-service communication via Kafka  
✅ **EventSearchIntegrationService removed** - Pure messaging implementation  
✅ **SyncController endpoints removed** - No manual sync options  

### **Key Components**
1. **EventManagement API** → Publishes events to Kafka topic `event-updates`
2. **EventSearch API** → Consumes from `event-updates`, indexes to Elasticsearch
3. **Kafka Topics**: `event-updates` (single partition, replication factor 1)
4. **Elasticsearch Index**: `events` with event data

### **Database Connection**
- **Azure SQL Database**: `ticketing-db-free.database.windows.net`
- **Connection string configured** in EventManagement API appsettings

---

## **Testing the System**

### **1. Create Test Events (EventManagement API)**
```bash
# Port forward first
kubectl port-forward service/eventmanagement-api 8080:80 -n ticketing-system &

# Create events
curl -X POST http://localhost:8080/api/events \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Event 1","description":"Test Description","eventDate":"2024-01-15T10:00:00Z","location":"Test Location","maxCapacity":100}'
```

### **2. Verify Kafka Processing**
```bash
# Check Kafka consumer group lag (should be 0)
kubectl exec -it deployment/kafka -n ticketing-system -- kafka-consumer-groups.sh \
  --bootstrap-server localhost:9092 --group eventsearch-group --describe
```

### **3. Verify Elasticsearch Indexing**
```bash
# Port forward Elasticsearch
kubectl port-forward service/elasticsearch 9200:9200 -n ticketing-system &

# Check indexed events
curl "http://localhost:9200/events/_search?pretty"
```

### **4. Test EventSearch API**
```bash
# Port forward EventSearch API
kubectl port-forward service/eventsearch-api 8081:80 -n ticketing-system &

# Search events
curl "http://localhost:8081/api/search/events?query=Test"
```

---

## **Resource Management**

### **Stop All Services (to prevent costs)**
```bash
kubectl scale deployment --all --replicas=0 -n ticketing-system
kubectl delete pod --all -n ticketing-system --grace-period=0 --force
```

### **Check Current Resource Usage**
```bash
kubectl top nodes
kubectl get pods -n ticketing-system
az aks show --resource-group rg-ticketing-free --name ticketing-aks-free --query "agentPoolProfiles[0].count"
```

### **Azure SQL Database Management**
```bash
# Stop database (if needed to reduce costs)
az sql db pause --resource-group rg-ticketing-free --server ticketing-db-free --name TicketingDB

# Resume database
az sql db resume --resource-group rg-ticketing-free --server ticketing-db-free --name TicketingDB
```

---

## **Memory Optimization Settings**

### **Kafka Configuration**
- **Memory Limit**: 768Mi
- **JVM Heap**: 512m  
- **Mode**: KRaft (no Zookeeper)

### **If Memory Issues Occur**
```bash
# Restart Kafka with optimized settings
kubectl delete pod -l app=kafka -n ticketing-system
kubectl wait --for=condition=ready pod -l app=kafka --timeout=180s -n ticketing-system
```

---

## **Troubleshooting**

### **Common Issues**
1. **Kafka OOMKilled** → Memory limits already optimized
2. **Service Discovery** → Use TCP health checks (already configured)  
3. **Consumer Lag** → Check EventSearch API logs

### **Debug Commands**
```bash
# Check logs
kubectl logs deployment/eventmanagement-api -n ticketing-system
kubectl logs deployment/eventsearch-api -n ticketing-system
kubectl logs deployment/kafka -n ticketing-system

# Check events
kubectl get events -n ticketing-system --sort-by='.lastTimestamp'
```

---

## **Cost Monitoring**
- **AKS Nodes**: 2 x Standard_B2s (running 24/7)
- **Azure SQL**: Basic tier (can be paused)
- **Storage**: Minimal usage
- **Recommendation**: Stop pods when not testing, pause SQL DB when not needed

**Current Status**: All pods stopped ✅ | AKS cluster running ⚠️ | SQL DB active ⚠️