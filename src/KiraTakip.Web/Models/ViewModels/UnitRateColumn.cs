using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public class UnitRateColumn
{
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public ChargeTypeBehavior ChargeTypeBehavior { get; set; }
}
