using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class RezervasyonTarifeRepository : BaseRepository<RezervasyonTarife>, IRezervasyonTarifeRepository
{
    public RezervasyonTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<ParentRezervasyonTarifeSatir>> GetGenelForKartAsync(int yil)
        => await _dbSet.AsNoTracking()
            .Where(r => r.UnitId == null
                     && r.UnitTypeId != null
                     && r.Yil == yil
                     && r.IsActive
                     && r.UnitType!.IsActive)
            .OrderBy(r => r.UnitType!.SortOrder)
            .Select(r => new ParentRezervasyonTarifeSatir
            {
                UnitTypeAd = r.UnitType!.Name,
                FreeDurationMinutes = r.FreeDurationMinutes,
                UcretlendirmePeriyoduDakika = r.UcretlendirmePeriyoduDakika,
                PeriyotUcreti = r.PeriyotUcreti,
                KdvRate = r.KdvRate
            })
            .ToListAsync();

    public async Task<List<RezervasyonTarifeKuralListItemDto>> GetUcretKurallariListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(r => r.UnitId == null ? 0 : 1)
            .ThenBy(r => r.Unit != null ? r.Unit.Property.Name : string.Empty)
            .ThenBy(r => r.Unit != null ? r.Unit.Name : string.Empty)
            .Select(r => new RezervasyonTarifeKuralListItemDto
            {
                Id = r.Id,
                BirimId = r.UnitId,
                BirimAd = r.Unit != null ? r.Unit.Name : null,
                TasinmazAd = r.Unit != null ? r.Unit.Property.Name : null,
                FreeDurationMinutes = r.FreeDurationMinutes,
                UcretlendirmePeriyoduDakika = r.UcretlendirmePeriyoduDakika,
                PeriyotUcreti = r.PeriyotUcreti,
                KdvRate = r.KdvRate,
                IsActive = r.IsActive,
                Aciklama = r.Aciklama
            })
            .ToListAsync();
}
