using KiraTakip.Domain.Leases;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public static class LeaseCreateRoutes
{
    public const string SharedType = "paylasimli";
}

public sealed record LeaseKindChoiceViewModel(
    bool CanCreate,
    bool CanCreateGroup,
    bool IsShared,
    string NormalUrl,
    string SharedUrl)
{
    public const string ViewDataKey = "LeaseKindChoice";
    public bool IsVisible => CanCreate && CanCreateGroup;
}

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
    public int? SpaceId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1).AddDays(-1);
    public DueDateRuleType DueDateRuleType { get; set; } = DueDateRuleType.FixedDayOfMonth;
    public int DueDay { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsRentFree { get; set; }
    public List<LeaseGroupMemberFormModel> Members { get; set; } = [new(), new()];

    public string SpacePath { get; set; } = string.Empty;
    public decimal SpaceArea { get; set; }
    public List<LeaseSpaceOptionDto> AvailableSpaces { get; set; } = [];
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
    public bool CanExtend { get; set; }
    public bool CanTerminate { get; set; }
    public bool CanRestructure { get; set; }
    public bool CanOverrideRate { get; set; }
    public bool AllExtendable => Group.Members.Count > 0 && Group.Members.All(member => LeaseLifecycle.CanExtend(member.Status));
    public bool AllTerminable => Group.Members.Count > 0 && Group.Members.All(member => LeaseLifecycle.CanTerminate(member.Status));
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

public sealed class ExtendLeaseGroupMemberFormModel
{
    public int LeaseId { get; set; }
    public List<LeaseLineItemInputDto> LeaseLineItems { get; set; } = [];
}

public sealed class ExtendLeaseGroupViewModel
{
    public int GroupId { get; set; }
    public DateTime NewEndDate { get; set; }
    public bool ApplyInflation { get; set; }
    public decimal? InflationRate { get; set; }
    public bool ApplyVat { get; set; }
    public decimal? VatRate { get; set; }
    public string? Description { get; set; }
    public bool UpdateRate { get; set; }
    public List<ExtendLeaseGroupMemberFormModel> Members { get; set; } = [];
}

public sealed class TerminateLeaseGroupViewModel
{
    public int GroupId { get; set; }
    public DateTime TerminationDate { get; set; } = DateTime.Today;
    public string TerminationReason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class LeaseGroupRestructureViewModel
{
    public int GroupId { get; set; }
    public DateTime TerminationDate { get; set; } = DateTime.Today;
    public string TerminationReason { get; set; } = string.Empty;
    public string? TerminationDescription { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);
    public DueDateRuleType DueDateRuleType { get; set; } = DueDateRuleType.FixedDayOfMonth;
    public int DueDay { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsRentFree { get; set; }
    public List<LeaseGroupMemberFormModel> Members { get; set; } = [new(), new()];

    public int? SpaceId { get; set; }
    public string SpacePath { get; set; } = string.Empty;
    public decimal SpaceArea { get; set; }
    public List<TenantListItemDto> Tenants { get; set; } = [];

    /// <summary>Üye/m² toplamı betiği (_LeaseGroupFormScripts) için ortak form modeline çevirir.</summary>
    public LeaseGroupFormViewModel ToFormModel() => new()
    {
        GroupId = GroupId,
        SpaceId = SpaceId,
        SpacePath = SpacePath,
        SpaceArea = SpaceArea,
        StartDate = StartDate,
        EndDate = EndDate,
        DueDateRuleType = DueDateRuleType,
        DueDay = DueDay,
        Description = Description,
        IsRentFree = IsRentFree,
        Members = Members,
        Tenants = Tenants
    };
}
