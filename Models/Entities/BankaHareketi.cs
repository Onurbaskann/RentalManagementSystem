namespace KiraTakip.Models.Entities;

public class BankaHareketi : BaseEntity
{
    public string? BankaReferansNo { get; set; }
    public string BankaKodu { get; set; } = string.Empty;
    public string? GonderenIban { get; set; }
    public string? GonderenBilgisi { get; set; }
    public decimal IslemTutari { get; set; }
    public DateTime IslemTarihi { get; set; }
    public BankaEslesmeDurumu EslesmeDurumu { get; set; } = BankaEslesmeDurumu.Eslestirilmedi;
    public string Aciklama { get; set; } = string.Empty;

    public List<OdemeBankaEslesme> OdemeEslesmeleri { get; set; } = [];
}
