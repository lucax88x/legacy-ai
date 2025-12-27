using Legacy.Api.Data;
using Legacy.Api.DTOs;
using Legacy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Api;

public static class OrderApis
{
    public static void Map(WebApplication app)
    {
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
            var order = await db.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
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
            if (await db.Orders.FindAsync(id) is not { } order) return Results.NotFound();
            db.Orders.Remove(order);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}