using KiraTakip.Data;
using KiraTakip.Domain.Charges;
using KiraTakip.Domain.Leases;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Pricing;

namespace KiraTakip.Services.Charges;

public class ChargeGenerationService(
    IChargeRepository chargeRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitOfWork uow,
    IRateResolverService rateResolver,
    ILeaseRepository leaseRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository) : IChargeGenerationService, ITransactionalService
{
    public async Task GenerateForLeaseAsync(GenerateLeaseChargesInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetByIdAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");
        Guard.Conflict(
            !LeaseLifecycle.CanGenerateCharges(lease.Status),
            "Yalnız aktif sözleşme için tahakkuk üretilebilir.",
            "Lease.NotActive");

        foreach (var periodStartDate in ChargePeriodPolicy.GetMonthlyPeriodStarts(
            lease.StartDate,
            lease.EndDate))
        {
            var exists = await chargeRepository.AnyAsync(t => t.LeaseId == input.LeaseId
                && t.PeriodStart == periodStartDate
                && t.SourceType == ChargeSourceType.Lease);
            if (exists) continue;

            var proRata = ChargeProrationPolicy.CalculatePeriodMultiplier(
                periodStartDate,
                lease.StartDate,
                lease.EndDate);
            var composedPreviews = await ComposeLineItemsAsync(
                new ComposeLeaseLineItemsInput(lease.UnitId, lease.TenantId, periodStartDate, input.LeaseId));
            var lineItems = new List<ChargeLineItem>();

            foreach (var preview in composedPreviews)
            {
                var lineItemProRata = ChargeProrationPolicy.ResolveLineItemMultiplier(
                    preview.Behavior,
                    proRata);
                var calculation = ChargeAmountPolicy.Calculate(
                    preview.Amount,
                    lineItemProRata,
                    preview.KdvRate);

                lineItems.Add(new ChargeLineItem
                {
                    ChargeTypeId = preview.ChargeTypeId,
                    Description = preview.Description ?? preview.ChargeTypeName,
                    CalculationMethod = preview.CalculationMethod,
                    UnitValue = preview.UnitValue,
                    Multiplier = ChargeAmountPolicy.CalculateLineItemMultiplier(
                        preview.Multiplier,
                        lineItemProRata),
                    Amount = calculation.Amount,
                    KdvRate = preview.KdvRate,
                    KdvAmount = calculation.VatAmount,
                    TotalAmount = calculation.TotalAmount,
                    SourceType = preview.SourceType
                });
            }

            var totalAmount = lineItems.Sum(lineItem => lineItem.TotalAmount);
            if (lease.IsRentFree && !LeaseBillingPolicy.HasChargeableAmount(totalAmount))
                continue;

            var periodEnd = ChargePeriodPolicy.GetPeriodEnd(periodStartDate, lease.EndDate);

            var charge = new Charge
            {
                TenantId = lease.TenantId,
                UnitId = lease.UnitId,
                LeaseId = input.LeaseId,
                PeriodStart = periodStartDate,
                PeriodEnd = periodEnd,
                DueDate = ChargeDueDatePolicy.Calculate(
                    periodStartDate,
                    lease.DueDateRuleType,
                    lease.DueDay),
                ExpectedAmount = lineItems.Sum(k => k.Amount),
                KdvAmount = lineItems.Sum(k => k.KdvAmount),
                TotalAmount = lineItems.Sum(k => k.TotalAmount),
                PaidAmount = 0,
                Status = ChargeStatus.Pending,
                SourceType = ChargeSourceType.Lease,
                LineItems = lineItems
            };

            await chargeRepository.AddAsync(charge);
        }

        await uow.SaveChangesAsync();
    }

    public async Task RegenerateAsync(RegenerateLeaseChargesInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetByIdAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");
        Guard.Conflict(
            !LeaseLifecycle.CanRegenerateCharges(lease.Status),
            "Yalnız aktif sözleşmenin tahakkukları yeniden üretilebilir.",
            "Lease.NotActive");

        var firstDay = new DateTime(input.StartDate.Year, input.StartDate.Month, 1);
        var toDelete = await chargeRepository.GetSilineceklerAsync(input.LeaseId, firstDay);

        await chargeRepository.DeleteRangeAsync(toDelete);
        await uow.SaveChangesAsync();
        await GenerateForLeaseAsync(new GenerateLeaseChargesInput(input.LeaseId));
    }

    public async Task RecalculatePendingDueDatesAsync(RecalculateLeaseDueDatesInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetByIdAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        var targetStatuses = new[] { ChargeStatus.Pending, ChargeStatus.PartiallyPaid, ChargeStatus.Overdue };
        var pendingCharges = await chargeRepository.GetAllAsync(t =>
            t.LeaseId == input.LeaseId
            && t.SourceType == ChargeSourceType.Lease
            && targetStatuses.Contains(t.Status));

        if (pendingCharges.Count == 0) return;

        var today = DateTime.Today;
        foreach (var t in pendingCharges)
        {
            t.DueDate = ChargeDueDatePolicy.Calculate(
                t.PeriodStart,
                lease.DueDateRuleType,
                lease.DueDay);

            t.Status = ChargeStatusPolicy.DetermineStatus(
                t.PaidAmount,
                t.TotalAmount,
                t.DueDate,
                today);
        }

        await uow.SaveChangesAsync();
    }

    public async Task CancelFutureChargesAsync(CancelFutureLeaseChargesInput input)
    {
        var lease = Guard.NotFound(
            await leaseRepository.GetByIdAsync(input.LeaseId),
            $"Sözleşme {input.LeaseId} bulunamadı.",
            "Lease.NotFound");

        var firstDay = new DateTime(input.TerminationDate.Year, input.TerminationDate.Month, 1).AddMonths(1);
        var toCancel = await chargeRepository.GetAllAsync(t =>
            t.LeaseId == input.LeaseId
            && t.PeriodStart >= firstDay
            && t.Status != ChargeStatus.Paid
            && t.SourceType == ChargeSourceType.Lease);

        foreach (var t in toCancel)
            t.Status = ChargeStatus.Cancelled;

        if (toCancel.Count > 0)
            await uow.SaveChangesAsync();
    }

    public async Task<IList<ChargeLineItemPreview>> ComposeLineItemsAsync(ComposeLeaseLineItemsInput input)
    {
        var unit = Guard.NotFound(
            await unitRepository.GetByIdAsync(input.UnitId),
            "Birim bulunamadı.",
            "Unit.NotFound");
        EnsureScope(unit.PropertyId, unit.Id, input.AccessScope);

        Guard.NotFound(
            await tenantRepository.GetByIdAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "Tenant.NotFound");

        if (input.LeaseId.HasValue)
        {
            var lease = Guard.NotFound(
                await leaseRepository.GetDetailsAsync(input.LeaseId.Value),
                $"Sözleşme {input.LeaseId.Value} bulunamadı.",
                "Lease.NotFound");
            Guard.Against(
                lease.UnitId != input.UnitId || lease.TenantId != input.TenantId,
                "Sözleşme, birim ve kiracı bilgileri birbiriyle uyuşmuyor.",
                "Lease.PricingContextMismatch");
        }

        var activeChargeTypes = await chargeTypeRepository.GetActiveGenerationTypesAsync();
        var previewList = new List<ChargeLineItemPreview>();
        var isRentFree = input.LeaseId.HasValue
            && await leaseRepository.GetByIdAsync<bool>(
                input.LeaseId.Value,
                lease => lease.IsRentFree);

        foreach (var ct in activeChargeTypes)
        {
            if (!LeaseBillingPolicy.ShouldIncludeChargeType(isRentFree, ct.Code))
                continue;

            if (ct.Behavior == ChargeTypeBehavior.FirstMonthOneTime)
            {
                DateTime? start = null;
                if (input.LeaseId.HasValue)
                {
                    start = await leaseRepository.GetByIdAsync<DateTime?>(input.LeaseId.Value, s => s.StartDate);
                }
                else
                {
                    start = input.Period;
                }

                if (start.HasValue && (input.Period.Year != start.Value.Year || input.Period.Month != start.Value.Month))
                    continue;
            }

            RateSnapshot? snapshot = await rateResolver.ResolveAsync(
                input.LeaseId,
                input.TenantId,
                input.UnitId,
                ct.Id,
                input.Period);

            if (snapshot != null)
            {
                var multiplierBase = LeaseRatePolicy.CalculateMultiplier(snapshot.CalculationMethod, unit.Area);
                var calculation = ChargeAmountPolicy.Calculate(
                    snapshot.UnitValue,
                    multiplierBase,
                    snapshot.KdvRate);

                previewList.Add(new ChargeLineItemPreview
                {
                    ChargeTypeId = ct.Id,
                    ChargeTypeName = ct.Name,
                    ChargeTypeCode = ct.Code,
                    Behavior = ct.Behavior,
                    CalculationMethod = snapshot.CalculationMethod,
                    UnitValue = snapshot.UnitValue,
                    Multiplier = multiplierBase,
                    Amount = calculation.Amount,
                    KdvRate = snapshot.KdvRate,
                    KdvAmount = calculation.VatAmount,
                    TotalAmount = calculation.TotalAmount,
                    SourceType = snapshot.SourceType,
                    IsRateFound = true,
                    Description = ct.Name
                });
            }
            else
            {
                previewList.Add(new ChargeLineItemPreview
                {
                    ChargeTypeId = ct.Id,
                    ChargeTypeName = ct.Name,
                    ChargeTypeCode = ct.Code,
                    Behavior = ct.Behavior,
                    CalculationMethod = CalculationMethod.Fixed,
                    UnitValue = 0m,
                    Multiplier = 0m,
                    Amount = 0m,
                    KdvRate = 0m,
                    KdvAmount = 0m,
                    TotalAmount = 0m,
                    SourceType = LineItemSourceType.UndefinedRate,
                    IsRateFound = false,
                    Description = $"{ct.Name} (Fiyat Tanımsız)"
                });
            }
        }

        return previewList;
    }

    private static void EnsureScope(
        int propertyId,
        int unitId,
        LeaseAccessScopeInput? accessScope)
    {
        if (accessScope == null
            || (accessScope.PropertyIds == null && accessScope.UnitIds == null))
            return;

        var propertyAccess = accessScope.PropertyIds?.Contains(propertyId) == true;
        var unitAccess = accessScope.UnitIds?.Contains(unitId) == true;
        Guard.Forbidden(
            !propertyAccess && !unitAccess,
            "Bu birim yetki kapsamınızın dışındadır.",
            "Lease.UnitOutOfScope");
    }
}
