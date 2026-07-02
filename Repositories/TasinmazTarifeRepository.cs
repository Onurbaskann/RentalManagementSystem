using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TasinmazTarifeRepository : BaseRepository<TasinmazTarife>, ITasinmazTarifeRepository
{
    public TasinmazTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<TasinmazTarife>> GetByTasinmazIdAsync(int tasinmazId)
        => await _dbSet
            .AsNoTracking()
            .Where(f => f.TasinmazId == tasinmazId)
            .ToListAsync();

    public async Task<List<Kategori>> GetKiraciKategorileriAsync()
        => await _ctx.Kategoriler
            .AsNoTracking()
            .Where(k => k.Tipi == KategoriTipi.Kiraci)
            .OrderBy(k => k.Ad)
            .ToListAsync();

    public async Task<List<BorcTipi>> GetBorcTipleriMatrisIcinAsync()
        => await _ctx.BorcTipleri
            .AsNoTracking()
            .Where(b => b.Davranis != ChargeTypeBehavior.UserManual
                     && b.Davranis != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(b => b.Sira)
            .ToListAsync();

    public async Task<List<TasinmazTarife>> GetForHiyerarsiAsync(int tasinmazId, int? kategoriId)
    {
        IQueryable<TasinmazTarife> q = _dbSet
            .AsNoTracking()
            .Include(f => f.KiraciKategori)
            .Include(f => f.BorcTipi)
            .Where(f => f.TasinmazId == tasinmazId && f.IsActive);

        if (kategoriId.HasValue)
            q = q.Where(f => f.KiraciKategoriId == kategoriId.Value);

        return await q
            .OrderBy(f => f.KiraciKategori.Sira)
            .ThenBy(f => f.BorcTipi.Sira)
            .ToListAsync();
    }

    public async Task<RateValueDto?> GetRateAsync(int tasinmazId, int kategoriId, int borcTipiId)
        => await _dbSet.AsNoTracking()
            .Where(f => f.TasinmazId == tasinmazId
                     && f.KiraciKategoriId == kategoriId
                     && f.BorcTipiId == borcTipiId
                     && f.IsActive)
            .Select(f => new RateValueDto
            {
                CalculationMethod = f.CalculationMethod,
                BirimDeger = f.BirimDeger,
                KdvOrani = f.KdvOrani
            })
            .FirstOrDefaultAsync();
}
