using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("HareketGecmisleri")]
public class AuditLog
{
    public long Id { get; set; }

    [Column("OlayTipi")]
    public string EventType { get; set; } = string.Empty;

    [Column("VarlikTipi")]
    public string? EntityType { get; set; }

    [Column("VarlikId")]
    public string? EntityId { get; set; }

    public string? UserId { get; set; }
    public UserType? UserType { get; set; }
    public int? KiraciId { get; set; }

    [Column("IpAdresi")]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    [Column("Detaylar")]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
