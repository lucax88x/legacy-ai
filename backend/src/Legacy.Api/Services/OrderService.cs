using Microsoft.EntityFrameworkCore;
using Legacy.Api.Data;
using Legacy.Api.DTOs;
using Legacy.Api.Models;

namespace Legacy.Api.Services;

public class OrderService : IOrderService
{
    private readonly LegacyDbContext _db;

    public OrderService(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _db.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ToListAsync();
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
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _db.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return null;

        return new OrderDto
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
        };
    }

    public async Task<OrderDto> CreateOrderAsync(Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        order.OrderDate = DateTime.UtcNow;

        foreach (var item in order.OrderItems)
        {
            item.TotalPrice = item.Quantity * item.UnitPrice;
        }

        order.TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var createdOrder = await GetOrderByIdAsync(order.Id);
        return createdOrder!;
    }

    public async Task<bool> UpdateOrderAsync(int id, Order inputOrder)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return false;

        order.CustomerName = inputOrder.CustomerName;
        order.CustomerEmail = inputOrder.CustomerEmail;
        order.CustomerAddress = inputOrder.CustomerAddress;
        order.Status = inputOrder.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        if (await _db.Orders.FindAsync(id) is not { } order) return false;
        
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return true;

    }
}
