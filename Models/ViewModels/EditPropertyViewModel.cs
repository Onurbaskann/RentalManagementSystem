namespace KiraTakip.Models.ViewModels;

public class EditPropertyViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? PropertyTypeId { get; set; }
    public UnitStructure UnitStructure { get; set; }
    public bool CanChangeUnitStructure { get; set; }
    public int? SingleUnitTypeId { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal OpenArea { get; set; }
    public decimal ClosedArea { get; set; }
    public int? FloorCount { get; set; }
    public string? Description { get; set; }
    public List<PropertyUnitEditViewModel> Units { get; set; } = [];
    public List<ReservationAreaEditViewModel> ReservationAreas { get; set; } = [];
    public PropertyPricingMatrixViewModel PricingMatrix { get; set; } = new();
    public ParentRateCardViewModel? ParentRate { get; set; }
    public ParentReservationRateOverrideCardViewModel? ParentReservationRateOverride { get; set; }
}
