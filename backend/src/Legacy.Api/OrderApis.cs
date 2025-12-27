using Legacy.Api.Models;
using Legacy.Api.Services;

namespace Legacy.Api;

public static class OrderApis
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/orders", async (IOrderService orderService) =>
        {
            var orders = await orderService.GetAllOrdersAsync();
            return Results.Ok(orders);
        });

        app.MapGet("/api/orders/{id}", async (int id, IOrderService orderService) =>
        {
            var order = await orderService.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();

            return Results.Ok(order);
        });

        app.MapPost("/api/orders", async (Order order, IOrderService orderService) =>
        {
            var created = await orderService.CreateOrderAsync(order);
            return Results.Created($"/api/orders/{created.Id}", created);
        });

        app.MapPut("/api/orders/{id}", async (int id, Order order, IOrderService orderService) =>
        {
            var success = await orderService.UpdateOrderAsync(id, order);
            if (!success) return Results.NotFound();

            return Results.NoContent();
        });

        app.MapDelete("/api/orders/{id}", async (int id, IOrderService orderService) =>
        {
            var success = await orderService.DeleteOrderAsync(id);
            if (!success) return Results.NotFound();

            return Results.NoContent();
        });
    }
}
