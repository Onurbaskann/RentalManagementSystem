using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Domain.Leases;
using KiraTakip.Domain.Pricing;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Documents;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Leases;

namespace KiraTakip.Services.Leases;

public class LeaseApplicationRules(
    ILeaseRepository leaseRepository,
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IChargeTypeRepository chargeTypeRepository,
    ITenantRepository tenantRepository,
    IChargeGenerationService chargeGenerationService,
    IDocumentRepository documentRepository,
    IDocumentTypeRepository documentTypeRepository,
    ILeaseReviewHistoryRepository reviewHistoryRepository) : ILeaseApplicationRules
{
    public async Task<ValidatedLeaseApplication> ValidateApplicationDataAsync(
        int spaceId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        int dueDay,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        SpaceAccessScope accessScope,
        bool isRentFree,
        decimal? rentedArea,
        bool isGroupFlow = false,
        int? excludeLeaseId = null,
        int? excludeLeaseGroupId = null)
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

        var space = Guard.NotFound(
            await leaseRepository.GetSpaceContextAsync(spaceId),
            "Seçilen alan bulunamadı.",
            "Lease.SpaceNotFound");
        EnsureSpaceInScope(space.Path, accessScope);
        Guard.InvalidField(
            !space.IsActive,
            nameof(spaceId),
            "Seçilen alan aktif değildir.",
            "Lease.SpaceNotActive");
        Guard.InvalidField(
            space.Usage != SpaceUsage.Rentable,
            nameof(spaceId),
            "Seçilen alan kiralanabilir değildir.",
            "Lease.SpaceNotRentable");
        if (isGroupFlow)
            Guard.InvalidField(
                !space.IsShared,
                nameof(spaceId),
                "Paylaşımlı grup yalnız paylaşımlı alanlarda açılabilir.",
                "Lease.GroupSpaceNotShared");
        else
            Guard.InvalidField(
                space.IsShared,
                nameof(spaceId),
                "Paylaşımlı alan yalnız paylaşımlı grup akışında kiralanabilir.",
                "Lease.SharedSpaceGroupOnly");
        Guard.InvalidField(
            await tenantRepository.GetByIdAsync(tenantId) == null,
            nameof(tenantId),
            "Seçilen kiracı bulunamadı.",
            "Lease.TenantNotFound");
        Guard.Forbidden(
            !await leaseRepository.IsTenantInScopeAsync(tenantId, accessScope),
            "Seçilen kiracı yetki kapsamınızın dışındadır.",
            "Lease.TenantOutOfScope");

        var rented = rentedArea ?? space.Area;
        Guard.InvalidField(
            rented <= 0,
            "RentedArea",
            "Kiralanan m² sıfırdan büyük olmalıdır.",
            "Lease.RentedAreaRequired");
        Guard.InvalidField(
            await leaseRepository.HasOverlappingActiveLeaseAsync(
                spaceId, startDate, endDate, excludeLeaseId, excludeLeaseGroupId),
            nameof(spaceId),
            "Seçilen alanda bu tarih aralığıyla çakışan açık sözleşme bulunmaktadır.",
            "Lease.ActiveSpaceConflict");
        await EnsureRateOverridesAsync(rateOverrides, rented, isRentFree);

        return new ValidatedLeaseApplication(space, rented);
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

        var area = Guard.NotFound(
            lease.Areas.OrderBy(item => item.Id).FirstOrDefault(),
            "Sözleşmede alan bulunamadı.",
            "Lease.SpaceMissing");
        Guard.Conflict(
            !area.Space.IsActive || area.Space.Usage != SpaceUsage.Rentable,
            "Alan artık aktif ve kiralanabilir olmadığından sözleşme uzatılamaz.",
            "Lease.SpaceNotRentable");
        Guard.Conflict(
            lease.LeaseGroupId != null && area.Space.Area != area.SnapshotArea,
            "Alanın yüzölçümü sözleşmeden sonra değişmiş; grubu yeniden yapılandırın.",
            "Lease.SpaceAreaChanged");
        Guard.InvalidField(
            await leaseRepository.HasOverlappingActiveLeaseAsync(
                area.SpaceId, lease.StartDate, newEndDate, lease.Id, excludeLeaseGroupId),
            "NewEndDate",
            "Seçilen alanda bu tarih aralığıyla çakışan açık sözleşme bulunmaktadır.",
            "Lease.ActiveSpaceConflict");
        Guard.Forbidden(
            updateRate && !canOverrideRate,
            "Sözleşme tarifesini değiştirme yetkiniz bulunmuyor.",
            "Lease.OverrideRateForbidden");
        await EnsureRateOverridesAsync(rateOverrides, area.RentedArea, lease.IsRentFree);
        Guard.InvalidField(
            isVatApplied && !RentIncreasePolicy.IsValidVatRate(vatRate),
            "VatRate",
            "KDV oranı 0-100 arasında olmalıdır.",
            "Lease.InvalidVatRate");
    }

    public async Task EnsureRateOverridesAsync(
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        decimal area,
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
            rateOverrides.Any(rate => !LeaseRatePolicy.CanUseCalculationMethod(area, rate.CalculationMethod)),
            "LeaseLineItems",
            "Yüzölçümü tanımlı olmayan alan için m² hesaplama yöntemi kullanılamaz.",
            "Lease.SpaceAreaRequired");
    }

    public async Task ActivateAsync(
        Lease lease,
        string actorUserId,
        string? explanation,
        SpaceAccessScope accessScope)
    {
        var storedArea = Guard.NotFound(
            lease.Areas.OrderBy(item => item.Id).FirstOrDefault(),
            "Sözleşmede alan bulunamadı.",
            "Lease.SpaceMissing");
        var validated = await ValidateApplicationDataAsync(
            storedArea.SpaceId,
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
            storedArea.RentedArea,
            isGroupFlow: lease.LeaseGroupId != null,
            excludeLeaseId: lease.Id,
            excludeLeaseGroupId: lease.LeaseGroupId);
        await leaseRepository.ReplaceLeaseAreasAsync(
            lease.Id,
            [(validated.Space, validated.RentedArea)]);
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
                storedArea.SpaceId,
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

    public static void EnsureSpaceInScope(string spacePath, SpaceAccessScope accessScope)
        => Guard.Forbidden(
            !accessScope.Includes(spacePath),
            "Bu sözleşme yetki kapsamınızın dışındadır.",
            "Lease.OutOfScope");

    /// <summary>Sözleşmenin alanı kapsamdaysa görünür (D13). Lease.Areas (Space yüklü) dolu olmalıdır.</summary>
    public static void EnsureLeaseInScope(Lease lease, SpaceAccessScope accessScope)
        => Guard.Forbidden(
            !lease.Areas.Any(area => accessScope.Includes(area.Space.Path)),
            "Bu sözleşme yetki kapsamınızın dışındadır.",
            "Lease.OutOfScope");
}
