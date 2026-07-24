using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold, DateTime soldSince)
    {
        var products = await _db.Products
            .Where(p => p.IsActive && p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var soldQuantities = await _db.OrderItems
            .Where(i => productIds.Contains(i.ProductId)
                     && i.Order!.Status != OrderStatus.Cancelled
                     && i.Order.CreatedAt >= soldSince)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Sold);

        return products.Select(p => new LowStockProduct
        {
            Sku = p.Sku,
            Name = p.Name,
            StockQuantity = p.StockQuantity,
            SoldQuantityLast30Days = soldQuantities.GetValueOrDefault(p.Id)
        }).ToList();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
