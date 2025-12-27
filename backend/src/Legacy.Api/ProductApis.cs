using Legacy.Api.Models;
using Legacy.Api.Services;

namespace Legacy.Api;

public static class ProductApis
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/products", async (IProductService productService) =>
        {
            var products = await productService.GetAllProductsAsync();
            return Results.Ok(products);
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
