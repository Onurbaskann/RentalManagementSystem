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

    public async Task<RateValueDto?> GetRateAsync(int kategoriId, int chargeTypeId, int donemYil)
        => await _dbSet.AsNoTracking()
            .Where(k => k.IsActive && k.KiraciKategoriId == kategoriId && k.ChargeTypeId == chargeTypeId)
            .OrderByDescending(k => k.Yil == donemYil ? 1 : 0)
            .ThenByDescending(k => k.Yil)
            .Select(k => new RateValueDto
            {
                CalculationMethod = k.CalculationMethod,
                UnitValue = k.UnitValue,
                KdvRate = k.KdvRate
            })
            .FirstOrDefaultAsync();

    public async Task<List<ParentTarifeSatir>> GetByYilKategoriForKartAsync(int yil, int? kategoriId)
    {
        var q = _dbSet.AsNoTracking()
            .Where(k => k.Yil == yil && k.IsActive
                     && k.ChargeType.Behavior != ChargeTypeBehavior.UserManual
                     && k.ChargeType.Behavior != ChargeTypeBehavior.ReservationSpecific);
        if (kategoriId.HasValue)
            q = q.Where(k => k.KiraciKategoriId == kategoriId.Value);

        return await q
            .OrderBy(k => k.KiraciKategori.Sira)
            .ThenBy(k => k.ChargeType.SortOrder)
            .Select(k => new ParentTarifeSatir
            {
                KategoriAd = k.KiraciKategori.Ad,
                ChargeTypeName = k.ChargeType.Name,
                CalculationMethod = k.CalculationMethod,
                UnitValue = k.UnitValue,
                KdvRate = k.KdvRate
            })
            .ToListAsync();
    }
}
