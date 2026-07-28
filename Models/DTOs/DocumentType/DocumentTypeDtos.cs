using KiraTakip.Models;

namespace KiraTakip.Models.Dtos.DocumentType;

public record CreateInput(
    string Name,
    string? Description,
    DocumentOwnerType TargetEntity,
    bool Required,
    string AllowedExtensions,
    int MaxSizeMb,
    int SortOrder,
    bool IsActive
);

public record EditInput(
    string Name,
    string? Description,
    DocumentOwnerType TargetEntity,
    bool Required,
    string AllowedExtensions,
    int MaxSizeMb,
    int SortOrder,
    bool IsActive
);
