using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("AlanTurleri")]
public class SpaceType : BaseEntity
{
    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Sira")]
    public int SortOrder { get; set; }

    [Column("VarsayilanKullanim")]
    public SpaceUsage DefaultUsage { get; set; } = SpaceUsage.Rentable;

    [Column("VarsayilanOnTahsis")]
    public bool DefaultIsPreAllocatable { get; set; }

    public ICollection<SpaceTypeDefaultAttribute> DefaultAttributes { get; set; } = new List<SpaceTypeDefaultAttribute>();
}
