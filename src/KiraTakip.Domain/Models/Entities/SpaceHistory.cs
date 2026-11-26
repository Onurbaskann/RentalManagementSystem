using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("AlanTarihcesi")]
public class SpaceHistory : BaseEntity
{
    [Column("AlanId")]
    public int SpaceId { get; set; }

    [Column("Alan")]
    public SpaceHistoryField Field { get; set; }

    [Column("EskiDeger")]
    public string? OldValue { get; set; }

    [Column("YeniDeger")]
    public string? NewValue { get; set; }

    [Column("Tarih")]
    public DateTime ChangedAt { get; set; }

    [Column("KullaniciId")]
    public string UserId { get; set; } = string.Empty;

    public Space Space { get; set; } = null!;
}
