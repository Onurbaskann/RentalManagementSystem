namespace KiraTakip.Models.Dtos;

public class PropertyListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string PropertyTypeName { get; set; } = string.Empty;
    public decimal ClosedArea { get; set; }
    public decimal OpenArea { get; set; }
    public UnitStructure UnitStructure { get; set; }
    public int UnitCount { get; set; }
    public int LeasedUnitCount { get; set; }
    public int ExpiringSoonUnitCount { get; set; }
    public int VacantUnitCount { get; set; }
}
