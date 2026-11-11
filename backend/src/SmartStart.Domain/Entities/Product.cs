namespace SmartStart.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal WholesalePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal ProfitMarginPercent { get; private set; } // Map to DB Generated Column
    public int TotalReceivedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int MinStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RestockEvent> RestockEvents { get; set; } = new List<RestockEvent>();
}