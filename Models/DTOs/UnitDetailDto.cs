namespace KiraTakip.Models.Dtos;

public class UnitDetailDto
{
    public int Id { get; set; }
    public string? UnitNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? FloorNo { get; set; }
    public decimal Area { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public bool CanBeReserved { get; set; }
    public bool CanBeRented { get; set; }
    public OccupancyStatus Status { get; set; }
    public int? ActiveLeaseId { get; set; }
    public int? ActiveLeaseTenantId { get; set; }
    public string? ActiveLeaseTenantDisplayName { get; set; }
    public DateTime? ActiveLeaseEndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public int? RezKuralId { get; set; }
    public decimal? RezKuralPeriyotUcreti { get; set; }
    public int? RezKuralUcretlendirmePeriyoduDakika { get; set; }
    public int? RezKuralUcretsizSureDakika { get; set; }
    public decimal? RezKuralKdvOrani { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
}
