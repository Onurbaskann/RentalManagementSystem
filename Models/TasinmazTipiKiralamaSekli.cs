namespace KiraTakip.Models;

public class TasinmazTipiKiralamaSekli
{
    public int Id { get; set; }
    public int TasinmazTipiId { get; set; }
    public Kategori TasinmazTipi { get; set; } = null!;
    public KiralamaSekli KiralamaSekli { get; set; }
}
