using KiraTakip.Models.Entities.Interfaces;

namespace KiraTakip.Models.Entities;

public abstract class BaseEntity : IAuditable
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}