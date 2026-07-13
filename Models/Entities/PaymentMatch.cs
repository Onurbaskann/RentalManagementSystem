using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("OdemeBankaEslesmeleri")]
public class PaymentMatch : BaseEntity
{
    [Column("TahakkukOdemesiId")]
    public int PaymentAllocationId { get; set; }

    [Column("BankaHareketId")]
    public int BankTransactionId { get; set; }

    [Column("EslesmeTipi")]
    public MatchType MatchType { get; set; }

    public PaymentAllocation PaymentAllocation { get; set; } = null!;
    public BankTransaction BankTransaction { get; set; } = null!;
}
