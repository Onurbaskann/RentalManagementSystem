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
            .Where(r => r.BirimId == null
                     && r.UnitTypeId != null
                     && r.Yil == yil
                     && r.IsActive
                     && r.UnitType!.Aktif)
            .OrderBy(r => r.UnitType!.Sira)
            .Select(r => new ParentRezervasyonTarifeSatir
            {
                UnitTypeAd = r.UnitType!.Ad,
                UcretsizSureDakika = r.UcretsizSureDakika,
                UcretlendirmePeriyoduDakika = r.UcretlendirmePeriyoduDakika,
                PeriyotUcreti = r.PeriyotUcreti,
                KdvOrani = r.KdvOrani
            })
            .ToListAsync();

    public async Task<List<RezervasyonTarifeKuralListItemDto>> GetUcretKurallariListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(r => r.BirimId == null ? 0 : 1)
            .ThenBy(r => r.Birim != null ? r.Birim.Tasinmaz.Ad : string.Empty)
            .ThenBy(r => r.Birim != null ? r.Birim.Ad : string.Empty)
            .Select(r => new RezervasyonTarifeKuralListItemDto
            {
                Id = r.Id,
                BirimId = r.BirimId,
                BirimAd = r.Birim != null ? r.Birim.Ad : null,
                TasinmazAd = r.Birim != null ? r.Birim.Tasinmaz.Ad : null,
                UcretsizSureDakika = r.UcretsizSureDakika,
                UcretlendirmePeriyoduDakika = r.UcretlendirmePeriyoduDakika,
                PeriyotUcreti = r.PeriyotUcreti,
                KdvOrani = r.KdvOrani,
                IsActive = r.IsActive,
                Aciklama = r.Aciklama
            })
            .ToListAsync();
}
