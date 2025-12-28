using Legacy.Api.DTOs;
using Legacy.Api.Models;

namespace Legacy.Api.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 10);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto> CreateProductAsync(Product product);
    Task<bool> UpdateProductAsync(int id, Product product);
    Task<bool> DeleteProductAsync(int id);
}
