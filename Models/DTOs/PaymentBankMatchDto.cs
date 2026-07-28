namespace KiraTakip.Models.Dtos;

public class PaymentBankMatchDto
{
    public int Id { get; set; }
    public MatchType MatchType { get; set; }
    public decimal BankTransactionAmount { get; set; }
    public DateTime BankTransactionDate { get; set; }
    public string BankTransactionDescription { get; set; } = string.Empty;
}
