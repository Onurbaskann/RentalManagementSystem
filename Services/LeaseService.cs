using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class LeaseService(
    ILeaseRepository leaseRepository,
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IChargeLineItemRepository chargeLineItemRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository,
    IChargeGenerationService chargeGenerationService,
    IUnitOfWork uow,
    IStatisticsService statisticsService) : ILeaseService, ITransactionalService
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
        var unitContext = await unitRepository.GetLeaseContextAsync(input.UnitId);
        Guard.InvalidField(
            unitContext == null,
            nameof(input.UnitId),
            "Seçilen birim bulunamadı.",
            "Lease.UnitNotFound");
        var selectedUnit = unitContext!;

        EnsureScope(selectedUnit.PropertyId, selectedUnit.UnitId, input.AccessScope);
        Guard.InvalidField(
            !selectedUnit.IsRentable,
            nameof(input.UnitId),
            "Seçilen birim kiralanabilir değildir.",
            "Lease.UnitNotRentable");

        Guard.InvalidField(
            await tenantRepository.GetByIdAsync(input.TenantId) == null,
            nameof(input.TenantId),
            "Seçilen kiracı bulunamadı.",
            "Lease.TenantNotFound");
        if (HasScopeRestriction(input.AccessScope))
        {
            Guard.Forbidden(
                !await tenantRepository.IsInScopeAsync(
                    input.TenantId,
                    input.AccessScope.PropertyIds?.ToList(),
                    input.AccessScope.UnitIds?.ToList()),
                "Seçilen kiracı yetki kapsamınızın dışındadır.",
                "Lease.TenantOutOfScope");
        }

        var now = DateTime.Now;
        var hasActiveLease = await leaseRepository.HasActiveLeaseForUnitAsync(input.UnitId, now);
        Guard.InvalidField(
            hasActiveLease,
            nameof(input.UnitId),
            "Seçilen birime ait halihazırda devam eden aktif bir sözleşme bulunmaktadır.",
            "Lease.ActiveUnitConflict");

        await EnsureRateOverridesAsync(input.RateOverrides, selectedUnit.Area);

        var lease = new Lease
        {
            UnitId = input.UnitId,
            TenantId = input.TenantId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Description = input.Description,
            Status = LeaseStatus.Active,
            DueDateRuleType = input.DueDateRuleType,
            DueDay = input.DueDay
        };

        await leaseRepository.AddAsync(lease);
        await uow.SaveChangesAsync();

        if (input.RateOverrides.Count > 0)
        {
            await leaseRateOverrideRepository.ReplaceAsync(
                lease.Id,
                BuildRateOverrides(lease.Id, input.RateOverrides));
            await uow.SaveChangesAsync();
        }

        var previews = await chargeGenerationService.ComposeLineItemsAsync(
            new ComposeLeaseLineItemsInput(
                lease.UnitId,
                lease.TenantId,
                lease.StartDate,
                lease.Id));
        lease.IsKdvApplied = previews.Any(preview =>
            preview.Behavior == ChargeTypeBehavior.MonthlyFixed
            && preview.KdvRate > 0);
        var monthlyAmount = previews
            .Where(preview => preview.Behavior == ChargeTypeBehavior.MonthlyFixed)
            .Sum(preview => preview.Amount);

        lease.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = DateTime.Now,
            Description = "Sözleşme oluşturuldu.",
            NewRentAmount = monthlyAmount
        });
        await uow.SaveChangesAsync();
        await chargeGenerationService.GenerateForLeaseAsync(new GenerateLeaseChargesInput(lease.Id));

        return lease;
    }

    public async Task ExtendAsync(ExtendLeaseInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetWithActivityLogAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        EnsureScope(lease.Unit.PropertyId, lease.UnitId, input.AccessScope);
        Guard.Conflict(
            lease.Status == LeaseStatus.Terminated,
            "Feshedilmiş sözleşme uzatılamaz.",
            "Lease.Terminated");
        Guard.Conflict(
            input.NewEndDate <= lease.EndDate,
            "Yeni bitiş tarihi mevcut bitiş tarihinden büyük olmalıdır.",
            "Lease.InvalidExtensionDate");
        EnsureOverridePermission(input.UpdateRate, input.CanOverrideRate);
        await EnsureRateOverridesAsync(input.RateOverrides, lease.Unit.Area);

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

        decimal? vatAmount = input.IsVatApplied ? newAmount * input.VatRate / 100 : null;
        decimal? vatIncludedAmount = input.IsVatApplied ? newAmount + vatAmount : null;

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
            lease.Status == LeaseStatus.Terminated,
            "Sözleşme zaten feshedilmiş.",
            "Lease.AlreadyTerminated");

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
            input.DueDay < 1 || input.DueDay > 31,
            "Vade günü 1-31 arasında olmalıdır.",
            "Lease.InvalidDueDay");

        var lease = Guard.NotFound(
            await leaseRepository.GetWithActivityLogAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        EnsureScope(lease.Unit.PropertyId, lease.UnitId, input.AccessScope);
        Guard.Conflict(
            lease.Status == LeaseStatus.Terminated,
            "Feshedilmiş sözleşmenin vadesi güncellenemez.",
            "Lease.Terminated");

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
            lease.Status == LeaseStatus.Terminated,
            "Feshedilmiş sözleşmenin tahakkukları yeniden üretilemez.",
            "Lease.Terminated");
        EnsureOverridePermission(input.UpdateRate, input.CanOverrideRate);
        await EnsureRateOverridesAsync(input.RateOverrides, lease.Unit.Area);

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

    private async Task EnsureRateOverridesAsync(
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        decimal unitArea)
    {
        if (rateOverrides.Count == 0) return;

        var validChargeTypeIds = (await chargeTypeRepository.GetActiveGenerationTypesAsync())
            .Select(chargeType => chargeType.Id)
            .ToHashSet();
        Guard.InvalidField(
            rateOverrides.Any(rate => !validChargeTypeIds.Contains(rate.ChargeTypeId)),
            "LeaseLineItems",
            "Geçersiz veya pasif bir borç tipi için sözleşme tarifesi oluşturulamaz.",
            "Lease.InvalidChargeType");
        Guard.InvalidField(
            unitArea <= 0 && rateOverrides.Any(rate => rate.CalculationMethod == CalculationMethod.M2),
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
                Unit = new Unit { Id = item.UnitId, Area = item.UnitArea }
            };
            item.MonthlyAmount = await statisticsService.GetMonthlyAmountAsync(lease);
        }
    }
}
