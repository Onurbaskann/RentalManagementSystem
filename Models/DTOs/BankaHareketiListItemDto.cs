namespace KiraTakip.Models.Dtos;

public class BankaHareketiListItemDto
{
    public int Id { get; set; }
    public DateTime HareketTarihi { get; set; }
    public decimal Tutar { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string? KarsiHesap { get; set; }
    public string? KarsiUnvan { get; set; }
    public string BankaKodu { get; set; } = string.Empty;
    public BankaEslesmeDurumu EslesmeDurumu { get; set; }
    public DateTime ImportTarihi { get; set; }
    public string? ImportEdenUserAdi { get; set; }
}
