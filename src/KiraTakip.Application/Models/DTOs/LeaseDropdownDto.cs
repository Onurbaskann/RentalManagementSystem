namespace KiraTakip.Models.Dtos;

public class LeaseDropdownDto
{
    public int Id { get; set; }
    public int SpaceId { get; set; }
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public string SpacePath { get; set; } = string.Empty;
}
