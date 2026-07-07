using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.DTOs;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ChargeGenerationService : IChargeGenerationService, ITransactionalService
{
    private readonly IChargeRepository _chargeRepo;
    private readonly IUnitOfWork _uow;
    private readonly IRateResolverService _rateResolver;
    private readonly ILeaseRepository _leaseRepo;
    private readonly IUnitRepository _unitRepo;

    public ChargeGenerationService(
        IChargeRepository chargeRepo,
        IUnitOfWork uow,
        IRateResolverService rateResolver,
        ILeaseRepository leaseRepo,
        IUnitRepository unitRepo)
    {
        _chargeRepo = chargeRepo;
        _uow = uow;
        _rateResolver = rateResolver;
        _leaseRepo = leaseRepo;
        _unitRepo = unitRepo;
    }

    public async Task GenerateForLeaseAsync(int leaseId)
    {
        var lease = await _leaseRepo.GetByIdAsync(leaseId);
        if (lease == null) return;

        foreach (var periodStartDate in GetPeriods(lease.StartDate, lease.EndDate))
        {
            var exists = await _chargeRepo.AnyAsync(t => t.LeaseId == leaseId
                && t.PeriodStart == periodStartDate
                && t.SourceType == ChargeSourceType.Lease);
            if (exists) continue;

            var proRata = CalculateProRataMultiplier(periodStartDate, lease.StartDate, lease.EndDate);
            var composedPreviews = await ComposeLineItemsAsync(lease.UnitId, lease.TenantId, periodStartDate, leaseId);
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
                LeaseId = leaseId,
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

            await _chargeRepo.AddAsync(charge);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task RegenerateAsync(int leaseId, DateTime startDate)
    {
        var firstDay = new DateTime(startDate.Year, startDate.Month, 1);
        var toDelete = await _chargeRepo.GetSilineceklerAsync(leaseId, firstDay);
        await _chargeRepo.DeleteRangeAsync(toDelete);
        await _uow.SaveChangesAsync();
        await GenerateForLeaseAsync(leaseId);
    }

    public async Task RecalculatePendingDueDatesAsync(int leaseId)
    {
        var lease = await _leaseRepo.GetByIdAsync(leaseId);
        if (lease == null) return;

        var targetStatuses = new[] { ChargeStatus.Pending, ChargeStatus.PartiallyPaid, ChargeStatus.Overdue };
        var pendingCharges = await _chargeRepo.GetAllAsync(t =>
            t.LeaseId == leaseId
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

        await _uow.SaveChangesAsync();
    }

    public async Task CancelFutureChargesAsync(int leaseId, DateTime terminationDate)
    {
        var firstDay = new DateTime(terminationDate.Year, terminationDate.Month, 1).AddMonths(1);
        var toCancel = await _chargeRepo.GetAllAsync(t =>
            t.LeaseId == leaseId
            && t.PeriodStart >= firstDay
            && t.Status != ChargeStatus.Paid
            && t.SourceType == ChargeSourceType.Lease);

        foreach (var t in toCancel)
            t.Status = ChargeStatus.Cancelled;

        if (toCancel.Count > 0)
            await _uow.SaveChangesAsync();
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

    public async Task<IList<ChargeLineItemPreview>> ComposeLineItemsAsync(int unitId, int tenantId, DateTime period, int? leaseId = null)
    {
        var unit = await _unitRepo.GetByIdAsync(unitId);
        if (unit == null) return new List<ChargeLineItemPreview>();

        var activeChargeTypes = await _chargeRepo.GetAktifUretimBorcTipleriAsync();
        var previewList = new List<ChargeLineItemPreview>();

        foreach (var ct in activeChargeTypes)
        {
            if (ct.Behavior == ChargeTypeBehavior.FirstMonthOneTime)
            {
                DateTime? start = null;
                if (leaseId.HasValue)
                {
                    start = await _leaseRepo.GetByIdAsync<DateTime?>(leaseId.Value, s => s.StartDate);
                }
                else
                {
                    start = period;
                }

                if (start.HasValue && (period.Year != start.Value.Year || period.Month != start.Value.Month))
                    continue;
            }

            RateSnapshot? snapshot = await _rateResolver.ResolveAsync(leaseId, tenantId, unitId, ct.Id, period);

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
}
