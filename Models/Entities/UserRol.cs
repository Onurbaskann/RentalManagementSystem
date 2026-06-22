namespace KiraTakip.Models.Entities;

public class UserRol
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int RolId { get; set; }
    public DateTime AtanmaTarihi { get; set; } = DateTime.UtcNow;
    public string? AtayanUserId { get; set; }

    public Rol? Rol { get; set; }
}
