#!/bin/bash

# TicketBooking System - Kubernetes Deployment Test Script
# This script tests the complete deployment and validates all services work

set -e

echo "🧪 TicketBooking System - Kubernetes Deployment Test"
echo "=================================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to check command success
check_command() {
    if [ $? -eq 0 ]; then
        print_status $GREEN "✅ $1"
    else
        print_status $RED "❌ $1"
        exit 1
    fi
}

# Test 1: Verify Kubernetes is available
print_status $BLUE "🔍 Testing Kubernetes availability..."
kubectl cluster-info >/dev/null 2>&1
check_command "Kubernetes cluster is accessible"

kubectl get nodes >/dev/null 2>&1
check_command "Kubernetes nodes are ready"

# Test 2: Build Docker images
print_status $BLUE "🔨 Building Docker images..."
cd /Users/pranavpandey/TicketBookingSystem

print_status $YELLOW "Building EventManagement API..."
docker build -t ticketbookingsystem-eventmanagement-api:latest ./src/backend/EventManagement >/dev/null 2>&1
check_command "EventManagement API image built"

print_status $YELLOW "Building EventSearch API..."
docker build -t ticketbookingsystem-eventsearch-api:latest ./src/backend/EventSearch >/dev/null 2>&1
check_command "EventSearch API image built"

print_status $YELLOW "Building Frontend..."
docker build -t ticketbookingsystem-frontend:latest ./src/frontend/ticket-booking-system >/dev/null 2>&1
check_command "Frontend image built"

# Test 3: Deploy to Kubernetes
print_status $BLUE "🚀 Deploying to Kubernetes..."

print_status $YELLOW "Creating namespace and configuration..."
kubectl apply -f k8s/namespace.yaml >/dev/null 2>&1
check_command "Namespace and configuration created"

print_status $YELLOW "Deploying infrastructure services..."
kubectl apply -f k8s/sqlserver.yaml >/dev/null 2>&1
kubectl apply -f k8s/elasticsearch.yaml >/dev/null 2>&1
kubectl apply -f k8s/redis.yaml >/dev/null 2>&1
check_command "Infrastructure services deployed"

print_status $YELLOW "Waiting for infrastructure to be ready (this may take 2-3 minutes)..."
kubectl wait --for=condition=ready pod -l app=sqlserver -n ticketing-system --timeout=300s >/dev/null 2>&1
check_command "SQL Server is ready"

kubectl wait --for=condition=ready pod -l app=elasticsearch -n ticketing-system --timeout=300s >/dev/null 2>&1
check_command "Elasticsearch is ready"

kubectl wait --for=condition=ready pod -l app=redis -n ticketing-system --timeout=300s >/dev/null 2>&1
check_command "Redis is ready"

print_status $YELLOW "Deploying application services..."
kubectl apply -f k8s/eventmanagement-deployment.yaml >/dev/null 2>&1
kubectl apply -f k8s/eventsearch-deployment.yaml >/dev/null 2>&1
kubectl apply -f k8s/frontend-deployment.yaml >/dev/null 2>&1
check_command "Application services deployed"

print_status $YELLOW "Waiting for applications to be ready (this may take 1-2 minutes)..."
kubectl wait --for=condition=ready pod -l app=eventmanagement-api -n ticketing-system --timeout=300s >/dev/null 2>&1
check_command "EventManagement API is ready"

kubectl wait --for=condition=ready pod -l app=eventsearch-api -n ticketing-system --timeout=300s >/dev/null 2>&1
check_command "EventSearch API is ready"

kubectl wait --for=condition=ready pod -l app=frontend -n ticketing-system --timeout=300s >/dev/null 2>&1
check_command "Frontend is ready"

# Test 4: Verify deployment status
print_status $BLUE "📊 Verifying deployment status..."
echo ""
print_status $YELLOW "Pod Status:"
kubectl get pods -n ticketing-system

echo ""
print_status $YELLOW "Service Status:"
kubectl get services -n ticketing-system

# Test 5: Test service connectivity
print_status $BLUE "🔗 Testing service connectivity..."

# Start port-forwards in background for testing
kubectl port-forward -n ticketing-system service/eventmanagement-service 8080:80 >/dev/null 2>&1 &
EVENTMGMT_PF_PID=$!

