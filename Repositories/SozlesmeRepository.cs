using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class SozlesmeRepository : BaseRepository<Lease>, ISozlesmeRepository
{
    public SozlesmeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<SozlesmeListItemDto>> GetListAsync(string? filtre, List<int>? yetkiliTasinmazIds)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (yetkiliTasinmazIds != null)
        {
            query = query.Where(s => yetkiliTasinmazIds.Contains(s.Unit.PropertyId));
        }

        query = filtre switch
        {
            "aktif" => query.Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now),
            "surek" => query.Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30)),
            "gecmis" => query.Where(s => s.Status == LeaseStatus.Ended),
            "feshedildi" => query.Where(s => s.Status == LeaseStatus.Terminated),
            _ => query
        };

        return await query
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SozlesmeListItemDto
            {
                Id = s.Id,
                KiraciId = s.TenantId,
                KiraciGosterimAdi = s.Tenant.DisplayName,
                KiraciKategoriAd = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Ad : string.Empty,
                BirimId = s.UnitId,
                BirimAd = s.Unit.Name,
                TasinmazId = s.Unit.PropertyId,
                TasinmazAd = s.Unit.Property.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                AylikBedel = 0,
                Durum = s.Status,
                BirimYuzolcumu = s.Unit.Area
            })
            .ToListAsync();
    }

    public async Task<SozlesmeDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SozlesmeDetayDto
            {
                Id = s.Id,
                KiraciId = s.TenantId,
                KiraciGosterimAdi = s.Tenant.DisplayName,
                KiraciTelefon = s.Tenant.Phone,
                KiraciEmail = s.Tenant.Email,
                KiraciKategoriId = s.Tenant.TenantCategoryId,
                KiraciKategoriAd = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Ad : string.Empty,
                BirimId = s.UnitId,
                BirimAd = s.Unit.Name,
                BirimNo = s.Unit.UnitNo,
                BirimKatNo = s.Unit.FloorNo,
                BirimYuzolcumu = s.Unit.Area,
                UnitKind = s.Unit.UnitKind,
                TasinmazId = s.Unit.PropertyId,
                TasinmazAd = s.Unit.Property.Name,
                TasinmazIl = s.Unit.Property.City,
                TasinmazIlce = s.Unit.Property.District,
                TasinmazMahalle = s.Unit.Property.Neighborhood,
                TasinmazAcikAdres = s.Unit.Property.Address,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Aciklama = s.Description,
                Durum = s.Status,
                FesihTarihi = s.TerminationDate,
                FesihNedeni = s.TerminationReason,
                KdvUygulanacakMi = s.IsKdvApplied,
                DueDateRuleType = s.DueDateRuleType,
                VadeGunu = s.DueDay,
                IslemGecmisi = s.ActivityLog
                    .OrderByDescending(ig => ig.TransactionDate)
                    .Select(ig => new SozlesmeIslemGecmisiDto
                    {
                        Id = ig.Id,
                        TransactionDate = ig.TransactionDate,
                        IslemTipi = ig.IslemTipi,
                        Aciklama = ig.Aciklama,
                        EskiKiraBedeli = ig.EskiKiraBedeli,
                        YeniKiraBedeli = ig.YeniKiraBedeli,
                        EskiBitisTarihi = ig.EskiBitisTarihi,
                        YeniBitisTarihi = ig.YeniBitisTarihi,
                        TufeOrani = ig.TufeOrani,
                        KdvUygulandiMi = ig.KdvUygulandiMi ?? false,
                        KdvRate = ig.KdvRate,
                        KdvTutari = ig.KdvTutari,
                        KdvDahilTutar = ig.KdvDahilTutar
                    }).ToList(),
                SozlesmeTarifeler = s.LeaseRateOverrides
                    .Select(st => new SozlesmeTarifeDto
                    {
                        Id = st.Id,
                        ChargeTypeId = st.ChargeTypeId,
                        ChargeTypeCode = st.ChargeType.Code,
                        ChargeTypeName = st.ChargeType.Name,
                        BorcTipiDavranis = st.ChargeType.Behavior,
                        UnitValue = st.UnitValue,
                        CalculationMethod = st.CalculationMethod,
                        KdvRate = st.KdvRate
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<SozlesmeListItemDto>> GetByKiraciIdAsync(int kiraciId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.TenantId == kiraciId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SozlesmeListItemDto
            {
                Id = s.Id,
                KiraciId = s.TenantId,
                KiraciGosterimAdi = s.Tenant.DisplayName,
                KiraciKategoriAd = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Ad : string.Empty,
                BirimId = s.UnitId,
                BirimAd = s.Unit.Name,
                TasinmazId = s.Unit.PropertyId,
                TasinmazAd = s.Unit.Property.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                AylikBedel = 0,
                Durum = s.Status,
                BirimYuzolcumu = s.Unit.Area
            })
            .ToListAsync();
    }

    public async Task<List<SozlesmeListItemDto>> GetByBirimIdAsync(int birimId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.UnitId == birimId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SozlesmeListItemDto
            {
                Id = s.Id,
                KiraciId = s.TenantId,
                KiraciGosterimAdi = s.Tenant.DisplayName,
                KiraciKategoriAd = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Ad : string.Empty,
                BirimId = s.UnitId,
                BirimAd = s.Unit.Name,
                TasinmazId = s.Unit.PropertyId,
                TasinmazAd = s.Unit.Property.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                AylikBedel = 0,
                Durum = s.Status,
                BirimYuzolcumu = s.Unit.Area
            })
            .ToListAsync();
    }

    public async Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> sozlesmeIds)
    {
        var ids = sozlesmeIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal?>();

        var kalemler = await _ctx.ChargeLineItems
            .Where(k => k.Charge.LeaseId.HasValue
                && ids.Contains(k.Charge.LeaseId.Value)
                && k.ChargeType.Code == BorcTipiConsts.Depozito
                && k.Charge.Status != ChargeStatus.Cancelled)
            .Select(k => new
            {
                SozlesmeId = k.Charge.LeaseId!.Value,
                Donem = k.Charge.PeriodStart,
                Amount = k.TotalAmount
            })
            .ToListAsync();

        return kalemler
            .GroupBy(x => x.SozlesmeId)
            .ToDictionary(g => g.Key, g => (decimal?)g.OrderBy(x => x.Donem).First().Amount);
    }

    public async Task<List<Lease>> GetAktiflerAsync()
        => await _dbSet
            .Include(s => s.Tenant)
            .Include(s => s.Unit).ThenInclude(b => b.Property)
            .Where(s => s.Status == LeaseStatus.Active)
            .OrderBy(s => s.Tenant.Name)
            .ToListAsync();

    public async Task<(int TasinmazId, int? KategoriId)?> GetTasinmazVeKategoriAsync(int sozlesmeId)
    {
        var info = await _dbSet.AsNoTracking()
            .Where(s => s.Id == sozlesmeId)
            .Select(s => new { s.Unit.PropertyId, s.Tenant.TenantCategoryId })
            .FirstOrDefaultAsync();
        return info == null ? null : (info.PropertyId, info.TenantCategoryId);
    }

    public async Task<List<SozlesmeDropdownDto>> GetAktifDropdownAsync()
        => await _dbSet.AsNoTracking()
            .Where(s => s.Status == LeaseStatus.Active)
            .OrderBy(s => s.Tenant.Name)
            .Select(s => new SozlesmeDropdownDto
            {
                Id = s.Id,
                BirimId = s.UnitId,
                KiraciId = s.TenantId,
                KiraciGosterimAdi = s.Tenant.DisplayName,
                BirimAd = s.Unit.Name,
                TasinmazAd = s.Unit.Property.Name
            })
            .ToListAsync();
}
