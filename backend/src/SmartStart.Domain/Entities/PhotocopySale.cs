namespace SmartStart.Domain.Entities;

public class PhotocopySale
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;

    public int CashierId { get; set; }
    public User Cashier { get; set; } = null!;

    public int PaperTypeId { get; set; }
    public PhotocopyPaperType PaperType { get; set; } = null!;
    public string PaperName { get; set; } = string.Empty;

    public InkCoverageLevel InkLevel { get; set; }
    public int PageCount { get; set; }

    public decimal PaperWholesalePrice { get; set; }
    public decimal PaperSellingPrice { get; set; }
    public decimal InkWholesalePerPage { get; set; }
    public decimal InkSellingPerPage { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal ElectricityCost { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitPercent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
