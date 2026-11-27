using KiraTakip.Data;
using KiraTakip.Domain.Leases;
using KiraTakip.Domain.Pricing;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Document;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Documents;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Reporting;
using static KiraTakip.Services.Leases.LeaseApplicationRules;

namespace KiraTakip.Services.Leases;

public class LeaseService(
    ILeaseRepository leaseRepository,
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IChargeLineItemRepository chargeLineItemRepository,
    IChargeTypeRepository chargeTypeRepository,
    ITenantRepository tenantRepository,
    IChargeGenerationService chargeGenerationService,
    IUnitOfWork uow,
    IStatisticsService statisticsService,
    ILeaseReviewHistoryRepository reviewHistoryRepository,
    IDocumentRepository documentRepository,
    IDocumentTypeRepository documentTypeRepository,
    IApplicationUserRepository applicationUserRepository,
    ICurrentUserContext currentUserContext,
    ILeaseApplicationRules rules) : ILeaseService, ITransactionalService
{
    public async Task<List<LeaseListItemDto>> GetAllAsync(GetLeasesInput input)
    {
        var list = await leaseRepository.GetListAsync(input.Filter, input.Scope);
        await PopulateMonthlyAmountsAsync(list);

        return list;
    }

    public Task<LeaseDetailDto?> GetDetailsAsync(GetLeaseDetailsInput input)
        => leaseRepository.GetDetailsAsync(input.LeaseId);

    public async Task<LeaseDetailDto> GetTenantDetailsAsync(GetTenantLeaseDetailsInput input)
        => Guard.NotFound(
            await leaseRepository.GetTenantDetailsAsync(
                input.LeaseId,
                input.TenantId,
                input.AccessScope),
            "Sözleşme bulunamadı.",
            "TENANT_LEASE_NOT_FOUND");

    public Task<List<LeaseSpaceOptionDto>> GetSelectableSpacesAsync(
        bool sharedOnly,
        SpaceAccessScope? scope,
        int? includeSpaceId = null)
        => leaseRepository.GetSelectableSpacesAsync(sharedOnly, scope, includeSpaceId);

    public async Task<Lease> CreateAsync(CreateLeaseInput input)
    {
        var actorUserId = Guard.NotFound(
            currentUserContext.UserId,
            "Oturum kullanıcısı bulunamadı.",
            "Lease.ActorNotFound");
        return await CreateDraftAsync(new CreateLeaseDraftInput(
            input.SpaceId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDateRuleType,
            input.DueDay,
            input.Description,
            input.RateOverrides,
            actorUserId,
            input.AccessScope,
            input.IsRentFree,
            input.RentedArea));
    }

    public async Task<Lease> CreateDraftAsync(CreateLeaseDraftInput input)
    {
        await EnsureActorAsync(input.ActorUserId);
        var validated = await rules.ValidateApplicationDataAsync(
            input.SpaceId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDay,
            input.RateOverrides,
            input.AccessScope,
            input.IsRentFree,
            input.RentedArea);
        Guard.InvalidField(
            await leaseRepository.HasOpenApplicationForSpaceAsync(input.SpaceId),
            nameof(input.SpaceId),
            "Seçilen alan için açık bir sözleşme başvurusu bulunmaktadır.",
            "Lease.OpenApplicationConflict");

        var lease = new Lease
        {
            LeaseNo = await GenerateLeaseNoAsync(),
            TenantId = input.TenantId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Description = input.Description,
            Status = LeaseStatus.Draft,
            DueDateRuleType = input.DueDateRuleType,
            DueDay = input.DueDay,
            IsRentFree = input.IsRentFree,
            CreatedBy = input.ActorUserId
        };
        await leaseRepository.AddAsync(lease);
        await uow.SaveChangesAsync();

        await leaseRepository.ReplaceLeaseAreasAsync(
            lease.Id,
            [(validated.Space, validated.RentedArea)]);
        await leaseRateOverrideRepository.ReplaceAsync(
            lease.Id,
            BuildRateOverrides(lease.Id, input.RateOverrides));
        await reviewHistoryRepository.AddAsync(new LeaseReviewHistory
        {
            LeaseId = lease.Id,
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = input.ActorUserId,
            ActionDate = DateTime.UtcNow
        });
        await uow.SaveChangesAsync();

        return lease;
    }

    public Task<LeaseDraftEditDto?> GetDraftForEditAsync(GetLeaseDraftInput input)
        => leaseRepository.GetDraftForEditAsync(input.LeaseId, input.AccessScope);

    public async Task<IReadOnlyList<LeaseReviewHistoryDto>> GetReviewHistoryAsync(int leaseId)
        => await reviewHistoryRepository.GetByLeaseIdAsync(leaseId);

    public async Task UpdateDraftAsync(UpdateLeaseDraftInput input)
    {
        var lease = await GetApplicationForMutationAsync(
            input.LeaseId,
            input.ActorUserId,
            input.ExpectedRowVersion,
            input.AccessScope,
            LeaseApplicationLifecycle.CanEditDraft,
            requireOwner: true);
        EnsureNotGrouped(lease);
        await ApplyApplicationChangesAsync(
            lease,
            input.SpaceId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDateRuleType,
            input.DueDay,
            input.Description,
            input.RateOverrides,
            input.AccessScope,
            input.IsRentFree,
            input.RentedArea);
        await AddReviewAsync(
            lease,
            LeaseReviewActionType.DraftUpdated,
            LeaseStatus.Draft,
            LeaseStatus.Draft,
            input.ActorUserId);
        await uow.SaveChangesAsync();
    }

    public async Task ResubmitRevisionAsync(ResubmitLeaseRevisionInput input)
    {
        var lease = await GetApplicationForMutationAsync(
            input.LeaseId,
            input.ActorUserId,
            input.ExpectedRowVersion,
            input.AccessScope,
            status => LeaseApplicationLifecycle.CanTransition(status, LeaseStatus.Draft),
            requireOwner: true);
        EnsureNotGrouped(lease);
        await ApplyApplicationChangesAsync(
            lease,
            input.SpaceId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDateRuleType,
            input.DueDay,
            input.Description,
            input.RateOverrides,
            input.AccessScope,
            input.IsRentFree,
            input.RentedArea);
        lease.Status = LeaseStatus.Draft;
        await AddReviewAsync(
            lease,
            LeaseReviewActionType.Resubmitted,
            LeaseStatus.RevisionRequested,
            LeaseStatus.Draft,
            input.ActorUserId,
            NormalizeOptionalExplanation(input.Explanation));
        await uow.SaveChangesAsync();
    }

    public async Task RequestRevisionAsync(RequestLeaseRevisionInput input)
    {
        var lease = await GetApplicationForMutationAsync(
            input.LeaseId,
            input.ActorUserId,
            input.ExpectedRowVersion,
            input.AccessScope,
            status => LeaseApplicationLifecycle.CanTransition(status, LeaseStatus.RevisionRequested),
            requireOwner: false);
        var explanation = NormalizeRequiredExplanation(input.Explanation);
        lease.Status = LeaseStatus.RevisionRequested;
        await AddReviewAsync(
            lease,
            LeaseReviewActionType.RevisionRequested,
            LeaseStatus.Draft,
            LeaseStatus.RevisionRequested,
            input.ActorUserId,
            explanation);
        await uow.SaveChangesAsync();
    }

    public async Task DeleteDraftAsync(DeleteLeaseDraftInput input)
    {
        var lease = await GetApplicationForMutationAsync(
            input.LeaseId,
            input.ActorUserId,
            input.ExpectedRowVersion,
            input.AccessScope,
            LeaseApplicationLifecycle.CanDeleteApplication,
            requireOwner: false);
        EnsureNotGrouped(lease);
        Guard.Conflict(
            await leaseRepository.HasChargesAsync(lease.Id),
            "Tahakkuk bulunan başvuru silinemez.",
            "Lease.DraftHasCharges");
        var explanation = NormalizeRequiredExplanation(input.Explanation);
        await AddReviewAsync(
            lease,
            LeaseReviewActionType.Deleted,
            lease.Status,
            null,
            input.ActorUserId,
            explanation);
        await leaseRateOverrideRepository.SoftDeleteByLeaseIdAsync(lease.Id);
        await documentRepository.SoftDeleteByOwnerAsync(DocumentOwnerType.Lease, lease.Id);
        lease.IsDeleted = true;
        lease.IsActive = false;
        await uow.SaveChangesAsync();
    }

    public async Task ApproveAsync(ApproveLeaseInput input)
    {
        var lease = await GetApplicationForMutationAsync(
            input.LeaseId,
            input.ActorUserId,
            input.ExpectedRowVersion,
            input.AccessScope,
            status => LeaseApplicationLifecycle.CanTransition(status, LeaseStatus.Active),
            requireOwner: false);
        EnsureNotGrouped(lease);
        await rules.ActivateAsync(
            lease,
            input.ActorUserId,
            input.Explanation,
            input.AccessScope);
        await uow.SaveChangesAsync();
        await chargeGenerationService.GenerateForLeaseAsync(
            new GenerateLeaseChargesInput(lease.Id));
    }

    public async Task ExtendAsync(ExtendLeaseInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetWithActivityLogAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        EnsureLeaseInScope(lease, input.AccessScope);
        EnsureNotGrouped(lease);
        await rules.EnsureExtensionAllowedAsync(
            lease,
            input.NewEndDate,
            input.IsVatApplied,
            input.VatRate,
            input.UpdateRate,
            input.CanOverrideRate,
            input.RateOverrides);

        var oldAmount = await statisticsService.GetMonthlyAmountAsync(lease);
        if (input.UpdateRate && input.RateOverrides.Count > 0)
        {
            await leaseRateOverrideRepository.ReplaceAsync(
                lease.Id,
                BuildRateOverrides(lease.Id, input.RateOverrides));
            await uow.SaveChangesAsync();
        }

        var newAmount = await statisticsService.GetMonthlyAmountAsync(lease);

        var oldEndDate = lease.EndDate;
        lease.EndDate = input.NewEndDate;
        lease.Status = LeaseStatus.Active;
        lease.IsKdvApplied = input.IsVatApplied;

        decimal? vatAmount = input.IsVatApplied ? RentIncreasePolicy.CalculateVatAmount(newAmount, input.VatRate) : null;
        decimal? vatIncludedAmount = input.IsVatApplied ? RentIncreasePolicy.CalculateVatIncludedAmount(newAmount, input.VatRate) : null;

        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = input.LeaseId,
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
            KdvAmount = vatAmount,
            KdvIncludedAmount = vatIncludedAmount
        });

        await uow.SaveChangesAsync();
        await chargeGenerationService.GenerateForLeaseAsync(new GenerateLeaseChargesInput(input.LeaseId));
    }

    public async Task TerminateAsync(TerminateLeaseInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetWithActivityLogAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        EnsureLeaseInScope(lease, input.AccessScope);
        EnsureNotGrouped(lease);
        Guard.Conflict(
            !LeaseLifecycle.CanTerminate(lease.Status),
            "Yalnız aktif sözleşme feshedilebilir.",
            "Lease.NotActive");

        lease.Status = LeaseStatus.Terminated;
        lease.TerminationDate = input.TerminationDate;
        lease.TerminationReason = input.TerminationReason;
        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = input.LeaseId,
            ActivityType = LeaseActivityType.Termination,
            TransactionDate = DateTime.Now,
            Description = input.Description ?? input.TerminationReason
        });

        await uow.SaveChangesAsync();
        await chargeGenerationService.CancelFutureChargesAsync(
            new CancelFutureLeaseChargesInput(input.LeaseId, input.TerminationDate));
    }

    public async Task UpdateDueDateAsync(UpdateLeaseDueDateInput input)
    {
        Guard.Against(
            !LeaseSchedulePolicy.IsValidDueDay(input.DueDay),
            "Vade günü 1-31 arasında olmalıdır.",
            "Lease.InvalidDueDay");

        var lease = Guard.NotFound(
            await leaseRepository.GetWithActivityLogAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        EnsureLeaseInScope(lease, input.AccessScope);
        Guard.Conflict(
            !LeaseLifecycle.CanUpdateDueDate(lease.Status),
            "Yalnız aktif sözleşmenin vadesi güncellenebilir.",
            "Lease.NotActive");

        var oldRuleType = lease.DueDateRuleType;
        var oldDueDay = lease.DueDay;
        if (oldRuleType == input.RuleType && oldDueDay == input.DueDay) return;

        lease.DueDateRuleType = input.RuleType;
        lease.DueDay = input.DueDay;
        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = input.LeaseId,
            ActivityType = LeaseActivityType.ChargeRegeneration,
            TransactionDate = DateTime.Now,
            Description = input.Description
                ?? $"Vade kuralı güncellendi: {oldRuleType}({oldDueDay}) → {input.RuleType}({input.DueDay})"
        });

        await uow.SaveChangesAsync();
        await chargeGenerationService.RecalculatePendingDueDatesAsync(
            new RecalculateLeaseDueDatesInput(input.LeaseId));
    }

    public async Task RegenerateAsync(RegenerateLeaseInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetWithActivityLogAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        EnsureLeaseInScope(lease, input.AccessScope);
        Guard.Conflict(
            !LeaseLifecycle.CanRegenerateCharges(lease.Status),
            "Yalnız aktif sözleşmenin tahakkukları yeniden üretilebilir.",
            "Lease.NotActive");
        EnsureOverridePermission(input.UpdateRate, input.CanOverrideRate);
        await rules.EnsureRateOverridesAsync(
            input.RateOverrides,
            lease.Areas.OrderBy(area => area.Id).Select(area => area.RentedArea).FirstOrDefault(),
            lease.IsRentFree);

        if (input.UpdateRate && input.RateOverrides.Count > 0)
        {
            await leaseRateOverrideRepository.ReplaceAsync(
                lease.Id,
                BuildRateOverrides(lease.Id, input.RateOverrides));
            await uow.SaveChangesAsync();
        }

        await chargeGenerationService.RegenerateAsync(
            new RegenerateLeaseChargesInput(input.LeaseId, input.StartDate));

        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = input.LeaseId,
            ActivityType = LeaseActivityType.ChargeRegeneration,
            TransactionDate = DateTime.Now,
            Description = $"{input.StartDate:MMMM yyyy} tarihinden itibaren ödenmemiş tahakkuklar yeniden üretildi."
                + (input.UpdateRate ? " (Tarife güncellendi.)" : "")
        });
        await uow.SaveChangesAsync();
    }

    public Task<IList<ChargeLineItemPreview>> GetDefaultLineItemsAsync(ComposeLeaseLineItemsInput input)
        => chargeGenerationService.ComposeLineItemsAsync(input);

    public async Task<List<LeaseListItemDto>> GetByTenantAsync(GetLeasesByTenantInput input)
    {
        var list = await leaseRepository.GetByTenantIdAsync(input.TenantId, input.AccessScope);
        await PopulateMonthlyAmountsAsync(list);

        return list;
    }

    public async Task<PagedResult<LeaseListItemDto>> GetPagedAsync(GetPagedLeasesInput input)
    {
        var result = await leaseRepository.GetPagedListAsync(input.Query, input.Filter, input.Scope);
        await PopulateMonthlyAmountsAsync(result.Items);
        return result;
    }

    public Task<int> GetTerminatedCountAsync(GetTerminatedLeaseCountInput input)
        => leaseRepository.GetTerminatedCountAsync(input.Scope);

    public async Task<List<LeaseListItemDto>> GetTenantPortalLeasesAsync(
        GetTenantPortalLeasesInput input)
    {
        var list = await leaseRepository.GetTenantPortalListAsync(input.TenantId, input.AccessScope);
        await PopulateMonthlyAmountsAsync(list);

        return list;
    }

    public async Task<PagedResult<LeaseListItemDto>> GetTenantPortalLeasesPageAsync(
        GetTenantPortalLeasesPageInput input)
    {
        var result = await leaseRepository.GetTenantPortalPagedListAsync(
            input.TenantId,
            input.Query,
            input.AccessScope);
        await PopulateMonthlyAmountsAsync(result.Items);
        return result;
    }

    public async Task<List<LeaseListItemDto>> GetBySpaceAsync(GetLeasesBySpaceInput input)
    {
        var list = await leaseRepository.GetBySpaceIdAsync(input.SpaceId);
        await PopulateMonthlyAmountsAsync(list);

        return list;
    }

    public Task<Dictionary<int, decimal?>> GetDepositsAsync(GetLeaseDepositsInput input)
        => chargeLineItemRepository.GetDepositAmountsByLeaseIdsAsync(
            input.LeaseIds,
            input.TenantId);

    private async Task ApplyApplicationChangesAsync(
        Lease lease,
        int spaceId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        DueDateRuleType dueDateRuleType,
        int dueDay,
        string? description,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        SpaceAccessScope accessScope,
        bool isRentFree,
        decimal? rentedArea)
    {
        var validated = await rules.ValidateApplicationDataAsync(
            spaceId,
            tenantId,
            startDate,
            endDate,
            dueDay,
            rateOverrides,
            accessScope,
            isRentFree,
            rentedArea,
            excludeLeaseId: lease.Id);
        Guard.InvalidField(
            await leaseRepository.HasOpenApplicationForSpaceAsync(spaceId, lease.Id),
            nameof(spaceId),
            "Seçilen alan için başka bir açık başvuru bulunmaktadır.",
            "Lease.OpenApplicationConflict");

        lease.TenantId = tenantId;
        lease.StartDate = startDate;
        lease.EndDate = endDate;
        lease.DueDateRuleType = dueDateRuleType;
        lease.DueDay = dueDay;
        lease.Description = description;
        lease.IsRentFree = isRentFree;
        await leaseRepository.ReplaceLeaseAreasAsync(
            lease.Id,
            [(validated.Space, validated.RentedArea)]);
        await leaseRateOverrideRepository.ReplaceAsync(
            lease.Id,
            BuildRateOverrides(lease.Id, rateOverrides));
    }

    private static void EnsureNotGrouped(Lease lease)
        => Guard.Conflict(
            lease.LeaseGroupId != null,
            "Paylaşımlı grup sözleşmesi için grup işlemleri kullanılmalıdır.",
            "Lease.GroupOperationRequired");

    private async Task<Lease> GetApplicationForMutationAsync(
        int leaseId,
        string actorUserId,
        byte[] expectedRowVersion,
        SpaceAccessScope accessScope,
        Func<LeaseStatus, bool> canProceed,
        bool requireOwner)
    {
        await EnsureActorAsync(actorUserId);
        var lease = Guard.NotFound(
            await leaseRepository.GetForDecisionAsync(leaseId, accessScope),
            "Sözleşme başvurusu bulunamadı veya yetki kapsamınızın dışında.",
            "Lease.DraftNotFound");
        Guard.Conflict(
            !canProceed(lease.Status),
            "İşlem başvurunun mevcut durumunda yapılamaz.",
            "Lease.InvalidDraftStatus");
        Guard.Conflict(
            expectedRowVersion.Length == 0
                || !lease.RowVersion.SequenceEqual(expectedRowVersion),
            "Başvuru başka bir kullanıcı tarafından güncellenmiş. Sayfayı yenileyin.",
            "Lease.ConcurrencyConflict");
        Guard.Forbidden(
            requireOwner && lease.CreatedBy != actorUserId,
            "Bu başvuruyu yalnız başvuru sahibi düzenleyebilir.",
            "Lease.NotApplicationOwner");
        return lease;
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

    private async Task<string> GenerateLeaseNoAsync()
    {
        var existingLeaseNos = await leaseRepository.GetExistingLeaseNosAsync();
        var usedLeaseNos = existingLeaseNos.ToHashSet();

        if (LeaseNumberPolicy.TryGenerateNextLeaseNo(usedLeaseNos, out var leaseNo))
            return leaseNo;

        throw new BusinessException(
            "Sözleşme No üretilemedi.",
            ErrorType.Failure,
            "Lease.NumberGenerationFailed");
    }

    private Task AddReviewAsync(
        Lease lease,
        LeaseReviewActionType actionType,
        LeaseStatus? fromStatus,
        LeaseStatus? toStatus,
        string actorUserId,
        string? explanation = null)
        => rules.AddReviewAsync(lease, actionType, fromStatus, toStatus, actorUserId, explanation);

    private static void EnsureOverridePermission(bool updateRate, bool canOverrideRate)
        => Guard.Forbidden(
            updateRate && !canOverrideRate,
            "Sözleşme tarifesini değiştirme yetkiniz bulunmuyor.",
            "Lease.OverrideRateForbidden");

    private async Task PopulateMonthlyAmountsAsync(IEnumerable<LeaseListItemDto> items)
    {
        var itemList = items.ToList();
        var leases = itemList.Select(item => new Lease
        {
            Id = item.Id,
            TenantId = item.TenantId,
            IsRentFree = item.IsRentFree,
            Areas =
            [
                new LeaseArea
                {
                    LeaseId = item.Id,
                    SpaceId = item.SpaceId,
                    RentedArea = item.RentedArea,
                    Space = new Space { Id = item.SpaceId, Area = item.SpaceArea }
                }
            ]
        }).ToList();

        var amounts = await statisticsService.GetMonthlyAmountsAsync(leases);
        foreach (var item in itemList)
        {
            if (amounts.TryGetValue(item.Id, out var amount)) item.MonthlyAmount = amount;
        }
    }
}
