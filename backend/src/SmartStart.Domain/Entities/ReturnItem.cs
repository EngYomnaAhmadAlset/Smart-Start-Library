namespace SmartStart.Domain.Entities;

public class ReturnItem
{
    public int Id { get; set; }
    public int InvoiceItemId { get; set; }
    public InvoiceItem InvoiceItem { get; set; } = null!;
    public int QuantityReturned { get; set; }
    public int? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }
    public DateTime ReturnedAt { get; set; } = DateTime.UtcNow;
}