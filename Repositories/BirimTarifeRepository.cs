using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BirimTarifeRepository : BaseRepository<BirimTarife>, IBirimTarifeRepository
{
    public BirimTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<RateValueDto?> GetRateAsync(int birimId, int kategoriId, int borcTipiId)
        => await _dbSet.AsNoTracking()
            .Where(r => r.BirimId == birimId
                     && r.KiraciKategoriId == kategoriId
                     && r.BorcTipiId == borcTipiId)
            .Select(r => new RateValueDto
            {
                HesaplamaYontemi = r.HesaplamaYontemi,
                BirimDeger = r.BirimDeger,
                KdvOrani = r.KdvOrani
            })
            .FirstOrDefaultAsync();

    public async Task<List<ParentTarifeKartViewModel>> GetByBirimForKartAsync(int birimId, int? kategoriId)
    {
        var q = _dbSet.AsNoTracking().Where(r => r.BirimId == birimId);
        if (kategoriId.HasValue)
            q = q.Where(r => r.KiraciKategoriId == kategoriId.Value);

        var rateler = await q
            .OrderBy(r => r.KiraciKategori.Sira)
            .ThenBy(r => r.BorcTipi.Sira)
            .Select(r => new ParentTarifeSatir
            {
                KategoriAd = r.KiraciKategori.Ad,
                BorcTipiAd = r.BorcTipi.Ad,
                HesaplamaYontemi = r.HesaplamaYontemi,
                BirimDeger = r.BirimDeger,
                KdvOrani = r.KdvOrani
            })
            .ToListAsync();

        if (rateler.Count == 0) return new List<ParentTarifeKartViewModel>();

        return new List<ParentTarifeKartViewModel>
        {
            new ParentTarifeKartViewModel
            {
                KaynakAdi = "Birim Tarifesi",
                Satirlar = rateler
            }
        };
    }
}
