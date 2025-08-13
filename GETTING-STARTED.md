# Getting Started - Ticket Booking System

This guide will help you get the containerized Ticket Booking System running locally with Docker and prepare for Azure deployment.

## Prerequisites

1. **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop/)
2. **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
3. **Azure CLI** (for deployment) - [Install guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
4. **Git** - For version control

## Quick Start (5 minutes)

### 1. Start Docker Desktop
Make sure Docker Desktop is running on your machine.

### 2. Clone and Navigate
```bash
cd /Users/pranavpandey/TicketBookingSystem
```

### 3. Build and Run with Docker Compose
```bash
# Build and start all services
docker-compose up --build

# Or run in background
docker-compose up --build -d
```

### 4. Verify Services
- **API Health Check**: http://localhost:8080/health
- **API Documentation**: http://localhost:8080/swagger
- **SQL Server**: localhost:1433 (sa/YourStrong!Passw0rd)

### 5. Test API Endpoints
```bash
# Get all events
curl http://localhost:8080/api/events

# Create a new event
curl -X POST http://localhost:8080/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Concert",
    "description": "A test concert event",
    "startDateTime": "2024-12-25T19:00:00Z",
    "endDateTime": "2024-12-25T22:00:00Z",
    "venue": "Test Arena",
    "totalTickets": 1000,
    "pricePerTicket": 50.00
  }'
```

## Development Workflow

### Local Development with Docker
```bash
# Start services in development mode
docker-compose -f docker-compose.yml up --build

# Watch logs
docker-compose logs -f eventmanagement

# Stop services
docker-compose down

# Clean up (removes volumes)
docker-compose down -v
```

### Rebuild After Code Changes
```bash
# Rebuild specific service
docker-compose build eventmanagement

# Rebuild and restart
docker-compose up --build eventmanagement
```

### Database Management
```bash
# Connect to SQL Server container
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong!Passw0rd

# Run database migrations
docker-compose exec eventmanagement dotnet ef database update
```

## Project Structure

```
TicketBookingSystem/
├── src/
│   └── EventManagement/
│       ├── EventManagement.API/          # REST API endpoints
│       ├── EventManagement.Core/         # Business logic & entities
│       ├── EventManagement.Infrastructure/ # Data access & repositories
│       └── EventManagement.Tests/        # Unit tests
├── docker-compose.yml                    # Local development
├── docker-compose.azure.yml             # Azure production
├── k8s/                                 # Kubernetes manifests
├── scripts/                             # Build & deployment scripts
└── docs/                               # Documentation
```

## Azure Deployment Options

### Option 1: Azure Container Apps (Recommended for Learning)
```bash
# Login to Azure
az login

# Run deployment script
./scripts/deploy-azure.sh
```

### Option 2: Azure App Service
- Deploy container directly to App Service
- Use Azure SQL Database
- Configure environment variables

### Option 3: Azure Kubernetes Service (AKS)
```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/
```

## Environment Configuration

### Local Development (.env.development)
- Uses Docker SQL Server
- Debug logging enabled
- CORS configured for frontend development

### Production (.env.production)
- Azure SQL Database or managed container
- Optimized logging
- Security headers enabled

## Monitoring & Health Checks

### Health Check Endpoints
- `/health` - Overall application health
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Logs
```bash
# View application logs
docker-compose logs eventmanagement

# View SQL Server logs
docker-compose logs sqlserver
```

## Troubleshooting

### Common Issues

1. **Docker not starting**
   ```bash
   # Check Docker is running
   docker --version
   docker ps
   ```

2. **Port conflicts**
   ```bash
   # Check what's using port 8080
   lsof -i :8080
   
   # Kill process if needed
   kill -9 <PID>
   ```

3. **Database connection issues**
   ```bash
   # Check SQL Server container
   docker-compose logs sqlserver
   
   # Test connection
   docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong!Passw0rd
   ```

4. **Build failures**
   ```bash
   # Clean Docker cache
   docker system prune -a
   
   # Rebuild from scratch
   docker-compose build --no-cache
   ```

### Debug Mode
```bash
# Run with debug output
DOCKER_BUILDKIT=1 docker-compose up --build

# Inspect container
docker-compose exec eventmanagement bash
```

## Next Steps

1. **Add More Services**: TicketInventory, UserManagement, PaymentProcessing
2. **Frontend Development**: Angular/React container
3. **API Gateway**: YARP or Ocelot gateway
4. **Message Queuing**: Redis or Azure Service Bus
5. **Monitoring**: Application Insights, Prometheus

## Learning Resources

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [.NET Docker Documentation](https://docs.microsoft.com/en-us/dotnet/core/docker/)
- [Azure Container Apps](https://docs.microsoft.com/en-us/azure/container-apps/)
- [Kubernetes Basics](https://kubernetes.io/docs/tutorials/kubernetes-basics/)

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review Docker logs
3. Verify environment configuration
4. Test individual components

Happy coding! 🚀
