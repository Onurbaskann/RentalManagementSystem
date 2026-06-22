namespace KiraTakip.Models.Entities;

public class RolPermission
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public string Permission { get; set; } = string.Empty;

    public Rol? Rol { get; set; }
}
