using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BankTransactions")]
public class BankTransaction : BaseEntity
{
    public string? BankReferenceNo { get; set; }

    public string BankCode { get; set; } = string.Empty;

    public string? SenderIban { get; set; }

    public string? SenderInfo { get; set; }

    public decimal TransactionAmount { get; set; }

    public DateTime TransactionDate { get; set; }

    public BankMatchStatus MatchStatus { get; set; } = BankMatchStatus.Unmatched;

    [Column("Aciklama")]
    public string Description { get; set; } = string.Empty;

    public List<PaymentMatch> Matches { get; set; } = [];
}
