namespace KiraTakip.Models.Dtos;

public class PropertyDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PropertyTypeName { get; set; } = string.Empty;
    public decimal ClosedArea { get; set; }
    public decimal OpenArea { get; set; }
    public UnitStructure UnitStructure { get; set; }
    public string? Description { get; set; }
    public List<UnitDetailDto> Units { get; set; } = [];
    public List<PropertyReservationDto> Reservations { get; set; } = [];
    public List<UnitReservationRateOverrideDto> UnitReservationRateOverrides { get; set; } = [];
    public List<UnitCustomRateSummaryDto> UnitCustomRates { get; set; } = [];
    public List<TasinmazSozlesmeGecmisiDto> LeaseHistory { get; set; } = [];
}
