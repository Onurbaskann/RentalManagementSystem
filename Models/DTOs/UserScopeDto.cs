namespace KiraTakip.Models.Dtos;

public class UserScopeDto
{
    public bool GlobalAccess { get; set; }
    public List<int> PropertyIds { get; set; } = new();
    public List<int> UnitIds { get; set; } = new();
}
