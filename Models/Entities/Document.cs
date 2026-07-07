using System.ComponentModel.DataAnnotations.Schema;
using KiraTakip.Models;

namespace KiraTakip.Models.Entities;

[Table("Belgeler")]
public class Document : BaseEntity
{
    [Column("BelgeTuruId")]
    public int DocumentTypeId { get; set; }

    [Column("SahipTipi")]
    public DocumentOwnerType OwnerType { get; set; }

    [Column("SahipId")]
    public int OwnerId { get; set; }

    [Column("DosyaAdi")]
    public string FileName { get; set; } = string.Empty;

    [Column("MimeTipi")]
    public string MimeType { get; set; } = string.Empty;

    [Column("BoyutByte")]
    public long FileSize { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Column("Gecersiz")]
    public bool IsInvalid { get; set; }

    [Column("GecersizlikTarihi")]
    public DateTime? InvalidationDate { get; set; }

    [Column("DegistirenBelgeId")]
    public int? ReplacedByDocumentId { get; set; }

    public DocumentType DocumentType { get; set; } = null!;
    public DocumentContent? Content { get; set; }
    public Document? ReplacedByDocument { get; set; }
}
