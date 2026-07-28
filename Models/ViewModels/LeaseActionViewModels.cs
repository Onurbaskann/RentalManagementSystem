using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class UpdateLeaseDueDateViewModel
{
    public DueDateRuleType RuleType { get; set; }
    public int DueDay { get; set; }
    public string? Description { get; set; }
}

public class RegenerateLeaseViewModel
{
    public DateTime StartDate { get; set; }
    public bool UpdateRate { get; set; }
    public List<LeaseLineItemInputDto>? LeaseLineItems { get; set; }
}

public class CalculateRentIncreaseViewModel
{
    public decimal CurrentAmount { get; set; }
    public decimal? InflationRate { get; set; }
    public bool ApplyVat { get; set; }
    public decimal? VatRate { get; set; }
}

public class GetDefaultLeaseLineItemsViewModel
{
    public int UnitId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public int? LeaseId { get; set; }
}
