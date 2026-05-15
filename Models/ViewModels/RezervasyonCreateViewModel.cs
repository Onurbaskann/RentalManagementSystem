using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class RezervasyonCreateViewModel
{
    public int BirimId { get; set; }

    public int KiraciId { get; set; }

    public int? KiraSozlesmesiId { get; set; }

    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;

    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddHours(2);

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public List<Birim> RezervasyonBirimleri { get; set; } = [];
    public List<Kiraci> Kiraciler { get; set; } = [];
    public List<KiraSozlesmesi> Sozlesmeler { get; set; } = [];

    public RezervasyonHesapSonucu? Hesap { get; set; }
}
