namespace KiraTakip.Models;

public class Birim
{
    public int Id { get; set; }
    public int TasinmazId { get; set; }
    public Tasinmaz Tasinmaz { get; set; } = null!;

    public BirimTipi BirimTipi { get; set; }
    public int? KatNo { get; set; }
    public string? BirimNo { get; set; }
    public string Ad { get; set; } = string.Empty;
    public decimal Yuzolcumu { get; set; }
    public string? Aciklama { get; set; }

    public int? BirimTuruId { get; set; }
    public BirimTuru? BirimTuru { get; set; }

    public List<KiraSozlesmesi> Sozlesmeler { get; set; } = new();
}
