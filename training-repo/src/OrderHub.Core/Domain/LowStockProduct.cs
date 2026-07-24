namespace OrderHub.Core.Domain;

/// <summary>唯讀查詢投影，非 DbSet 實體。</summary>
public class LowStockProduct
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int SoldQuantityLast30Days { get; set; }
}
