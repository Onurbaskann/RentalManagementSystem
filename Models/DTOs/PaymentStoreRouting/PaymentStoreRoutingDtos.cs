using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos.PaymentStoreRouting;

public record UpsertPaymentStoreRoutingInput(
    int ChargeTypeId,
    PaymentRoutingScope Scope,
    int? PropertyId,
    int? UnitId,
    int StoreId);

public class PaymentStoreRoutingListItemDto
{
    public int Id { get; set; }
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public PaymentRoutingScope Scope { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public int? PropertyId { get; set; }
    public int? UnitId { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public bool IsStoreActive { get; set; }
    public bool HasActiveStoreAccount { get; set; }
    public string? ProviderCode { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; }
}

public record PaymentStoreRoutingLookupDto(int Id, string Name, string? ParentName = null);

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
    public int UnitId { get; set; }
    public int PropertyId { get; set; }
    public int? RoutingId { get; set; }
    public PaymentRoutingScope? MatchedScope { get; set; }
    public int? StoreId { get; set; }
    public bool IsStoreActive { get; set; }
    public int ActiveAccountCount { get; set; }
    public int? StoreAccountId { get; set; }
    public string? ProviderCode { get; set; }
    public string? Currency { get; set; }
}

public record ResolvedPaymentStoreAccountDto(
    int RoutingId,
    PaymentRoutingScope MatchedScope,
    int ChargeTypeId,
    int UnitId,
    int PropertyId,
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
    public List<PaymentStoreRoutingLookupDto> Properties { get; set; } = [];
    public List<PaymentStoreRoutingLookupDto> Units { get; set; } = [];
    public List<StoreRoutingOptionDto> Stores { get; set; } = [];
}
