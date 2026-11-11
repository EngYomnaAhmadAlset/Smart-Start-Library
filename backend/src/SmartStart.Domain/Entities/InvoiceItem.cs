namespace SmartStart.Domain.Entities;

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitSellingPrice { get; set; }
    public decimal UnitWholesalePrice { get; set; }
    public decimal LineTotal { get; private set; } // DB Generated
    public decimal LineProfit { get; private set; } // DB Generated

    public ICollection<ReturnItem> Returns { get; set; } = new List<ReturnItem>();
}