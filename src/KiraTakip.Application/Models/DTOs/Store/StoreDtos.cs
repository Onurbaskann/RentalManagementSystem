namespace KiraTakip.Models.Dtos.Store;

public record CreateStoreInput(string Name, string? Description, bool IsActive);

public record UpdateStoreInput(string Name, string? Description, bool IsActive);

public record CreateStoreAccountVersionInput(
    int StoreId,
    string ProviderCode,
    string Currency,
    string MerchantId,
    string MerchantUser,
    string MerchantPassword);

public class StoreListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool HasActiveAccount { get; set; }
    public string? ActiveProviderCode { get; set; }
    public string? ActiveCurrency { get; set; }
}

public class StoreDetailDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<StoreAccountHistoryItemDto> Accounts { get; set; } = [];
}

public class StoreAccountHistoryItemDto
{
    public int Id { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantUser { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; }
}
