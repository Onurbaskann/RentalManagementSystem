using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Alanlar")]
public class Space : BaseEntity
{
    [Column("UstAlanId")]
    public int? ParentId { get; set; }

    [Column("AlanTuruId")]
    public int SpaceTypeId { get; set; }

    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Sira")]
    public int SortOrder { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Column("Yol")]
    public string Path { get; set; } = string.Empty;

    [Column("Derinlik")]
    public int Depth { get; set; }

    [Column("Yuzolcumu")]
    public decimal Area { get; set; }

    [Column("Kullanim")]
    public SpaceUsage Usage { get; set; } = SpaceUsage.Rentable;

    [Column("OnTahsiseAcik")]
    public bool IsPreAllocatable { get; set; }

    [Column("Paylasimli")]
    public bool IsShared { get; set; }

    public Space? Parent { get; set; }
    public SpaceType SpaceType { get; set; } = null!;
    public SpaceAddress? Address { get; set; }
    public ICollection<Space> Children { get; set; } = new List<Space>();
    public ICollection<SpaceAttributeValue> AttributeValues { get; set; } = new List<SpaceAttributeValue>();
    public ICollection<SpaceHistory> History { get; set; } = new List<SpaceHistory>();
}
