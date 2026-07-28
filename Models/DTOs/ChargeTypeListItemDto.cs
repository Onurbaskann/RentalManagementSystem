using KiraTakip.Models;

namespace KiraTakip.Models.Dtos;

public class ChargeTypeListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ChargeTypeBehavior Behavior { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
}
