using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PaymentAllocationRepository : RepositoryBase<PaymentAllocation>, IPaymentAllocationRepository
{
    public PaymentAllocationRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<PaymentListItemDto>> GetListAsync(
        int? chargeId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking();

        if (chargeId.HasValue)
            query = query.Where(o => o.ChargeId == chargeId.Value);

        query = ApplyScope(query, authorizedPropertyIds, authorizedUnitIds);

        return await query
            .OrderByDescending(o => o.EntryDate)
            .Select(o => new PaymentListItemDto
            {
                Id = o.Id,
                ChargeId = o.ChargeId,
                ChargeLineItemId = o.ChargeLineItemId,
                ChargeLineItemDescription = o.ChargeLineItem.Description,
                ChargeTypeName = o.ChargeLineItem.ChargeType.Name,
                LeaseId = o.LeaseId,
                PaymentDate = o.PaymentDate,
                Amount = o.Amount,
                PaymentChannel = o.PaymentChannel,
                PaymentSourceType = o.PaymentSourceType,
                Status = o.Status,
                EntryDate = o.EntryDate,
                Description = o.Description,
                TenantDisplayName = o.Charge.Tenant.Name,
                ChargePeriodStart = o.Charge.PeriodStart,
                CreatedByUserDisplayName = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            })
            .ToListAsync();
    }

    public async Task<PagedResult<PaymentListItemDto>> GetPagedListAsync(
        TableQuery tableQuery,
        int? chargeId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking();

        if (chargeId.HasValue)
            query = query.Where(o => o.ChargeId == chargeId.Value);

        query = ApplyScope(query, authorizedPropertyIds, authorizedUnitIds);

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var searchTerm = tableQuery.Q.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Charge.Tenant.Name, $"%{searchTerm}%") ||
                (o.Description != null && EF.Functions.Like(o.Description, $"%{searchTerm}%")));
        }

        if (tableQuery.From.HasValue) query = query.Where(o => o.PaymentDate >= tableQuery.From.Value);
        if (tableQuery.To.HasValue) query = query.Where(o => o.PaymentDate <= tableQuery.To.Value);
        if (tableQuery.Min.HasValue) query = query.Where(o => o.Amount >= tableQuery.Min.Value);
        if (tableQuery.Max.HasValue) query = query.Where(o => o.Amount <= tableQuery.Max.Value);

        if (!string.IsNullOrWhiteSpace(tableQuery.Status) && tableQuery.Status != "tum")
        {
            PaymentStatus? filteredStatus = tableQuery.Status switch
            {
                "onaybekliyor" => PaymentStatus.PendingApproval,
                "onaylandi" => PaymentStatus.Approved,
                "reddedildi" => PaymentStatus.Rejected,
                _ => null
            };
            if (filteredStatus.HasValue)
                query = query.Where(o => o.Status == filteredStatus.Value);
        }

        var itemsQuery = query
            .OrderByDescending(o => o.EntryDate)
            .ThenByDescending(o => o.Id)
            .Select(o => new PaymentListItemDto
            {
                Id = o.Id,
                ChargeId = o.ChargeId,
                ChargeLineItemId = o.ChargeLineItemId,
                ChargeLineItemDescription = o.ChargeLineItem.Description,
                ChargeTypeName = o.ChargeLineItem.ChargeType.Name,
                LeaseId = o.LeaseId,
                PaymentDate = o.PaymentDate,
                Amount = o.Amount,
                PaymentChannel = o.PaymentChannel,
                PaymentSourceType = o.PaymentSourceType,
                Status = o.Status,
                EntryDate = o.EntryDate,
                Description = o.Description,
                TenantDisplayName = o.Charge.Tenant.Name,
                ChargePeriodStart = o.Charge.PeriodStart,
                CreatedByUserDisplayName = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            });

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    public async Task<PaymentDetailDto?> GetDetailsAsync(
        int id,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(payment => payment.Id == id),
            authorizedPropertyIds,
            authorizedUnitIds);

        return await query
            .Select(o => new PaymentDetailDto
            {
                Id = o.Id,
                ChargeId = o.ChargeId,
                ChargeLineItemId = o.ChargeLineItemId,
                ChargeLineItemDescription = o.ChargeLineItem.Description,
                ChargeTypeName = o.ChargeLineItem.ChargeType.Name,
                LeaseId = o.LeaseId,
                PaymentDate = o.PaymentDate,
                Amount = o.Amount,
                PaymentChannel = o.PaymentChannel,
                PaymentSourceType = o.PaymentSourceType,
                PosReferenceNo = o.PosReferenceNo,
                Description = o.Description,
                Status = o.Status,
                EntryDate = o.EntryDate,
                ApprovalDate = o.ApprovalDate,
                RejectionReason = o.RejectionReason,
                StoreAccountId = o.StoreAccountId,
                StoreName = o.StoreAccount.Store.Name,
                StoreProviderCode = o.StoreAccount.ProviderCode,
                StoreCurrency = o.StoreAccount.Currency,
                PropertyId = o.Charge.Unit.PropertyId,
                TenantDisplayName = o.Charge.Tenant.Name,
                ChargePeriodStart = o.Charge.PeriodStart,
                CreatedByUserDisplayName = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null,
                ApprovedByUserDisplayName = o.OnaylayanUser != null ? o.OnaylayanUser.AdSoyad : null,
                BankMatches = o.BankMatches.Select(e => new PaymentBankMatchDto
                {
                    Id = e.Id,
                    MatchType = e.MatchType,
                    BankTransactionAmount = e.BankTransaction.TransactionAmount,
                    BankTransactionDate = e.BankTransaction.TransactionDate,
                    BankTransactionDescription = e.BankTransaction.Description ?? string.Empty
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PaymentAllocation?> GetForDecisionAsync(
        int id,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet
            .Include(payment => payment.Charge)
            .Include(payment => payment.ChargeLineItem)
            .Where(payment => payment.Id == id);

        query = ApplyScope(query, authorizedPropertyIds, authorizedUnitIds);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<decimal> GetPaidAmountAsync(int chargeId)
        => await _dbSet.AsNoTracking()
            .Where(payment => payment.ChargeId == chargeId && payment.Status == PaymentStatus.Approved)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0m;

    public Task<int?> GetChargeLineItemIdAsync(int paymentId)
        => _dbSet.AsNoTracking()
            .Where(payment => payment.Id == paymentId)
            .Select(payment => (int?)payment.ChargeLineItemId)
            .FirstOrDefaultAsync();

    public async Task<decimal> GetTenantApprovedTotalAsync(
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking().Where(payment =>
            payment.Charge.TenantId == tenantId
            && payment.Status == PaymentStatus.Approved);
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(payment =>
                propertyIds.Contains(payment.Charge.Unit.PropertyId)
                || unitIds.Contains(payment.Charge.UnitId));
        }

        return await query.SumAsync(payment => (decimal?)payment.Amount) ?? 0m;
    }
    public async Task<decimal> GetPendingAmountAsync(int chargeId, int tenantId)
        => await _dbSet.AsNoTracking()
            .Where(payment => payment.ChargeId == chargeId
                && payment.Charge.TenantId == tenantId
                && payment.Status == PaymentStatus.PendingApproval)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0m;

    public async Task<TenantPanelPaymentDataDto> GetTenantPanelDataAsync(
        GetTenantPanelPaymentDataInput input)
    {
        var propertyIds = input.PropertyIds?.ToList();
        var unitIds = input.UnitIds?.ToList();
        var sixMonthStart = new DateTime(input.Today.Year, input.Today.Month, 1).AddMonths(-5);
        var query = _dbSet.AsNoTracking().Where(payment =>
            payment.Charge.TenantId == input.TenantId
            && (propertyIds == null && unitIds == null
                || propertyIds != null && propertyIds.Contains(payment.Charge.Unit.PropertyId)
                || unitIds != null && unitIds.Contains(payment.Charge.UnitId)));
        var monthlyPaid = await query
            .Where(payment => payment.Status == PaymentStatus.Approved
                && payment.PaymentDate >= sixMonthStart)
            .GroupBy(payment => new { payment.PaymentDate.Year, payment.PaymentDate.Month })
            .Select(group => new TenantPanelMonthlyTotalDto(
                group.Key.Year,
                group.Key.Month,
                group.Sum(payment => payment.Amount)))
            .ToListAsync();
        var recentPayments = await query
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(5)
            .Select(payment => new TenantPanelRecentPaymentDataDto(
                payment.Id,
                payment.PaymentDate,
                payment.Amount,
                payment.PaymentChannel,
                payment.Status))
            .ToListAsync();

        return new TenantPanelPaymentDataDto(monthlyPaid, recentPayments);
    }
    public Task<PaymentMatchingContextDto?> GetMatchingContextAsync(int paymentId)
        => _dbSet.AsNoTracking()
            .Where(payment => payment.Id == paymentId)
            .Select(payment => new PaymentMatchingContextDto(
                payment.Id,
                payment.Charge.Unit.PropertyId,
                payment.Charge.UnitId,
                payment.Status,
                payment.StoreAccountId))
            .FirstOrDefaultAsync();

    public Task<PaymentMatchingBasisDto?> GetMatchingBasisAsync(int paymentId)
        => _dbSet.AsNoTracking()
            .Where(payment => payment.Id == paymentId)
            .Select(payment => new PaymentMatchingBasisDto(
                payment.Amount, payment.PaymentDate, payment.StoreAccountId))
            .FirstOrDefaultAsync();

    public async Task<List<PaymentCandidateDto>> GetCandidatesAsync(
        PaymentMatchingBasisDto basis,
        PaymentMatchingPolicyDto policy,
        IReadOnlyList<int>? propertyIds,
        IReadOnlyList<int>? unitIds = null)
    {
        var tolerance = basis.Amount * policy.AmountTolerancePercent / 100m;
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.PendingApproval || payment.Status == PaymentStatus.Approved)
            .Where(payment => payment.StoreAccountId == basis.StoreAccountId)
            .Where(payment => !_ctx.PaymentMatches.Any(match => match.PaymentAllocationId == payment.Id));

        query = ApplyScope(
            query,
            propertyIds?.ToList(),
            unitIds?.ToList());

        var candidates = await query.Select(payment => new PaymentCandidateDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Status = payment.Status,
            TenantDisplayName = payment.Charge.Tenant.Name,
            PeriodStart = payment.Charge.PeriodStart
        }).ToListAsync();

        return candidates.OrderBy(payment =>
        {
            var isExactAmount = payment.Amount == basis.Amount;
            var isCloseAmount = Math.Abs(payment.Amount - basis.Amount) <= tolerance;
            var dayDifference = Math.Abs((payment.PaymentDate - basis.Date).Days);
            if (isExactAmount && dayDifference <= policy.DateToleranceDays) return 0;
            if (isExactAmount) return 1;
            if (isCloseAmount && dayDifference <= policy.DateToleranceDays) return 2;
            return 3;
        })
        .ThenBy(payment => Math.Abs((payment.PaymentDate - basis.Date).Days))
        .ThenBy(payment => Math.Abs(payment.Amount - basis.Amount))
        .ToList();
    }

    public async Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int paymentId)
    {
        var context = await _dbSet
            .AsNoTracking()
            .Where(payment => payment.Id == paymentId)
            .Select(payment => new
            {
                payment.Charge.TenantId,
                payment.Charge.UnitId,
                payment.Charge.Unit.PropertyId
            })
            .FirstOrDefaultAsync();

        return context == null
            ? null
            : new DocumentOwnerContextDto(
                context.TenantId,
                [context.PropertyId],
                [context.UnitId]);
    }

    private static IQueryable<PaymentAllocation> ApplyScope(
        IQueryable<PaymentAllocation> query,
        IReadOnlyCollection<int>? propertyIds,
        IReadOnlyCollection<int>? unitIds)
    {
        if (propertyIds == null && unitIds == null)
            return query;

        return query.Where(payment =>
            (propertyIds != null && propertyIds.Contains(payment.Charge.Unit.PropertyId))
            || (unitIds != null && unitIds.Contains(payment.Charge.UnitId)));
    }
}
