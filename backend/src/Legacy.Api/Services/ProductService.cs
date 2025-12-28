using Microsoft.EntityFrameworkCore;
using Legacy.Api.Data;
using Legacy.Api.DTOs;
using Legacy.Api.Models;

namespace Legacy.Api.Services;

public class ProductService(LegacyDbContext db) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 10)
    {
        var totalCount = await db.Products.CountAsync();

        var products = await db.Products
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Category = p.Category,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<ProductDto>
        {
            Items = products,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<ProductDto> CreateProductAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<bool> UpdateProductAsync(int id, Product inputProduct)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return false;

        product.Name = inputProduct.Name;
        product.Description = inputProduct.Description;
        product.Price = inputProduct.Price;
        product.StockQuantity = inputProduct.StockQuantity;
        product.Category = inputProduct.Category;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        if (await db.Products.FindAsync(id) is not { } product) return false;

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return true;
    }
}