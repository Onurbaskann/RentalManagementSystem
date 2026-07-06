using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ChargeRepository : BaseRepository<Charge>, IChargeRepository
{
    public ChargeRepository(ApplicationDbContext ctx) : base(ctx) { }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking();

        if (sozlesmeId.HasValue)
            q = q.Where(t => t.LeaseId == sozlesmeId.Value);

        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.UnitId));
        else if (yetkiliTasinmazIds != null)
            q = q.Where(t => yetkiliTasinmazIds.Contains(t.Unit.PropertyId));

        return await q.OrderByDescending(t => t.PeriodStart)
                      .Select(t => new TahakkukListItemDto
                      {
                          Id = t.Id,
                          LeaseId = t.LeaseId,
                          KiraciId = t.TenantId,
                          KiraciGosterimAdi = t.Tenant.Name,
                          TasinmazId = t.Unit.PropertyId,
                          TasinmazAd = t.Unit.Property.Name,
                          BirimId = t.UnitId,
                          BirimAd = t.Unit.Name,
                          PeriodStart = t.PeriodStart,
                          DueDate = t.DueDate,
                          ToplamTutar = t.TotalAmount,
                          PaidAmount = t.PaidAmount,
                          Durum = t.Status,
                          SourceType = t.SourceType,
                          BekleyenOdemeSayisi = _ctx.PaymentAllocations.IgnoreQueryFilters()
                              .Count(o => o.ChargeId == t.Id && !o.IsDeleted && o.Status == PaymentStatus.PendingApproval),
                          LineItems = t.LineItems.Select(k => new TahakkukKalemDto
                          {
                              ChargeTypeCode = k.ChargeType.Code,
                              BorcTipiSira = k.ChargeType.SortOrder,
                              ChargeTypeName = k.ChargeType.Name,
                              Aciklama = k.Description,
                              CalculationMethod = k.CalculationMethod,
                              UnitValue = k.UnitValue,
                              Multiplier = k.Multiplier,
                              Amount = k.Amount,
                              KdvRate = k.KdvRate,
                              KdvTutari = k.KdvAmount,
                              ToplamTutar = k.TotalAmount,
                              SourceType = k.SourceType
                          }).ToList()
                      })
                      .ToListAsync();
    }

    // ── Sayfalı listeleme (DTO) ───────────────────────────────────────────
    public async Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Charge> query = _dbSet.AsNoTracking();

        if (sozlesmeId.HasValue)
            query = query.Where(t => t.LeaseId == sozlesmeId.Value);

        if (yetkiliBirimIds != null)
            query = query.Where(t => yetkiliBirimIds.Contains(t.UnitId));
        else if (yetkiliTasinmazIds != null)
            query = query.Where(t => yetkiliTasinmazIds.Contains(t.Unit.PropertyId));

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(t => EF.Functions.Like(t.Tenant.Name, $"%{s}%") ||
                                     (t.Lease != null && EF.Functions.Like(t.Lease.Unit.Property.Name, $"%{s}%")));
        }

        if (q.From.HasValue) query = query.Where(t => t.DueDate >= q.From.Value);
        if (q.To.HasValue) query = query.Where(t => t.DueDate <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(t => t.TotalAmount >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(t => t.TotalAmount <= q.Max.Value);
        if (q.TasinmazId.HasValue) query = query.Where(t => t.Lease!.Unit.PropertyId == q.TasinmazId.Value);
        if (q.BirimId.HasValue) query = query.Where(t => t.Lease!.UnitId == q.BirimId.Value);
        if (q.KiraciId.HasValue) query = query.Where(t => t.TenantId == q.KiraciId.Value);
        if (q.Yil.HasValue) query = query.Where(t => t.PeriodStart.Year == q.Yil.Value);

        if (!string.IsNullOrWhiteSpace(q.Kaynak))
        {
            ChargeSourceType? kt = q.Kaynak.ToLower() switch
            {
                "manuel" => ChargeSourceType.Manual,
                "sozlesme" => ChargeSourceType.Lease,
                "rezervasyon" => ChargeSourceType.Reservation,
                _ => null
            };
            if (kt.HasValue) query = query.Where(t => t.SourceType == kt.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            if (q.Durum == "odeme_onay")
            {
                query = query.Where(t => t.Allocations.Any(o =>
                    o.Status == PaymentStatus.PendingApproval &&
                    o.PaymentSourceType != PaymentSourceType.VirtualPos));
            }
            else if (q.Durum == "iptal")
            {
                query = query.Where(t => t.Status == ChargeStatus.Cancelled);
            }
            else
            {
                ChargeStatus? d = q.Durum.ToLower() switch
                {
                    "bekliyor" => ChargeStatus.Pending,
                    "kismi" => ChargeStatus.PartiallyPaid,
                    "tamodendi" => ChargeStatus.Paid,
                    "gecikti" => ChargeStatus.Overdue,
                    _ => null
                };
                if (d.HasValue) query = query.Where(t => t.Status == d.Value);
            }
        }
        else
        {
            query = query.Where(t => t.Status != ChargeStatus.Cancelled);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.PeriodStart)
                               .Skip(q.Skip).Take(q.Take)
                               .Select(t => new TahakkukListItemDto
                               {
                                   Id = t.Id,
                                   LeaseId = t.LeaseId,
                                   KiraciId = t.TenantId,
                                   KiraciGosterimAdi = t.Tenant.Name,
                                   TasinmazId = t.Unit.PropertyId,
                                   TasinmazAd = t.Unit.Property.Name,
                                   BirimId = t.UnitId,
                                   BirimAd = t.Unit.Name,
                                   PeriodStart = t.PeriodStart,
                                   DueDate = t.DueDate,
                                   ToplamTutar = t.TotalAmount,
                                   PaidAmount = t.PaidAmount,
                                   Durum = t.Status,
                                   SourceType = t.SourceType,
                                   BekleyenOdemeSayisi = t.Allocations.Count(o => o.Status == PaymentStatus.PendingApproval),
                                   LineItems = t.LineItems.Select(k => new TahakkukKalemDto
                                   {
                                       ChargeTypeCode = k.ChargeType.Code,
                                       BorcTipiSira = k.ChargeType.SortOrder,
                                       ChargeTypeName = k.ChargeType.Name,
                                       Aciklama = k.Description,
                                       CalculationMethod = k.CalculationMethod,
                                       UnitValue = k.UnitValue,
                                       Multiplier = k.Multiplier,
                                       Amount = k.Amount,
                                       KdvRate = k.KdvRate,
                                       KdvTutari = k.KdvAmount,
                                       ToplamTutar = k.TotalAmount,
                                       SourceType = k.SourceType
                                   }).ToList()
                               })
                               .ToListAsync();

        return new PagedResult<TahakkukListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    // ── Detay (DTO) ───────────────────────────────────────────────────────
    public async Task<TahakkukDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
                           .Where(t => t.Id == id)
                           .Select(t => new TahakkukDetayDto
                           {
                               Id = t.Id,
                               LeaseId = t.LeaseId,
                               KiraciId = t.TenantId,
                               KiraciGosterimAdi = t.Tenant.Name,
                               TasinmazId = t.Unit.PropertyId,
                               TasinmazAd = t.Unit.Property.Name,
                               BirimId = t.UnitId,
                               BirimAd = t.Unit.Name,
                               PeriodStart = t.PeriodStart,
                               PeriodEnd = t.PeriodEnd,
                               DueDate = t.DueDate,
                               ExpectedAmount = t.ExpectedAmount,
                               KdvTutari = t.KdvAmount,
                               ToplamTutar = t.TotalAmount,
                               PaidAmount = t.PaidAmount,
                               Durum = t.Status,
                               SourceType = t.SourceType,
                               OlusturmaTarihi = t.CreatedAt,
                               LineItems = t.LineItems.Select(k => new TahakkukKalemDto
                               {
                                   ChargeTypeCode = k.ChargeType.Code,
                                   BorcTipiSira = k.ChargeType.SortOrder,
                                   ChargeTypeName = k.ChargeType.Name,
                                   Aciklama = k.Description,
                                   CalculationMethod = k.CalculationMethod,
                                   UnitValue = k.UnitValue,
                                   Multiplier = k.Multiplier,
                                   Amount = k.Amount,
                                   KdvRate = k.KdvRate,
                                   KdvTutari = k.KdvAmount,
                                   ToplamTutar = k.TotalAmount,
                                   SourceType = k.SourceType
                               }).ToList(),
                               Allocations = t.Allocations.Select(o => new TahakkukOdemeDto
                               {
                                   Id = o.Id,
                                   PaymentDate = o.PaymentDate,
                                   Amount = o.Amount,
                                   PaymentChannel = o.PaymentChannel,
                                   Durum = o.Status,
                                   EntryDate = o.EntryDate,
                                   Aciklama = o.Description,
                                   RejectionReason = o.RejectionReason
                               }).ToList()
                           })
                           .FirstOrDefaultAsync();
    }

    // ── Manuel Borç — DTO ─────────────────────────────────────────────────
    public async Task<List<ManuelBorcListItemDto>> GetManuelBorcListAsync(List<int>? yetkiliTasinmazIds, string? durum = null, string? baglanti = null, int? sozlesmeId = null, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking()
            .Where(t => t.SourceType == ChargeSourceType.Manual);

        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.UnitId));
        else if (yetkiliTasinmazIds != null)
            q = q.Where(t => yetkiliTasinmazIds.Contains(t.Unit.PropertyId));

        if (sozlesmeId.HasValue)
            q = q.Where(t => t.LeaseId == sozlesmeId.Value);

        if (!string.IsNullOrWhiteSpace(baglanti))
        {
            if (baglanti == "sozlesmeli") q = q.Where(t => t.LeaseId != null);
            else if (baglanti == "sozlesmesiz") q = q.Where(t => t.LeaseId == null);
        }

        if (!string.IsNullOrWhiteSpace(durum) && durum != "tum")
        {
            if (durum == "iptal")
                q = q.Where(t => t.Status == ChargeStatus.Cancelled);
            else
            {
                q = q.Where(t => t.Status != ChargeStatus.Cancelled);
                ChargeStatus? d = durum switch
                {
                    "bekliyor"  => ChargeStatus.Pending,
                    "kismi"     => ChargeStatus.PartiallyPaid,
                    "tamodendi" => ChargeStatus.Paid,
                    "gecikti"   => ChargeStatus.Overdue,
                    _           => null
                };
                if (d.HasValue) q = q.Where(t => t.Status == d.Value);
            }
        }
        else
        {
            q = q.Where(t => t.Status != ChargeStatus.Cancelled);
        }

        return await q.OrderByDescending(t => t.CreatedAt)
                      .Select(t => new ManuelBorcListItemDto
                      {
                          Id = t.Id,
                          LeaseId = t.LeaseId,
                          KiraciId = t.TenantId,
                          KiraciKategoriAd = t.Tenant.TenantCategory != null ? t.Tenant.TenantCategory.Ad : null,
                          KiraciGosterimAdi = t.Tenant.Name,
                          TasinmazAd = t.Unit.Property.Name,
                          BirimAd = t.Unit.Name,
                          ChargeTypeCode = t.LineItems
                              .OrderBy(k => k.ChargeType.SortOrder)
                              .Select(k => k.ChargeType.Code)
                              .FirstOrDefault(),
                          IlkKalemAciklama = t.LineItems
                              .OrderBy(k => k.ChargeType.SortOrder)
                              .Select(k => k.Description)
                              .FirstOrDefault(),
                          ExpectedAmount = t.ExpectedAmount,
                          KdvTutari = t.KdvAmount,
                          ToplamTutar = t.TotalAmount,
                          PaidAmount = t.PaidAmount,
                          DueDate = t.DueDate,
                          Durum = t.Status,
                          CancellationNote = t.CancellationNote
                      })
                      .ToListAsync();
    }

    public async Task<int> GetManuelBorcIptalSayisiAsync(List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking()
            .Where(t => t.SourceType == ChargeSourceType.Manual && t.Status == ChargeStatus.Cancelled);
        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.UnitId));
        else if (yetkiliTasinmazIds != null)
            q = q.Where(t => yetkiliTasinmazIds.Contains(t.Unit.PropertyId));
        return await q.CountAsync();
    }

    // ── Business logic — entity döner ─────────────────────────────────────
    public async Task<Charge?> GetManuelBorcByIdAsync(int id)
    {
        return await _dbSet
            .Include(t => t.Allocations)
            .FirstOrDefaultAsync(t => t.Id == id && t.SourceType == ChargeSourceType.Manual);
    }

    public async Task<List<Charge>> GetGeciktirileceklerAsync(DateTime bugun)
    {
        return await _dbSet.Where(t => t.Status != ChargeStatus.Paid &&
                                       t.Status != ChargeStatus.Cancelled &&
                                       t.DueDate < bugun)
                           .ToListAsync();
    }

    public async Task<List<Charge>> GetBekleyenBorclarAsync(DateTime limitVade, CancellationToken ct)
        => await _dbSet
            .Include(t => t.Tenant)
            .Include(t => t.Lease!).ThenInclude(s => s!.Unit).ThenInclude(b => b.Property)
            .Include(t => t.Allocations)
            .Where(t => t.Status != ChargeStatus.Paid
                     && t.Status != ChargeStatus.Cancelled
                     && t.DueDate <= limitVade)
            .ToListAsync(ct);

    // ── Hesaplama ─────────────────────────────────────────────────────────
    public async Task<decimal> GetOdenenTutarAsync(int tahakkukId)
    {
        return await _ctx.PaymentAllocations.AsNoTracking()
                                      .Where(o => o.ChargeId == tahakkukId && o.Status == PaymentStatus.Approved)
                                      .SumAsync(o => (decimal?)o.Amount) ?? 0m;
    }

    // ── Üretim yardımcıları ───────────────────────────────────────────────
    public async Task<List<ChargeType>> GetAktifUretimBorcTipleriAsync()
        => await _ctx.ChargeTypes.AsNoTracking()
                                 .Where(b => b.IsActive && (b.Behavior == ChargeTypeBehavior.MonthlyFixed || b.Behavior == ChargeTypeBehavior.FirstMonthOneTime))
                                 .OrderBy(b => b.SortOrder)
                                 .ToListAsync();

    public async Task<List<Charge>> GetSilineceklerAsync(int sozlesmeId, DateTime ilkGun)
        => await _dbSet.Where(t => t.LeaseId == sozlesmeId
                                && t.PeriodStart >= ilkGun
                                && t.Status != ChargeStatus.Paid
                                && t.SourceType == ChargeSourceType.Lease
                                && !_ctx.PaymentAllocations.Any(o => o.ChargeId == t.Id))
                       .ToListAsync();

    public Task DeleteRangeAsync(IEnumerable<Charge> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
}
