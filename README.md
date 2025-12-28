# Legacy - AI-Enhanced CRUD Application

A legacy CRUD application enhanced with AI capabilities.

## What This Project Demonstrates

Taking a traditional CRUD backend/frontend and adding:

- **Semantic Kernel** with plugins to let an LLM interact with the database
- **Qdrant** for vector search on products
- **Debezium + Kafka** to keep PostgreSQL and Qdrant in sync via CDC

## Architecture

```
Angular Frontend  →  .NET API + Semantic Kernel  →  PostgreSQL
                            ↓                           ↓ (CDC)
                        Qdrant  ←─────────────────  Debezium/Kafka
```

## Stack

- **Backend**: .NET 10, Semantic Kernel, OpenAI
- **Frontend**: Angular 20 with chatbot component
- **Data**: PostgreSQL, Qdrant, Debezium, Kafka
- **Observability**: OpenTelemetry → Grafana LGTM

## Quick Start

```bash
# Start infrastructure
pnpm docker:up

# Configure OpenAI key
cp backend/src/Legacy.Api/.env.template backend/src/Legacy.Api/.env

# Register Debezium connector
./debezium/register_connector.sh

# Run 
pnpm run dev
```

