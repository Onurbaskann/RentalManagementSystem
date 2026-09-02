using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Models.ViewModels;

public class PaymentStoreRoutingFormViewModel
{
    public int ChargeTypeId { get; set; }
    public PaymentRoutingScope Scope { get; set; }
    public int? PropertyId { get; set; }
    public int? UnitId { get; set; }
    public int StoreId { get; set; }
}

public record ChargeTypeSetupGuideViewModel(
    int ChargeTypeId,
    string ChargeTypeName,
    bool IsChargeTypeActive,
    bool HasUsableDefault);

public class PaymentStoreRoutingIndexViewModel
{
    public TableQuery Query { get; set; } = new();
    public PaymentStoreRoutingIndexDataDto Data { get; set; } = new();
    public PaymentStoreRoutingFormViewModel Form { get; set; } = new();
    public ChargeTypeSetupGuideViewModel? Guide { get; set; }
}
