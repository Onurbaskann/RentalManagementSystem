using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;
using KiraTakip.Domain.Leases;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Documents;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Leases;
using static KiraTakip.Services.Leases.LeaseApplicationRules;

namespace KiraTakip.Services.Leases;

public class LeaseGroupService(
    ILeaseGroupRepository groupRepository,
    ILeaseRepository leaseRepository,
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IUnitRepository unitRepository,
    IDocumentRepository documentRepository,
    IApplicationUserRepository applicationUserRepository,
    IChargeGenerationService chargeGenerationService,
    ILeaseApplicationRules rules,
    ICurrentUserContext currentUserContext,
    IUnitOfWork uow) : ILeaseGroupService, ITransactionalService
{
    public async Task<LeaseGroup> CreateGroupAsync(CreateLeaseGroupInput input)
    {
        await EnsureActorAsync(input.ActorUserId);
        var unit = await ValidateGroupAsync(
            input.UnitId,
            input.StartDate,
            input.EndDate,
            input.DueDay,
            input.IsRentFree,
            input.Members,
            input.AccessScope,
            excludeGroupId: null,
            existingLeaseIds: new HashSet<int>());

        var group = new LeaseGroup { UnitId = input.UnitId };
        await groupRepository.AddAsync(group);
        await uow.SaveChangesAsync();

        var usedLeaseNos = (await leaseRepository.GetExistingLeaseNosAsync()).ToHashSet();
        foreach (var member in input.Members)
        {
            await AddMemberAsync(
                group,
                unit,
                member,
                input.StartDate,
                input.EndDate,
                input.DueDateRuleType,
                input.DueDay,
                input.Description,
                input.IsRentFree,
                input.ActorUserId,
                usedLeaseNos);
        }
        await uow.SaveChangesAsync();
        return group;
    }

    public async Task UpdateGroupAsync(UpdateLeaseGroupInput input)
    {
        var group = await GetGroupForMutationAsync(
            input.GroupId,
            input.ActorUserId,
            input.AccessScope,
            status => status is LeaseStatus.Draft or LeaseStatus.RevisionRequested,
            requireOwner: true);
        var existingIds = group.Leases.Select(lease => lease.Id).ToHashSet();
        var unit = await ValidateGroupAsync(
            group.UnitId,
            input.StartDate,
            input.EndDate,
            input.DueDay,
            input.IsRentFree,
            input.Members,
            input.AccessScope,
            group.Id,
            existingIds);

        var keptIds = input.Members
            .Where(member => member.LeaseId.HasValue)
            .Select(member => member.LeaseId!.Value)
            .ToHashSet();
        foreach (var removed in group.Leases.Where(lease => !keptIds.Contains(lease.Id)).ToList())
        {
            await rules.AddReviewAsync(
                removed,
                LeaseReviewActionType.Deleted,
                removed.Status,
                null,
                input.ActorUserId,
                "Grup düzenlemesiyle gruptan çıkarıldı.");
            await SoftDeleteLeaseAsync(removed);
        }

        var usedLeaseNos = (await leaseRepository.GetExistingLeaseNosAsync()).ToHashSet();
        foreach (var member in input.Members)
        {
            if (member.LeaseId is not { } leaseId)
            {
                await AddMemberAsync(
                    group,
                    unit,
                    member,
                    input.StartDate,
                    input.EndDate,
                    input.DueDateRuleType,
                    input.DueDay,
                    input.Description,
                    input.IsRentFree,
                    input.ActorUserId,
                    usedLeaseNos);
                continue;
            }

            var lease = group.Leases.Single(item => item.Id == leaseId);
            lease.TenantId = member.TenantId;
            lease.StartDate = input.StartDate;
            lease.EndDate = input.EndDate;
            lease.DueDateRuleType = input.DueDateRuleType;
            lease.DueDay = input.DueDay;
            lease.Description = input.Description;
            lease.IsRentFree = input.IsRentFree;
            lease.UnitTypeNameSnapshot = unit.UnitTypeName;
            lease.UnitTypeUsageSnapshot = unit.UnitTypeUsage;
            lease.RentalModeSnapshot = unit.RentalMode;
            lease.UnitAreaSnapshot = unit.Area;
            await leaseRepository.UpsertLeaseAreaAsync(lease.Id, member.Area, member.Area);
            await leaseRateOverrideRepository.ReplaceAsync(
                lease.Id,
                BuildRateOverrides(lease.Id, member.RateOverrides));
            await rules.AddReviewAsync(
                lease,
                LeaseReviewActionType.DraftUpdated,
                lease.Status,
                lease.Status,
                input.ActorUserId);
        }
        await uow.SaveChangesAsync();
    }

    public async Task ResubmitGroupAsync(ResubmitLeaseGroupInput input)
    {
        var group = await GetGroupForMutationAsync(
            input.GroupId,
            input.ActorUserId,
            input.AccessScope,
            status => status is LeaseStatus.Draft or LeaseStatus.RevisionRequested,
            requireOwner: true);
        var revisions = group.Leases
            .Where(lease => lease.Status == LeaseStatus.RevisionRequested)
            .ToList();
        Guard.Conflict(
            revisions.Count == 0,
            "İşlem başvurunun mevcut durumunda yapılamaz.",
            "Lease.InvalidDraftStatus");
        var explanation = NormalizeOptionalExplanation(input.Explanation);
        foreach (var lease in revisions)
        {
            lease.Status = LeaseStatus.Draft;
            await rules.AddReviewAsync(
                lease,
                LeaseReviewActionType.Resubmitted,
                LeaseStatus.RevisionRequested,
                LeaseStatus.Draft,
                input.ActorUserId,
                explanation);
        }
        await uow.SaveChangesAsync();
    }

    public async Task DeleteGroupDraftAsync(DeleteLeaseGroupDraftInput input)
    {
        var group = await GetGroupForMutationAsync(
            input.GroupId,
            input.ActorUserId,
            input.AccessScope,
            LeaseApplicationLifecycle.CanDeleteApplication,
            requireOwner: false);
        foreach (var lease in group.Leases)
            Guard.Conflict(
                await leaseRepository.HasChargesAsync(lease.Id),
                "Tahakkuk bulunan başvuru silinemez.",
                "Lease.DraftHasCharges");
        var explanation = NormalizeRequiredExplanation(input.Explanation);
        foreach (var lease in group.Leases.ToList())
        {
            await rules.AddReviewAsync(
                lease,
                LeaseReviewActionType.Deleted,
                lease.Status,
                null,
                input.ActorUserId,
                explanation);
            await SoftDeleteLeaseAsync(lease);
        }
        group.IsDeleted = true;
        group.IsActive = false;
        await uow.SaveChangesAsync();
    }

    public async Task ApproveGroupAsync(ApproveLeaseGroupInput input)
    {
        var group = await GetGroupForMutationAsync(
            input.GroupId,
            input.ActorUserId,
            input.AccessScope,
            status => status is LeaseStatus.Draft or LeaseStatus.RevisionRequested,
            requireOwner: false);
        Guard.Conflict(
            group.Leases.Any(lease => lease.Status == LeaseStatus.RevisionRequested),
            "Revizyon istenen sözleşme bulunan grup onaylanamaz.",
            "Lease.GroupRevisionRequested");

        foreach (var lease in group.Leases)
        {
            await rules.ActivateAsync(
                lease,
                input.ActorUserId,
                input.Explanation,
                input.AccessScope);
        }
        await uow.SaveChangesAsync();
        foreach (var lease in group.Leases)
            await chargeGenerationService.GenerateForLeaseAsync(
                new GenerateLeaseChargesInput(lease.Id));
    }

    public Task<LeaseGroupDetailDto?> GetDetailAsync(GetLeaseGroupInput input)
        => groupRepository.GetDetailAsync(
            input.GroupId,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());

    public Task<LeaseGroupLinkDto?> GetLinkByLeaseIdAsync(int leaseId)
        => groupRepository.GetLinkByLeaseIdAsync(leaseId);

    private async Task<LeaseUnitContextDto> ValidateGroupAsync(
        int unitId,
        DateTime startDate,
        DateTime endDate,
        int dueDay,
        bool isRentFree,
        IReadOnlyCollection<LeaseGroupMemberInput> members,
        LeaseAccessScopeInput accessScope,
        int? excludeGroupId,
        IReadOnlySet<int> existingLeaseIds)
    {
        var unit = Guard.NotFound(
            await unitRepository.GetLeaseContextAsync(unitId),
            "Seçilen birim bulunamadı.",
            "Lease.UnitNotFound");
        EnsureScope(unit.PropertyId, unit.UnitId, accessScope);
        Guard.InvalidField(
            !unit.IsRentable || unit.UnitTypeUsage != UnitTypeUsage.Rentable,
            nameof(unitId),
            "Seçilen birim kiralanabilir değildir.",
            "Lease.UnitNotRentable");
        Guard.InvalidField(
            unit.RentalMode != UnitRentalMode.UnitBased,
            nameof(unitId),
            "Paylaşımlı grup yalnız birim bazlı birimlerde açılabilir.",
            "Lease.GroupUnitNotUnitBased");

        Guard.InvalidField(
            members.Any(member => member.Area <= 0),
            "Members",
            "Her kiracının m² değeri sıfırdan büyük olmalıdır.",
            "LeaseGroup.InvalidArea");
        var memberLeaseIds = members
            .Where(member => member.LeaseId.HasValue)
            .Select(member => member.LeaseId!.Value)
            .ToList();
        Guard.InvalidField(
            memberLeaseIds.Distinct().Count() != memberLeaseIds.Count
                || memberLeaseIds.Any(id => !existingLeaseIds.Contains(id)),
            "Members",
            "Grup satırlarından biri bu gruba ait değil.",
            "LeaseGroup.MemberNotFound");
        var violations = LeaseGroupPolicy.GetViolations(
            unit.Area,
            members.Select(member => new LeaseGroupShare(
                member.TenantId, member.Area, startDate, endDate)).ToList());
        Guard.InvalidField(
            violations.Contains(LeaseGroupViolation.TooFewLeases),
            "Members",
            "Paylaşımlı grupta en az 2 sözleşme olmalıdır.",
            "LeaseGroup.TooFewLeases");
        Guard.InvalidField(
            violations.Contains(LeaseGroupViolation.DuplicateTenant),
            "Members",
            "Bir kiracı grupta yalnız bir pay alabilir.",
            "LeaseGroup.DuplicateTenant");
        Guard.InvalidField(
            violations.Contains(LeaseGroupViolation.AreaSumMismatch),
            "Members",
            "Payların m² toplamı birim alanına eşit olmalıdır.",
            "LeaseGroup.AreaSumMismatch");

        foreach (var member in members)
        {
            await rules.ValidateApplicationDataAsync(
                unitId,
                member.TenantId,
                startDate,
                endDate,
                dueDay,
                member.RateOverrides,
                accessScope,
                isRentFree,
                null,
                null,
                member.LeaseId,
                excludeGroupId,
                member.Area);
        }
        Guard.InvalidField(
            await leaseRepository.HasOpenApplicationForUnitAsync(unitId, null, excludeGroupId),
            nameof(unitId),
            "Seçilen birim için açık bir sözleşme başvurusu bulunmaktadır.",
            "Lease.OpenApplicationConflict");
        return unit;
    }

    private async Task AddMemberAsync(
        LeaseGroup group,
        LeaseUnitContextDto unit,
        LeaseGroupMemberInput member,
        DateTime startDate,
        DateTime endDate,
        DueDateRuleType dueDateRuleType,
        int dueDay,
        string? description,
        bool isRentFree,
        string actorUserId,
        HashSet<string> usedLeaseNos)
    {
        if (!LeaseNumberPolicy.TryGenerateNextLeaseNo(usedLeaseNos, out var leaseNo))
            throw new BusinessException(
                "Sözleşme No üretilemedi.",
                ErrorType.Failure,
                "Lease.NumberGenerationFailed");
        usedLeaseNos.Add(leaseNo);

        var lease = new Lease
        {
            LeaseNo = leaseNo,
            UnitId = group.UnitId,
            LeaseGroupId = group.Id,
            TenantId = member.TenantId,
            StartDate = startDate,
            EndDate = endDate,
            Description = description,
            Status = LeaseStatus.Draft,
            DueDateRuleType = dueDateRuleType,
            DueDay = dueDay,
            IsRentFree = isRentFree,
            UnitTypeNameSnapshot = unit.UnitTypeName,
            UnitTypeUsageSnapshot = unit.UnitTypeUsage,
            RentalModeSnapshot = unit.RentalMode,
            UnitAreaSnapshot = unit.Area,
            CreatedBy = actorUserId
        };
        await leaseRepository.AddAsync(lease);
        await uow.SaveChangesAsync();

        await leaseRepository.UpsertLeaseAreaAsync(lease.Id, member.Area, member.Area);
        await leaseRateOverrideRepository.ReplaceAsync(
            lease.Id,
            BuildRateOverrides(lease.Id, member.RateOverrides));
        await rules.AddReviewAsync(
            lease,
            LeaseReviewActionType.DraftCreated,
            null,
            LeaseStatus.Draft,
            actorUserId);
        await uow.SaveChangesAsync();
    }

    private async Task SoftDeleteLeaseAsync(Lease lease)
    {
        await leaseRateOverrideRepository.SoftDeleteByLeaseIdAsync(lease.Id);
        await documentRepository.SoftDeleteByOwnerAsync(DocumentOwnerType.Lease, lease.Id);
        lease.IsDeleted = true;
        lease.IsActive = false;
    }

    private async Task<LeaseGroup> GetGroupForMutationAsync(
        int groupId,
        string actorUserId,
        LeaseAccessScopeInput accessScope,
        Func<LeaseStatus, bool> canProceed,
        bool requireOwner)
    {
        await EnsureActorAsync(actorUserId);
        var group = Guard.NotFound(
            await groupRepository.GetForMutationAsync(
                groupId,
                accessScope.PropertyIds?.ToList(),
                accessScope.UnitIds?.ToList()),
            "Sözleşme grubu bulunamadı veya yetki kapsamınızın dışında.",
            "LeaseGroup.NotFound");
        Guard.Conflict(
            group.Leases.Count == 0 || group.Leases.Any(lease => !canProceed(lease.Status)),
            "İşlem grubun mevcut durumunda yapılamaz.",
            "Lease.InvalidDraftStatus");
        Guard.Forbidden(
            requireOwner && group.CreatedBy != actorUserId,
            "Bu grubu yalnız oluşturan kişi düzenleyebilir.",
            "Lease.NotApplicationOwner");
        return group;
    }

    private async Task EnsureActorAsync(string actorUserId)
    {
        Guard.Forbidden(
            string.IsNullOrWhiteSpace(actorUserId)
                || (currentUserContext.UserId != null
                    && currentUserContext.UserId != actorUserId),
            "Geçersiz işlem kullanıcısı.",
            "Lease.InvalidActor");
        Guard.NotFound(
            (await applicationUserRepository.GetByIdsAsync([actorUserId])).FirstOrDefault(),
            "İşlem kullanıcısı bulunamadı.",
            "Lease.ActorNotFound");
    }
}
