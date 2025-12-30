# Backend - .NET API

.NET 10 Web API with Semantic Kernel integration.

## Commands

```bash
cd src/Legacy.Api && dotnet watch run   # Development
dotnet build                            # Build
```

## Project Structure

```
src/Legacy.Api/
├── Program.cs          # Entry point, DI configuration
├── Data/               # DbContext
├── Models/             # Entity models (Product, Order, OrderItem)
├── DTOs/               # Data transfer objects (ProductDto, OrderDto, PagedResult)
├── Services/           # Business logic (IProductService, IOrderService)
├── Plugins/            # Semantic Kernel plugins (ProductsPlugin, OrdersPlugin)
├── *Apis.cs            # Minimal API endpoint definitions
└── Filters/            # Exception filters
```

## Key Dependencies

- **Semantic Kernel** - AI orchestration with plugins
- **Npgsql + EF Core** - PostgreSQL with Entity Framework
- **Qdrant** - Vector search connector
- **OpenTelemetry** - Distributed tracing

## Environment

Requires `src/Legacy.Api/.env` with OpenAI key (copy from `.env.template`).
