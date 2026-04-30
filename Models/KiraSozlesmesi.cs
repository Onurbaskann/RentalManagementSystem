namespace KiraTakip.Models;

public class KiraSozlesmesi
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public Birim Birim { get; set; } = null!;
    public int KiraciId { get; set; }
    public Kiraci Kiraci { get; set; } = null!;

    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }

    // KDV hariç ana kira bedeli
    public decimal KiraBedeli { get; set; }
    public KiraPeriyodu Periyot { get; set; }

    public decimal? Depozito { get; set; }
    public string? Notlar { get; set; }

    public SozlesmeDurumu Durum { get; set; } = SozlesmeDurumu.Aktif;

    // Fesih bilgileri
    public DateTime? FesihTarihi { get; set; }
    public string? FesihNedeni { get; set; }

    // KDV bilgileri
    public bool KdvUygulanacakMi { get; set; }
    public decimal KdvOrani { get; set; } = 20;

    // İşlem geçmişi
    public List<SozlesmeIslemGecmisi> IslemGecmisi { get; set; } = new();
}
