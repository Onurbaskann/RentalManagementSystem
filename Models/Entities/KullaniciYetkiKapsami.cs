namespace KiraTakip.Models.Entities;

public class KullaniciYetkiKapsami : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public KapsamTipi KapsamTipi { get; set; }
    public int KapsamId { get; set; }
}
