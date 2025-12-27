using Legacy.Api.Data;
using Legacy.Api.DTOs;
using Legacy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Api;

public static class ProductApis
{
    public static void Map(WebApplication app)
    {
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
    }
}