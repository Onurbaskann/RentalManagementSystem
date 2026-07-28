namespace KiraTakip.Models.Dtos;

public record GetUnitTypeByIdInput(int Id);

public record CreateUnitTypeInput(
    string Name,
    int SortOrder,
    UnitTypeUsage Usage,
    int? ChargeTypeId,
    bool IsActive);

public record EditUnitTypeInput(
    int Id,
    string Name,
    int SortOrder,
    UnitTypeUsage Usage,
    int? ChargeTypeId,
    bool IsActive);

public record ToggleUnitTypeStatusInput(int Id);

public record UnitTypeDetailDto(
    int Id,
    string Name,
    int SortOrder,
    UnitTypeUsage Usage,
    int? ChargeTypeId,
    bool IsActive);

public record UnitTypeChargeTypeCandidateDto(
    int Id,
    string Name,
    string Code);
