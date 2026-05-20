namespace KiraTakip.Models.Entities;

public class TasinmazTipiKiralamaSekli : BaseEntity
{
    public int TasinmazTipiId { get; set; }
    public KiralamaSekli KiralamaSekli { get; set; }

    public Kategori TasinmazTipi { get; set; } = null!;
}
