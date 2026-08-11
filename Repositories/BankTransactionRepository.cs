using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BankTransactionRepository : RepositoryBase<BankTransaction>, IBankTransactionRepository
{
    public BankTransactionRepository(ApplicationDbContext ctx) : base(ctx) { }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<BankTransactionListItemDto>> GetListAsync(BankMatchStatus? status = null)
    {
        IQueryable<BankTransaction> query = _dbSet.AsNoTracking();
        if (status.HasValue) query = query.Where(transaction => transaction.MatchStatus == status.Value);
        return await query.OrderByDescending(transaction => transaction.TransactionDate)
                      .Select(transaction => new BankTransactionListItemDto
                      {
                          Id = transaction.Id,
                          TransactionDate = transaction.TransactionDate,
                          TransactionAmount = transaction.TransactionAmount,
                          Description = transaction.Description,
                          SenderIban = transaction.SenderIban,
                          SenderInfo = transaction.SenderInfo,
                          BankCode = transaction.BankCode,
                          MatchStatus = transaction.MatchStatus,
                      })
                      .ToListAsync();
    }

    public async Task<PagedResult<BankTransactionListItemDto>> GetPagedListAsync(TableQuery tableQuery)
    {
        IQueryable<BankTransaction> query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(transaction =>
                EF.Functions.Like(transaction.Description, $"%{search}%") ||
                (transaction.SenderInfo != null && EF.Functions.Like(transaction.SenderInfo, $"%{search}%")) ||
                (transaction.SenderIban != null && EF.Functions.Like(transaction.SenderIban, $"%{search}%")));
        }
        if (tableQuery.From.HasValue) query = query.Where(transaction => transaction.TransactionDate >= tableQuery.From.Value);
        if (tableQuery.To.HasValue) query = query.Where(transaction => transaction.TransactionDate <= tableQuery.To.Value);
        if (tableQuery.Min.HasValue) query = query.Where(transaction => transaction.TransactionAmount >= tableQuery.Min.Value);
        if (tableQuery.Max.HasValue) query = query.Where(transaction => transaction.TransactionAmount <= tableQuery.Max.Value);
        if (!string.IsNullOrWhiteSpace(tableQuery.Status) && tableQuery.Status != "tum")
        {
            BankMatchStatus? status = tableQuery.Status switch
            {
                "eslestirilmedi" => BankMatchStatus.Unmatched,
                "eslesti" => BankMatchStatus.Matched,
                "manuel" => BankMatchStatus.ManuallyMatched,
                _ => null
            };
            if (status.HasValue) query = query.Where(transaction => transaction.MatchStatus == status.Value);
        }

        var itemsQuery = query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .Select(transaction => new BankTransactionListItemDto
            {
                Id = transaction.Id,
                TransactionDate = transaction.TransactionDate,
                TransactionAmount = transaction.TransactionAmount,
                Description = transaction.Description,
                SenderIban = transaction.SenderIban,
                SenderInfo = transaction.SenderInfo,
                BankCode = transaction.BankCode,
                MatchStatus = transaction.MatchStatus,
            });

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    public async Task<BankTransactionDetailDto?> GetDetailAsync(int id)
        => await _dbSet.AsNoTracking()
                       .Where(transaction => transaction.Id == id)
                       .Select(transaction => new BankTransactionDetailDto
                       {
                           Id = transaction.Id,
                           TransactionDate = transaction.TransactionDate,
                           TransactionAmount = transaction.TransactionAmount,
                           Description = transaction.Description,
                           SenderIban = transaction.SenderIban,
                           SenderInfo = transaction.SenderInfo,
                           BankCode = transaction.BankCode,
                           MatchStatus = transaction.MatchStatus,
                           Matches = transaction.Matches.Select(match => new PaymentBankMatchDto
                           {
                               Id = match.Id,
                               MatchType = match.MatchType,
                               BankTransactionAmount = transaction.TransactionAmount,
                               BankTransactionDate = transaction.TransactionDate,
                               BankTransactionDescription = transaction.Description
                           }).ToList()
                       })
                       .FirstOrDefaultAsync();

    // ── Eşleştirme adayları ───────────────────────────────────────────────
    public Task<PaymentMatchingBasisDto?> GetMatchingBasisAsync(int bankTransactionId)
        => _dbSet.AsNoTracking()
            .Where(transaction => transaction.Id == bankTransactionId)
            .Select(transaction => new PaymentMatchingBasisDto(transaction.TransactionAmount, transaction.TransactionDate))
            .FirstOrDefaultAsync();

    public async Task<List<BankTransactionListItemDto>> GetTransactionCandidatesAsync(PaymentMatchingBasisDto basis)
    {
        var amount = basis.Amount;
        var date = basis.Date;
        var tolerance = amount * 0.02m;

        var candidates = await _dbSet.AsNoTracking()
            .Where(transaction => transaction.MatchStatus == BankMatchStatus.Unmatched)
            .Where(transaction => !_ctx.PaymentMatches.Any(match => match.BankTransactionId == transaction.Id))
            .Select(transaction => new BankTransactionListItemDto
            {
                Id = transaction.Id,
                TransactionDate = transaction.TransactionDate,
                TransactionAmount = transaction.TransactionAmount,
                Description = transaction.Description,
                SenderIban = transaction.SenderIban,
                SenderInfo = transaction.SenderInfo,
                BankCode = transaction.BankCode,
                MatchStatus = transaction.MatchStatus,
            })
            .ToListAsync();

        return candidates.OrderBy(transaction =>
        {
            var isExactAmount = transaction.TransactionAmount == amount;
            var isCloseAmount = Math.Abs(transaction.TransactionAmount - amount) <= tolerance;
            var dayDifference = Math.Abs((transaction.TransactionDate - date).Days);
            if (isExactAmount && dayDifference <= 15) return 0;
            if (isExactAmount) return 1;
            if (isCloseAmount && dayDifference <= 15) return 2;
            return 3;
        })
        .ThenBy(transaction => Math.Abs((transaction.TransactionDate - date).Days))
        .ThenBy(transaction => Math.Abs(transaction.TransactionAmount - amount))
        .ToList();
    }

    // ── Eşleştirme yazma işlemleri ────────────────────────────────────────
    // ── Toplu ekleme (CSV import) ─────────────────────────────────────────
    public async Task AddRangeAsync(IEnumerable<BankTransaction> entities)
        => await _ctx.BankTransactions.AddRangeAsync(entities);
}
