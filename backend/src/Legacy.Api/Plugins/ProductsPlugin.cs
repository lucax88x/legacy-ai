using System.ComponentModel;
using Microsoft.SemanticKernel;
using Legacy.Api.Services;
using Legacy.Api.Models;
using System.Text.Json;

namespace Legacy.Api.Plugins;

public class ProductsPlugin
{
    private readonly IProductService _productService;

    public ProductsPlugin(IProductService productService)
    {
        _productService = productService;
    }

    [KernelFunction]
    [Description("Get products from the catalog with pagination")]
    public async Task<string> GetProducts(
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Number of products per page (default: 10, max: 100)")] int pageSize = 10)
    {
        var result = await _productService.GetProductsAsync(page, pageSize);
        return JsonSerializer.Serialize(result);
    }

    [KernelFunction]
    [Description("Get a product by its ID")]
    public async Task<string> GetProductById([Description("The ID of the product")] int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product is null)
            return $"Product with ID {productId} not found.";

        return JsonSerializer.Serialize(product);
    }

    [KernelFunction]
    [Description("Create a new product in the catalog")]
    public async Task<string> CreateProduct(
        [Description("Product name")] string name,
        [Description("Product description")] string description,
        [Description("Product price")] decimal price,
        [Description("Stock quantity")] int stockQuantity,
        [Description("Product category")] string category)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            Category = category
        };

        var result = await _productService.CreateProductAsync(product);
        return $"Product created successfully with ID: {result.Id}";
    }

    [KernelFunction]
    [Description("Update an existing product")]
    public async Task<string> UpdateProduct(
        [Description("The ID of the product to update")] int productId,
        [Description("Product name")] string? name = null,
        [Description("Product description")] string? description = null,
        [Description("Product price")] decimal? price = null,
        [Description("Stock quantity")] int? stockQuantity = null,
        [Description("Product category")] string? category = null)
    {
        var existingProduct = await _productService.GetProductByIdAsync(productId);
        if (existingProduct is null)
            return $"Product with ID {productId} not found.";

        var product = new Product
        {
            Name = name ?? existingProduct.Name,
            Description = description ?? existingProduct.Description,
            Price = price ?? existingProduct.Price,
            StockQuantity = stockQuantity ?? existingProduct.StockQuantity,
            Category = category ?? existingProduct.Category
        };

        var success = await _productService.UpdateProductAsync(productId, product);
        return success ? $"Product {productId} updated successfully." : $"Failed to update product {productId}.";
    }

    [KernelFunction]
    [Description("Delete a product by its ID")]
    public async Task<string> DeleteProduct([Description("The ID of the product to delete")] int productId)
    {
        var success = await _productService.DeleteProductAsync(productId);
        return success ? $"Product {productId} deleted successfully." : $"Product with ID {productId} not found.";
    }
}
