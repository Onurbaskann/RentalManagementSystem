using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

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
            lease.Status != LeaseStatus.Active,
            "Yalnız aktif sözleşme için tahakkuk üretilebilir.",
            "Lease.NotActive");

        foreach (var periodStartDate in GetPeriods(lease.StartDate, lease.EndDate))
        {
            var exists = await chargeRepository.AnyAsync(t => t.LeaseId == input.LeaseId
                && t.PeriodStart == periodStartDate
                && t.SourceType == ChargeSourceType.Lease);
            if (exists) continue;

            var proRata = CalculateProRataMultiplier(periodStartDate, lease.StartDate, lease.EndDate);
            var composedPreviews = await ComposeLineItemsAsync(
                new ComposeLeaseLineItemsInput(lease.UnitId, lease.TenantId, periodStartDate, input.LeaseId));
            var lineItems = new List<ChargeLineItem>();

            foreach (var preview in composedPreviews)
            {
                var lineItemProRata = preview.Behavior == ChargeTypeBehavior.FirstMonthOneTime ? 1m : proRata;
                var amount = Math.Round(preview.Amount * lineItemProRata, 2);
                var kdvAmount = Math.Round(amount * preview.KdvRate / 100, 2);

                lineItems.Add(new ChargeLineItem
                {
                    ChargeTypeId = preview.ChargeTypeId,
                    Description = preview.Description ?? preview.ChargeTypeName,
                    CalculationMethod = preview.CalculationMethod,
                    UnitValue = preview.UnitValue,
                    Multiplier = Math.Round(preview.Multiplier * lineItemProRata, 6),
                    Amount = amount,
                    KdvRate = preview.KdvRate,
                    KdvAmount = kdvAmount,
                    TotalAmount = amount + kdvAmount,
                    SourceType = preview.SourceType
                });
            }

            var monthEnd = periodStartDate.AddMonths(1).AddDays(-1);
            var periodEnd = lease.EndDate < monthEnd ? lease.EndDate : monthEnd;

            var charge = new Charge
            {
                TenantId = lease.TenantId,
                UnitId = lease.UnitId,
                LeaseId = input.LeaseId,
                PeriodStart = periodStartDate,
                PeriodEnd = periodEnd,
                DueDate = CalculateDueDate(periodStartDate, lease.DueDateRuleType, lease.DueDay),
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
            lease.Status != LeaseStatus.Active,
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
            t.DueDate = CalculateDueDate(t.PeriodStart, lease.DueDateRuleType, lease.DueDay);

            t.Status = t.PaidAmount >= t.TotalAmount
                ? ChargeStatus.Paid
                : t.PaidAmount > 0
                    ? ChargeStatus.PartiallyPaid
                    : today > t.DueDate
                        ? ChargeStatus.Overdue
                        : ChargeStatus.Pending;
        }

        await uow.SaveChangesAsync();
    }

    public async Task CancelFutureChargesAsync(CancelFutureLeaseChargesInput input)
    {
        Guard.NotFound(
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

    private static DateTime CalculateDueDate(DateTime periodStartDate, DueDateRuleType ruleType, int dueDay)
    {
        return ruleType switch
        {
            DueDateRuleType.FixedDayOfMonth =>
                new DateTime(periodStartDate.Year, periodStartDate.Month,
                    Math.Min(Math.Max(dueDay, 1), DateTime.DaysInMonth(periodStartDate.Year, periodStartDate.Month))),
            DueDateRuleType.PeriodStartOffset =>
                periodStartDate.AddDays(Math.Max(dueDay - 1, 0)),
            _ => periodStartDate
        };
    }

    private static decimal CalculateProRataMultiplier(DateTime periodStartDate, DateTime leaseStartDate, DateTime leaseEndDate)
    {
        var monthEnd = periodStartDate.AddMonths(1).AddDays(-1);
        var activeStart = leaseStartDate > periodStartDate ? leaseStartDate : periodStartDate;
        var activeEnd = leaseEndDate < monthEnd ? leaseEndDate : monthEnd;

        if (activeStart == periodStartDate && activeEnd == monthEnd)
            return 1.0m;

        var dayCount = (activeEnd - activeStart).Days + 1;

        return Math.Min(1.0m, (decimal)dayCount / 30m);
    }

    private static IEnumerable<DateTime> GetPeriods(DateTime start, DateTime end)
    {
        var month = new DateTime(start.Year, start.Month, 1);
        var lastMonth = new DateTime(end.Year, end.Month, 1);
        while (month <= lastMonth)
        {
            yield return month;
            month = month.AddMonths(1);
        }
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

        foreach (var ct in activeChargeTypes)
        {
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
                var multiplierBase = snapshot.CalculationMethod == CalculationMethod.M2 ? unit.Area : 1m;
                var amount = Math.Round(snapshot.UnitValue * multiplierBase, 2);
                var kdvAmount = Math.Round(amount * snapshot.KdvRate / 100, 2);

                previewList.Add(new ChargeLineItemPreview
                {
                    ChargeTypeId = ct.Id,
                    ChargeTypeName = ct.Name,
                    ChargeTypeCode = ct.Code,
                    Behavior = ct.Behavior,
                    CalculationMethod = snapshot.CalculationMethod,
                    UnitValue = snapshot.UnitValue,
                    Multiplier = multiplierBase,
                    Amount = amount,
                    KdvRate = snapshot.KdvRate,
                    KdvAmount = kdvAmount,
                    TotalAmount = amount + kdvAmount,
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
