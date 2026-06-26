namespace KiraTakip.Models.Entities;

public class UserRol : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int RolId { get; set; }

    public Rol? Rol { get; set; }
}
