using Legacy.Api.Models;
using Legacy.Api.Services;

namespace Legacy.Api;

public static class OrderApis
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/orders", async (int? page, int? pageSize, IOrderService orderService) =>
        {
            var p = page ?? 1;
            var ps = pageSize ?? 10;
            if (p < 1) p = 1;
            if (ps < 1) ps = 10;
            if (ps > 100) ps = 100;

            var result = await orderService.GetOrdersAsync(p, ps);
            return Results.Ok(result);
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
