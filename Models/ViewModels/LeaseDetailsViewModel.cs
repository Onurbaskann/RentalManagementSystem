using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class LeaseDetailsViewModel
{
    public LeaseDetailDto Lease { get; set; } = null!;
    public int RemainingDays { get; set; }
    public decimal MonthlyAmount { get; set; }
    public decimal AnnualAmount { get; set; }
    public bool IsActive { get; set; }
    public double DurationPercentage { get; set; }
    public OccupancyStatus UnitStatus { get; set; }
    public List<LeaseListItemDto> PreviousLeases { get; set; } = [];
    public List<LeaseListItemDto> TenantOtherLeases { get; set; } = [];
    public List<ChargeListItemDto> Charges { get; set; } = [];
    public bool HasPaymentAccess { get; set; }
    public ParentRateCardViewModel? ParentRate { get; set; }
    public List<ChargeLineItem> CurrentLineItems { get; set; } = [];
    public DateTime? CurrentLineItemPeriod { get; set; }
    public decimal? DepositAmount { get; set; }
    public decimal EffectiveVatRate { get; set; }
    public DateTime DefaultRegenerationStartDate { get; set; } = DateTime.Today;
    public DateTime? LastPaidPeriod { get; set; }
    public int UnpaidChargeCount { get; set; }
    public List<Document> Documents { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}
