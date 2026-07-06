using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BankTransactionRepository : BaseRepository<BankTransaction>, IBankTransactionRepository
{
    public BankTransactionRepository(ApplicationDbContext ctx) : base(ctx) { }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<BankaHareketiListItemDto>> GetListAsync(BankMatchStatus? durum = null)
    {
        IQueryable<BankTransaction> q = _dbSet.AsNoTracking();
        if (durum.HasValue) q = q.Where(b => b.MatchStatus == durum.Value);
        return await q.OrderByDescending(b => b.TransactionDate)
                      .Select(b => new BankaHareketiListItemDto
                      {
                          Id = b.Id,
                          TransactionDate = b.TransactionDate,
                          TransactionAmount = b.TransactionAmount,
                          Aciklama = b.Description,
                          SenderIban = b.SenderIban,
                          SenderInfo = b.SenderInfo,
                          BankCode = b.BankCode,
                          MatchStatus = b.MatchStatus,
                      })
                      .ToListAsync();
    }

    public async Task<PagedResult<BankaHareketiListItemDto>> GetPagedListAsync(TableQuery q)
    {
        IQueryable<BankTransaction> query = _dbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(b =>
                EF.Functions.Like(b.Description, $"%{s}%") ||
                (b.SenderInfo != null && EF.Functions.Like(b.SenderInfo, $"%{s}%")) ||
                (b.SenderIban != null && EF.Functions.Like(b.SenderIban, $"%{s}%")));
        }
        if (q.From.HasValue) query = query.Where(b => b.TransactionDate >= q.From.Value);
        if (q.To.HasValue) query = query.Where(b => b.TransactionDate <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(b => b.TransactionAmount >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(b => b.TransactionAmount <= q.Max.Value);
        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            BankMatchStatus? d = q.Durum switch
            {
                "eslestirilmedi" => BankMatchStatus.Unmatched,
                "eslesti" => BankMatchStatus.Matched,
                "manuel" => BankMatchStatus.ManuallyMatched,
                _ => null
            };
            if (d.HasValue) query = query.Where(b => b.MatchStatus == d.Value);
        }

        int total = await query.CountAsync();
        var items = await query.OrderByDescending(b => b.TransactionDate)
                               .Skip(q.Skip).Take(q.Take)
                               .Select(b => new BankaHareketiListItemDto
                               {
                                   Id = b.Id,
                                   TransactionDate = b.TransactionDate,
                                   TransactionAmount = b.TransactionAmount,
                                   Aciklama = b.Description,
                                   SenderIban = b.SenderIban,
                                   SenderInfo = b.SenderInfo,
                                   BankCode = b.BankCode,
                                   MatchStatus = b.MatchStatus,
                               })
                               .ToListAsync();

        return new PagedResult<BankaHareketiListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    public async Task<BankaHareketiDetayDto?> GetDetayAsync(int id)
        => await _dbSet.AsNoTracking()
                       .Where(b => b.Id == id)
                       .Select(b => new BankaHareketiDetayDto
                       {
                           Id = b.Id,
                           TransactionDate = b.TransactionDate,
                           TransactionAmount = b.TransactionAmount,
                           Aciklama = b.Description,
                           SenderIban = b.SenderIban,
                           SenderInfo = b.SenderInfo,
                           BankCode = b.BankCode,
                           MatchStatus = b.MatchStatus,
                           Eslesmeleri = b.Matches.Select(e => new OdemeBankaEslesmeDto
                           {
                               Id = e.Id,
                               MatchType = e.MatchType,
                               BankaHareketiTutar = b.TransactionAmount,
                               BankaHareketiTarih = b.TransactionDate,
                               BankaHareketiAciklama = b.Description
                           }).ToList()
                       })
                       .FirstOrDefaultAsync();

    // ── Eşleştirme adayları ───────────────────────────────────────────────
    public async Task<List<OdemeAdayDto>> GetOdemeAdaylariAsync(int bankaHareketiId, IReadOnlyList<int>? tasinmazIds = null)
    {
        var hareketi = await _dbSet.AsNoTracking()
                                   .Where(b => b.Id == bankaHareketiId)
                                   .Select(b => new { b.TransactionAmount, b.TransactionDate })
                                   .FirstOrDefaultAsync();
        if (hareketi == null) return [];

        decimal tutar = hareketi.TransactionAmount;
        DateTime tarih = hareketi.TransactionDate;
        decimal tolerans = tutar * 0.02m;

        IQueryable<PaymentAllocation> query = _ctx.PaymentAllocations.AsNoTracking()
            .Where(o => o.Status == PaymentStatus.PendingApproval || o.Status == PaymentStatus.Approved)
            .Where(o => !_ctx.PaymentMatches.Any(e => e.PaymentAllocationId == o.Id && e.BankTransactionId == bankaHareketiId));

        if (tasinmazIds != null)
        {
            var ids = tasinmazIds.ToList();
            query = query.Where(o => ids.Contains(o.Charge.Unit.PropertyId));
        }

        var liste = await query.Select(o => new OdemeAdayDto
        {
            Id = o.Id,
            Amount = o.Amount,
            PaymentDate = o.PaymentDate,
            Durum = o.Status,
            KiraciGosterimAdi = o.Charge.Tenant.Name,
            PeriodStart = o.Charge.PeriodStart
        }).ToListAsync();

        return liste.OrderBy(o =>
        {
            bool tutarExact = o.Amount == tutar;
            bool tutarClose = Math.Abs(o.Amount - tutar) <= tolerans;
            int gunFark = Math.Abs((o.PaymentDate - tarih).Days);
            if (tutarExact && gunFark <= 15) return 0;
            if (tutarExact) return 1;
            if (tutarClose && gunFark <= 15) return 2;
            return 3;
        })
        .ThenBy(o => Math.Abs((o.PaymentDate - tarih).Days))
        .ThenBy(o => Math.Abs(o.Amount - tutar))
        .ToList();
    }

    public async Task<List<BankaHareketiListItemDto>> GetHareketAdaylariAsync(int odemeId)
    {
        var payment = await _ctx.PaymentAllocations.AsNoTracking()
                              .Where(o => o.Id == odemeId)
                              .Select(o => new { o.Amount, o.PaymentDate })
                              .FirstOrDefaultAsync();
        if (payment == null) return [];

        decimal tutar = payment.Amount;
        DateTime tarih = payment.PaymentDate;
        decimal tolerans = tutar * 0.02m;

        var liste = await _dbSet.AsNoTracking()
            .Where(b => b.MatchStatus == BankMatchStatus.Unmatched)
            .Where(b => !_ctx.PaymentMatches.Any(e => e.BankTransactionId == b.Id && e.PaymentAllocationId == odemeId))
            .Select(b => new BankaHareketiListItemDto
            {
                Id = b.Id,
                TransactionDate = b.TransactionDate,
                TransactionAmount = b.TransactionAmount,
                Aciklama = b.Description,
                SenderIban = b.SenderIban,
                SenderInfo = b.SenderInfo,
                BankCode = b.BankCode,
                MatchStatus = b.MatchStatus,
            })
            .ToListAsync();

        return liste.OrderBy(b =>
        {
            bool tutarExact = b.TransactionAmount == tutar;
            bool tutarClose = Math.Abs(b.TransactionAmount - tutar) <= tolerans;
            int gunFark = Math.Abs((b.TransactionDate - tarih).Days);
            if (tutarExact && gunFark <= 15) return 0;
            if (tutarExact) return 1;
            if (tutarClose && gunFark <= 15) return 2;
            return 3;
        })
        .ThenBy(b => Math.Abs((b.TransactionDate - tarih).Days))
        .ThenBy(b => Math.Abs(b.TransactionAmount - tutar))
        .ToList();
    }

    // ── Eşleştirme yazma işlemleri ────────────────────────────────────────
    public Task<bool> EslesmeVarMiAsync(int kiraOdemeId, int bankaHareketiId)
        => _ctx.PaymentMatches.AsNoTracking()
               .AnyAsync(e => e.PaymentAllocationId == kiraOdemeId && e.BankTransactionId == bankaHareketiId);

    public async Task AddEslesmeAsync(PaymentMatch eslesme)
        => await _ctx.PaymentMatches.AddAsync(eslesme);

    public Task<PaymentMatch?> GetEslesmeWithBankaHareketiAsync(int eslesmeId)
        => _ctx.PaymentMatches
               .Include(e => e.BankTransaction)
               .FirstOrDefaultAsync(e => e.Id == eslesmeId);

    public Task RemoveEslesmeAsync(PaymentMatch eslesme)
    {
        _ctx.PaymentMatches.Remove(eslesme);
        return Task.CompletedTask;
    }

    public Task<bool> KalanEslesmeVarMiAsync(int bankaHareketiId, int excludeEslesmeId)
        => _ctx.PaymentMatches.AsNoTracking()
               .AnyAsync(e => e.BankTransactionId == bankaHareketiId && e.Id != excludeEslesmeId);

    // ── Toplu ekleme (CSV import) ─────────────────────────────────────────
    public async Task AddRangeAsync(IEnumerable<BankTransaction> entities)
        => await _ctx.BankTransactions.AddRangeAsync(entities);
}
