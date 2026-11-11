namespace SmartStart.Domain.Entities;

public class PhotocopyInkLevel
{
    public int Id { get; set; }
    public InkCoverageLevel Level { get; set; }
    public decimal WholesalePerPage { get; set; }
    public decimal SellingPerPage { get; set; }
    public int RemainingPages { get; set; }
}
