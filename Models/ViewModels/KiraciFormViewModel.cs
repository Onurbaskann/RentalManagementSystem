using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class KiraciFormViewModel
{
    public int? Id { get; set; }
    public string KiraciNo { get; set; } = string.Empty;
    public KiraciTuru? KiraciTuru { get; set; }
    public string? Ad { get; set; }
    public string? GercekAd { get; set; }
    public string? TuzelAd { get; set; }
    public string? Soyad { get; set; }
    public bool TcVatandasiDegil { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? PasaportNo { get; set; }
    public string? Unvan { get; set; }
    public string? AnneAdi { get; set; }
    public string? BabaAdi { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public string? DogumYeri { get; set; }
    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }
    public string Telefon { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }
    public bool KvkkOnayi { get; set; }
    public int? KiraciKategoriId { get; set; }
    public int? SektorId { get; set; }
}
