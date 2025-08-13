# 🐳 Docker Setup Instructions

## Quick Start Guide

### 1. **Start Docker Desktop**
Make sure Docker Desktop is running on your macOS system.

### 2. **Test Local Development Environment**

```bash
# Navigate to project root
cd /Users/pranavpandey/TicketBookingSystem

# Start all services (SQL Server + EventManagement API)
docker-compose up --build

# Or start in background
docker-compose up -d --build
```

### 3. **What This Will Do:**

- **🗄️ SQL Server**: Runs on `localhost:1433`
- **🔗 EventManagement API**: Available at `http://localhost:5001`
- **📊 Health Checks**: Available at `http://localhost:5001/health`

### 4. **Test API Endpoints:**

```bash
# Health check
curl http://localhost:5001/health

# Get all events
curl http://localhost:5001/api/v1/events

# Create a test event
curl -X POST http://localhost:5001/api/v1/events \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Docker Test Event",
    "description": "Testing our containerized API",
    "category": "Technology",
    "eventDate": "2025-09-15T09:00:00",
    "location": "Docker Container",
    "maxCapacity": 100,
    "ticketPrice": 25.00,
    "organizerUserId": "123e4567-e89b-12d3-a456-426614174000"
  }'
```

### 5. **Docker Commands:**

```bash
# View running containers
docker ps

# View logs for specific service
docker-compose logs eventmanagement-api
docker-compose logs sqlserver

# Stop all services
docker-compose down

# Stop and remove volumes (fresh start)
docker-compose down -v

# Rebuild specific service
docker-compose up --build eventmanagement-api
```

## 🚀 Azure Deployment

### **Same Docker Images for Azure!**

The **same Docker images** we test locally can be deployed to Azure:

1. **Azure Container Instances** (Simple, good for learning)
2. **Azure App Service** (Recommended for web APIs)
3. **Azure Kubernetes Service** (Production-ready, scalable)

### **Deployment Steps:**

```bash
# Build and deploy to Azure
./scripts/deploy-azure.sh

# Follow the prompts to choose:
# 1) Container Instances
# 2) App Service  
# 3) Kubernetes
```

## 🔧 Troubleshooting

### **Docker Issues:**
- **Docker not running**: Start Docker Desktop
- **Port conflicts**: Change ports in docker-compose.yml
- **Build failures**: Check Dockerfile syntax

### **SQL Connection Issues:**
- **Connection refused**: Wait for SQL Server to be ready (health check)
- **Login failed**: Check password in docker-compose.yml
- **Database not found**: API will create it automatically

### **API Issues:**
- **404 errors**: Check if API is running on correct port
- **Health check fails**: Check database connection
- **CORS errors**: Verify frontend URL in Program.cs

## 📋 Next Steps

1. **✅ Test locally**: `docker-compose up --build`
2. **🔍 Verify endpoints**: Test with curl or Postman
3. **☁️ Deploy to Azure**: Use deployment script
4. **🎯 Build next service**: TicketInventory with Cosmos DB

This containerized approach ensures your local development environment **exactly matches** production!
