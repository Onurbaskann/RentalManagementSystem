namespace KiraTakip.Models.Dtos;

public class UnitTypeListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool CanBeRented { get; set; }
    public bool CanBeReserved { get; set; }
    public int? ChargeTypeId { get; set; }
    public string? ChargeTypeName { get; set; }
    public bool IsActive { get; set; }
}
