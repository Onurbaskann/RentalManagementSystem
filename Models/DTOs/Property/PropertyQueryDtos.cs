namespace KiraTakip.Models.Dtos;

public record GetPropertiesInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetAvailableUnitsInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null,
    int? IncludedUnitId = null);
