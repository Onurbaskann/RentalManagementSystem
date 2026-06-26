namespace KiraTakip.Models.Entities;

public class UserPermission : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}
