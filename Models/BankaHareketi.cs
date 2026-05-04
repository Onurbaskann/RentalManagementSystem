namespace KiraTakip.Models;

public class BankaHareketi
{
    public int Id { get; set; }

    public Guid ImportBatchId { get; set; }
    public DateTime HareketTarihi { get; set; }
    public decimal Tutar { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string? KarsiHesap { get; set; }
    public string? KarsiUnvan { get; set; }
    public decimal? Bakiye { get; set; }
    public string BankaKodu { get; set; } = string.Empty;

    public BankaEslesmeDurumu EslesmeDurumu { get; set; } = BankaEslesmeDurumu.Eslestirilmedi;

    public DateTime ImportTarihi { get; set; } = DateTime.Now;
    public string ImportEdenUserId { get; set; } = string.Empty;
    public ApplicationUser ImportEdenUser { get; set; } = null!;

    public List<OdemeBankaEslesme> OdemeEslesmeleri { get; set; } = new();
}
