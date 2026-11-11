namespace SmartStart.Domain.Entities;

public class BarcodeChangeLog
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string OldBarcode { get; set; } = string.Empty;
    public string NewBarcode { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
