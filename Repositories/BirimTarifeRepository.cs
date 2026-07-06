using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BirimTarifeRepository : BaseRepository<BirimTarife>, IBirimTarifeRepository
{
    public BirimTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<RateValueDto?> GetRateAsync(int unitId, int kategoriId, int chargeTypeId)
        => await _dbSet.AsNoTracking()
            .Where(r => r.UnitId == unitId
                     && r.KiraciKategoriId == kategoriId
                     && r.ChargeTypeId == chargeTypeId)
            .Select(r => new RateValueDto
            {
                CalculationMethod = r.CalculationMethod,
                UnitValue = r.UnitValue,
                KdvRate = r.KdvRate
            })
            .FirstOrDefaultAsync();

    public async Task<List<ParentTarifeKartViewModel>> GetByBirimForKartAsync(int unitId, int? kategoriId)
    {
        var q = _dbSet.AsNoTracking().Where(r => r.UnitId == unitId);
        if (kategoriId.HasValue)
            q = q.Where(r => r.KiraciKategoriId == kategoriId.Value);

        var rateler = await q
            .OrderBy(r => r.KiraciKategori.Sira)
            .ThenBy(r => r.ChargeType.SortOrder)
            .Select(r => new ParentTarifeSatir
            {
                KategoriAd = r.KiraciKategori.Ad,
                ChargeTypeName = r.ChargeType.Name,
                CalculationMethod = r.CalculationMethod,
                UnitValue = r.UnitValue,
                KdvRate = r.KdvRate
            })
            .ToListAsync();

        if (rateler.Count == 0) return new List<ParentTarifeKartViewModel>();

        return new List<ParentTarifeKartViewModel>
        {
            new ParentTarifeKartViewModel
            {
                KaynakAdi = "Unit Tarifesi",
                Satirlar = rateler
            }
        };
    }
}
