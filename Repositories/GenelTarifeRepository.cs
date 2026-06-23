using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class GenelTarifeRepository : BaseRepository<GenelTarife>, IGenelTarifeRepository
{
    public GenelTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<RateValueDto?> GetRateAsync(int kategoriId, int borcTipiId, int donemYil)
        => await _dbSet.AsNoTracking()
            .Where(k => k.IsActive && k.KiraciKategoriId == kategoriId && k.BorcTipiId == borcTipiId)
            .OrderByDescending(k => k.Yil == donemYil ? 1 : 0)
            .ThenByDescending(k => k.Yil)
            .Select(k => new RateValueDto
            {
                HesaplamaYontemi = k.HesaplamaYontemi,
                BirimDeger = k.BirimDeger,
                KdvOrani = k.KdvOrani
            })
            .FirstOrDefaultAsync();

    public async Task<List<ParentTarifeSatir>> GetByYilKategoriForKartAsync(int yil, int? kategoriId)
    {
        var q = _dbSet.AsNoTracking()
            .Where(k => k.Yil == yil && k.IsActive
                     && k.BorcTipi.Davranis != BorcTipiDavranisi.KullaniciManuel
                     && k.BorcTipi.Davranis != BorcTipiDavranisi.RezervasyonOzel);
        if (kategoriId.HasValue)
            q = q.Where(k => k.KiraciKategoriId == kategoriId.Value);

        return await q
            .OrderBy(k => k.KiraciKategori.Sira)
            .ThenBy(k => k.BorcTipi.Sira)
            .Select(k => new ParentTarifeSatir
            {
                KategoriAd = k.KiraciKategori.Ad,
                BorcTipiAd = k.BorcTipi.Ad,
                HesaplamaYontemi = k.HesaplamaYontemi,
                BirimDeger = k.BirimDeger,
                KdvOrani = k.KdvOrani
            })
            .ToListAsync();
    }
}
