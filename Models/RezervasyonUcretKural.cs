namespace KiraTakip.Models;

public class RezervasyonUcretKural
{
    public int Id { get; set; }

    public int? BirimId { get; set; }
    public Birim? Birim { get; set; }

    public int UcretsizSureDakika { get; set; }

    public int UcretlendirmePeriyoduDakika { get; set; }

    public decimal PeriyotUcreti { get; set; }

    public decimal KdvOrani { get; set; } = 20;

    public bool Aktif { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; }

    public string? Aciklama { get; set; }
}
