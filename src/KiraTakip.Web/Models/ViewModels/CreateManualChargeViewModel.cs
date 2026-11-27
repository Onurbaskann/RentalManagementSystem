using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Charge;

namespace KiraTakip.Web.Models.ViewModels;

public class CreateManualChargeViewModel
{
    public int TenantId { get; set; }
    public int? LeaseId { get; set; }
    public int SpaceId { get; set; }
    public int ChargeTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsVatApplied { get; set; }
    public decimal VatRate { get; set; } = 20;
    public DateTime DueDate { get; set; } = DateTime.Today;
    public string? Note { get; set; }
    public List<LeaseDropdownDto> ActiveLeases { get; set; } = [];
    public List<ChargeTypeLookupDto> ChargeTypes { get; set; } = [];
    public List<ChargeSpaceOptionDto> Spaces { get; set; } = [];
}
