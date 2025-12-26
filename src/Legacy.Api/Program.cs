using Microsoft.EntityFrameworkCore;
using Legacy.Api.Data;
using Legacy.Api.Models;
using Legacy.Api.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<LegacyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Product endpoints
app.MapGet("/api/products", async (LegacyDbContext db) =>
{
    var products = await db.Products.ToListAsync();
    return products.Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        Category = p.Category,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    }).ToList();
});

app.MapGet("/api/products/{id}", async (int id, LegacyDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    return Results.Ok(new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        Category = product.Category,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    });
});

app.MapPost("/api/products", async (Product product, LegacyDbContext db) =>
{
    product.CreatedAt = DateTime.UtcNow;
    product.UpdatedAt = DateTime.UtcNow;
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id}", async (int id, Product inputProduct, LegacyDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = inputProduct.Name;
    product.Description = inputProduct.Description;
    product.Price = inputProduct.Price;
    product.StockQuantity = inputProduct.StockQuantity;
    product.Category = inputProduct.Category;
    product.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/products/{id}", async (int id, LegacyDbContext db) =>
{
    if (await db.Products.FindAsync(id) is not { } product) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Order endpoints
app.MapGet("/api/orders", async (LegacyDbContext db) =>
{
    var orders = await db.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ToListAsync();
    return orders.Select(o => new OrderDto
    {
        Id = o.Id,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        CustomerAddress = o.CustomerAddress,
        OrderDate = o.OrderDate,
        Status = o.Status,
        TotalAmount = o.TotalAmount,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        OrderItems = o.OrderItems.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            ProductId = oi.ProductId,
            ProductName = oi.Product.Name,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            TotalPrice = oi.TotalPrice
        }).ToList()
    }).ToList();
});

app.MapGet("/api/orders/{id}", async (int id, LegacyDbContext db) =>
{
    var order = await db.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == id);
    if (order is null) return Results.NotFound();

    return Results.Ok(new OrderDto
    {
        Id = order.Id,
        CustomerName = order.CustomerName,
        CustomerEmail = order.CustomerEmail,
        CustomerAddress = order.CustomerAddress,
        OrderDate = order.OrderDate,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        OrderItems = order.OrderItems.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            ProductId = oi.ProductId,
            ProductName = oi.Product.Name,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            TotalPrice = oi.TotalPrice
        }).ToList()
    });
});

app.MapPost("/api/orders", async (Order order, LegacyDbContext db) =>
{
    order.CreatedAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;
    order.OrderDate = DateTime.UtcNow;

    foreach (var item in order.OrderItems)
    {
        item.TotalPrice = item.Quantity * item.UnitPrice;
    }

    order.TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice);

    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/api/orders/{order.Id}", order);
});

app.MapPut("/api/orders/{id}", async (int id, Order inputOrder, LegacyDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    order.CustomerName = inputOrder.CustomerName;
    order.CustomerEmail = inputOrder.CustomerEmail;
    order.CustomerAddress = inputOrder.CustomerAddress;
    order.Status = inputOrder.Status;
    order.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/orders/{id}", async (int id, LegacyDbContext db) =>
{
    if (await db.Orders.FindAsync(id) is Order order)
    {
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();