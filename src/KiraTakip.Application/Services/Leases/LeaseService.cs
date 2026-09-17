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
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Interfaces.Reporting;

namespace KiraTakip.Services.Leases;

public class LeaseService(
    ILeaseRepository leaseRepository,
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IChargeLineItemRepository chargeLineItemRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository,
    IChargeGenerationService chargeGenerationService,
    IUnitOfWork uow,
    IStatisticsService statisticsService,
    ILeaseReviewHistoryRepository reviewHistoryRepository,
    IDocumentRepository documentRepository,
    IDocumentTypeRepository documentTypeRepository,
    IApplicationUserRepository applicationUserRepository,
    ICurrentUserContext currentUserContext) : ILeaseService, ITransactionalService
{
    public async Task<List<LeaseListItemDto>> GetAllAsync(GetLeasesInput input)
    {
        var list = await leaseRepository.GetListAsync(
            input.Filter,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());
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
                input.AccessScope.PropertyIds?.ToList(),
                input.AccessScope.UnitIds?.ToList()),
            "Sözleşme bulunamadı.",
            "TENANT_LEASE_NOT_FOUND");

    public async Task<Lease> CreateAsync(CreateLeaseInput input)
    {
        var actorUserId = Guard.NotFound(
            currentUserContext.UserId,
            "Oturum kullanıcısı bulunamadı.",
            "Lease.ActorNotFound");
        return await CreateDraftAsync(new CreateLeaseDraftInput(
            input.UnitId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDateRuleType,
            input.DueDay,
            input.Description,
            input.RateOverrides,
            actorUserId,
            input.AccessScope,
            input.IsRentFree));
    }

    public async Task<Lease> CreateDraftAsync(CreateLeaseDraftInput input)
    {
        await EnsureActorAsync(input.ActorUserId);
        var unit = await ValidateApplicationDataAsync(
            input.UnitId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDay,
            input.RateOverrides,
            input.AccessScope,
            input.IsRentFree);
        Guard.InvalidField(
            await leaseRepository.HasOpenApplicationForUnitAsync(input.UnitId),
            nameof(input.UnitId),
            "Seçilen birim için açık bir sözleşme başvurusu bulunmaktadır.",
            "Lease.OpenApplicationConflict");

        var lease = new Lease
        {
            UnitId = input.UnitId,
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

        _ = unit;
        return lease;
    }

    public Task<LeaseDraftEditDto?> GetDraftForEditAsync(GetLeaseDraftInput input)
        => leaseRepository.GetDraftForEditAsync(
            input.LeaseId,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());

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
        await ApplyApplicationChangesAsync(
            lease,
            input.UnitId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDateRuleType,
            input.DueDay,
            input.Description,
            input.RateOverrides,
            input.AccessScope,
            input.IsRentFree);
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
        await ApplyApplicationChangesAsync(
            lease,
            input.UnitId,
            input.TenantId,
            input.StartDate,
            input.EndDate,
            input.DueDateRuleType,
            input.DueDay,
            input.Description,
            input.RateOverrides,
            input.AccessScope,
            input.IsRentFree);
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
        EnsureNotSelfReview(lease, input.ActorUserId);
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
        EnsureNotSelfReview(lease, input.ActorUserId);
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
        EnsureNotSelfReview(lease, input.ActorUserId);
        await ValidateApplicationDataAsync(
            lease.UnitId,
            lease.TenantId,
            lease.StartDate,
            lease.EndDate,
            lease.DueDay,
            (await leaseRateOverrideRepository.GetWithChargeTypeAsync(lease.Id))
                .Select(rate => new LeaseRateOverrideInput(
                    rate.ChargeTypeId,
                    rate.UnitValue,
                    rate.CalculationMethod,
                    rate.KdvRate))
                .ToList(),
            input.AccessScope,
            lease.IsRentFree);
        Guard.Conflict(
            await leaseRepository.HasChargesAsync(lease.Id),
            "Taslak başvuruda beklenmeyen tahakkuk bulundu.",
            "Lease.DraftHasCharges");
        Guard.Conflict(
            await leaseRepository.HasCreationActivityAsync(lease.Id),
            "Taslak başvuruda beklenmeyen oluşturma geçmişi bulundu.",
            "Lease.DraftHasCreationActivity");
        await EnsureRequiredDocumentsAsync(lease.Id);

        var previews = await chargeGenerationService.ComposeLineItemsAsync(
            new ComposeLeaseLineItemsInput(
                lease.UnitId,
                lease.TenantId,
                lease.StartDate,
                lease.Id));
        if (!lease.IsRentFree)
        {
            var rentPreview = previews.SingleOrDefault(preview =>
                string.Equals(preview.ChargeTypeCode, BorcTipiConsts.Kira, StringComparison.OrdinalIgnoreCase));
            Guard.Conflict(
                rentPreview == null,
                "Kira Bedeli borç tipi aktif olmadan ücretli sözleşme onaylanamaz.",
                "Lease.ActiveRentChargeTypeRequired");
            Guard.Conflict(
                !rentPreview!.IsRateFound || rentPreview.Amount <= 0,
                "Ücretli sözleşme için pozitif bir kira bedeli tarifesi tanımlanmalıdır.",
                "Lease.PositiveRentRateRequired");
        }
        lease.IsKdvApplied = previews.Any(preview =>
            preview.Behavior == ChargeTypeBehavior.MonthlyFixed && preview.KdvRate > 0);
        var monthlyAmount = previews
            .Where(preview => string.Equals(
                preview.ChargeTypeCode,
                BorcTipiConsts.Kira,
                StringComparison.OrdinalIgnoreCase))
            .Sum(preview => preview.Amount);
        lease.Status = LeaseStatus.Active;
        await AddReviewAsync(
            lease,
            LeaseReviewActionType.Approved,
            LeaseStatus.Draft,
            LeaseStatus.Active,
            input.ActorUserId,
            NormalizeOptionalExplanation(input.Explanation));
        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = lease.Id,
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = DateTime.UtcNow,
            Description = "Sözleşme başvurusu onaylandı.",
            NewRentAmount = monthlyAmount
        });
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

        EnsureScope(lease.Unit.PropertyId, lease.UnitId, input.AccessScope);
        Guard.Conflict(
            !LeaseLifecycle.CanExtend(lease.Status),
            "Yalnız aktif sözleşme uzatılabilir.",
            "Lease.NotActive");
        Guard.Conflict(
            !LeaseSchedulePolicy.CanExtend(lease.EndDate, input.NewEndDate),
            "Yeni bitiş tarihi mevcut bitiş tarihinden büyük olmalıdır.",
            "Lease.InvalidExtensionDate");
        EnsureOverridePermission(input.UpdateRate, input.CanOverrideRate);
        await EnsureRateOverridesAsync(input.RateOverrides, lease.Unit.Area, lease.IsRentFree);
        Guard.InvalidField(
            input.IsVatApplied && !RentIncreasePolicy.IsValidVatRate(input.VatRate),
            nameof(input.VatRate),
            "KDV oranı 0-100 arasında olmalıdır.",
            "Lease.InvalidVatRate");

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

        EnsureScope(lease.Unit.PropertyId, lease.UnitId, input.AccessScope);
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

        EnsureScope(lease.Unit.PropertyId, lease.UnitId, input.AccessScope);
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

        EnsureScope(lease.Unit.PropertyId, lease.UnitId, input.AccessScope);
        Guard.Conflict(
            !LeaseLifecycle.CanRegenerateCharges(lease.Status),
            "Yalnız aktif sözleşmenin tahakkukları yeniden üretilebilir.",
            "Lease.NotActive");
        EnsureOverridePermission(input.UpdateRate, input.CanOverrideRate);
        await EnsureRateOverridesAsync(input.RateOverrides, lease.Unit.Area, lease.IsRentFree);

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
        var list = await leaseRepository.GetByTenantIdAsync(
            input.TenantId,
            input.AccessScope?.PropertyIds?.ToList(),
            input.AccessScope?.UnitIds?.ToList());
        await PopulateMonthlyAmountsAsync(list);

        return list;
    }

    public async Task<PagedResult<LeaseListItemDto>> GetPagedAsync(GetPagedLeasesInput input)
    {
        var result = await leaseRepository.GetPagedListAsync(
            input.Query,
            input.Filter,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());
        await PopulateMonthlyAmountsAsync(result.Items);
        return result;
    }

    public async Task<List<LeaseListItemDto>> GetTenantPortalLeasesAsync(
        GetTenantPortalLeasesInput input)
    {
        var list = await leaseRepository.GetTenantPortalListAsync(
            input.TenantId,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());
        await PopulateMonthlyAmountsAsync(list);

        return list;
    }

    public async Task<PagedResult<LeaseListItemDto>> GetTenantPortalLeasesPageAsync(
        GetTenantPortalLeasesPageInput input)
    {
        var result = await leaseRepository.GetTenantPortalPagedListAsync(
            input.TenantId,
            input.Query,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());
        await PopulateMonthlyAmountsAsync(result.Items);
        return result;
    }

    public async Task<List<LeaseListItemDto>> GetByUnitAsync(GetLeasesByUnitInput input)
    {
        var list = await leaseRepository.GetByUnitIdAsync(input.UnitId);
        await PopulateMonthlyAmountsAsync(list);

        return list;
    }

    public Task<Dictionary<int, decimal?>> GetDepositsAsync(GetLeaseDepositsInput input)
        => chargeLineItemRepository.GetDepositAmountsByLeaseIdsAsync(
            input.LeaseIds,
            input.TenantId);

    private async Task<LeaseUnitContextDto> ValidateApplicationDataAsync(
        int unitId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        int dueDay,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        LeaseAccessScopeInput accessScope,
        bool isRentFree)
    {
        Guard.InvalidField(
            !LeaseSchedulePolicy.HasValidDateRange(startDate, endDate),
            nameof(endDate),
            "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.",
            "Lease.InvalidDateRange");
        Guard.InvalidField(
            !LeaseSchedulePolicy.IsValidDueDay(dueDay),
            nameof(dueDay),
            "Vade günü 1-31 arasında olmalıdır.",
            "Lease.InvalidDueDay");

        var unit = Guard.NotFound(
            await unitRepository.GetLeaseContextAsync(unitId),
            "Seçilen birim bulunamadı.",
            "Lease.UnitNotFound");
        EnsureScope(unit.PropertyId, unit.UnitId, accessScope);
        Guard.InvalidField(
            !unit.IsRentable,
            nameof(unitId),
            "Seçilen birim kiralanabilir değildir.",
            "Lease.UnitNotRentable");
        Guard.InvalidField(
            await tenantRepository.GetByIdAsync(tenantId) == null,
            nameof(tenantId),
            "Seçilen kiracı bulunamadı.",
            "Lease.TenantNotFound");
        if (HasScopeRestriction(accessScope))
        {
            Guard.Forbidden(
                !await tenantRepository.IsInScopeAsync(
                    tenantId,
                    accessScope.PropertyIds?.ToList(),
                    accessScope.UnitIds?.ToList()),
                "Seçilen kiracı yetki kapsamınızın dışındadır.",
                "Lease.TenantOutOfScope");
        }
        Guard.InvalidField(
            await leaseRepository.HasActiveLeaseForUnitAsync(unitId, DateTime.Now),
            nameof(unitId),
            "Seçilen birimde devam eden aktif sözleşme bulunmaktadır.",
            "Lease.ActiveUnitConflict");
        await EnsureRateOverridesAsync(rateOverrides, unit.Area, isRentFree);

        return unit;
    }

    private async Task ApplyApplicationChangesAsync(
        Lease lease,
        int unitId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        DueDateRuleType dueDateRuleType,
        int dueDay,
        string? description,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        LeaseAccessScopeInput accessScope,
        bool isRentFree)
    {
        await ValidateApplicationDataAsync(
            unitId,
            tenantId,
            startDate,
            endDate,
            dueDay,
            rateOverrides,
            accessScope,
            isRentFree);
        Guard.InvalidField(
            await leaseRepository.HasOpenApplicationForUnitAsync(unitId, lease.Id),
            nameof(unitId),
            "Seçilen birim için başka bir açık başvuru bulunmaktadır.",
            "Lease.OpenApplicationConflict");

        lease.UnitId = unitId;
        lease.TenantId = tenantId;
        lease.StartDate = startDate;
        lease.EndDate = endDate;
        lease.DueDateRuleType = dueDateRuleType;
        lease.DueDay = dueDay;
        lease.Description = description;
        lease.IsRentFree = isRentFree;
        await leaseRateOverrideRepository.ReplaceAsync(
            lease.Id,
            BuildRateOverrides(lease.Id, rateOverrides));
    }

    private async Task<Lease> GetApplicationForMutationAsync(
        int leaseId,
        string actorUserId,
        byte[] expectedRowVersion,
        LeaseAccessScopeInput accessScope,
        Func<LeaseStatus, bool> canProceed,
        bool requireOwner)
    {
        await EnsureActorAsync(actorUserId);
        var lease = Guard.NotFound(
            await leaseRepository.GetForDecisionAsync(
                leaseId,
                accessScope.PropertyIds?.ToList(),
                accessScope.UnitIds?.ToList()),
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

    private void EnsureNotSelfReview(Lease lease, string actorUserId)
        => Guard.Forbidden(
            lease.CreatedBy == actorUserId && !currentUserContext.IsSuperAdmin,
            "Başvuru sahibi kendi başvurusunu değerlendiremez.",
            "Lease.SelfReviewForbidden");

    private async Task EnsureRequiredDocumentsAsync(int leaseId)
    {
        var requiredTypes = await documentTypeRepository.GetForTargetAsync(
            DocumentOwnerType.Lease,
            requiredOnly: true);
        if (requiredTypes.Count == 0) return;

        var documents = await documentRepository.GetListAsync(DocumentOwnerType.Lease, leaseId);
        var existingTypeIds = documents.Select(document => document.DocumentTypeId).ToHashSet();
        Guard.Conflict(
            requiredTypes.Any(type => !existingTypeIds.Contains(type.Id)),
            "Zorunlu sözleşme belgeleri tamamlanmadan başvuru onaylanamaz.",
            "Lease.RequiredDocumentsMissing");
    }

    private async Task AddReviewAsync(
        Lease lease,
        LeaseReviewActionType actionType,
        LeaseStatus? fromStatus,
        LeaseStatus? toStatus,
        string actorUserId,
        string? explanation = null)
        => await reviewHistoryRepository.AddAsync(new LeaseReviewHistory
        {
            LeaseId = lease.Id,
            ActionType = actionType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Explanation = explanation,
            ActorUserId = actorUserId,
            ActionDate = DateTime.UtcNow
        });

    private static string NormalizeRequiredExplanation(string? explanation)
    {
        var normalized = explanation?.Trim();
        Guard.Against(
            string.IsNullOrEmpty(normalized) || normalized.Length > 1000,
            "Açıklama zorunludur ve en fazla 1000 karakter olabilir.",
            "Lease.InvalidExplanation");
        return normalized!;
    }

    private static string? NormalizeOptionalExplanation(string? explanation)
    {
        var normalized = explanation?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        Guard.Against(
            normalized.Length > 1000,
            "Açıklama en fazla 1000 karakter olabilir.",
            "Lease.InvalidExplanation");
        return normalized;
    }

    private async Task EnsureRateOverridesAsync(
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        decimal unitArea,
        bool isRentFree = false)
    {
        if (rateOverrides.Count == 0) return;

        var validChargeTypeIds = (await chargeTypeRepository.GetActiveGenerationTypesAsync())
            .ToList();
        var rentChargeTypeIds = validChargeTypeIds
            .Where(chargeType => string.Equals(
                chargeType.Code,
                BorcTipiConsts.Kira,
                StringComparison.OrdinalIgnoreCase))
            .Select(chargeType => chargeType.Id)
            .ToHashSet();
        Guard.InvalidField(
            isRentFree && rateOverrides.Any(rate => rentChargeTypeIds.Contains(rate.ChargeTypeId)),
            "LeaseLineItems",
            "Bedelsiz sözleşmeye kira bedeli tarifesi eklenemez.",
            "Lease.RentFreeRentRateForbidden");
        var validChargeTypeIdSet = validChargeTypeIds
            .Select(chargeType => chargeType.Id)
            .ToHashSet();
        Guard.InvalidField(
            rateOverrides.Any(rate => !validChargeTypeIdSet.Contains(rate.ChargeTypeId)),
            "LeaseLineItems",
            "Geçersiz veya pasif bir borç tipi için sözleşme tarifesi oluşturulamaz.",
            "Lease.InvalidChargeType");
        Guard.InvalidField(
            rateOverrides.Any(rate => !RateValuePolicy.IsValid(rate.UnitValue, rate.VatRate, rate.CalculationMethod)),
            "LeaseLineItems",
            "Sözleşme tarifesi değerlerinden biri geçersiz.",
            "Lease.InvalidRateValue");
        Guard.InvalidField(
            rateOverrides.Any(rate => !LeaseRatePolicy.CanUseCalculationMethod(unitArea, rate.CalculationMethod)),
            "LeaseLineItems",
            "Yüzölçümü tanımlı olmayan birim için m² hesaplama yöntemi kullanılamaz.",
            "Lease.UnitAreaRequired");
    }

    private static List<LeaseRateOverride> BuildRateOverrides(
        int leaseId,
        IEnumerable<LeaseRateOverrideInput> inputs)
        => inputs.Select(input => new LeaseRateOverride
        {
            LeaseId = leaseId,
            ChargeTypeId = input.ChargeTypeId,
            UnitValue = input.UnitValue,
            CalculationMethod = input.CalculationMethod,
            KdvRate = input.VatRate
        }).ToList();

    private static void EnsureScope(
        int propertyId,
        int unitId,
        LeaseAccessScopeInput accessScope)
    {
        if (!HasScopeRestriction(accessScope)) return;

        var propertyAccess = accessScope.PropertyIds?.Contains(propertyId) == true;
        var unitAccess = accessScope.UnitIds?.Contains(unitId) == true;
        Guard.Forbidden(
            !propertyAccess && !unitAccess,
            "Bu sözleşme yetki kapsamınızın dışındadır.",
            "Lease.OutOfScope");
    }

    private static bool HasScopeRestriction(LeaseAccessScopeInput accessScope)
        => accessScope.PropertyIds != null || accessScope.UnitIds != null;

    private static void EnsureOverridePermission(bool updateRate, bool canOverrideRate)
        => Guard.Forbidden(
            updateRate && !canOverrideRate,
            "Sözleşme tarifesini değiştirme yetkiniz bulunmuyor.",
            "Lease.OverrideRateForbidden");

    private async Task PopulateMonthlyAmountsAsync(IEnumerable<LeaseListItemDto> items)
    {
        foreach (var item in items)
        {
            var lease = new Lease
            {
                Id = item.Id,
                TenantId = item.TenantId,
                UnitId = item.UnitId,
                IsRentFree = item.IsRentFree,
                Unit = new Unit { Id = item.UnitId, Area = item.UnitArea }
            };
            item.MonthlyAmount = await statisticsService.GetMonthlyAmountAsync(lease);
        }
    }
}
