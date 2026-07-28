using KiraTakip.Models.Entities;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class DocumentTypeFormViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DocumentOwnerType TargetEntity { get; set; } = DocumentOwnerType.Tenant;

    public bool Required { get; set; }

    public string AllowedExtensions { get; set; } = "pdf,jpg,png";

    public int MaxSizeMb { get; set; } = 5;

    public int SortOrder { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
}
