using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

/// <summary>
/// Sanal POS işleminin append-only teknik/audit geçmişi. AuditLog gibi BaseEntity'den
/// türemez — güncelleme/soft-delete alanı yok, yalnız ekleme yapılır. Kart verisi, merchant
/// secret veya tekrar kullanılabilir credential burada asla tutulmaz.
/// </summary>
[Table("SanalPosIslemOlaylari")]
public class OnlinePaymentEvent
{
    public int Id { get; set; }

    [Column("SanalPosIslemiId")]
    public int OnlinePaymentTransactionId { get; set; }

    [Column("OlayTipi")]
    public OnlinePaymentEventType EventType { get; set; }

    [Column("SaglayiciYanitKodu")]
    public string? ProviderResponseCode { get; set; }

    [Column("SaglayiciIslemDurumu")]
    public string? ProviderTransactionStatus { get; set; }

    [Column("GuvenliOzet")]
    public string? SafeSummary { get; set; }

    [Column("SaglayiciZamani")]
    public DateTime? ProviderTimestamp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OnlinePaymentTransaction OnlinePaymentTransaction { get; set; } = null!;
}
