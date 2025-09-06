#!/bin/bash

# Quick Kubernetes Status Check
echo "🔍 TicketBooking System - Kubernetes Status Check"
echo "=============================================="

# Check if Kubernetes is available
if kubectl cluster-info >/dev/null 2>&1; then
    echo "✅ Kubernetes cluster is accessible"
    echo ""
    
    # Check if our namespace exists
    if kubectl get namespace ticketing-system >/dev/null 2>&1; then
        echo "✅ ticketing-system namespace exists"
        echo ""
        
        echo "📊 Pod Status:"
        kubectl get pods -n ticketing-system -o wide
        echo ""
        
        echo "🔗 Service Status:"
        kubectl get services -n ticketing-system
        echo ""
        
        echo "💾 Storage Status:"
        kubectl get pvc -n ticketing-system
        echo ""
        
        # Check pod health
        echo "🏥 Health Status:"
        READY_PODS=$(kubectl get pods -n ticketing-system --no-headers | grep -c "Running")
        TOTAL_PODS=$(kubectl get pods -n ticketing-system --no-headers | wc -l)
        
        if [ "$READY_PODS" -eq "$TOTAL_PODS" ] && [ "$TOTAL_PODS" -gt 0 ]; then
            echo "✅ All $TOTAL_PODS pods are running and ready"
        else
            echo "⚠️  $READY_PODS out of $TOTAL_PODS pods are ready"
        fi
        
    else
        echo "❌ ticketing-system namespace not found"
        echo "Run: ./k8s/deploy.sh to deploy the system"
    fi
    
else
    echo "❌ Kubernetes cluster not accessible"
    echo ""
    echo "To enable Docker Desktop Kubernetes:"
    echo "1. Open Docker Desktop"
    echo "2. Go to Settings > Kubernetes"
    echo "3. Check 'Enable Kubernetes'"
    echo "4. Click 'Apply & Restart'"
fi