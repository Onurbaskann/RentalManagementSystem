using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeIncelemeGecmisleri")]
public class LeaseReviewHistory : BaseEntity
{
    [Column("SozlesmeId")]
    public int LeaseId { get; set; }

    [Column("IslemTipi")]
    public LeaseReviewActionType ActionType { get; set; }

    [Column("OncekiDurum")]
    public LeaseStatus? FromStatus { get; set; }

    [Column("YeniDurum")]
    public LeaseStatus? ToStatus { get; set; }

    [Column("Aciklama")]
    public string? Explanation { get; set; }

    [Column("IslemYapanKullaniciId")]
    public string ActorUserId { get; set; } = string.Empty;

    [Column("IslemTarihi")]
    public DateTime ActionDate { get; set; }

    public Lease Lease { get; set; } = null!;
    public ApplicationUser ActorUser { get; set; } = null!;
}