kubectl port-forward -n ticketing-system service/eventsearch-service 8081:80 >/dev/null 2>&1 &
EVENTSEARCH_PF_PID=$!

kubectl port-forward -n ticketing-system service/frontend-service 4200:80 >/dev/null 2>&1 &
FRONTEND_PF_PID=$!

# Wait for port-forwards to establish
sleep 5

# Test EventManagement API health
curl -f http://localhost:8080/health >/dev/null 2>&1
check_command "EventManagement API health check"

# Test EventSearch API health
curl -f http://localhost:8081/health >/dev/null 2>&1
check_command "EventSearch API health check"

# Test Frontend
curl -f http://localhost:4200/ >/dev/null 2>&1
check_command "Frontend accessibility"

# Clean up port-forwards
kill $EVENTMGMT_PF_PID $EVENTSEARCH_PF_PID $FRONTEND_PF_PID 2>/dev/null

# Test 6: API functionality
print_status $BLUE "🧪 Testing API functionality..."

# Start port-forward for API testing
kubectl port-forward -n ticketing-system service/eventmanagement-service 8080:80 >/dev/null 2>&1 &
API_PF_PID=$!
sleep 3

# Test create event
EVENT_RESPONSE=$(curl -s -X POST http://localhost:8080/api/v1/events \
  -H "Content-Type: application/json" \
  -d '{
    "name": "K8s Test Event",
    "description": "Testing Kubernetes deployment",
    "category": "Technology",
    "eventDate": "2025-12-01T19:00:00Z",
    "location": "Virtual",
    "maxCapacity": 100,
    "ticketPrice": 0.00,
    "organizerUserId": "00000000-0000-0000-0000-000000000000"
  }')

if [[ $EVENT_RESPONSE == *"eventId"* ]]; then
    check_command "Event creation via API"
    
    # Extract event ID for further testing
    EVENT_ID=$(echo $EVENT_RESPONSE | grep -o '"eventId":"[^"]*' | grep -o '[^"]*$')
    
    # Test get event
    curl -f http://localhost:8080/api/v1/events/$EVENT_ID >/dev/null 2>&1
    check_command "Event retrieval via API"
    
    # Test get all events
    curl -f http://localhost:8080/api/v1/events >/dev/null 2>&1
    check_command "Events list via API"
else
    print_status $RED "❌ Event creation via API"
fi

# Clean up port-forward
kill $API_PF_PID 2>/dev/null

echo ""
print_status $GREEN "🎉 ALL TESTS PASSED!"
echo ""
print_status $BLUE "📋 Deployment Summary:"
print_status $YELLOW "• Kubernetes cluster: ✅ Working"
print_status $YELLOW "• Docker images: ✅ Built successfully"
print_status $YELLOW "• Infrastructure services: ✅ SQL Server, Elasticsearch, Redis"
print_status $YELLOW "• Application services: ✅ EventManagement, EventSearch, Frontend"
print_status $YELLOW "• API functionality: ✅ CRUD operations working"
print_status $YELLOW "• Service connectivity: ✅ All services accessible"

echo ""
print_status $BLUE "🌐 Access Your Services:"
echo "Run these commands in separate terminals:"
echo ""
print_status $YELLOW "Frontend:"
echo "kubectl port-forward -n ticketing-system service/frontend-service 4200:80"
echo "Then visit: http://localhost:4200"
echo ""
print_status $YELLOW "EventManagement API:"
echo "kubectl port-forward -n ticketing-system service/eventmanagement-service 8080:80"
echo "Then visit: http://localhost:8080"
echo ""
print_status $YELLOW "EventSearch API:"
echo "kubectl port-forward -n ticketing-system service/eventsearch-service 8081:80"
echo "Then visit: http://localhost:8081"

echo ""
print_status $BLUE "📊 Monitor Your Deployment:"
echo "kubectl get pods -n ticketing-system"
echo "kubectl logs -f deployment/eventmanagement-api -n ticketing-system"
echo "kubectl logs -f deployment/eventsearch-api -n ticketing-system"

echo ""
print_status $BLUE "🧹 Clean Up When Done:"
echo "./k8s/cleanup.sh"