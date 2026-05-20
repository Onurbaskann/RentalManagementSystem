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

    public string? Notlar { get; set; }

    public SozlesmeDurumu Durum { get; set; } = SozlesmeDurumu.Aktif;

    // Fesih bilgileri
    public DateTime? FesihTarihi { get; set; }
    public string? FesihNedeni { get; set; }

    // KDV bilgileri
    public bool KdvUygulanacakMi { get; set; }

    // İşlem geçmişi
    public List<SozlesmeIslemGecmisi> IslemGecmisi { get; set; } = new();

    public List<SozlesmeRate> SozlesmeRateler { get; set; } = new();
}
