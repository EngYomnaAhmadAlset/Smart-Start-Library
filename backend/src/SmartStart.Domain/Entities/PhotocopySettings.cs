namespace SmartStart.Domain.Entities;

public class PhotocopySettings
{
    public int Id { get; set; }
    public decimal MaintenanceCost { get; set; } = 10m;
    public decimal ElectricityCost { get; set; } = 20m;
}
