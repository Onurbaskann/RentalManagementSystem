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
    public async Task<List<ChargeListItemDto>> GetListAsync(int? leaseId, List<int>? authorizedPropertyIds, List<int>? authorizedUnitIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking();

        if (leaseId.HasValue)
            q = q.Where(t => t.LeaseId == leaseId.Value);

        if (authorizedUnitIds != null)
            q = q.Where(t => authorizedUnitIds.Contains(t.UnitId));
        else if (authorizedPropertyIds != null)
            q = q.Where(t => authorizedPropertyIds.Contains(t.Unit.PropertyId));

        return await q.OrderByDescending(t => t.PeriodStart)
                      .Select(t => new ChargeListItemDto
                      {
                          Id = t.Id,
                          LeaseId = t.LeaseId,
                          TenantId = t.TenantId,
                          TenantDisplayName = t.Tenant.Name,
                          PropertyId = t.Unit.PropertyId,
                          PropertyName = t.Unit.Property.Name,
                          UnitId = t.UnitId,
                          UnitName = t.Unit.Name,
                          PeriodStart = t.PeriodStart,
                          DueDate = t.DueDate,
                          TotalAmount = t.TotalAmount,
                          PaidAmount = t.PaidAmount,
                          Status = t.Status,
                          SourceType = t.SourceType,
                          PendingPaymentCount = _ctx.PaymentAllocations.IgnoreQueryFilters()
                              .Count(o => o.ChargeId == t.Id && !o.IsDeleted && o.Status == PaymentStatus.PendingApproval),
                          LineItems = t.LineItems.Select(k => new ChargeLineItemDto
                          {
                              ChargeTypeCode = k.ChargeType.Code,
                              ChargeTypeSortOrder = k.ChargeType.SortOrder,
                              ChargeTypeName = k.ChargeType.Name,
                              Description = k.Description,
                              CalculationMethod = k.CalculationMethod,
                              UnitValue = k.UnitValue,
                              Multiplier = k.Multiplier,
                              Amount = k.Amount,
                              KdvRate = k.KdvRate,
                              VatAmount = k.KdvAmount,
                              TotalAmount = k.TotalAmount,
                              SourceType = k.SourceType
                          }).ToList()
                      })
                      .ToListAsync();
    }

    // ── Sayfalı listeleme (DTO) ───────────────────────────────────────────
    public async Task<PagedResult<ChargeListItemDto>> GetPagedListAsync(TableQuery q, int? leaseId, List<int>? authorizedPropertyIds, List<int>? authorizedUnitIds = null)
    {
        IQueryable<Charge> query = _dbSet.AsNoTracking();

        if (leaseId.HasValue)
            query = query.Where(t => t.LeaseId == leaseId.Value);

        if (authorizedUnitIds != null)
            query = query.Where(t => authorizedUnitIds.Contains(t.UnitId));
        else if (authorizedPropertyIds != null)
            query = query.Where(t => authorizedPropertyIds.Contains(t.Unit.PropertyId));

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
        if (q.PropertyId.HasValue) query = query.Where(t => t.Lease!.Unit.PropertyId == q.PropertyId.Value);
        if (q.UnitId.HasValue) query = query.Where(t => t.Lease!.UnitId == q.UnitId.Value);
        if (q.TenantId.HasValue) query = query.Where(t => t.TenantId == q.TenantId.Value);
        if (q.Year.HasValue) query = query.Where(t => t.PeriodStart.Year == q.Year.Value);

        if (!string.IsNullOrWhiteSpace(q.Source))
        {
            ChargeSourceType? kt = q.Source.ToLower() switch
            {
                "manuel" => ChargeSourceType.Manual,
                "lease" => ChargeSourceType.Lease,
                "reservation" => ChargeSourceType.Reservation,
                _ => null
            };
            if (kt.HasValue) query = query.Where(t => t.SourceType == kt.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status != "tum")
        {
            if (q.Status == "odeme_onay")
            {
                query = query.Where(t => t.Allocations.Any(o =>
                    o.Status == PaymentStatus.PendingApproval &&
                    o.PaymentSourceType != PaymentSourceType.VirtualPos));
            }
            else if (q.Status == "iptal")
            {
                query = query.Where(t => t.Status == ChargeStatus.Cancelled);
            }
            else
            {
                ChargeStatus? d = q.Status.ToLower() switch
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
                               .Select(t => new ChargeListItemDto
                               {
                                   Id = t.Id,
                                   LeaseId = t.LeaseId,
                                   TenantId = t.TenantId,
                                   TenantDisplayName = t.Tenant.Name,
                                   PropertyId = t.Unit.PropertyId,
                                   PropertyName = t.Unit.Property.Name,
                                   UnitId = t.UnitId,
                                   UnitName = t.Unit.Name,
                                   PeriodStart = t.PeriodStart,
                                   DueDate = t.DueDate,
                                   TotalAmount = t.TotalAmount,
                                   PaidAmount = t.PaidAmount,
                                   Status = t.Status,
                                   SourceType = t.SourceType,
                                   PendingPaymentCount = t.Allocations.Count(o => o.Status == PaymentStatus.PendingApproval),
                                   LineItems = t.LineItems.Select(k => new ChargeLineItemDto
                                   {
                                       ChargeTypeCode = k.ChargeType.Code,
                                       ChargeTypeSortOrder = k.ChargeType.SortOrder,
                                       ChargeTypeName = k.ChargeType.Name,
                                       Description = k.Description,
                                       CalculationMethod = k.CalculationMethod,
                                       UnitValue = k.UnitValue,
                                       Multiplier = k.Multiplier,
                                       Amount = k.Amount,
                                       KdvRate = k.KdvRate,
                                       VatAmount = k.KdvAmount,
                                       TotalAmount = k.TotalAmount,
                                       SourceType = k.SourceType
                                   }).ToList()
                               })
                               .ToListAsync();

        return new PagedResult<ChargeListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    // ── Detay (DTO) ───────────────────────────────────────────────────────
    public async Task<ChargeDetailDto?> GetDetailsAsync(int id)
    {
        return await _dbSet.AsNoTracking()
                           .Where(t => t.Id == id)
                           .Select(t => new ChargeDetailDto
                           {
                               Id = t.Id,
                               LeaseId = t.LeaseId,
                               TenantId = t.TenantId,
                               TenantDisplayName = t.Tenant.Name,
                               PropertyId = t.Unit.PropertyId,
                               PropertyName = t.Unit.Property.Name,
                               UnitId = t.UnitId,
                               UnitName = t.Unit.Name,
                               PeriodStart = t.PeriodStart,
                               PeriodEnd = t.PeriodEnd,
                               DueDate = t.DueDate,
                               ExpectedAmount = t.ExpectedAmount,
                               VatAmount = t.KdvAmount,
                               TotalAmount = t.TotalAmount,
                               PaidAmount = t.PaidAmount,
                               Status = t.Status,
                               SourceType = t.SourceType,
                               CreatedAt = t.CreatedAt,
                               LineItems = t.LineItems.Select(k => new ChargeLineItemDto
                               {
                                   ChargeTypeCode = k.ChargeType.Code,
                                   ChargeTypeSortOrder = k.ChargeType.SortOrder,
                                   ChargeTypeName = k.ChargeType.Name,
                                   Description = k.Description,
                                   CalculationMethod = k.CalculationMethod,
                                   UnitValue = k.UnitValue,
                                   Multiplier = k.Multiplier,
                                   Amount = k.Amount,
                                   KdvRate = k.KdvRate,
                                   VatAmount = k.KdvAmount,
                                   TotalAmount = k.TotalAmount,
                                   SourceType = k.SourceType
                               }).ToList(),
                               Allocations = t.Allocations.Select(o => new PaymentAllocationDto
                               {
                                   Id = o.Id,
                                   PaymentDate = o.PaymentDate,
                                   Amount = o.Amount,
                                   PaymentChannel = o.PaymentChannel,
                                   Status = o.Status,
                                   EntryDate = o.EntryDate,
                                   Description = o.Description,
                                   RejectionReason = o.RejectionReason
                               }).ToList()
                           })
                           .FirstOrDefaultAsync();
    }

    // ── Manuel Borç — DTO ─────────────────────────────────────────────────
    public async Task<List<ManuelBorcListItemDto>> GetManuelBorcListAsync(List<int>? yetkiliPropertyIds, string? durum = null, string? baglanti = null, int? leaseId = null, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking()
            .Where(t => t.SourceType == ChargeSourceType.Manual);

        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.UnitId));
        else if (yetkiliPropertyIds != null)
            q = q.Where(t => yetkiliPropertyIds.Contains(t.Unit.PropertyId));

        if (leaseId.HasValue)
            q = q.Where(t => t.LeaseId == leaseId.Value);

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

    public async Task<int> GetManuelBorcIptalSayisiAsync(List<int>? yetkiliPropertyIds, List<int>? yetkiliBirimIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking()
            .Where(t => t.SourceType == ChargeSourceType.Manual && t.Status == ChargeStatus.Cancelled);
        if (yetkiliBirimIds != null)
            q = q.Where(t => yetkiliBirimIds.Contains(t.UnitId));
        else if (yetkiliPropertyIds != null)
            q = q.Where(t => yetkiliPropertyIds.Contains(t.Unit.PropertyId));
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
    public async Task<decimal> GetOdenenTutarAsync(int chargeId)
    {
        return await _ctx.PaymentAllocations.AsNoTracking()
                                      .Where(o => o.ChargeId == chargeId && o.Status == PaymentStatus.Approved)
                                      .SumAsync(o => (decimal?)o.Amount) ?? 0m;
    }

    // ── Üretim yardımcıları ───────────────────────────────────────────────
    public async Task<List<ChargeType>> GetAktifUretimBorcTipleriAsync()
        => await _ctx.ChargeTypes.AsNoTracking()
                                 .Where(b => b.IsActive && (b.Behavior == ChargeTypeBehavior.MonthlyFixed || b.Behavior == ChargeTypeBehavior.FirstMonthOneTime))
                                 .OrderBy(b => b.SortOrder)
                                 .ToListAsync();

    public async Task<List<Charge>> GetSilineceklerAsync(int leaseId, DateTime ilkGun)
        => await _dbSet.Where(t => t.LeaseId == leaseId
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
