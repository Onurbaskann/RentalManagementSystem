using System.ComponentModel.DataAnnotations.Schema;
using KiraTakip.Infrastructure;

namespace KiraTakip.Models.Entities;

[Table("MagazaHesapBilgileri")]
public class StoreAccount : BaseEntity
{
    [Column("MagazaId")]
    public int StoreId { get; set; }

    [Column("SaglayiciKodu")]
    public string ProviderCode { get; set; } = string.Empty;

    [Column("ParaBirimi")]
    public string Currency { get; set; } = string.Empty;

    [Column("MerchantId")]
    public string MerchantId { get; set; } = string.Empty;

    [Column("MerchantUser")]
    public string MerchantUser { get; set; } = string.Empty;

    [Column("SifreliMerchantPassword")]
    [AuditIgnore]
    public string ProtectedMerchantPassword { get; set; } = string.Empty;

    [Column("GecerlilikBaslangici")]
    public DateTime ValidFrom { get; set; }

    [Column("GecerlilikBitisi")]
    public DateTime? ValidUntil { get; set; }

    public Store Store { get; set; } = null!;
}
