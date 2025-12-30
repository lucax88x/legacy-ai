using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Bogus;
using Legacy.Api.Models;

var services = new ServiceCollection();

services.AddDbContext<Legacy.Api.Data.LegacyDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=legacy_db;Username=postgres;Password=postgres"));

services.AddLogging(builder => builder.AddConsole());

var serviceProvider = services.BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<Legacy.Api.Data.LegacyDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting data generation...");

    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    logger.LogInformation("Database created/ensured");

    // Check if data already exists
    var existingProductCount = await context.Products.CountAsync();
    if (existingProductCount > 0)
    {
        logger.LogInformation($"Database already has {existingProductCount} products. Skipping data generation.");
        return;
    }

    // Generate fake products
    var productFaker = new Faker<Product>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
        .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(1, 1000), 2))
        .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 1000))
        .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
        .RuleFor(p => p.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
        .RuleFor(p => p.UpdatedAt, (f, p) => p.CreatedAt.AddDays(f.Random.Double(0, 30)));

    var products = productFaker.Generate(100);
    await context.Products.AddRangeAsync(products);
    await context.SaveChangesAsync();
    logger.LogInformation($"Generated {products.Count} products");

    // Generate fake orders with order items
    var customerFaker = new Faker<Order>()
        .RuleFor(o => o.CustomerName, f => f.Person.FullName)
        .RuleFor(o => o.CustomerEmail, f => f.Internet.Email())
        .RuleFor(o => o.CustomerAddress, f => f.Address.FullAddress())
        .RuleFor(o => o.OrderDate, f => f.Date.Past(6).ToUniversalTime())
        .RuleFor(o => o.Status, f => f.PickRandom<OrderStatus>())
        .RuleFor(o => o.CreatedAt, (f, o) => o.OrderDate)
        .RuleFor(o => o.UpdatedAt, (f, o) => o.CreatedAt.AddDays(f.Random.Double(0, 5)));

    var orders = new List<Order>();
    var random = new Random();

    for (int i = 0; i < 50; i++)
    {
        var order = customerFaker.Generate();

        // Generate 1-5 order items per order
        var orderItemCount = random.Next(1, 6);
        var selectedProducts = products.OrderBy(x => Guid.NewGuid()).Take(orderItemCount).ToList();

        foreach (var product in selectedProducts)
        {
            var quantity = random.Next(1, 5);
            var unitPrice = product.Price * (decimal)(0.8 + random.NextDouble() * 0.4); // ±20% price variation

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = Math.Round(unitPrice, 2),
                TotalPrice = Math.Round(unitPrice * quantity, 2)
            };

            order.OrderItems.Add(orderItem);
        }

        order.TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice);
        orders.Add(order);
    }

    await context.Orders.AddRangeAsync(orders);
    await context.SaveChangesAsync();

    var totalOrderItems = orders.Sum(o => o.OrderItems.Count);
    logger.LogInformation($"Generated {orders.Count} orders with {totalOrderItems} order items");
    logger.LogInformation("Data generation completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error occurred during data generation");
    throw;
}
finally
{
    await serviceProvider.DisposeAsync();
}
