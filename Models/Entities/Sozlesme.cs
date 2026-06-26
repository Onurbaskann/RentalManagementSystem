namespace KiraTakip.Models.Entities;

public class Sozlesme : BaseEntity
{
    public int BirimId { get; set; }
    public int KiraciId { get; set; }
    public SozlesmeDurumu Durum { get; set; } = SozlesmeDurumu.Aktif;
    public bool KdvUygulanacakMi { get; set; }
    public VadeKuraliTipi VadeKuraliTipi { get; set; } = VadeKuraliTipi.SabitAyGunu;
    public int VadeGunu { get; set; } = 1;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public DateTime? FesihTarihi { get; set; }
    public string? FesihNedeni { get; set; }
    public string? Aciklama { get; set; }

    public Birim Birim { get; set; } = null!;
    public Kiraci Kiraci { get; set; } = null!;
    public List<SozlesmeIslemGecmisi> IslemGecmisi { get; set; } = [];
    public List<SozlesmeTarife> SozlesmeTarifeler { get; set; } = [];
}
