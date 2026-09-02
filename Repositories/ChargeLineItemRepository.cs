using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ChargeLineItemRepository(ApplicationDbContext context)
    : RepositoryBase<ChargeLineItem>(context), IChargeLineItemRepository
{
    public async Task<Dictionary<int, decimal?>> GetDepositAmountsByLeaseIdsAsync(
        IEnumerable<int> leaseIds,
        int? tenantId = null)
    {
        var ids = leaseIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal?>();

        var query = _dbSet
            .AsNoTracking()
            .Where(lineItem => lineItem.Charge.LeaseId.HasValue
                && ids.Contains(lineItem.Charge.LeaseId.Value)
                && lineItem.ChargeType.Code == BorcTipiConsts.Depozito
                && lineItem.Charge.Status != ChargeStatus.Cancelled);

        if (tenantId.HasValue)
            query = query.Where(lineItem => lineItem.Charge.TenantId == tenantId.Value);

        var lineItems = await query
            .Select(lineItem => new
            {
                LeaseId = lineItem.Charge.LeaseId!.Value,
                Period = lineItem.Charge.PeriodStart,
                lineItem.TotalAmount
            })
            .ToListAsync();

        return lineItems
            .GroupBy(item => item.LeaseId)
            .ToDictionary(group => group.Key, group => (decimal?)group.OrderBy(item => item.Period).First().TotalAmount);
    }

    public async Task<List<TenantPanelDebtSliceDto>> GetTenantDebtDistributionAsync(
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(lineItem => lineItem.Charge.TenantId == tenantId
                && lineItem.Charge.Status != ChargeStatus.Cancelled
                && lineItem.Charge.TotalAmount > lineItem.Charge.PaidAmount
                && lineItem.Charge.TotalAmount > 0);
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(lineItem =>
                propertyIds.Contains(lineItem.Charge.Unit.PropertyId)
                || unitIds.Contains(lineItem.Charge.UnitId));
        }

        var amounts = await query.Select(lineItem => new
        {
            Name = lineItem.ChargeType.Name,
            Amount = lineItem.TotalAmount
        }).ToListAsync();

        return amounts.GroupBy(item => item.Name)
            .Select(group => new TenantPanelDebtSliceDto(
                group.Key,
                group.Sum(item => item.Amount)))
            .OrderByDescending(item => item.Amount)
            .Take(5)
            .ToList();
    }

    public Task<ChargeLineItemPaymentBalanceDto?> GetPaymentBalanceAsync(
        int chargeLineItemId,
        CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking()
            .Where(lineItem => lineItem.Id == chargeLineItemId)
            .Select(lineItem => new ChargeLineItemPaymentBalanceDto(
                lineItem.Id,
                lineItem.ChargeId,
                lineItem.ChargeTypeId,
                lineItem.Charge.UnitId,
                lineItem.Charge.TenantId,
                lineItem.ChargeType.Name,
                lineItem.Description,
                lineItem.TotalAmount,
                lineItem.Allocations
                    .Where(allocation => allocation.Status == PaymentStatus.Approved)
                    .Sum(allocation => (decimal?)allocation.Amount) ?? 0m,
                lineItem.Allocations
                    .Where(allocation => allocation.Status == PaymentStatus.PendingApproval)
                    .Sum(allocation => (decimal?)allocation.Amount) ?? 0m))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<ChargeLineItemPaymentBalanceDto>> GetPaymentBalancesByChargeAsync(
        int chargeId,
        CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking()
            .Where(lineItem => lineItem.ChargeId == chargeId)
            .OrderBy(lineItem => lineItem.ChargeType.SortOrder)
            .ThenBy(lineItem => lineItem.Id)
            .Select(lineItem => new ChargeLineItemPaymentBalanceDto(
                lineItem.Id,
                lineItem.ChargeId,
                lineItem.ChargeTypeId,
                lineItem.Charge.UnitId,
                lineItem.Charge.TenantId,
                lineItem.ChargeType.Name,
                lineItem.Description,
                lineItem.TotalAmount,
                lineItem.Allocations
                    .Where(allocation => allocation.Status == PaymentStatus.Approved)
                    .Sum(allocation => (decimal?)allocation.Amount) ?? 0m,
                lineItem.Allocations
                    .Where(allocation => allocation.Status == PaymentStatus.PendingApproval)
                    .Sum(allocation => (decimal?)allocation.Amount) ?? 0m))
            .ToListAsync(cancellationToken);

    public Task<ChargeLineItem?> GetForPaymentUpdateAsync(int chargeLineItemId)
        => _dbSet.FirstOrDefaultAsync(lineItem => lineItem.Id == chargeLineItemId);

    public async Task<decimal> GetChargePaidAmountTotalAsync(int chargeId)
        => await _dbSet.AsNoTracking()
            .Where(lineItem => lineItem.ChargeId == chargeId)
            .SumAsync(lineItem => (decimal?)lineItem.PaidAmount) ?? 0m;

    public Task AcquirePaymentLockAsync(int chargeLineItemId)
    {
        if (_ctx.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Tahakkuk kalemi ödeme kilidi aktif transaction gerektirir.");

        var resource = $"KiraTakip.Payment.ChargeLineItem.{chargeLineItemId}";
        return _ctx.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            IF @result < 0
                THROW 51000, 'Tahakkuk kalemi ödeme kilidi alınamadı.', 1;");
    }
}
