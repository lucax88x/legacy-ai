using Legacy.Api.DTOs;
using Legacy.Api.Models;

namespace Legacy.Api.Services;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 10);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto> CreateOrderAsync(Order order);
    Task<bool> UpdateOrderAsync(int id, Order order);
    Task<bool> DeleteOrderAsync(int id);
}
