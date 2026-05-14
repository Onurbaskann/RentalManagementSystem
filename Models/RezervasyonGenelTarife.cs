namespace KiraTakip.Models;

public class RezervasyonGenelTarife
{
    public int Id { get; set; }
    public int TarifeId { get; set; }
    public int BirimTuruId { get; set; }

    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; } = 20;

    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public Tarife Tarife { get; set; } = null!;
    public BirimTuru BirimTuru { get; set; } = null!;
}
