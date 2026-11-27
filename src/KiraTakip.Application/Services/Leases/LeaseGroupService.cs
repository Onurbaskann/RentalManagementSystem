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
using KiraTakip.Services.Interfaces.Reporting;
using KiraTakip.Domain.Pricing;
using static KiraTakip.Services.Leases.LeaseApplicationRules;

namespace KiraTakip.Services.Leases;

public class LeaseGroupService(
    ILeaseGroupRepository groupRepository,
    ILeaseRepository leaseRepository,
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IDocumentRepository documentRepository,
    IApplicationUserRepository applicationUserRepository,
    IChargeGenerationService chargeGenerationService,
    ILeaseApplicationRules rules,
    IStatisticsService statisticsService,
    IDepositRefundStatusService depositRefundStatusService,
    ICurrentUserContext currentUserContext,
    IUnitOfWork uow) : ILeaseGroupService, ITransactionalService
{
    public async Task<LeaseGroup> CreateGroupAsync(CreateLeaseGroupInput input)
    {
        await EnsureActorAsync(input.ActorUserId);
        var space = await ValidateGroupAsync(
            input.SpaceId,
            input.StartDate,
            input.EndDate,
            input.DueDay,
            input.IsRentFree,
            input.Members,
            input.AccessScope,
            excludeGroupId: null,
            existingLeaseIds: new HashSet<int>());

        return await CreateValidatedGroupAsync(space, input, previousGroupId: null);
    }

    private async Task<LeaseGroup> CreateValidatedGroupAsync(
        LeaseSpaceContextDto space,
        CreateLeaseGroupInput input,
        int? previousGroupId)
    {
        var group = new LeaseGroup { SpaceId = input.SpaceId, PreviousGroupId = previousGroupId };
        await groupRepository.AddAsync(group);
        await uow.SaveChangesAsync();

        var usedLeaseNos = (await leaseRepository.GetExistingLeaseNosAsync()).ToHashSet();
        foreach (var member in input.Members)
        {
            await AddMemberAsync(
                group,
                space,
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
        var space = await ValidateGroupAsync(
            group.SpaceId,
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
                    space,
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
            await leaseRepository.ReplaceLeaseAreasAsync(lease.Id, [(space, member.Area)]);
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
        await EnsurePreviousGroupSettledAsync(group);

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

    public async Task ExtendGroupAsync(ExtendLeaseGroupInput input)
    {
        var group = await GetGroupForMutationAsync(
            input.GroupId, input.ActorUserId, input.AccessScope, _ => true, requireOwner: false);
        EnsureGroupLeasesCan(
            group,
            LeaseLifecycle.CanExtend,
            "Yalnız aktif veya süresi dolmuş sözleşmelerden oluşan grup uzatılabilir.");
        Guard.InvalidField(
            input.Members.GroupBy(member => member.LeaseId).Any(g => g.Count() > 1),
            "Members",
            "Aynı sözleşme birden fazla kez gönderildi.",
            "LeaseGroup.MemberNotFound");
        var rateInputs = input.Members.ToDictionary(member => member.LeaseId, member => member.RateOverrides);
        Guard.InvalidField(
            rateInputs.Keys.Any(id => group.Leases.All(lease => lease.Id != id)),
            "Members",
            "Grup satırlarından biri bu gruba ait değil.",
            "LeaseGroup.MemberNotFound");
        IReadOnlyCollection<LeaseRateOverrideInput> RatesOf(Lease lease)
            => input.UpdateRate && rateInputs.TryGetValue(lease.Id, out var rates) ? rates : [];

        var leases = await LoadLeasesWithActivityLogAsync(group);
        foreach (var lease in leases)
            await rules.EnsureExtensionAllowedAsync(
                lease,
                input.NewEndDate,
                input.IsVatApplied,
                input.VatRate,
                input.UpdateRate,
                input.CanOverrideRate,
                RatesOf(lease),
                excludeLeaseGroupId: group.Id);

        foreach (var lease in leases)
        {
            var rates = RatesOf(lease);
            var oldAmount = await statisticsService.GetMonthlyAmountAsync(lease);
            if (input.UpdateRate && rates.Count > 0)
            {
                await leaseRateOverrideRepository.ReplaceAsync(
                    lease.Id,
                    BuildRateOverrides(lease.Id, rates));
                await uow.SaveChangesAsync();
            }
            var newAmount = await statisticsService.GetMonthlyAmountAsync(lease);

            var oldEndDate = lease.EndDate;
            lease.EndDate = input.NewEndDate;
            lease.Status = LeaseStatus.Active;
            lease.IsKdvApplied = input.IsVatApplied;
            lease.ActivityLog.Add(new LeaseActivityLog
            {
                LeaseId = lease.Id,
                ActivityType = LeaseActivityType.Extension,
                TransactionDate = DateTime.Now,
                Description = input.Description ?? "Sözleşme süresi uzatıldı.",
                OldEndDate = oldEndDate,
                NewEndDate = input.NewEndDate,
                OldRentAmount = oldAmount,
                NewRentAmount = newAmount,
                InflationRate = input.InflationRate,
                IsKdvApplied = input.IsVatApplied,
                KdvRate = input.IsVatApplied ? input.VatRate : null,
                KdvAmount = input.IsVatApplied
                    ? RentIncreasePolicy.CalculateVatAmount(newAmount, input.VatRate) : null,
                KdvIncludedAmount = input.IsVatApplied
                    ? RentIncreasePolicy.CalculateVatIncludedAmount(newAmount, input.VatRate) : null
            });
        }
        await uow.SaveChangesAsync();
        foreach (var lease in leases)
            await chargeGenerationService.GenerateForLeaseAsync(
                new GenerateLeaseChargesInput(lease.Id));
    }

    public async Task TerminateGroupAsync(TerminateLeaseGroupInput input)
    {
        var group = await GetGroupForMutationAsync(
            input.GroupId, input.ActorUserId, input.AccessScope, _ => true, requireOwner: false);
        EnsureGroupLeasesCan(
            group,
            LeaseLifecycle.CanTerminate,
            "Yalnız aktif sözleşmelerden oluşan grup feshedilebilir.");
        var leases = await LoadLeasesWithActivityLogAsync(group);
        TerminateLeases(leases, input.TerminationDate, input.TerminationReason, input.Description);
        await uow.SaveChangesAsync();
        foreach (var lease in leases)
            await chargeGenerationService.CancelFutureChargesAsync(
                new CancelFutureLeaseChargesInput(lease.Id, input.TerminationDate));
    }

    public async Task<LeaseGroup> RestructureGroupAsync(RestructureLeaseGroupInput input)
    {
        var oldGroup = await GetGroupForMutationAsync(
            input.GroupId, input.ActorUserId, input.AccessScope, _ => true, requireOwner: false);
        EnsureGroupLeasesCan(
            oldGroup,
            LeaseLifecycle.CanTerminate,
            "Yalnız aktif sözleşmelerden oluşan grup yeniden yapılandırılabilir.");
        Guard.InvalidField(
            string.IsNullOrWhiteSpace(input.TerminationReason),
            "TerminationReason",
            "Fesih nedeni zorunludur.",
            "LeaseGroup.TerminationReasonRequired");
        Guard.InvalidField(
            input.StartDate.Date <= input.TerminationDate.Date,
            "StartDate",
            "Yeni başlangıç tarihi fesih tarihinden sonra olmalıdır.",
            "LeaseGroup.RestructureStartNotAfterTermination");

        var createInput = new CreateLeaseGroupInput(
            oldGroup.SpaceId,
            input.StartDate,
            input.EndDate,
            input.DueDateRuleType,
            input.DueDay,
            input.Description,
            input.IsRentFree,
            input.Members,
            input.ActorUserId,
            input.AccessScope);
        var space = await ValidateGroupAsync(
            createInput.SpaceId,
            createInput.StartDate,
            createInput.EndDate,
            createInput.DueDay,
            createInput.IsRentFree,
            createInput.Members,
            createInput.AccessScope,
            excludeGroupId: oldGroup.Id,
            existingLeaseIds: new HashSet<int>());

        var leases = await LoadLeasesWithActivityLogAsync(oldGroup);
        TerminateLeases(
            leases, input.TerminationDate, input.TerminationReason, input.TerminationDescription);
        await uow.SaveChangesAsync();
        var newGroup = await CreateValidatedGroupAsync(space, createInput, oldGroup.Id);
        foreach (var lease in leases)
            await chargeGenerationService.CancelFutureChargesAsync(
                new CancelFutureLeaseChargesInput(lease.Id, input.TerminationDate));
        return newGroup;
    }

    private static void EnsureGroupLeasesCan(
        LeaseGroup group, Func<LeaseStatus, bool> canProceed, string message)
        => Guard.Conflict(
            group.Leases.Any(lease => !canProceed(lease.Status)),
            message,
            "Lease.NotActive");

    private async Task<List<Lease>> LoadLeasesWithActivityLogAsync(LeaseGroup group)
    {
        var leases = new List<Lease>();
        foreach (var lease in group.Leases.OrderBy(item => item.Id))
            leases.Add(Guard.NotFound(
                await leaseRepository.GetWithActivityLogAsync(lease.Id),
                $"Sözleşme {lease.Id} bulunamadı.",
                "Lease.NotFound"));
        return leases;
    }

    private static void TerminateLeases(
        IEnumerable<Lease> leases, DateTime terminationDate, string reason, string? description)
    {
        foreach (var lease in leases)
        {
            lease.Status = LeaseStatus.Terminated;
            lease.TerminationDate = terminationDate;
            lease.TerminationReason = reason;
            lease.ActivityLog.Add(new LeaseActivityLog
            {
                LeaseId = lease.Id,
                ActivityType = LeaseActivityType.Termination,
                TransactionDate = DateTime.Now,
                Description = description ?? reason
            });
        }
    }

    private async Task EnsurePreviousGroupSettledAsync(LeaseGroup group)
    {
        if (group.PreviousGroupId is not { } previousGroupId) return;
        var previousLeaseIds = await groupRepository.GetLeaseIdsAsync(previousGroupId);
        foreach (var leaseId in previousLeaseIds)
            Guard.Conflict(
                await leaseRepository.HasUnsettledChargesAsync(leaseId),
                "Önceki gruptaki sözleşmelerin tüm tahakkukları ödenmeden yeni grup onaylanamaz.",
                "LeaseGroup.PreviousGroupUnsettled");
        foreach (var leaseId in previousLeaseIds)
            Guard.Conflict(
                !await depositRefundStatusService.IsRefundedAsync(leaseId),
                "Önceki gruptaki sözleşmelerin depozitosu iade edilmeden yeni grup onaylanamaz.",
                "LeaseGroup.PreviousDepositNotRefunded");
    }

    public Task<LeaseGroupDetailDto?> GetDetailAsync(GetLeaseGroupInput input)
        => groupRepository.GetDetailAsync(input.GroupId, input.AccessScope);

    public Task<LeaseGroupLinkDto?> GetLinkByLeaseIdAsync(int leaseId)
        => groupRepository.GetLinkByLeaseIdAsync(leaseId);

    private async Task<LeaseSpaceContextDto> ValidateGroupAsync(
        int spaceId,
        DateTime startDate,
        DateTime endDate,
        int dueDay,
        bool isRentFree,
        IReadOnlyCollection<LeaseGroupMemberInput> members,
        SpaceAccessScope accessScope,
        int? excludeGroupId,
        IReadOnlySet<int> existingLeaseIds)
    {
        var space = Guard.NotFound(
            await leaseRepository.GetSpaceContextAsync(spaceId),
            "Seçilen alan bulunamadı.",
            "Lease.SpaceNotFound");
        EnsureSpaceInScope(space.Path, accessScope);
        Guard.InvalidField(
            !space.IsActive || space.Usage != SpaceUsage.Rentable,
            nameof(spaceId),
            "Seçilen alan aktif ve kiralanabilir değildir.",
            "Lease.SpaceNotRentable");
        Guard.InvalidField(
            !space.IsShared,
            nameof(spaceId),
            "Paylaşımlı grup yalnız paylaşımlı alanlarda açılabilir.",
            "Lease.GroupSpaceNotShared");

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
            space.Area,
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
            "Payların m² toplamı alan yüzölçümüne eşit olmalıdır.",
            "LeaseGroup.AreaSumMismatch");

        foreach (var member in members)
        {
            await rules.ValidateApplicationDataAsync(
                spaceId,
                member.TenantId,
                startDate,
                endDate,
                dueDay,
                member.RateOverrides,
                accessScope,
                isRentFree,
                member.Area,
                isGroupFlow: true,
                excludeLeaseId: member.LeaseId,
                excludeLeaseGroupId: excludeGroupId);
        }
        Guard.InvalidField(
            await leaseRepository.HasOpenApplicationForSpaceAsync(spaceId, null, excludeGroupId),
            nameof(spaceId),
            "Seçilen alan için açık bir sözleşme başvurusu bulunmaktadır.",
            "Lease.OpenApplicationConflict");
        return space;
    }

    private async Task AddMemberAsync(
        LeaseGroup group,
        LeaseSpaceContextDto space,
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
            LeaseGroupId = group.Id,
            TenantId = member.TenantId,
            StartDate = startDate,
            EndDate = endDate,
            Description = description,
            Status = LeaseStatus.Draft,
            DueDateRuleType = dueDateRuleType,
            DueDay = dueDay,
            IsRentFree = isRentFree,
            CreatedBy = actorUserId
        };
        await leaseRepository.AddAsync(lease);
        await uow.SaveChangesAsync();

        await leaseRepository.ReplaceLeaseAreasAsync(lease.Id, [(space, member.Area)]);
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
        SpaceAccessScope accessScope,
        Func<LeaseStatus, bool> canProceed,
        bool requireOwner)
    {
        await EnsureActorAsync(actorUserId);
        var group = Guard.NotFound(
            await groupRepository.GetForMutationAsync(groupId, accessScope),
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
