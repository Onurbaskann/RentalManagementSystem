using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Space;

public record SpaceAddressInput(string City, string District, string Neighborhood, string Address);

public record CreateSpaceInput(
    int? ParentId,
    int SpaceTypeId,
    string Code,
    string Name,
    int SortOrder,
    string? Description,
    decimal Area,
    SpaceUsage Usage,
    bool IsPreAllocatable,
    bool IsShared,
    SpaceAddressInput? Address);

public record UpdateSpaceInput(
    int Id,
    int? ParentId,
    int SpaceTypeId,
    string Name,
    int SortOrder,
    string? Description,
    decimal Area,
    SpaceUsage Usage,
    bool IsPreAllocatable,
    bool IsShared);

public record SpaceSaveResult(int Id, IReadOnlyList<SpaceChangeWarning> Warnings);

public record SpaceOptionDto(int Id, string Name, string NamePath);

public record SpaceTreeRow(
    int Id,
    int? ParentId,
    string Code,
    string Name,
    string SpaceTypeName,
    SpaceUsage Usage,
    decimal Area,
    bool IsActive,
    int Depth,
    int SortOrder);

public class SpaceTreeNodeDto
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SpaceTypeName { get; init; } = string.Empty;
    public SpaceUsage Usage { get; init; }
    public decimal Area { get; init; }
    public bool IsActive { get; init; }
    public int Depth { get; init; }
    public List<SpaceTreeNodeDto> Children { get; } = new();
}

public record SpaceDetailDto(
    int Id,
    int? ParentId,
    int SpaceTypeId,
    string Code,
    string Name,
    int SortOrder,
    string? Description,
    decimal Area,
    SpaceUsage Usage,
    bool IsPreAllocatable,
    bool IsShared,
    bool IsActive,
    int Depth);

public record SpaceHistoryDto(SpaceHistoryField Field, string? OldValue, string? NewValue, DateTime ChangedAt, string UserId);

public record SpaceDeletePreviewDto(
    IReadOnlyList<SpaceChangeViolation> Violations,
    bool HasAddress,
    IReadOnlyList<string> AttributeNames,
    IReadOnlyList<string> RateDescriptions,
    IReadOnlyList<string> ReservationRuleDescriptions,
    IReadOnlyList<string> PaymentRoutingDescriptions)
{
    public bool CanDelete => Violations.Count == 0;
}

public record SpaceLinkedDefinitions(
    SpaceAddress? Address,
    List<SpaceAttributeValue> AttributeValues,
    List<SpaceRate> Rates,
    List<ReservationRateOverride> ReservationRules,
    List<KiraTakip.Models.Entities.PaymentStoreRouting> PaymentRoutings);

public enum SpaceTypeDeleteResult { Deleted = 1, Deactivated = 2 }

public record SpaceTypeInput(
    string Code,
    string Name,
    int SortOrder,
    SpaceUsage DefaultUsage,
    bool DefaultIsPreAllocatable,
    IReadOnlyList<int>? DefaultAttributeIds);

public record SpaceTypeDto(
    int Id,
    string Code,
    string Name,
    int SortOrder,
    SpaceUsage DefaultUsage,
    bool DefaultIsPreAllocatable,
    bool IsActive,
    IReadOnlyList<int> DefaultAttributeIds);

public record SpaceAttributeInput(string Code, string Name, AttributeDataType DataType, string? Options);

public record SpaceAttributeDto(int Id, string Code, string Name, AttributeDataType DataType, IReadOnlyList<string> Options, bool IsActive);

public record SpaceAttributeValueInput(int AttributeDefinitionId, string? Value);

public record SpaceAttributeValueDto(int AttributeDefinitionId, string Name, AttributeDataType DataType, string Value);

public record SpaceRateInput(
    int SpaceId,
    int TenantCategoryId,
    int ChargeTypeId,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal KdvRate);

public record SpaceRateDto(
    int Id,
    int SpaceId,
    int TenantCategoryId,
    int ChargeTypeId,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal KdvRate);
