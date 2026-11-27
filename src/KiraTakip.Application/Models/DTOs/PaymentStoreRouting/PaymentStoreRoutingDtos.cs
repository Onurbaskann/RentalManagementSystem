using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.Space;

namespace KiraTakip.Models.Dtos.PaymentStoreRouting;

public record UpsertPaymentStoreRoutingInput(
    int ChargeTypeId,
    int? SpaceId,
    int StoreId);

public class PaymentStoreRoutingListItemDto
{
    public int Id { get; set; }
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public string ScopeName { get; set; } = string.Empty;
    public int? SpaceId { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public bool IsStoreActive { get; set; }
    public bool HasActiveStoreAccount { get; set; }
    public string? ProviderCode { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; }
}

public record ChargeTypeRoutingOptionDto(int Id, string Name, bool IsActive);

public record StoreRoutingOptionDto(
    int Id,
    string Name,
    string ProviderCode,
    string Currency);

public record MissingDefaultRoutingDto(
    int ChargeTypeId,
    string ChargeTypeName,
    string ChargeTypeCode,
    bool IsChargeTypeActive);

public class PaymentRoutingResolutionCandidateDto
{
    public int? SpaceId { get; set; }
    public int? RoutingId { get; set; }
    public int? MatchedSpaceId { get; set; }
    public int? StoreId { get; set; }
    public bool IsStoreActive { get; set; }
    public int ActiveAccountCount { get; set; }
    public int? StoreAccountId { get; set; }
    public string? ProviderCode { get; set; }
    public string? Currency { get; set; }
}

public record ResolvedPaymentStoreAccountDto(
    int RoutingId,
    int? MatchedSpaceId,
    int ChargeTypeId,
    int? SpaceId,
    int StoreId,
    int StoreAccountId,
    string ProviderCode,
    string Currency);

public class PaymentStoreRoutingIndexDataDto
{
    public PagedResult<PaymentStoreRoutingListItemDto> Routings { get; set; } = new();
    public int HistoryCount { get; set; }
    public List<MissingDefaultRoutingDto> MissingDefaults { get; set; } = [];
    public List<ChargeTypeRoutingOptionDto> ChargeTypes { get; set; } = [];
    public List<SpaceOptionDto> Spaces { get; set; } = [];
    public List<StoreRoutingOptionDto> Stores { get; set; } = [];
}
