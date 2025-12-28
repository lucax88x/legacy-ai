# Legacy E-commerce API

A .NET 10 minimal API project with Entity Framework Core and PostgreSQL for managing products and orders.

## Project Structure

- **Legacy.Api**: Main web API project with CRUD endpoints
- **Legacy.DataGenerator**: Console application for generating fake data using Bogus
- **docker-compose.yml**: PostgreSQL database setup

## Features

- Minimal API with CRUD operations for Products and Orders
- Entity Framework Core with PostgreSQL
- Automatic fake data generation
- Docker Compose for easy database setup

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker and Docker Compose

### Setup

1. **Start PostgreSQL database**:
   ```bash
   docker-compose up -d
   ```

2. **Generate fake data**:
   ```bash
   cd Legacy.DataGenerator
   dotnet run
   ```

3. **Run the API**:
   ```bash
   cd Legacy.Api
   dotnet run
   ```

4. **Access the API**:
   - Base URL: `https://localhost:7000` or `http://localhost:5179`
   - OpenAPI/Swagger: Available in development mode

## API Endpoints

### Products
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create a new product
- `PUT /api/products/{id}` - Update a product
- `DELETE /api/products/{id}` - Delete a product

### Orders
- `GET /api/orders` - Get all orders with order items
- `GET /api/orders/{id}` - Get order by ID with order items
- `POST /api/orders` - Create a new order
- `PUT /api/orders/{id}` - Update an order
- `DELETE /api/orders/{id}` - Delete an order

## Data Models

### Product
- Id, Name, Description, Price, StockQuantity, Category
- CreatedAt, UpdatedAt timestamps

### Order
- Id, CustomerName, CustomerEmail, CustomerAddress
- OrderDate, Status (Pending, Processing, Shipped, Delivered, Cancelled)
- TotalAmount, CreatedAt, UpdatedAt timestamps

### OrderItem
- Id, OrderId, ProductId, Quantity, UnitPrice, TotalPrice

## Database Configuration

The connection string is configured in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=legacy_db;Username=postgres;Password=postgres"
  }
}
```

## Data Generation

The CLI tool generates:
- 100 fake products with realistic names, descriptions, prices, and categories
- 50 fake orders with 1-5 random order items each
- Realistic customer information and order statuses

Run the data generator:
```bash
cd Legacy.DataGenerator
dotnet run
```
