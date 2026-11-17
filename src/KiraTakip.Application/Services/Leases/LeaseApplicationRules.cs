using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Domain.Leases;
using KiraTakip.Domain.Pricing;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Documents;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Interfaces.Payments;

namespace KiraTakip.Services.Leases;

public class LeaseApplicationRules(
    ILeaseRepository leaseRepository,
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository,
    IChargeGenerationService chargeGenerationService,
    IDocumentRepository documentRepository,
    IDocumentTypeRepository documentTypeRepository,
    ILeaseReviewHistoryRepository reviewHistoryRepository,
    IOperationalPolicyProvider operationalPolicyProvider) : ILeaseApplicationRules
{
    public async Task<ValidatedLeaseApplication> ValidateApplicationDataAsync(
        int unitId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        int dueDay,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        LeaseAccessScopeInput accessScope,
        bool isRentFree,
        decimal? rentedArea,
        decimal? allocatedArea,
        int? excludeLeaseId = null,
        int? excludeLeaseGroupId = null,
        decimal? rateArea = null)
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
        (decimal Rented, decimal Allocated)? area = null;
        if (unit.RentalMode == UnitRentalMode.CapacityBased)
            area = await ValidateCapacityLeaseAsync(
                unit, startDate, endDate, rentedArea, allocatedArea, excludeLeaseId);
        else
            Guard.InvalidField(
                await leaseRepository.HasOverlappingActiveLeaseAsync(
                    unitId, startDate, endDate, excludeLeaseId, excludeLeaseGroupId),
                nameof(unitId),
                "Seçilen birimde bu tarih aralığıyla çakışan açık sözleşme bulunmaktadır.",
                "Lease.ActiveUnitConflict");
        await EnsureRateOverridesAsync(rateOverrides, rateArea ?? area?.Rented ?? unit.Area, isRentFree);

        return new ValidatedLeaseApplication(unit, area);
    }

    public async Task EnsureExtensionAllowedAsync(
        Lease lease,
        DateTime newEndDate,
        bool isVatApplied,
        decimal vatRate,
        bool updateRate,
        bool canOverrideRate,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        int? excludeLeaseGroupId = null)
    {
        Guard.Conflict(
            !LeaseLifecycle.CanExtend(lease.Status),
            "Yalnız aktif veya süresi dolmuş sözleşme uzatılabilir.",
            "Lease.NotActive");
        Guard.Conflict(
            !LeaseSchedulePolicy.CanExtend(lease.EndDate, newEndDate),
            "Yeni bitiş tarihi mevcut bitiş tarihinden büyük olmalıdır.",
            "Lease.InvalidExtensionDate");
        Guard.InvalidField(
            lease.Status == LeaseStatus.Ended && newEndDate.Date < DateTime.Now.Date,
            "NewEndDate",
            "Süresi dolmuş sözleşme için yeni bitiş tarihi bugünden önce olamaz.",
            "Lease.ExtendEndedRequiresFutureEnd");
        if (lease.RentalModeSnapshot == UnitRentalMode.CapacityBased)
        {
            Guard.InvalidField(
                lease.Unit.Capacity is null or <= 0,
                "NewEndDate",
                "Birim için kontenjan tanımlı olmadığından sözleşme uzatılamaz.",
                "Lease.UnitCapacityMissing");
            var leaseArea = await leaseRepository.GetLeaseAreaAsync(lease.Id);
            var extensionStart = lease.EndDate.Date.AddDays(1);
            var occupants = (await leaseRepository.GetOpenLeaseAreasForUnitAsync(
                lease.UnitId, extensionStart, newEndDate, lease.Id)).ToList();
            occupants.Add(new CapacityOccupant(
                lease.StartDate, newEndDate, leaseArea?.AllocatedArea ?? 0m));
            EnsureCapacityAvailable(
                lease.Unit.Capacity!.Value,
                lease.Unit.Area,
                occupants,
                extensionStart,
                newEndDate,
                "NewEndDate");
        }
        else
            Guard.InvalidField(
                await leaseRepository.HasOverlappingActiveLeaseAsync(
                    lease.UnitId, lease.StartDate, newEndDate, lease.Id, excludeLeaseGroupId),
                "NewEndDate",
                "Seçilen birimde bu tarih aralığıyla çakışan açık sözleşme bulunmaktadır.",
                "Lease.ActiveUnitConflict");
        Guard.Forbidden(
            updateRate && !canOverrideRate,
            "Sözleşme tarifesini değiştirme yetkiniz bulunmuyor.",
            "Lease.OverrideRateForbidden");
        await EnsureRateOverridesAsync(
            rateOverrides,
            await leaseRepository.GetRentedAreaAsync(lease.Id) ?? lease.Unit.Area,
            lease.IsRentFree);
        Guard.InvalidField(
            isVatApplied && !RentIncreasePolicy.IsValidVatRate(vatRate),
            "VatRate",
            "KDV oranı 0-100 arasında olmalıdır.",
            "Lease.InvalidVatRate");
    }

    private async Task<(decimal Rented, decimal Allocated)> ValidateCapacityLeaseAsync(
        LeaseUnitContextDto unit,
        DateTime startDate,
        DateTime endDate,
        decimal? rentedArea,
        decimal? allocatedArea,
        int? excludeLeaseId)
    {
        Guard.InvalidField(
            unit.Capacity is null or <= 0,
            "UnitId",
            "Birim için kontenjan tanımlı olmadığından sözleşme açılamaz.",
            "Lease.UnitCapacityMissing");
        var capacity = unit.Capacity!.Value;
        var minimumUsageArea = operationalPolicyProvider.Current.LeaseMinimumUsageArea;

        Guard.InvalidField(
            rentedArea is null or <= 0 || rentedArea < minimumUsageArea,
            "RentedArea",
            $"Kiralanan m² en az {minimumUsageArea:0.##} m² olmalıdır.",
            "Lease.RentedAreaBelowMinimum");
        var rented = rentedArea!.Value;

        decimal allocated;
        if (rented <= UnitCapacityPolicy.CalculateNormArea(unit.Area, capacity))
            allocated = rented;
        else
        {
            var (minimum, maximum) = UnitCapacityPolicy.GetAllocatedAreaRange(
                rented, unit.Area, capacity, minimumUsageArea);
            Guard.InvalidField(
                allocatedArea is null
                    || !UnitCapacityPolicy.IsAllocatedAreaAllowed(
                        allocatedArea.Value, rented, unit.Area, capacity, minimumUsageArea),
                "AllocatedArea",
                $"Fiziksel m² {minimum:0.##} ile {maximum:0.##} m² arasında olmalıdır.",
                "Lease.AllocatedAreaOutOfRange");
            allocated = allocatedArea!.Value;
        }

        var occupants = (await leaseRepository.GetOpenLeaseAreasForUnitAsync(
            unit.UnitId, startDate, endDate, excludeLeaseId)).ToList();
        occupants.Add(new CapacityOccupant(startDate, endDate, allocated));
        EnsureCapacityAvailable(capacity, unit.Area, occupants, startDate, endDate, "UnitId");

        return (rented, allocated);
    }

    public void EnsureCapacityAvailable(
        int capacity,
        decimal unitArea,
        IReadOnlyCollection<CapacityOccupant> occupants,
        DateTime rangeStart,
        DateTime rangeEnd,
        string field)
    {
        var violations = UnitCapacityPolicy.GetViolations(
            capacity,
            unitArea,
            operationalPolicyProvider.Current.LeaseMinimumUsageArea,
            occupants,
            rangeStart,
            rangeEnd);
        Guard.InvalidField(
            violations.Contains(CapacityViolation.CapacityExceeded),
            field,
            "Seçilen tarih aralığında birimin kontenjanı dolu.",
            "Lease.CapacityExceeded");
        Guard.InvalidField(
            violations.Contains(CapacityViolation.AreaExceeded),
            field,
            "Seçilen tarih aralığında fiziksel m² toplamı birim alanını aşıyor.",
            "Lease.CapacityAreaExceeded");
        Guard.InvalidField(
            violations.Contains(CapacityViolation.NormBelowMinimum),
            field,
            "Boş kontenjan varken kişi başı kalan alan minimum kullanım m² değerinin altına düşüyor.",
            "Lease.CapacityFreeAreaBelowMinimum");
    }

    public async Task EnsureRateOverridesAsync(
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

    public async Task ActivateAsync(
        Lease lease,
        string actorUserId,
        string? explanation,
        LeaseAccessScopeInput accessScope)
    {
        var isCapacity = lease.RentalModeSnapshot == UnitRentalMode.CapacityBased;
        var isGrouped = lease.LeaseGroupId != null;
        var storedArea = isCapacity || isGrouped
            ? await leaseRepository.GetLeaseAreaAsync(lease.Id)
            : null;
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
            accessScope,
            lease.IsRentFree,
            isCapacity ? storedArea?.RentedArea : null,
            isCapacity ? storedArea?.AllocatedArea : null,
            lease.Id,
            lease.LeaseGroupId,
            isGrouped ? storedArea?.RentedArea : null);
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
            actorUserId,
            NormalizeOptionalExplanation(explanation));
        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = lease.Id,
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = DateTime.UtcNow,
            Description = "Sözleşme başvurusu onaylandı.",
            NewRentAmount = monthlyAmount
        });
    }

    public async Task AddReviewAsync(
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

    public static List<LeaseRateOverride> BuildRateOverrides(
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

    public static string NormalizeRequiredExplanation(string? explanation)
    {
        var normalized = explanation?.Trim();
        Guard.Against(
            string.IsNullOrEmpty(normalized) || normalized.Length > 1000,
            "Açıklama zorunludur ve en fazla 1000 karakter olabilir.",
            "Lease.InvalidExplanation");
        return normalized!;
    }

    public static string? NormalizeOptionalExplanation(string? explanation)
    {
        var normalized = explanation?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        Guard.Against(
            normalized.Length > 1000,
            "Açıklama en fazla 1000 karakter olabilir.",
            "Lease.InvalidExplanation");
        return normalized;
    }

    public static void EnsureScope(
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

    public static bool HasScopeRestriction(LeaseAccessScopeInput accessScope)
        => accessScope.PropertyIds != null || accessScope.UnitIds != null;
}
