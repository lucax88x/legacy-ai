#!/bin/bash

# Wait for Debezium Connect to be ready
echo "Waiting for Debezium Connect to start..."
until curl -s http://localhost:8083/connectors > /dev/null 2>&1; do
    sleep 2
done
echo "Debezium Connect is ready!"

# Register the PostgreSQL connector
echo "Registering PostgreSQL connector..."
curl -X POST http://localhost:8083/connectors \
  -H "Content-Type: application/json" \
  -d @connector-config.json

echo ""
echo "Connector registered! Check status with:"
echo "  curl http://localhost:8083/connectors/legacy-postgres-connector/status"
