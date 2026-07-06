namespace KiraTakip.Models.Dtos;

public class BankaHareketiDetayDto
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal TransactionAmount { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string? SenderIban { get; set; }
    public string? SenderInfo { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public BankMatchStatus MatchStatus { get; set; }
    public List<OdemeBankaEslesmeDto> Eslesmeleri { get; set; } = [];
}
