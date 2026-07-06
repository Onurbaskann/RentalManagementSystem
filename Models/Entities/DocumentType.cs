using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BelgeTurleri")]
public class DocumentType : BaseEntity
{
    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Column("HedefEntite")]
    public BelgeOwnerTipi TargetEntity { get; set; }

    [Column("Zorunlu")]
    public bool Required { get; set; }

    [Column("IzinVerilenUzantilar")]
    public string AllowedExtensions { get; set; } = "pdf,jpg,png";

    [Column("MaxBoyutMb")]
    public int MaxSizeMb { get; set; } = 5;

    [Column("SablonBelgeId")]
    public int? TemplateDocumentId { get; set; }

    [Column("Sira")]
    public int SortOrder { get; set; }

    [Column("Sistem")]
    public bool IsSystem { get; set; } = false;

    public Belge? TemplateDocument { get; set; }
}
