namespace KiraTakip.Models;

public class Tarife
{
    public int Id { get; set; }
    public int Yil { get; set; }
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public ICollection<TarifeKalemi> Kalemler { get; set; } = [];
}

public class TarifeKalemi
{
    public int Id { get; set; }
    public int TarifeId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public Tarife Tarife { get; set; } = null!;
    public KiraciKategori KiraciKategori { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
