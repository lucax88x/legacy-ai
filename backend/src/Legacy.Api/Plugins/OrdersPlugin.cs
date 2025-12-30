using System.ComponentModel;
using Microsoft.SemanticKernel;
using Legacy.Api.Services;
using Legacy.Api.Models;
using System.Text.Json;

namespace Legacy.Api.Plugins;

public class OrdersPlugin(IOrderService orderService)
{
    [KernelFunction]
    [Description("Get orders from the system with pagination")]
    public async Task<string> GetOrders(
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of orders per page")] int pageSize = 10)
    {
        var orders = await orderService.GetOrdersAsync(page, pageSize);
        return JsonSerializer.Serialize(orders);
    }

    [KernelFunction]
    [Description("Get an order by its ID")]
    public async Task<string> GetOrderById([Description("The ID of the order")] int orderId)
    {
        var order = await orderService.GetOrderByIdAsync(orderId);
        if (order is null)
            return $"Order with ID {orderId} not found.";

        return JsonSerializer.Serialize(order);
    }

    [KernelFunction]
    [Description("Create a new order. Provide customer details and order items in JSON format.")]
    public async Task<string> CreateOrder(
        [Description("Customer name")] string customerName,
        [Description("Customer email")] string customerEmail,
        [Description("Customer address")] string customerAddress,
        [Description("Order status (Pending, Processing, Shipped, Delivered, Cancelled)")]
        string status = "Pending")
    {
        var orderStatus = Enum.Parse<OrderStatus>(status, true);
        var order = new Order
        {
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CustomerAddress = customerAddress,
            Status = orderStatus,
            OrderItems = new List<OrderItem>()
        };

        var result = await orderService.CreateOrderAsync(order);
        return $"Order created successfully with ID: {result.Id}. Note: You need to add order items separately.";
    }

    [KernelFunction]
    [Description("Update an existing order")]
    public async Task<string> UpdateOrder(
        [Description("The ID of the order to update")]
        int orderId,
        [Description("Customer name")] string? customerName = null,
        [Description("Customer email")] string? customerEmail = null,
        [Description("Customer address")] string? customerAddress = null,
        [Description("Order status (Pending, Processing, Shipped, Delivered, Cancelled)")]
        string? status = null)
    {
        var existingOrder = await orderService.GetOrderByIdAsync(orderId);
        if (existingOrder is null)
            return $"Order with ID {orderId} not found.";

        var order = new Order
        {
            CustomerName = customerName ?? existingOrder.CustomerName,
            CustomerEmail = customerEmail ?? existingOrder.CustomerEmail,
            CustomerAddress = customerAddress ?? existingOrder.CustomerAddress,
            Status = status != null ? Enum.Parse<OrderStatus>(status, true) : existingOrder.Status
        };

        var success = await orderService.UpdateOrderAsync(orderId, order);
        return success ? $"Order {orderId} updated successfully." : $"Failed to update order {orderId}.";
    }

    [KernelFunction]
    [Description("Delete an order by its ID")]
    public async Task<string> DeleteOrder([Description("The ID of the order to delete")] int orderId)
    {
        var success = await orderService.DeleteOrderAsync(orderId);
        return success ? $"Order {orderId} deleted successfully." : $"Order with ID {orderId} not found.";
    }
}