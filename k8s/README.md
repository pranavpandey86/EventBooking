# 🚀 Kubernetes Deployment Guide

This directory contains Kubernetes manifests to deploy your entire TicketBooking System to Kubernetes.

## 📋 Prerequisites

### Option 1: Docker Desktop Kubernetes (Recommended for Local)
1. **Install Docker Desktop** (if not already installed)
2. **Enable Kubernetes**:
   - Open Docker Desktop
   - Go to Settings → Kubernetes
   - Check "Enable Kubernetes"
   - Click "Apply & Restart"
   - Wait for Kubernetes to be ready (green status)

### Option 2: Minikube (Alternative)
```bash
# Install minikube
brew install minikube

# Start cluster
minikube start --memory=4096 --cpus=4

# Enable ingress (for frontend access)
minikube addons enable ingress
```

### Option 3: Cloud Kubernetes (Azure AKS, AWS EKS, GCP GKE)
- Ensure you have `kubectl` configured to connect to your cluster
- Verify with: `kubectl config current-context`

## 🏗️ Architecture Overview

The Kubernetes deployment mirrors your Docker Compose setup:

```
┌─────────────────────────────────────────────────────────────┐
│                    ticketing-system namespace               │
├─────────────────────────────────────────────────────────────┤
│  Frontend (Angular)     ←→  EventManagement API            │
│       ↓                           ↓                        │
│  Ingress Controller        EventSearch API                 │
│                                   ↓                        │
│                           Elasticsearch + Redis            │
│                                   ↓                        │
│                            SQL Server                      │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Components Deployed

### Infrastructure Services
- **SQL Server** (StatefulSet): Primary database with persistent storage
- **Elasticsearch** (StatefulSet): Search engine with persistent storage  
- **Redis** (Deployment): Caching layer

### Application Services
- **EventManagement API** (Deployment): CRUD operations for events
- **EventSearch API** (Deployment): Advanced search capabilities
- **Frontend** (Deployment): Angular application with nginx

### Configuration
- **Namespace**: `ticketing-system` (isolated environment)
- **ConfigMap**: Common application configuration
- **Secret**: Sensitive data (database passwords, connection strings)

## 🚀 Quick Deployment

### Deploy Everything
```bash
# From the TicketBookingSystem root directory
./k8s/deploy.sh
```

This script will:
1. ✅ Build all Docker images locally
2. ✅ Create the Kubernetes namespace
3. ✅ Deploy all infrastructure services
4. ✅ Wait for infrastructure to be ready
5. ✅ Deploy all application services
6. ✅ Display status and access information

### Clean Up Everything
```bash
./k8s/cleanup.sh
```

## 📊 Manual Deployment (Step by Step)

### 1. Build Docker Images
```bash
cd /Users/pranavpandey/TicketBookingSystem

# Build all images
docker build -t ticketbookingsystem-eventmanagement-api:latest ./src/backend/EventManagement
docker build -t ticketbookingsystem-eventsearch-api:latest ./src/backend/EventSearch  
docker build -t ticketbookingsystem-frontend:latest ./src/frontend/ticket-booking-system
```

### 2. Deploy Infrastructure
```bash
# Create namespace and configuration
kubectl apply -f k8s/namespace.yaml

# Deploy databases and infrastructure
kubectl apply -f k8s/sqlserver.yaml
kubectl apply -f k8s/elasticsearch.yaml
kubectl apply -f k8s/redis.yaml

# Wait for infrastructure
kubectl wait --for=condition=ready pod -l app=sqlserver -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=elasticsearch -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n ticketing-system --timeout=300s
```

### 3. Deploy Applications
```bash
# Deploy application services
kubectl apply -f k8s/eventmanagement-deployment.yaml
kubectl apply -f k8s/eventsearch-deployment.yaml
kubectl apply -f k8s/frontend-deployment.yaml

# Wait for applications
kubectl wait --for=condition=ready pod -l app=eventmanagement-api -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=eventsearch-api -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=frontend -n ticketing-system --timeout=300s
```

## 🔗 Accessing Services

### Port Forwarding (Local Access)
```bash
# Frontend (Angular app)
kubectl port-forward -n ticketing-system service/frontend-service 4200:80
# Access: http://localhost:4200

# EventManagement API
kubectl port-forward -n ticketing-system service/eventmanagement-service 8080:80
# Access: http://localhost:8080

