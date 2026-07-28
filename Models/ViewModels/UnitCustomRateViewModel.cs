using System.Collections.Generic;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class UnitCustomRateViewModel
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public bool IsLeasable { get; set; }
    public bool IsReservable { get; set; }
    public string? UnitTypeName { get; set; }

    // Scenario A — Lease: TenantCategory × ChargeType matrix
    public List<UnitRateCategoryRow> Rows { get; set; } = [];
    public List<UnitRateColumn> Columns { get; set; } = [];
    public ParentRateCardViewModel? ParentRate { get; set; }

    // Scenario B — Reservation rate rule
    public ReservationRateOverride? CustomReservationRule { get; set; }
    public ParentReservationRateOverrideCardViewModel? ParentReservationRateOverride { get; set; }
}
