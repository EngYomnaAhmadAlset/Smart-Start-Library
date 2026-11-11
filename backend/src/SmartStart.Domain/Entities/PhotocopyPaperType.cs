namespace SmartStart.Domain.Entities;

public class PhotocopyPaperType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal WholesalePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int TotalReceivedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
