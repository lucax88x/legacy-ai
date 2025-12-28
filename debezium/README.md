# Debezium CDC to Qdrant Sync

This setup captures database changes from PostgreSQL and syncs them to Qdrant (vector database) for semantic search.

## Architecture

```
PostgreSQL → Debezium → Kafka → Sync Service → Qdrant
```

- **Debezium**: Watches PostgreSQL transaction log (WAL) and captures all INSERT/UPDATE/DELETE
- **Kafka**: Message broker that stores the change events
- **Sync Service**: Python app that consumes events, generates embeddings, and writes to Qdrant
- **Qdrant**: Vector database for semantic/similarity search

## Quick Start

### 1. Start all services

```bash
docker compose up -d
```

### 2. Wait for services to be ready (~30 seconds)

Check Debezium is ready:
```bash
curl http://localhost:8083/connectors
# Should return: []
```

### 3. Register the PostgreSQL connector

```bash
./register_connector.sh
```

### 4. Verify connector is running

```bash
curl http://localhost:8083/connectors/legacy-postgres-connector/status
```

## How It Works

1. When you INSERT/UPDATE/DELETE in PostgreSQL tables (Products, Orders, OrderItems)
2. Debezium captures the change and publishes to Kafka topics
3. The sync service consumes these events
4. It generates vector embeddings from the text data
5. Vectors are stored in Qdrant for semantic search

## Ports

| Service | Port | Description |
|---------|------|-------------|
| PostgreSQL | 5432 | Database |
| Qdrant REST | 6333 | Vector DB API |
| Qdrant gRPC | 6334 | Vector DB gRPC |
| Kafka | 9092 | Message broker |
| Debezium | 8083 | CDC connector API |

## Useful Commands

### Check Kafka topics
```bash
docker exec legacy_kafka kafka-topics --list --bootstrap-server localhost:9092
```

### View sync service logs
```bash
docker logs -f legacy_qdrant_sync
```

### Query Qdrant collections
```bash
# List collections
curl http://localhost:6333/collections

# Get collection info
curl http://localhost:6333/collections/products

# Search products (example)
curl -X POST http://localhost:6333/collections/products/points/search \
  -H "Content-Type: application/json" \
  -d '{
    "vector": [0.1, 0.2, ...],  # Your query vector
    "limit": 5
  }'
```

### Delete and recreate connector
```bash
curl -X DELETE http://localhost:8083/connectors/legacy-postgres-connector
./register_connector.sh
```

## Troubleshooting

### Connector fails to start
- Ensure PostgreSQL is running with `wal_level=logical`
- Check Debezium logs: `docker logs legacy_debezium`

### No events in sync service
- Verify connector status is "RUNNING"
- Check if topics were created in Kafka
- Make sure tables have data

### Sync service can't connect
- Wait longer for Kafka/Qdrant to be ready
- Check logs: `docker logs legacy_qdrant_sync`