# EventSearch API  
kubectl port-forward -n ticketing-system service/eventsearch-service 8081:80
# Access: http://localhost:8081

# Direct database access (for debugging)
kubectl port-forward -n ticketing-system service/sqlserver-service 1433:1433
kubectl port-forward -n ticketing-system service/elasticsearch-service 9200:9200
kubectl port-forward -n ticketing-system service/redis-service 6379:6379
```

### Ingress (External Access)
If using minikube or a cloud cluster with ingress controller:
```bash
# Get ingress IP
kubectl get ingress -n ticketing-system

# Access via ingress
curl -H "Host: localhost" http://<INGRESS-IP>/
```

## 📊 Monitoring and Debugging

### Check Pod Status
```bash
# All pods
kubectl get pods -n ticketing-system

# Specific service
kubectl get pods -l app=eventmanagement-api -n ticketing-system

# Detailed pod info
kubectl describe pod <pod-name> -n ticketing-system
```

### View Logs
```bash
# Real-time logs
kubectl logs -f deployment/eventmanagement-api -n ticketing-system
kubectl logs -f deployment/eventsearch-api -n ticketing-system
kubectl logs -f deployment/frontend -n ticketing-system

# Previous logs (if pod crashed)
kubectl logs deployment/eventmanagement-api -n ticketing-system --previous
```

### Check Services and Endpoints
```bash
# Services
kubectl get services -n ticketing-system

# Endpoints (should show pod IPs)
kubectl get endpoints -n ticketing-system

# Test service connectivity
kubectl run test-pod --image=busybox -i --tty --rm -- /bin/sh
# Inside pod: wget -qO- http://eventmanagement-service.ticketing-system/health
```

### Resource Usage
```bash
# Resource consumption
kubectl top pods -n ticketing-system
kubectl top nodes

# Resource requests/limits
kubectl describe deployment eventmanagement-api -n ticketing-system
```

## 🔧 Troubleshooting

### Common Issues

#### 1. ImagePullBackOff
```bash
# Issue: Kubernetes can't find the Docker image
# Solution: Ensure images are built locally
docker images | grep ticketbookingsystem

# If using minikube, load images
minikube image load ticketbookingsystem-eventmanagement-api:latest
minikube image load ticketbookingsystem-eventsearch-api:latest
minikube image load ticketbookingsystem-frontend:latest
```

#### 2. CrashLoopBackOff
```bash
# Check logs for error details
kubectl logs deployment/eventmanagement-api -n ticketing-system

# Common causes:
# - Database connection issues
# - Missing environment variables
# - Application startup errors
```

#### 3. Service Connection Issues
```bash
# Test DNS resolution
kubectl run test-pod --image=busybox -i --tty --rm -- nslookup eventmanagement-service.ticketing-system

# Check service endpoints
kubectl get endpoints -n ticketing-system
```

#### 4. Persistent Volume Issues
```bash
# Check PV/PVC status
kubectl get pv
kubectl get pvc -n ticketing-system

# Describe for details
kubectl describe pvc sqlserver-storage-sqlserver-0 -n ticketing-system
```

## 🔄 Comparison with Docker Compose

| Aspect | Docker Compose | Kubernetes |
|--------|----------------|------------|
| **Orchestration** | Single host | Multi-host cluster |
| **Scaling** | Manual scaling | Auto-scaling (HPA) |
| **High Availability** | Single point of failure | Pod replicas across nodes |
| **Service Discovery** | Built-in networking | DNS-based service discovery |
| **Configuration** | Environment variables | ConfigMaps + Secrets |
| **Storage** | Local volumes | Persistent Volumes |
| **Load Balancing** | External tools | Built-in service load balancing |
| **Rolling Updates** | Manual | Automated rolling deployments |
| **Resource Management** | Basic limits | Requests, limits, quotas |
| **Monitoring** | External tools | Built-in health checks + metrics |

## 🚀 Next Steps

After getting familiar with this Kubernetes deployment:

1. **Add Kafka** for event-driven architecture
2. **Implement HorizontalPodAutoscaler** for auto-scaling
3. **Add monitoring** with Prometheus and Grafana
4. **Implement CI/CD** with GitOps (ArgoCD)
5. **Deploy to cloud** (Azure AKS, AWS EKS, GCP GKE)

## 📚 Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [kubectl Cheat Sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
- [Docker Desktop Kubernetes](https://docs.docker.com/desktop/kubernetes/)
- [Minikube Documentation](https://minikube.sigs.k8s.io/docs/)