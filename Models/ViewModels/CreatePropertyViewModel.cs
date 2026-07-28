namespace KiraTakip.Models.ViewModels;

public class CreatePropertyViewModel
{
    public string Name { get; set; } = string.Empty;
    public ParentRateCardViewModel? ParentRate { get; set; }
    public ParentReservationRateOverrideCardViewModel? ParentReservationRateOverride { get; set; }
    public int? PropertyTypeId { get; set; }
    public UnitStructure UnitStructure { get; set; } = UnitStructure.SingleUnit;
    public int? SingleUnitTypeId { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal OpenArea { get; set; }
    public decimal ClosedArea { get; set; }
    public int? FloorCount { get; set; }
    public string? Description { get; set; }
    public List<PropertyUnitInputViewModel> Units { get; set; } = [];
    public List<ReservationAreaInputViewModel> ReservationAreas { get; set; } = [];
    public PropertyPricingMatrixViewModel? PricingMatrix { get; set; }
}
