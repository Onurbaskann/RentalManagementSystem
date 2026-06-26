namespace KiraTakip.Models.Entities;

public class Birim : BaseEntity
{
    public int TasinmazId { get; set; }
    public int? BirimTuruId { get; set; }
    public BirimTipi BirimTipi { get; set; }
    public int? KatNo { get; set; }
    public string? BirimNo { get; set; }
    public string Ad { get; set; } = string.Empty;
    public decimal Yuzolcumu { get; set; }
    public string? Aciklama { get; set; }

    public Tasinmaz Tasinmaz { get; set; } = null!;
    public BirimTuru? BirimTuru { get; set; }
    public List<Sozlesme> Sozlesmeler { get; set; } = [];
}
