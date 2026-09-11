namespace KiraTakip.Models.Dtos;

public class LeaseDropdownDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
}
