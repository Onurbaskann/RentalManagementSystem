namespace KiraTakip.Models.Entities;

public class Tasinmaz : BaseEntity
{
    public int? TasinmazTipiId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public KiralamaSekli KiralamaSekli { get; set; }
    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }
    public int? KatSayisi { get; set; }

    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;
    public string? Aciklama { get; set; }

    public Kategori? TasinmazTipi { get; set; }
    public List<Birim> Birimler { get; set; } = [];
}
