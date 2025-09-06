#!/bin/bash

# Kubernetes Cleanup Script for TicketBooking System

set -e

echo "🧹 Cleaning up TicketBooking System from Kubernetes"
echo "=================================================="

# Check if namespace exists
if kubectl get namespace ticketing-system &> /dev/null; then
    echo "🗑️ Deleting ticketing-system namespace and all resources..."
    kubectl delete namespace ticketing-system
    
    echo "⏳ Waiting for namespace deletion to complete..."
    kubectl wait --for=delete namespace/ticketing-system --timeout=120s
    
    echo "✅ Cleanup completed successfully!"
else
    echo "ℹ️ ticketing-system namespace doesn't exist - nothing to clean up"
fi

echo ""
echo "🐳 Note: Docker images are still available locally"
echo "To remove them:"
echo "docker rmi ticketbookingsystem-eventmanagement-api:latest"
echo "docker rmi ticketbookingsystem-eventsearch-api:latest"
echo "docker rmi ticketbookingsystem-frontend:latest"