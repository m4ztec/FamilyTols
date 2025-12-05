#!/bin/bash
set -e

echo "Starting HomeInventory services..."

# Start Nginx in the background
echo "Starting Nginx..."
nginx -g "daemon off;" &
NGINX_PID=$!

echo "Nginx started (PID: $NGINX_PID)"

# Start the .NET API application
echo "Starting API application..."
cd /app/api
dotnet HomeInventory.api.dll &
API_PID=$!

echo "API started (PID: $API_PID)"

# Function to handle termination
cleanup() {
    echo "Shutting down services..."
    kill $NGINX_PID $API_PID 2>/dev/null || true
    wait $NGINX_PID $API_PID 2>/dev/null || true
    exit 0
}

# Trap SIGTERM and SIGINT
trap cleanup SIGTERM SIGINT

# Wait for both processes
wait $NGINX_PID $API_PID
