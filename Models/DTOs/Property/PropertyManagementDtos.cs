namespace KiraTakip.Models.Dtos;

public record GetPropertyDetailsInput(int PropertyId);
public record GetPropertyForEditInput(int PropertyId);
public record PropertyTypeOptionDto(
    int Id,
    string Name,
    bool SupportsSingleUnit,
    bool SupportsMultipleUnits);

public record UnitTypeOptionDto(
    int Id,
    string Name,
    UnitTypeUsage Usage);

public record PropertyFormOptionsDto(
    IReadOnlyList<PropertyTypeOptionDto> PropertyTypes,
    IReadOnlyList<UnitTypeOptionDto> UnitTypes);

public record PropertyStructureSupportDto(bool SupportsSingleUnit, bool SupportsMultipleUnits);
public record UnitTypeUsageDto(int UnitTypeId, UnitTypeUsage Usage);
public class PropertyUnitInputDto
{
    public int? Id { get; set; }
    public string UnitNo { get; set; } = string.Empty;
    public int? FloorNo { get; set; }
    public string? Name { get; set; }
    public decimal Area { get; set; }
    public string? Description { get; set; }
    public int? UnitTypeId { get; set; }
}

public class ReservationAreaInputDto
{
    public int? Id { get; set; }
    public string? UnitNo { get; set; }
    public string? Name { get; set; }
    public decimal Area { get; set; }
    public int? UnitTypeId { get; set; }
    public string? Description { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal VatRate { get; set; }
}

public class CreatePropertyInput
{
    public string Name { get; set; } = string.Empty;
    public int? PropertyTypeId { get; set; }
    public UnitStructure UnitStructure { get; set; }
    public int? SingleUnitTypeId { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal OpenArea { get; set; }
    public decimal ClosedArea { get; set; }
    public int? FloorCount { get; set; }
    public string? Description { get; set; }
    public List<PropertyUnitInputDto> Units { get; set; } = [];
    public List<ReservationAreaInputDto> ReservationAreas { get; set; } = [];
    public SavePropertyPricingMatrixInput PricingMatrix { get; set; } = new();
}

public class UpdatePropertyInput : CreatePropertyInput
{
    public int PropertyId { get; set; }
    public IReadOnlyCollection<int>? AccessiblePropertyIds { get; set; }
}

public record CreatedPropertyDto(int PropertyId, string PropertyName);

public class PropertyEditDto : UpdatePropertyInput
{
    public bool CanChangeUnitStructure { get; set; }
    public HashSet<int> ActiveLeaseUnitIds { get; set; } = [];
    public HashSet<int> ActiveReservationUnitIds { get; set; } = [];
}

public record GetPropertyPricingMatrixInput(
    int PropertyId,
    int Page = 1,
    int PageSize = 10,
    IReadOnlyCollection<int>? AccessiblePropertyIds = null);

public record PropertyPricingCategoryDto(int Id, string Name);
public record PropertyPricingChargeTypeDto(
    int Id,
    string Name,
    string Code,
    ChargeTypeBehavior Behavior);
public record PropertyPricingRateDto(
    int Id,
    int PropertyId,
    int TenantCategoryId,
    int ChargeTypeId,
    decimal UnitValue,
    CalculationMethod CalculationMethod,
    decimal VatRate);
public record PropertyPricingContextDto(
    bool PropertyExists,
    string PropertyName,
    IReadOnlyList<PropertyPricingCategoryDto> Categories,
    IReadOnlyList<PropertyPricingChargeTypeDto> ChargeTypes,
    IReadOnlyList<PropertyPricingRateDto> Rates);

public class SavePropertyPricingMatrixInput
{
    public int PropertyId { get; set; }
    public List<PropertyPricingRowDto> Rows { get; set; } = [];
}

public class PropertyPricingMatrixDto
{
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public List<PropertyPricingRowDto> Rows { get; set; } = [];
    public List<PropertyPricingColumnDto> Columns { get; set; } = [];
    public int TotalRows { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class PropertyPricingRowDto
{
    public int TenantCategoryId { get; set; }
    public string TenantCategoryName { get; set; } = string.Empty;
    public List<PropertyPricingCellDto> Cells { get; set; } = [];
}

public record PropertyPricingColumnDto(
    int ChargeTypeId,
    string ChargeTypeName,
    string ChargeTypeCode,
    ChargeTypeBehavior ChargeTypeBehavior);

public class PropertyPricingCellDto
{
    public int? PropertyRateOverrideId { get; set; }
    public int PropertyId { get; set; }
    public int TenantCategoryId { get; set; }
    public int ChargeTypeId { get; set; }
    public decimal? UnitValue { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public decimal? VatRate { get; set; }
    public bool HasRate { get; set; }
}
