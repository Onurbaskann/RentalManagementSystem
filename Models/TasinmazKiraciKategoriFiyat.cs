namespace KiraTakip.Models;

public class TasinmazKiraciKategoriFiyat
{
    public int Id { get; set; }

    public int TasinmazId { get; set; }
    public Tasinmaz Tasinmaz { get; set; } = null!;

    public int KiraciKategoriId { get; set; }
    public KiraciKategori KiraciKategori { get; set; } = null!;

    public int BorcTipiId { get; set; }
    public BorcTipi BorcTipi { get; set; } = null!;

    public decimal BirimDeger { get; set; }
    
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;

    public decimal KdvOrani { get; set; }
    
    public bool Aktif { get; set; } = true;
    
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    
    public string? Aciklama { get; set; }
}
