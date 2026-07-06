using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class Belge : BaseEntity
{
    [Column("DocumentTypeId")]
    public int DocumentTypeId { get; set; }
    public BelgeOwnerTipi OwnerType { get; set; }
    public int OwnerId { get; set; }

    public string DosyaAdi { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long BoyutByte { get; set; }
    public string? Aciklama { get; set; }

    public bool Gecersiz { get; set; }
    public DateTime? GecersizlikTarihi { get; set; }
    public int? DegistirenBelgeId { get; set; }

    public DocumentType DocumentType { get; set; } = null!;
    public BelgeIcerik? Icerik { get; set; }
    public Belge? DegistirenBelge { get; set; }
}
