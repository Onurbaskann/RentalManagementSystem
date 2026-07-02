using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class KullaniciYetkiKapsami : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    [Column("KapsamTipi")]
    public ScopeType ScopeType { get; set; }
    public int KapsamId { get; set; }
}
