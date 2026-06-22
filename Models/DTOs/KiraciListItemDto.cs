namespace KiraTakip.Models.Dtos;

public class KiraciListItemDto
{
    public int Id { get; set; }
    public string KiraciNo { get; set; } = string.Empty;
    public string GosterimAdi { get; set; } = string.Empty;
    public string? VergiNo { get; set; }
    public string? KiraciKategoriAd { get; set; }
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime KayitTarihi { get; set; }
}
