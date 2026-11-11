namespace SmartStart.Domain.Entities;

public class RestockEvent
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QuantityAdded { get; set; }
    public decimal PreviousWholesalePrice { get; set; }
    public decimal EnteredWholesalePrice { get; set; }
    public decimal ResultingWholesalePrice { get; set; }
    public int? RestockedByUserId { get; set; }
    public User? RestockedByUser { get; set; }
    public DateTime RestockedAt { get; set; } = DateTime.UtcNow;
}