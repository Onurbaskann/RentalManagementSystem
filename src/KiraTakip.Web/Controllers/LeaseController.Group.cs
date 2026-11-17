using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Dtos.Tenant;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Web.Controllers;

// Paylaşımlı sözleşme grubu endpoint'leri (F7a).
public partial class LeaseController
{
    private const string GroupFormView = "GroupForm";
    private const string GroupRestructureView = "GroupRestructure";

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> CreateGroup()
    {
        var viewModel = new LeaseGroupFormViewModel();
        await PopulateGroupCreateOptionsAsync(viewModel);
        return View(GroupFormView, viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.CreateGroup)]
    public async Task<IActionResult> CreateGroup(LeaseGroupFormViewModel viewModel)
    {
        viewModel.GroupId = 0;
        if (!ModelState.IsValid)
        {
            await PopulateGroupCreateOptionsAsync(viewModel);
            return View(GroupFormView, viewModel);
        }

        try
        {
            var group = await leaseGroupService.CreateGroupAsync(new CreateLeaseGroupInput(
                viewModel.UnitId!.Value,
                viewModel.StartDate,
                viewModel.EndDate,
                viewModel.DueDateRuleType,
                viewModel.DueDay,
                viewModel.Description,
                viewModel.IsRentFree,
                BuildGroupMemberInputs(viewModel),
                CurrentActorUserId(),
                BuildAccessScope()));
            return RedirectToAction(nameof(Group), new { id = group.Id });
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateGroupCreateOptionsAsync(viewModel);
            return View(GroupFormView, viewModel);
        }
    }

    [HttpGet("Lease/Group/{id}")]
    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> Group(int id)
    {
        var group = await leaseGroupService.GetDetailAsync(new GetLeaseGroupInput(id, BuildAccessScope()));
        if (group == null) return NotFound();

        var isOwner = string.Equals(group.OwnerUserId, CurrentActorUserId(), StringComparison.Ordinal);
        var viewModel = new LeaseGroupDetailViewModel { Group = group };
        viewModel.CanEdit = isOwner && viewModel.IsPending
            && await permissionService.HasPermissionAsync(PermissionCatalog.Lease.UpdateGroup);
        viewModel.CanResubmit = isOwner && viewModel.HasRevisionRequested
            && await permissionService.HasPermissionAsync(PermissionCatalog.Lease.ResubmitGroup);
        viewModel.CanDelete = viewModel.IsPending
            && await permissionService.HasPermissionAsync(PermissionCatalog.Lease.DeleteGroupDraft);
        viewModel.CanApprove = viewModel.AllDraft
            && await permissionService.HasPermissionAsync(PermissionCatalog.Lease.ApproveGroup);
        viewModel.CanExtend = viewModel.AllExtendable
            && await permissionService.HasPermissionAsync(PermissionCatalog.Lease.ExtendGroup);
        viewModel.CanTerminate = viewModel.AllTerminable
            && await permissionService.HasPermissionAsync(PermissionCatalog.Lease.TerminateGroup);
        viewModel.CanRestructure = viewModel.AllTerminable
            && await permissionService.HasPermissionAsync(PermissionCatalog.Lease.RestructureGroup);
        viewModel.CanOverrideRate = await permissionService.HasPermissionAsync(PermissionCatalog.Lease.OverrideRate);
        return View("Group", viewModel);
    }

    [HttpGet("Lease/Group/{id}/Edit")]
    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> EditGroup(int id)
    {
        var group = await leaseGroupService.GetDetailAsync(new GetLeaseGroupInput(id, BuildAccessScope()));
        if (group == null) return NotFound();
        if (!string.Equals(group.OwnerUserId, CurrentActorUserId(), StringComparison.Ordinal)
            || !await permissionService.HasPermissionAsync(PermissionCatalog.Lease.UpdateGroup))
            return Forbid();
        if (!new LeaseGroupDetailViewModel { Group = group }.IsPending)
        {
            TempData["Error"] = "Grup bu durumda düzenlenemez.";
            return RedirectToAction(nameof(Group), new { id });
        }

        var viewModel = new LeaseGroupFormViewModel
        {
            GroupId = group.GroupId,
            UnitId = group.UnitId,
            UnitName = $"{group.PropertyName} — {group.UnitName}",
            UnitArea = group.UnitArea,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            DueDateRuleType = group.DueDateRuleType,
            DueDay = group.DueDay,
            Description = group.Description,
            IsRentFree = group.IsRentFree,
            Members = group.Members.Select(member => new LeaseGroupMemberFormModel
            {
                LeaseId = member.LeaseId,
                LeaseNo = member.LeaseNo,
                TenantId = member.TenantId,
                Area = member.Area,
                LeaseLineItems = member.RateOverrides.Select(ToLeaseLineItemInput).ToList()
            }).ToList()
        };
        await PopulateGroupEditOptionsAsync(viewModel);
        return View(GroupFormView, viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.UpdateGroup)]
    public async Task<IActionResult> UpdateGroup(LeaseGroupFormViewModel viewModel)
    {
        if (viewModel.GroupId <= 0) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateGroupEditOptionsAsync(viewModel);
            return View(GroupFormView, viewModel);
        }

