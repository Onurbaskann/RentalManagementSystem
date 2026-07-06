using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("PaymentMatches")]
public class PaymentMatch : BaseEntity
{
    public int PaymentAllocationId { get; set; }

    public int BankTransactionId { get; set; }

    [Column("EslesmeTipi")]
    public MatchType MatchType { get; set; }

    public PaymentAllocation PaymentAllocation { get; set; } = null!;
    public BankTransaction BankTransaction { get; set; } = null!;
}
