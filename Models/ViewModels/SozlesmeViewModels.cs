using System.ComponentModel.DataAnnotations;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class SozlesmeDetayViewModel
{
    public KiraSozlesmesi Sozlesme { get; set; } = null!;
    public int KalanGun { get; set; }
    public decimal AylikBedel { get; set; }
    public decimal YillikBedel { get; set; }
    public bool Aktif { get; set; }
    public double SureYuzdesi { get; set; }
    public KiraDurumu Durum { get; set; }
    public List<KiraSozlesmesi> GecmisSozlesmeler { get; set; } = new();
    public List<KiraTahakkuk> Tahakkuklar { get; set; } = new();
    public bool HasOdemeAccess { get; set; }
}

public class SozlesmeEkleViewModel
{
    public int? BirimId { get; set; }
    public int KiraciId { get; set; }
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;
    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddYears(1);
    public decimal KiraBedeli { get; set; }
    public KiraPeriyodu Periyot { get; set; } = KiraPeriyodu.Aylik;
    public decimal? Depozito { get; set; }
    public string? Notlar { get; set; }

    public bool KdvUygulanacakMi { get; set; }
    public decimal KdvOrani { get; set; } = 20;

    public List<Birim> MevcutBirimler { get; set; } = new();
    public List<Kiraci> Kiraciler { get; set; } = new();
}

public class SozlesmeUzatViewModel
{
    public int SozlesmeId { get; set; }

    [Required(ErrorMessage = "Yeni bitiş tarihi zorunludur.")]
    public DateTime YeniBitisTarihi { get; set; }

    [Required(ErrorMessage = "Yeni kira bedeli zorunludur.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Kira bedeli sıfırdan büyük olmalıdır.")]
    public decimal YeniKiraBedeli { get; set; }

    public bool TufeUygulanacakMi { get; set; }
    public decimal? TufeOrani { get; set; }

    public bool KdvUygulanacakMi { get; set; }
    public decimal? KdvOrani { get; set; }

    public string? Aciklama { get; set; }
}

public class SozlesmeFesihViewModel
{
    public int SozlesmeId { get; set; }

    [Required(ErrorMessage = "Fesih tarihi zorunludur.")]
    public DateTime FesihTarihi { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Fesih nedeni zorunludur.")]
    public string FesihNedeni { get; set; } = string.Empty;

    public string? Aciklama { get; set; }
}

public class KiraHesaplamaSonucu
{
    public decimal MevcutKiraBedeli { get; set; }
    public decimal? TufeOrani { get; set; }
    public decimal TufeArtisTutari { get; set; }
    public decimal TufeSonrasiKiraBedeli { get; set; }

    public bool KdvUygulandiMi { get; set; }
    public decimal? KdvOrani { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal KdvDahilToplam { get; set; }
}
