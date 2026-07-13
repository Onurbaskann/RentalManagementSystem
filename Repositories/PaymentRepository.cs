using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PaymentRepository : BaseRepository<PaymentAllocation>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<OdemeListItemDto>> GetListAsync(int? tahakkukId, List<int>? yetkiliPropertyIds)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.ChargeId == tahakkukId.Value);

        if (yetkiliPropertyIds != null)
            query = query.Where(o => yetkiliPropertyIds.Contains(o.Charge.Unit.PropertyId));

        return await query
            .OrderByDescending(o => o.EntryDate)
            .Select(o => new OdemeListItemDto
            {
                Id = o.Id,
                ChargeId = o.ChargeId,
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
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            })
            .ToListAsync();
    }

    public async Task<PagedResult<OdemeListItemDto>> GetPagedListAsync(TableQuery q, int? tahakkukId, List<int>? yetkiliPropertyIds)
    {
        IQueryable<PaymentAllocation> query = _dbSet.AsNoTracking();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.ChargeId == tahakkukId.Value);

        if (yetkiliPropertyIds != null)
            query = query.Where(o => yetkiliPropertyIds.Contains(o.Charge.Unit.PropertyId));

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Charge.Tenant.Name, $"%{s}%") ||
                (o.Description != null && EF.Functions.Like(o.Description, $"%{s}%")));
        }

        if (q.From.HasValue) query = query.Where(o => o.PaymentDate >= q.From.Value);
        if (q.To.HasValue) query = query.Where(o => o.PaymentDate <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(o => o.Amount >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(o => o.Amount <= q.Max.Value);

        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status != "tum")
        {
            PaymentStatus? d = q.Status switch
            {
                "onaybekliyor" => PaymentStatus.PendingApproval,
                "onaylandi" => PaymentStatus.Approved,
                "reddedildi" => PaymentStatus.Rejected,
                _ => null
            };
            if (d.HasValue) query = query.Where(o => o.Status == d.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.EntryDate)
            .Skip(q.Skip).Take(q.Take)
            .Select(o => new OdemeListItemDto
            {
                Id = o.Id,
                ChargeId = o.ChargeId,
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
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            })
            .ToListAsync();

        return new PagedResult<OdemeListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    public async Task<PaymentDetailDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new PaymentDetailDto
            {
                Id = o.Id,
                ChargeId = o.ChargeId,
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
                PropertyId = o.Charge.Lease != null && o.Charge.Lease.Unit != null ? (int?)o.Charge.Lease.Unit.PropertyId : null,
                TenantDisplayName = o.Charge.Tenant.Name,
                ChargePeriodStart = o.Charge.PeriodStart,
                CreatedByUserDisplayName = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null,
                ApprovedByUserDisplayName = o.OnaylayanUser != null ? o.OnaylayanUser.AdSoyad : null,
                BankMatches = o.BankMatches.Select(e => new OdemeBankaEslesmeDto
                {
                    Id = e.Id,
                    MatchType = e.MatchType,
                    BankaHareketiTutar = e.BankTransaction.TransactionAmount,
                    BankaHareketiTarih = e.BankTransaction.TransactionDate,
                    BankaHareketiAciklama = e.BankTransaction.Description ?? string.Empty
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
