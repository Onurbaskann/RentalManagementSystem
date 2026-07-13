using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext ctx) : base(ctx)
    {
    }

    public async Task<List<KiraciListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds)
    {
        IQueryable<Tenant> q = _dbSet.AsNoTracking();

        if (yetkiliPropertyIds != null)
        {
            var yetkiliKiraciIds = _ctx.Leases
                .Where(s => yetkiliPropertyIds.Contains(s.Unit.PropertyId))
                .Select(s => s.TenantId)
                .Distinct();

            q = q.Where(k => yetkiliKiraciIds.Contains(k.Id));
        }

        return await q
            .OrderBy(k => k.TenantNo)
            .Select(k => new KiraciListItemDto
            {
                Id = k.Id,
                KiraciNo = k.TenantNo,
                GosterimAdi = k.Name,
                VergiNo = k.TaxNo,
                KiraciKategoriAd = k.TenantCategory != null ? k.TenantCategory.Name : null,
                Telefon = k.Phone,
                Email = k.Email,
                KayitTarihi = k.RegistrationDate
            })
            .ToListAsync();
    }

    public async Task<KiraciDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(k => k.Id == id)
            .Select(k => new KiraciDetayDto
            {
                Id = k.Id,
                KiraciKategoriId = k.TenantCategoryId,
                KiraciKategoriAd = k.TenantCategory != null ? k.TenantCategory.Name : null,
                SektorId = k.SectorId,
                SektorAd = k.Sector != null ? k.Sector.Name : null,
                KiraciNo = k.TenantNo,
                Ad = k.Name,
                TicaretSicilNo = k.TradeRegistryNo,
                VergiNo = k.TaxNo,
                VergiDairesi = k.TaxOffice,
                MersisNo = k.MersisNo,
                Telefon = k.Phone,
                Email = k.Email,
                Adres = k.Address,
                KayitTarihi = k.RegistrationDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<string>> GetExistingTenantNosAsync()
    {
        return await _dbSet.AsNoTracking()
            .Select(k => k.TenantNo)
            .ToListAsync();
    }

    public async Task<int?> GetKategoriIdAsync(int tenantId)
        => await _dbSet.AsNoTracking()
            .Where(k => k.Id == tenantId)
            .Select(k => k.TenantCategoryId)
            .FirstOrDefaultAsync();
}
