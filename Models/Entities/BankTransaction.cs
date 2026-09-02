using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BankaHareketleri")]
public class BankTransaction : BaseEntity
{
    [Column("BankaReferansNo")]
    public string? BankReferenceNo { get; set; }

    [Column("BankaKodu")]
    public string BankCode { get; set; } = string.Empty;

    [Column("GondericiIban")]
    public string? SenderIban { get; set; }

    [Column("GondericiBilgisi")]
    public string? SenderInfo { get; set; }

    [Column("IslemTutari")]
    public decimal TransactionAmount { get; set; }

    [Column("IslemTarihi")]
    public DateTime TransactionDate { get; set; }

    [Column("EslesmeDurumu")]
    public BankMatchStatus MatchStatus { get; set; } = BankMatchStatus.Unmatched;

    [Column("Aciklama")]
    public string Description { get; set; } = string.Empty;

    [Column("MagazaHesapBilgisiId")]
    public int StoreAccountId { get; set; }

    public StoreAccount StoreAccount { get; set; } = null!;
    public List<PaymentMatch> Matches { get; set; } = [];
}
