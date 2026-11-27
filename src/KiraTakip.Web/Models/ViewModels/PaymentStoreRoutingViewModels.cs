using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Web.Models.ViewModels;

public class PaymentStoreRoutingFormViewModel
{
    public int ChargeTypeId { get; set; }
    public int? SpaceId { get; set; }
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
