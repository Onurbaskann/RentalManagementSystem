namespace KiraTakip.Models.Entities;

public class RezervasyonTarife : BaseEntity
{
    public int? BirimId { get; set; }
    public int? BirimTuruId { get; set; }
    public int? Yil { get; set; }
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; } = 20;
    public string? Aciklama { get; set; }

    public Birim? Birim { get; set; }
    public BirimTuru? BirimTuru { get; set; }
}
