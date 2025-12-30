# Legacy Project

AI-enhanced CRUD application with Semantic Kernel, Qdrant vector search, and CDC sync.

## Architecture

```
Angular Frontend → .NET API + Semantic Kernel → PostgreSQL
                          ↓                          ↓ (CDC)
                      Qdrant ←────────────────── Debezium/Kafka
```

## Quick Start

```bash
pnpm docker:up          # Start infrastructure
pnpm run dev            # Run both frontend and backend
```

## Project Structure

- `backend/` - .NET 10 API with Semantic Kernel
- `frontend/` - Angular 21 application
- `debezium/` - CDC connector and Qdrant sync service
- `k6/` - Load testing
- `observability/` - Tempo config for tracing

## Infrastructure (docker-compose.yml)

| Service | Port | Purpose |
|---------|------|---------|
| PostgreSQL | 5432 | Primary database |
| Grafana | 3000 | Observability UI |
| Tempo | 3200 | Trace backend |
| OTLP | 4317/4318 | Telemetry collector |
| Qdrant | 6333/6334 | Vector database |
| Kafka | 9092 | Message broker |
| Debezium | 8083 | CDC connector |

## Environment

Requires `.env` file at root with `ANTHROPIC_API_KEY` for Qdrant sync service.
