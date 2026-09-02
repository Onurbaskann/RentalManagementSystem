namespace KiraTakip.Models.Dtos;

public class BankTransactionListItemDto
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal TransactionAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SenderIban { get; set; }
    public string? SenderInfo { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public BankMatchStatus MatchStatus { get; set; }
    public string StoreName { get; set; } = string.Empty;
}
