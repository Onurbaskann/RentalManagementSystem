using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class TenantLeaseDetailsViewModel
{
    public LeaseDetailDto Lease { get; set; } = null!;
    public int RemainingDays { get; set; }
    public decimal MonthlyAmount { get; set; }
    public decimal AnnualAmount { get; set; }
    public bool IsActive { get; set; }
    public double DurationPercentage { get; set; }
    public OccupancyStatus UnitStatus { get; set; }
    public List<ChargeListItemDto> Charges { get; set; } = [];
    public bool HasChargeAccess { get; set; }
    public List<ChargeLineItem> CurrentLineItems { get; set; } = [];
    public DateTime? CurrentLineItemPeriod { get; set; }
    public decimal? DepositAmount { get; set; }
    public decimal EffectiveVatRate { get; set; }
    public List<Document> Documents { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}
