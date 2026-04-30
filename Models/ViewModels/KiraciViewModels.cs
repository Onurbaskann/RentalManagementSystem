using System.ComponentModel.DataAnnotations;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class KiraciDetayViewModel
{
    public Kiraci Kiraci { get; set; } = null!;
    public List<KiraSozlesmesi> Sozlesmeler { get; set; } = new();
}

public class KiraciFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Kiracı No zorunludur.")]
    public string KiraciNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kiracı türü zorunludur.")]
    public KiraciTuru? KiraciTuru { get; set; }
    
    public string? Ad { get; set; }
    public string? GercekAd { get; set; }
    public string? TuzelAd { get; set; }

    // Gerçek kişi alanları
    public string? Soyad { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? PasaportNo { get; set; }
    public string? Unvan { get; set; }
    public string? AnneAdi { get; set; }
    public string? BabaAdi { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public string? DogumYeri { get; set; }

    // Tüzel kişi alanları
    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }

    [Required(ErrorMessage = "Telefon zorunludur.")]
    public string Telefon { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    public string? Adres { get; set; }
}
