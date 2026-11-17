using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public sealed class LeaseGroupMemberFormModel
{
    public int? LeaseId { get; set; }
    public int TenantId { get; set; }
    public decimal Area { get; set; }
    public string? LeaseNo { get; set; }
    public List<LeaseLineItemInputDto> LeaseLineItems { get; set; } = [];
}

public sealed class LeaseGroupFormViewModel
{
    public int GroupId { get; set; }
    public int? UnitId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1).AddDays(-1);
    public DueDateRuleType DueDateRuleType { get; set; } = DueDateRuleType.FixedDayOfMonth;
    public int DueDay { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsRentFree { get; set; }
    public List<LeaseGroupMemberFormModel> Members { get; set; } = [new(), new()];

    public string UnitName { get; set; } = string.Empty;
    public decimal UnitArea { get; set; }
    public List<UnitLookupDto> AvailableUnits { get; set; } = [];
    public List<TenantListItemDto> Tenants { get; set; } = [];

    public bool IsEdit => GroupId > 0;
}

public sealed class LeaseGroupDetailViewModel
{
    public LeaseGroupDetailDto Group { get; set; } = null!;
    public bool CanEdit { get; set; }
    public bool CanResubmit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public decimal TotalArea => Group.Members.Sum(member => member.Area);
    public bool HasRevisionRequested => Group.Members.Any(member => member.Status == LeaseStatus.RevisionRequested);
    public bool AllDraft => Group.Members.All(member => member.Status == LeaseStatus.Draft);
    public bool IsPending => Group.Members.Any(member =>
        member.Status is LeaseStatus.Draft or LeaseStatus.RevisionRequested);
}

public sealed class ResubmitLeaseGroupViewModel
{
    public int GroupId { get; set; }
    public string? Explanation { get; set; }
}

public sealed class ApproveLeaseGroupViewModel
{
    public int GroupId { get; set; }
    public string? Explanation { get; set; }
}

public sealed class DeleteLeaseGroupDraftViewModel
{
    public int GroupId { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
