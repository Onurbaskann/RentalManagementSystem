namespace KiraTakip.Models.Dtos;

public class BankaHareketiDetayDto
{
    public int Id { get; set; }
    public DateTime IslemTarihi { get; set; }
    public decimal IslemTutari { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string? GonderenIban { get; set; }
    public string? GonderenBilgisi { get; set; }
    public string BankaKodu { get; set; } = string.Empty;
    public BankMatchStatus EslesmeDurumu { get; set; }
    public List<OdemeBankaEslesmeDto> Eslesmeleri { get; set; } = [];
}
