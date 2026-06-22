namespace KiraTakip.Models.Dtos;

public class KiraciDetayDto
{
    public int Id { get; set; }
    public int? KiraciKategoriId { get; set; }
    public string? KiraciKategoriAd { get; set; }
    public int? SektorId { get; set; }
    public string? SektorAd { get; set; }
    public string KiraciNo { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }
    public DateTime KayitTarihi { get; set; }

    public string GosterimAdi => Ad;
}