        try
        {
            await leaseGroupService.UpdateGroupAsync(new UpdateLeaseGroupInput(
                viewModel.GroupId,
                viewModel.StartDate,
                viewModel.EndDate,
                viewModel.DueDateRuleType,
                viewModel.DueDay,
                viewModel.Description,
                viewModel.IsRentFree,
                BuildGroupMemberInputs(viewModel),
                CurrentActorUserId(),
                BuildAccessScope()));
            return RedirectToAction(nameof(Group), new { id = viewModel.GroupId });
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateGroupEditOptionsAsync(viewModel);
            return View(GroupFormView, viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.ResubmitGroup)]
    public async Task<IActionResult> ResubmitGroup(ResubmitLeaseGroupViewModel viewModel)
    {
        ThrowIfModelStateInvalid();
        await leaseGroupService.ResubmitGroupAsync(new ResubmitLeaseGroupInput(
            viewModel.GroupId, viewModel.Explanation?.Trim(), CurrentActorUserId(), BuildAccessScope()));
        return RedirectToAction(nameof(Group), new { id = viewModel.GroupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.DeleteGroupDraft)]
    public async Task<IActionResult> DeleteGroupDraft(DeleteLeaseGroupDraftViewModel viewModel)
    {
        ThrowIfModelStateInvalid();
        await leaseGroupService.DeleteGroupDraftAsync(new DeleteLeaseGroupDraftInput(
            viewModel.GroupId, viewModel.Explanation.Trim(), CurrentActorUserId(), BuildAccessScope()));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.ApproveGroup)]
    public async Task<IActionResult> ApproveGroup(ApproveLeaseGroupViewModel viewModel)
    {
        ThrowIfModelStateInvalid();
        await leaseGroupService.ApproveGroupAsync(new ApproveLeaseGroupInput(
            viewModel.GroupId, viewModel.Explanation?.Trim(), CurrentActorUserId(), BuildAccessScope()));
        return RedirectToAction(nameof(Group), new { id = viewModel.GroupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.ExtendGroup)]
    public async Task<IActionResult> ExtendGroup(ExtendLeaseGroupViewModel viewModel)
    {
        ThrowIfModelStateInvalid();
        await leaseGroupService.ExtendGroupAsync(new ExtendLeaseGroupInput(
            viewModel.GroupId,
            viewModel.NewEndDate,
            viewModel.ApplyVat,
            viewModel.VatRate ?? 20,
            viewModel.ApplyInflation ? viewModel.InflationRate : null,
            viewModel.Description?.Trim(),
            viewModel.UpdateRate,
            await permissionService.HasPermissionAsync(PermissionCatalog.Lease.OverrideRate),
            viewModel.Members
                .Select(member => new ExtendLeaseGroupMemberInput(
                    member.LeaseId, BuildRateOverrideInputs(member.LeaseLineItems)))
                .ToList(),
            CurrentActorUserId(),
            BuildAccessScope()));
        TempData["Success"] = "Grup uzatıldı.";
        return RedirectToAction(nameof(Group), new { id = viewModel.GroupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.TerminateGroup)]
    public async Task<IActionResult> TerminateGroup(TerminateLeaseGroupViewModel viewModel)
    {
        ThrowIfModelStateInvalid();
        await leaseGroupService.TerminateGroupAsync(new TerminateLeaseGroupInput(
            viewModel.GroupId,
            viewModel.TerminationDate,
            viewModel.TerminationReason,
            viewModel.Description?.Trim(),
            CurrentActorUserId(),
            BuildAccessScope()));
        TempData["Success"] = "Grup feshedildi.";
        return RedirectToAction(nameof(Group), new { id = viewModel.GroupId });
    }

    [HttpGet("Lease/Group/{id}/Restructure")]
    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> RestructureGroup(int id)
    {
        var group = await leaseGroupService.GetDetailAsync(new GetLeaseGroupInput(id, BuildAccessScope()));
        if (group == null) return NotFound();
        if (!await permissionService.HasPermissionAsync(PermissionCatalog.Lease.RestructureGroup))
            return Forbid();
        if (!new LeaseGroupDetailViewModel { Group = group }.AllTerminable)
        {
            TempData["Error"] = "Grup bu durumda yeniden yapılandırılamaz.";
            return RedirectToAction(nameof(Group), new { id });
        }

        var terminationDate = DateTime.Today;
        var viewModel = new LeaseGroupRestructureViewModel
        {
            GroupId = group.GroupId,
            TerminationDate = terminationDate,
            StartDate = terminationDate.AddDays(1),
            EndDate = group.EndDate,
            DueDateRuleType = group.DueDateRuleType,
            DueDay = group.DueDay,
            Description = group.Description,
            IsRentFree = group.IsRentFree,
            Members = group.Members.Select(member => new LeaseGroupMemberFormModel
            {
                TenantId = member.TenantId,
                Area = member.Area
            }).ToList()
        };
        await PopulateGroupRestructureOptionsAsync(viewModel);
        return View(GroupRestructureView, viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.RestructureGroup)]
    public async Task<IActionResult> RestructureGroup(LeaseGroupRestructureViewModel viewModel)
    {
        if (viewModel.GroupId <= 0) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateGroupRestructureOptionsAsync(viewModel);
            return View(GroupRestructureView, viewModel);
        }

        try
        {
            var newGroup = await leaseGroupService.RestructureGroupAsync(new RestructureLeaseGroupInput(
                viewModel.GroupId,
                viewModel.TerminationDate,
                viewModel.TerminationReason,
                viewModel.TerminationDescription?.Trim(),
                viewModel.StartDate,
                viewModel.EndDate,
                viewModel.DueDateRuleType,
                viewModel.DueDay,
                viewModel.Description,
                viewModel.IsRentFree,
                viewModel.Members
                    .Select(member => new LeaseGroupMemberInput(
                        null,
                        member.TenantId,
                        member.Area,
                        BuildRateOverrideInputs(member.LeaseLineItems, viewModel.IsRentFree)))
                    .ToList(),
                CurrentActorUserId(),
                BuildAccessScope()));
            TempData["Success"] = "Grup yeniden yapılandırıldı. Yeni grup onay bekliyor.";
            return RedirectToAction(nameof(Group), new { id = newGroup.Id });
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateGroupRestructureOptionsAsync(viewModel);
            return View(GroupRestructureView, viewModel);
        }
    }

    private async Task PopulateGroupRestructureOptionsAsync(LeaseGroupRestructureViewModel viewModel)
    {
        var accessScope = BuildAccessScope();
        var group = await leaseGroupService.GetDetailAsync(
            new GetLeaseGroupInput(viewModel.GroupId, accessScope));
        if (group != null)
        {
            viewModel.UnitId = group.UnitId;
            viewModel.UnitName = $"{group.PropertyName} — {group.UnitName}";
            viewModel.UnitArea = group.UnitArea;
        }
        viewModel.Tenants = await tenantService.GetAllAsync(
            new GetTenantsInput(accessScope.PropertyIds, accessScope.UnitIds));
        ViewBag.UnitAreas = System.Text.Json.JsonSerializer.Serialize(
            new Dictionary<int, double> { [viewModel.UnitId ?? 0] = (double)viewModel.UnitArea });
    }


    private List<LeaseGroupMemberInput> BuildGroupMemberInputs(LeaseGroupFormViewModel viewModel)
        => viewModel.Members
            .Select(member => new LeaseGroupMemberInput(
                member.LeaseId,
                member.TenantId,
                member.Area,
                BuildRateOverrideInputs(member.LeaseLineItems, viewModel.IsRentFree)))
            .ToList();

    private async Task PopulateGroupCreateOptionsAsync(LeaseGroupFormViewModel viewModel)
    {
        var accessScope = BuildAccessScope();
        viewModel.AvailableUnits = await propertyService.GetAvailableUnitsAsync(
            new GetAvailableUnitsInput(accessScope.PropertyIds, accessScope.UnitIds, null, false));
        viewModel.Tenants = await tenantService.GetAllAsync(
            new GetTenantsInput(accessScope.PropertyIds, accessScope.UnitIds));
        ViewBag.UnitAreas = System.Text.Json.JsonSerializer.Serialize(
            viewModel.AvailableUnits.ToDictionary(unit => unit.Id, unit => (double)unit.Area));
    }

    private async Task PopulateGroupEditOptionsAsync(LeaseGroupFormViewModel viewModel)
    {
        var accessScope = BuildAccessScope();
        var group = await leaseGroupService.GetDetailAsync(
            new GetLeaseGroupInput(viewModel.GroupId, accessScope));
        if (group != null)
        {
            viewModel.UnitId = group.UnitId;
            viewModel.UnitName = $"{group.PropertyName} — {group.UnitName}";
            viewModel.UnitArea = group.UnitArea;
        }
        viewModel.AvailableUnits = [];
        viewModel.Tenants = await tenantService.GetAllAsync(
            new GetTenantsInput(accessScope.PropertyIds, accessScope.UnitIds));
        ViewBag.UnitAreas = System.Text.Json.JsonSerializer.Serialize(
            new Dictionary<int, double> { [viewModel.UnitId ?? 0] = (double)viewModel.UnitArea });
    }
}
