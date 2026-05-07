namespace KiraTakip.Models;

public class Tasinmaz
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int? TasinmazTipiId { get; set; }
    public TasinmazTipi? TasinmazTipi { get; set; }
    public KiralamaSekli KiralamaSekli { get; set; }

    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;

    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }

    public int? KatSayisi { get; set; }

    public string? Aciklama { get; set; }
    public DateTime KayitTarihi { get; set; }

    public List<Birim> Birimler { get; set; } = new();
}
