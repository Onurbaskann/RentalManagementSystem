namespace KiraTakip.Models.Entities.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    bool IsActive { get; set; }
}
