namespace KiraTakip.Models;

public class TasinmazKategoriCarpan
{
    public int Id { get; set; }

    public int TasinmazId { get; set; }
    public Tasinmaz Tasinmaz { get; set; } = null!;

    public int KiraciKategoriId { get; set; }
    public KiraciKategori KiraciKategori { get; set; } = null!;

    public decimal Carpan { get; set; }

    public bool Aktif { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; }

    public string? Aciklama { get; set; }
}
