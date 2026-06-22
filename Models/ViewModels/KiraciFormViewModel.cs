using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class KiraciFormViewModel
{
    public int? Id { get; set; }
    public string KiraciNo { get; set; } = string.Empty;
    public string? Ad { get; set; }
    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }
    public string Telefon { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }
    public int? KiraciKategoriId { get; set; }
    public int? SektorId { get; set; }

    // Kiracı oluşturulurken opsiyonel olarak ilk firma yetkilisi davet edilir
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string? IlkYetkiliEmail { get; set; }
    public string? IlkYetkiliAdSoyad { get; set; }
}
