namespace KiraTakip.Models.Dtos.PropertyType;

public record PropertyTypeListItemDto(
    int Id,
    string Name,
    string Code,
    int SortOrder,
    bool IsActive,
    bool SupportsSingleUnit,
    bool SupportsMultipleUnits
);

public record CreateInput(
    string Name,
    int SortOrder,
    bool IsActive,
    bool SupportsSingleUnit,
    bool SupportsMultipleUnits
);

public record EditInput(
    string Name,
    int SortOrder,
    bool IsActive,
    bool SupportsSingleUnit,
    bool SupportsMultipleUnits
);
