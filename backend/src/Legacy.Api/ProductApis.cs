using Legacy.Api.Models;
using Legacy.Api.Services;

namespace Legacy.Api;

public static class ProductApis
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/products", async (int? page, int? pageSize, IProductService productService) =>
        {
            var p = page ?? 1;
            var ps = pageSize ?? 10;
            if (p < 1) p = 1;
            if (ps < 1) ps = 10;
            if (ps > 100) ps = 100;

            var result = await productService.GetProductsAsync(p, ps);
            return Results.Ok(result);
        });

        app.MapGet("/api/products/{id}", async (int id, IProductService productService) =>
        {
            var product = await productService.GetProductByIdAsync(id);
            if (product is null) return Results.NotFound();

            return Results.Ok(product);
        });

        app.MapPost("/api/products", async (Product product, IProductService productService) =>
        {
            var created = await productService.CreateProductAsync(product);
            return Results.Created($"/api/products/{created.Id}", created);
        });

        app.MapPut("/api/products/{id}", async (int id, Product product, IProductService productService) =>
        {
            var success = await productService.UpdateProductAsync(id, product);
            if (!success) return Results.NotFound();

            return Results.NoContent();
        });

        app.MapDelete("/api/products/{id}", async (int id, IProductService productService) =>
        {
            var success = await productService.DeleteProductAsync(id);
            if (!success) return Results.NotFound();

            return Results.NoContent();
        });
    }
}
