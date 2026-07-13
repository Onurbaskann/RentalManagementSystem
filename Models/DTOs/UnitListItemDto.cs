namespace KiraTakip.Models.Dtos;

public class UnitListItemDto
{
    public int Id { get; set; }
    public string? UnitNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? FloorNo { get; set; }
    public decimal Area { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public OccupancyStatus Status { get; set; }
    public decimal MonthlyRent { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
}
