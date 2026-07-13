namespace KiraTakip.Models.Dtos;

public class UnitLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public UnitStructure UnitStructure { get; set; }
    public string? UnitNo { get; set; }
    public int? FloorNo { get; set; }
}
