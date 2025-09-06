#!/bin/bash

# Kubernetes Deployment Script for TicketBooking System
# This script deploys all current Docker Compose services to Kubernetes

set -e

echo "🚀 TicketBooking System - Kubernetes Deployment"
echo "================================================"

# Check if kubectl is available
if ! command -v kubectl &> /dev/null; then
    echo "❌ kubectl is not installed or not in PATH"
    echo "Please install kubectl and ensure it's configured"
    exit 1
fi

# Check if Docker Desktop Kubernetes is running or if we have a context
if ! kubectl config current-context &> /dev/null; then
    echo "❌ No Kubernetes context configured"
    echo ""
    echo "To enable Docker Desktop Kubernetes:"
    echo "1. Open Docker Desktop"
    echo "2. Go to Settings > Kubernetes"
    echo "3. Check 'Enable Kubernetes'"
    echo "4. Click 'Apply & Restart'"
    echo ""
    echo "Alternatively, you can use minikube:"
    echo "1. Install minikube: brew install minikube"
    echo "2. Start cluster: minikube start"
    echo ""
    exit 1
fi

echo "✅ Kubernetes context: $(kubectl config current-context)"
echo ""

# Build Docker images first (they need to exist locally for K8s)
echo "🔨 Building Docker images..."
cd /Users/pranavpandey/TicketBookingSystem

echo "Building EventManagement API..."
docker build -t ticketbookingsystem-eventmanagement-api:latest ./src/backend/EventManagement

echo "Building EventSearch API..."
docker build -t ticketbookingsystem-eventsearch-api:latest ./src/backend/EventSearch

echo "Building Frontend..."
docker build -t ticketbookingsystem-frontend:latest ./src/frontend/ticket-booking-system

echo "✅ All images built successfully"
echo ""

# Deploy to Kubernetes
echo "🚀 Deploying to Kubernetes..."

# Apply namespace and configs first
echo "📦 Creating namespace and configuration..."
kubectl apply -f k8s/namespace.yaml

# Deploy infrastructure services
echo "🗄️ Deploying infrastructure services..."
kubectl apply -f k8s/sqlserver.yaml
kubectl apply -f k8s/elasticsearch.yaml
kubectl apply -f k8s/redis.yaml

# Wait for infrastructure to be ready
echo "⏳ Waiting for infrastructure services to be ready..."
kubectl wait --for=condition=ready pod -l app=sqlserver -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=elasticsearch -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n ticketing-system --timeout=300s

# Deploy application services
echo "🚀 Deploying application services..."
kubectl apply -f k8s/eventmanagement-deployment.yaml
kubectl apply -f k8s/eventsearch-deployment.yaml
kubectl apply -f k8s/frontend-deployment.yaml

# Wait for applications to be ready
echo "⏳ Waiting for application services to be ready..."
kubectl wait --for=condition=ready pod -l app=eventmanagement-api -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=eventsearch-api -n ticketing-system --timeout=300s
kubectl wait --for=condition=ready pod -l app=frontend -n ticketing-system --timeout=300s

echo ""
echo "🎉 Deployment completed successfully!"
echo ""

# Display status
echo "📊 Deployment Status:"
kubectl get pods -n ticketing-system -o wide

echo ""
echo "🔗 Service Information:"
kubectl get services -n ticketing-system

echo ""
echo "🌐 Access Information:"
echo "Frontend: http://localhost (after port-forward)"
echo "EventManagement API: http://localhost:8080 (after port-forward)"
echo "EventSearch API: http://localhost:8081 (after port-forward)"

echo ""
echo "🚀 To access services locally, run:"
echo "kubectl port-forward -n ticketing-system service/frontend-service 4200:80"
echo "kubectl port-forward -n ticketing-system service/eventmanagement-service 8080:80"
echo "kubectl port-forward -n ticketing-system service/eventsearch-service 8081:80"

echo ""
echo "📋 To check logs:"
echo "kubectl logs -f deployment/eventmanagement-api -n ticketing-system"
echo "kubectl logs -f deployment/eventsearch-api -n ticketing-system"
echo "kubectl logs -f deployment/frontend -n ticketing-system"

echo ""
echo "🧹 To clean up:"
echo "kubectl delete namespace ticketing-system"