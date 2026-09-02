using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos.Store;

namespace KiraTakip.Models.ViewModels;

public class StoreFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StoreAccountFormViewModel
{
    public int StoreId { get; set; }
    public string ProviderCode { get; set; } = PaymentProviderCodes.Paratika;
    public string Currency { get; set; } = CurrencyCodes.Try;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantUser { get; set; } = string.Empty;
    public string MerchantPassword { get; set; } = string.Empty;
}

public class StoreEditViewModel
{
    public StoreDetailDto Store { get; set; } = new();
    public StoreAccountFormViewModel AccountForm { get; set; } = new();
}
