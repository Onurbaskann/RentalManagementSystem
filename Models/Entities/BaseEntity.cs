using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public abstract class BaseEntity<TKey> : IAuditable, ISoftDeletable where TKey : notnull
{
    public TKey Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    [Column("Aktif")]
    public bool IsActive { get; set; } = true;
}

public abstract class BaseEntity : BaseEntity<int>
{
}
