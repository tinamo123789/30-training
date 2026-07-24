using OrderHub.Core.Domain;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    private static void SeedOrder(OrderHubDbContext db, Customer customer, OrderStatus status, DateTime createdAt, Product product, int quantity)
    {
        var order = new Order
        {
            CustomerId = customer.Id,
            Status = status,
            CreatedAt = createdAt,
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, Quantity = quantity, UnitPriceSnapshot = product.UnitPrice }
            }
        };
        db.Orders.Add(order);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetLowStock_FiltersAndSortsByThreshold()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-LOW1", stock: 3);
        TestSetup.AddProduct(db, sku: "SKU-LOW2", stock: 8);
        TestSetup.AddProduct(db, sku: "SKU-HIGH", stock: 20);

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("SKU-LOW1", result[0].Sku);
        Assert.Equal("SKU-LOW2", result[1].Sku);
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-INACTIVE", stock: 2, isActive: false);

        var result = await service.GetLowStockAsync(10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLowStock_SoldQuantityExcludesCancelledAndOlderThan30Days()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, sku: "SKU-SOLD", stock: 5);
        SeedOrder(db, customer, OrderStatus.Confirmed, DateTime.UtcNow.AddDays(-10), product, 4);
        SeedOrder(db, customer, OrderStatus.Cancelled, DateTime.UtcNow.AddDays(-5), product, 100);
        SeedOrder(db, customer, OrderStatus.Confirmed, DateTime.UtcNow.AddDays(-40), product, 100);

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(4, Assert.Single(result).SoldQuantityLast30Days);
    }
}
