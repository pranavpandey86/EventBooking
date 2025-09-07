# 🔐 Secrets Management Guide

## **The Problem We Solved**

Previously, we accidentally committed sensitive credentials to git, which GitHub's security scanning detected. This guide shows the secure approach we implemented.

## **✅ Secure Secrets Management**

### **1. Never Store Secrets in Git**
- All template files use placeholders like `YOUR-PASSWORD`
- Real credentials are retrieved dynamically from Azure
- Local secrets are stored in `.secrets/` directory (gitignored)

### **2. Dynamic Secrets Retrieval**
```bash
# Get live secrets from Azure
./scripts/get-azure-secrets.sh

# This creates:
# .secrets/azure-config.env       (environment variables)
# .secrets/k8s-secrets.yaml      (Kubernetes secrets)
```

### **3. Use Secrets for Deployment**
```bash
# Load environment variables
source .secrets/azure-config.env

# Deploy Kubernetes secrets
kubectl apply -f .secrets/k8s-secrets.yaml

# Verify secrets are deployed
kubectl get secrets -n ticketing-system
```

## **📋 What the Script Retrieves**

### **SQL Database Connection**
- Server: `ticketing-sql-free-1757240316.database.windows.net`
- Database: `EventManagementDB`
- Authentication: SQL Server authentication
- SSL: Required

### **Container Registry**
- Login Server: `ticketingfreeacr1757240088.azurecr.io`
- Credentials: Retrieved from Azure ACR
- Docker pull secrets: Auto-generated for Kubernetes

### **Application Insights**
- Instrumentation Key: Retrieved from Azure resource
- Used for: Application monitoring and telemetry

### **Redis Cache** (Optional)
- Status: Currently deleted for cost savings
- Connection: Can be recreated when needed
- Fallback: Application works without Redis

## **🚀 Deployment Workflow**

### **Step 1: Get Fresh Secrets**
```bash
# Always get latest credentials before deployment
./scripts/get-azure-secrets.sh
```

### **Step 2: Start Azure Services**
```bash
# Resume paused services
az sql db resume --resource-group rg-ticketing-free --server ticketing-sql-free-1757240316 --name EventManagementDB
az aks start --resource-group rg-ticketing-free --name ticketing-aks-free
```

### **Step 3: Deploy Secrets to Kubernetes**
```bash
# Load environment
source .secrets/azure-config.env

# Deploy secrets
kubectl apply -f .secrets/k8s-secrets.yaml
```

### **Step 4: Deploy Applications**
```bash
# Deploy all services
kubectl apply -f k8s/azure-applications.yaml
kubectl apply -f k8s/azure-kafka-kraft.yaml
kubectl apply -f k8s/azure-elasticsearch.yaml

# Scale up services
kubectl scale deployment --all --replicas=1 -n ticketing-system
```

## **🔒 Security Best Practices**

### **What's Protected**
✅ SQL Database passwords  
✅ Redis access keys  
✅ Application Insights keys  
✅ Container Registry credentials  
✅ Kubernetes secrets  

### **What's Safe to Commit**
✅ Template configuration files  
✅ Kubernetes deployment YAML (without secrets)  
✅ Scripts that retrieve secrets  
✅ Documentation and guides  

### **What's Never Committed**
❌ Real connection strings  
❌ Passwords or access keys  
❌ Kubernetes secrets with real data  
❌ Environment files with credentials  

## **🔄 Rotating Secrets**

### **Database Password**
```bash
# Change in Azure Portal or CLI
az sql server update --name ticketing-sql-free-1757240316 --resource-group rg-ticketing-free --admin-password "NewPassword123!"

# Re-run secrets script
./scripts/get-azure-secrets.sh

# Update Kubernetes secrets
kubectl apply -f .secrets/k8s-secrets.yaml
```

### **Container Registry Keys**
```bash
# Regenerate ACR credentials
az acr credential renew --name ticketingfreeacr1757240088 --password-name password

# Re-run secrets script
./scripts/get-azure-secrets.sh
```

## **🚨 If Secrets Are Exposed**

### **Immediate Actions**
1. **Rotate all exposed credentials** in Azure Portal
2. **Re-run secrets retrieval script**
3. **Update Kubernetes secrets**
4. **Restart affected services**

### **Prevention**
- Always check `.gitignore` includes `.secrets/`
- Use `git status` before committing
- Enable GitHub secret scanning (already active)
- Use Azure Key Vault for production environments

## **💡 Environment-Specific Management**

### **Development**
- Use local `.secrets/` directory
- Individual developer Azure subscriptions
- Separate resource groups per developer

### **Production**
- Use Azure Key Vault
- Managed identities where possible
- Automated secret rotation
- Audit logging for secret access

## **📁 File Structure**
```
TicketBookingSystem/
├── .secrets/                     # Local secrets (gitignored)
│   ├── azure-config.env         # Environment variables
│   ├── k8s-secrets.yaml         # Kubernetes secrets
│   └── README.md                # Warning file
├── scripts/
│   └── get-azure-secrets.sh     # Secrets retrieval script
├── azure-production-config.env  # Template file (safe)
└── .gitignore                   # Excludes .secrets/
```

Your secrets are now properly managed and your repository is secure! 🔐