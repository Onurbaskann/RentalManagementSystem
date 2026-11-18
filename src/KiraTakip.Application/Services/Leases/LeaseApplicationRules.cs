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
        LeaseAreaMode? allocationMode,
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
        (decimal Rented, decimal Allocated, LeaseAreaMode Mode)? area = null;
        if (unit.RentalMode == UnitRentalMode.CapacityBased)
            area = await ValidateCapacityLeaseAsync(
                unit, startDate, endDate, rentedArea, allocatedArea, allocationMode, excludeLeaseId);
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
        Guard.Conflict(
            lease.Unit.UnitType.RentalMode != lease.RentalModeSnapshot,
            "Birimin kiralama biçimi sözleşmeden sonra değişmiş; sözleşme uzatılamaz.",
            "Lease.UnitRentalModeChanged");
        Guard.Conflict(
            lease.LeaseGroupId != null && lease.Unit.Area != lease.UnitAreaSnapshot,
            "Birimin alanı sözleşmeden sonra değişmiş; grubu yeniden yapılandırın.",
            "Lease.UnitAreaChanged");
        Guard.InvalidField(
            lease.Status == LeaseStatus.Ended && newEndDate.Date < DateTime.Now.Date,
            "NewEndDate",
            "Süresi dolmuş sözleşme için yeni bitiş tarihi bugünden önce olamaz.",
            "Lease.ExtendEndedRequiresFutureEnd");
        if (lease.RentalModeSnapshot == UnitRentalMode.CapacityBased)
        {
            await unitRepository.AcquireCapacityLocksAsync(lease.UnitId);
            var unitContext = Guard.NotFound(
                await unitRepository.GetLeaseContextAsync(lease.UnitId),
                "Birim bulunamadı.");
            Guard.InvalidField(
                unitContext.Capacity is null or <= 0,
                "NewEndDate",
                "Birim için kontenjan tanımlı olmadığından sözleşme uzatılamaz.",
                "Lease.UnitCapacityMissing");
            var leaseArea = await leaseRepository.GetLeaseAreaAsync(lease.Id);
            var extensionStart = Max(lease.EndDate.Date.AddDays(1), DateTime.Today);
            var occupants = (await leaseRepository.GetOpenLeaseAreasForUnitAsync(
                lease.UnitId, extensionStart, newEndDate, lease.Id)).ToList();
            occupants.Add(new CapacityOccupant(
                lease.StartDate,
                newEndDate,
                leaseArea?.AllocatedArea ?? 0m,
                lease.Id,
                leaseArea?.RentedArea,
                leaseArea?.AllocationMode));
            EnsureCapacityAvailable(
                unitContext.Capacity!.Value,
                unitContext.Area,
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

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private async Task<(decimal Rented, decimal Allocated, LeaseAreaMode Mode)> ValidateCapacityLeaseAsync(
        LeaseUnitContextDto unit,
        DateTime startDate,
        DateTime endDate,
        decimal? rentedArea,
        decimal? allocatedArea,
        LeaseAreaMode? allocationMode,
        int? excludeLeaseId)
    {
        await unitRepository.AcquireCapacityLocksAsync(unit.UnitId);
        unit = Guard.NotFound(
            await unitRepository.GetLeaseContextAsync(unit.UnitId),
            "Birim bulunamadı.");

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

        var mode = allocationMode ?? LeaseAreaMode.Automatic;
        var allocated = mode == LeaseAreaMode.Automatic
            ? UnitCapacityPolicy.CalculateAutomaticArea(
                rented, UnitCapacityPolicy.CalculateEffectiveNorm(unit.Area, capacity))
            : allocatedArea ?? 0m;
        Guard.InvalidField(
            (mode == LeaseAreaMode.Manual && allocatedArea is null)
                || UnitCapacityPolicy.ValidateArea(rented, allocated, minimumUsageArea).Any(violation =>
                    violation is LeaseAreaViolation.AllocatedBelowMinimum
                        or LeaseAreaViolation.AllocatedAboveRented),
            "AllocatedArea",
            $"Fiziksel m² {minimumUsageArea:0.##} ile {rented:0.##} m² arasında olmalıdır.",
            "Lease.AllocatedAreaOutOfRange");

        var rangeStart = Max(startDate.Date, DateTime.Today);
        if (rangeStart <= endDate)
        {
            var occupants = (await leaseRepository.GetOpenLeaseAreasForUnitAsync(
                unit.UnitId, rangeStart, endDate, excludeLeaseId)).ToList();
            occupants.Add(new CapacityOccupant(
                startDate, endDate, allocated, excludeLeaseId, rented, mode));
            EnsureCapacityAvailable(capacity, unit.Area, occupants, rangeStart, endDate, "UnitId");
        }

        return (rented, allocated, mode);
    }

    public void EnsureCapacityAvailable(
        int capacity,
        decimal unitArea,
        IReadOnlyCollection<CapacityOccupant> occupants,
        DateTime rangeStart,
        DateTime rangeEnd,
        string field)
    {
        var violations = UnitCapacityPolicy.GetHardViolations(
            capacity, unitArea, occupants, rangeStart, rangeEnd);
        Guard.InvalidField(
            violations.Contains(CapacityHardViolation.CapacityExceeded),
            field,
            "Seçilen tarih aralığında birimin kontenjanı dolu.",
            "Lease.CapacityExceeded");
        Guard.InvalidField(
            violations.Contains(CapacityHardViolation.AreaExceeded),
            field,
            "Seçilen tarih aralığında fiziksel m² toplamı birim alanını aşıyor.",
            "Lease.CapacityAreaExceeded");
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
        var validated = await ValidateApplicationDataAsync(
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
            isCapacity ? storedArea?.AllocationMode : null,
            lease.Id,
            lease.LeaseGroupId,
            isGrouped ? storedArea?.RentedArea : null);
        if (isCapacity && validated.Area is { } activatedArea)
        {
            var area = await leaseRepository.UpsertLeaseAreaAsync(
                lease.Id, activatedArea.Rented, activatedArea.Allocated, activatedArea.Mode);
            leaseRepository.AddLeaseAreaHistory(
                area, activatedArea.Mode, lease.StartDate.Date, LeaseAreaChangeReason.Activation);
        }
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
