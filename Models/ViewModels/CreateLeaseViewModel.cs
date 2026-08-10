using System;
using System.Collections.Generic;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class CreateLeaseViewModel : ILeaseFormViewModel
{
    public int? UnitId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);
    public DueDateRuleType DueDateRuleType { get; set; } = DueDateRuleType.FixedDayOfMonth;
    public int DueDay { get; set; } = 1;
    public string? Description { get; set; }
    public List<UnitLookupDto> AvailableUnits { get; set; } = [];
    public List<TenantListItemDto> Tenants { get; set; } = [];
    public List<LeaseLineItemInputDto> LeaseLineItems { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}
