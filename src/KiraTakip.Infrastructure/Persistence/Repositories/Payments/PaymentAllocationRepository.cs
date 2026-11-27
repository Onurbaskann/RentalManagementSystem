using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Payments;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.TenantPanel;
using KiraTakip.Models.Dtos.BankTransaction;
using KiraTakip.Models.Dtos.Document;
using KiraTakip.Models.Dtos.Report;

namespace KiraTakip.Repositories.Payments;

public class PaymentAllocationRepository : RepositoryBase<PaymentAllocation>, IPaymentAllocationRepository
{
    public PaymentAllocationRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<PaymentListItemDto>> GetListAsync(
        int? chargeId,
        SpaceAccessScope? scope)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking();

        if (chargeId.HasValue)
            query = query.Where(o => o.ChargeId == chargeId.Value);

        query = ApplyScope(query, scope);

        return await query
            .OrderByDescending(o => o.EntryDate)
            .Select(o => new PaymentListItemDto
            {
                Id = o.Id,
                PaymentNo = o.PaymentNo,
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
                CreatedByUserDisplayName = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null,
                SpaceName = o.Charge.Lease != null
                    ? o.Charge.Lease.Areas.Select(area => area.SnapshotPath).FirstOrDefault() ?? string.Empty
                    : o.Charge.Space != null ? o.Charge.Space.Name : string.Empty,
                StoreId = o.StoreAccount.StoreId,
                StoreName = o.StoreAccount.Store.Name
            })
            .ToListAsync();
    }

    public async Task<PagedResult<PaymentListItemDto>> GetPagedListAsync(
        TableQuery tableQuery,
        int? chargeId,
        SpaceAccessScope? scope)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking();

        if (chargeId.HasValue)
            query = query.Where(o => o.ChargeId == chargeId.Value);

        query = ApplyScope(query, scope);

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var searchTerm = tableQuery.Q.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Charge.Tenant.Name, $"%{searchTerm}%") ||
                EF.Functions.Like(o.PaymentNo, $"%{searchTerm}%") ||
                (o.Charge.Space != null && EF.Functions.Like(o.Charge.Space.Name, $"%{searchTerm}%")) ||
                o.Charge.Lease!.Areas.Any(area => EF.Functions.Like(area.SnapshotPath, $"%{searchTerm}%")) ||
                EF.Functions.Like(o.ChargeLineItem.ChargeType.Name, $"%{searchTerm}%") ||
                (o.Description != null && EF.Functions.Like(o.Description, $"%{searchTerm}%")));
        }

        if (tableQuery.From.HasValue) query = query.Where(o => o.PaymentDate >= tableQuery.From.Value);
        if (tableQuery.To.HasValue) query = query.Where(o => o.PaymentDate <= tableQuery.To.Value);
        if (tableQuery.PeriodFrom.HasValue) query = query.Where(o => o.Charge.PeriodStart >= tableQuery.PeriodFrom.Value);
        if (tableQuery.PeriodTo.HasValue) query = query.Where(o => o.Charge.PeriodStart <= tableQuery.PeriodTo.Value);
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
                PaymentNo = o.PaymentNo,
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
                CreatedByUserDisplayName = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null,
                SpaceName = o.Charge.Lease != null
                    ? o.Charge.Lease.Areas.Select(area => area.SnapshotPath).FirstOrDefault() ?? string.Empty
                    : o.Charge.Space != null ? o.Charge.Space.Name : string.Empty,
                StoreId = o.StoreAccount.StoreId,
                StoreName = o.StoreAccount.Store.Name
            });

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    public async Task<PaymentDetailDto?> GetDetailsAsync(
        int id,
        SpaceAccessScope? scope)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(payment => payment.Id == id),
            scope);

        return await query
            .Select(o => new PaymentDetailDto
            {
                Id = o.Id,
                PaymentNo = o.PaymentNo,
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
        SpaceAccessScope? scope)
    {
        var query = _dbSet
            .Include(payment => payment.Charge)
            .Include(payment => payment.ChargeLineItem)
            .Where(payment => payment.Id == id);

        query = ApplyScope(query, scope);

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
        SpaceAccessScope? scope = null)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(payment =>
                payment.Charge.TenantId == tenantId
                && payment.Status == PaymentStatus.Approved),
            scope);

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
        var sixMonthStart = new DateTime(input.Today.Year, input.Today.Month, 1).AddMonths(-5);
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(payment => payment.Charge.TenantId == input.TenantId),
            input.Scope);
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
        SpaceAccessScope? scope)
    {
        var tolerance = basis.Amount * policy.AmountTolerancePercent / 100m;
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.PendingApproval || payment.Status == PaymentStatus.Approved)
            .Where(payment => payment.StoreAccountId == basis.StoreAccountId)
            .Where(payment => !_ctx.PaymentMatches.Any(match => match.PaymentAllocationId == payment.Id));

        query = ApplyScope(query, scope);

        var candidates = await query.Select(payment => new PaymentCandidateDto
        {
            Id = payment.Id,
            PaymentNo = payment.PaymentNo,
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
                SpacePaths = payment.Charge.LeaseId != null
                    ? payment.Charge.Lease!.Areas.Select(area => area.Space.Path).ToList()
                    : payment.Charge.Space != null
                        ? new List<string> { payment.Charge.Space.Path }
                        : new List<string>()
            })
            .FirstOrDefaultAsync();

        return context == null
            ? null
            : new DocumentOwnerContextDto(context.TenantId, context.SpacePaths);
    }

    public Task<List<string>> GetExistingPaymentNosAsync()
        => _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(payment => payment.PaymentNo)
            .ToListAsync();

    private static IQueryable<PaymentAllocation> ApplyScope(
        IQueryable<PaymentAllocation> query,
        SpaceAccessScope? scope)
    {
        if (scope == null || scope.HasGlobalAccess)
            return query;

        var roots = scope.RootPaths.ToList();
        return query.Where(payment => payment.Charge.LeaseId != null
            ? payment.Charge.Lease!.Areas.Any(area => roots.Any(root => area.Space.Path.StartsWith(root)))
            : payment.Charge.Space != null && roots.Any(root => payment.Charge.Space.Path.StartsWith(root)));
    }

    // Dönem Bazlı blok — yalnız onaylanmış ödemeler, tahakkukun ait olduğu DÖNEM (Charge.PeriodStart)
    // eksenli (ödemenin ne zaman yapıldığına bakılmaz) — "bu yılın tahakkukundan ne kadarı ödendi".
    // Mağaza/kalem tipi filtresi burada uygulanır (Tahsilat Raporu mağaza öncelikli görünümü).
    public async Task<List<MonthlyCollectionReportRowDto>> GetCollectedByMonthAsync(
        GetMonthlyCollectionReportInput input)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Approved
                && payment.Charge.PeriodStart.Year == input.Year);

        query = ApplyScope(query, input.Scope);

        if (input.StoreId.HasValue)
            query = query.Where(payment => payment.StoreAccount.StoreId == input.StoreId.Value);

        if (input.ChargeTypeId.HasValue)
            query = query.Where(payment => payment.ChargeLineItem.ChargeTypeId == input.ChargeTypeId.Value);

        return await query
            .GroupBy(payment => payment.Charge.PeriodStart.Month)
            .Select(group => new MonthlyCollectionReportRowDto
            {
                Month = group.Key,
                CollectedAmount = group.Sum(payment => payment.Amount),
                CollectedPaymentCount = group.Count()
            })
            .ToListAsync();
    }

    // Ödenen kartı kırılımı — GetCollectedByMonthAsync ile birebir aynı taban (o yılın
    // tahakkukuna yapılan tüm ödemeler, ödeme tarihi ne olursa olsun), ama ödeme tarihinin
    // YILINA göre gruplanır — "bu yılın borcu hangi yıllarda ödenmiş" sorusuna cevap verir.
    public async Task<List<ReportYearCountDto>> GetPaymentYearBreakdownForChargeYearAsync(
        GetMonthlyCollectionReportInput input)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Approved
                && payment.Charge.PeriodStart.Year == input.Year);

        query = ApplyScope(query, input.Scope);

        if (input.StoreId.HasValue)
            query = query.Where(payment => payment.StoreAccount.StoreId == input.StoreId.Value);

        if (input.ChargeTypeId.HasValue)
            query = query.Where(payment => payment.ChargeLineItem.ChargeTypeId == input.ChargeTypeId.Value);

        var grouped = await query
            .GroupBy(payment => payment.PaymentDate.Year)
            .Select(group => new { Year = group.Key, Count = group.Count() })
            .ToListAsync();

        return grouped
            .OrderBy(item => item.Year)
            .Select(item => new ReportYearCountDto(item.Year, item.Count))
            .ToList();
    }

    // Nakit Bazlı blok kırılımı — yalnız onaylanmış ödemeler, ÖDEME TARİHİ (PaymentDate) yılı
    // seçili yıla eşit olanlar ("bu yıl yapılan tüm tahsilat"), ait oldukları tahakkukun
    // YILINA göre gruplanır — "bu yıl yapılan ödeme hangi yılların borcuna gitti" sorusuna cevap verir.
    public async Task<List<ReportYearAmountDto>> GetCashBasisByChargeYearAsync(
        GetMonthlyCollectionReportInput input)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Approved
                && payment.PaymentDate.Year == input.Year);

        query = ApplyScope(query, input.Scope);

        if (input.StoreId.HasValue)
            query = query.Where(payment => payment.StoreAccount.StoreId == input.StoreId.Value);

        if (input.ChargeTypeId.HasValue)
            query = query.Where(payment => payment.ChargeLineItem.ChargeTypeId == input.ChargeTypeId.Value);

        var grouped = await query
            .GroupBy(payment => payment.Charge.PeriodStart.Year)
            .Select(group => new { Year = group.Key, Count = group.Count(), Amount = group.Sum(payment => payment.Amount) })
            .ToListAsync();

        return grouped
            .OrderBy(item => item.Year)
            .Select(item => new ReportYearAmountDto(item.Year, item.Count, item.Amount))
            .ToList();
    }
}
