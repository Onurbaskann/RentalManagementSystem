using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class RezervasyonRepository : BaseRepository<Rezervasyon>, IRezervasyonRepository
{
    public RezervasyonRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<RezervasyonListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (yetkiliTasinmazIds != null)
            query = query.Where(r => yetkiliTasinmazIds.Contains(r.Birim.TasinmazId));

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RezervasyonListItemDto
            {
                Id = r.Id,
                BirimId = r.BirimId,
                BirimAd = r.Birim.Ad,
                TasinmazId = r.Birim.TasinmazId,
                TasinmazAd = r.Birim.Tasinmaz.Ad,
                KiraciId = r.KiraciId,
                KiraciGosterimAdi = r.Kiraci.GosterimAdi,
                TahakkukId = _ctx.Tahakkuklar.Where(t => t.RezervasyonId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                BaslangicTarihi = r.BaslangicTarihi,
                BitisTarihi = r.BitisTarihi,
                ToplamSureDakika = r.ToplamSureDakika,
                UcretsizSureDakika = r.UcretsizSureDakika,
                UcretliSureDakika = r.UcretliSureDakika,
                ToplamTutar = r.ToplamTutar,
                Durum = r.Durum,
                Aciklama = r.Aciklama
            })
            .ToListAsync();
    }

    public async Task<RezervasyonListItemDto?> GetByIdAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RezervasyonListItemDto
            {
                Id = r.Id,
                BirimId = r.BirimId,
                BirimAd = r.Birim.Ad,
                TasinmazId = r.Birim.TasinmazId,
                TasinmazAd = r.Birim.Tasinmaz.Ad,
                KiraciId = r.KiraciId,
                KiraciGosterimAdi = r.Kiraci.GosterimAdi,
                TahakkukId = _ctx.Tahakkuklar.Where(t => t.RezervasyonId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                BaslangicTarihi = r.BaslangicTarihi,
                BitisTarihi = r.BitisTarihi,
                ToplamSureDakika = r.ToplamSureDakika,
                UcretsizSureDakika = r.UcretsizSureDakika,
                UcretliSureDakika = r.UcretliSureDakika,
                ToplamTutar = r.ToplamTutar,
                Durum = r.Durum,
                Aciklama = r.Aciklama
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsConflictAsync(int birimId, DateTime baslangic, DateTime bitis)
    {
        return await _dbSet.AnyAsync(r =>
            r.BirimId == birimId &&
            r.Durum != RezervasyonDurumu.IptalEdildi &&
            r.BaslangicTarihi < bitis &&
            r.BitisTarihi > baslangic);
    }

    public async Task<RezervasyonTarife?> GetAktifTarifeForBirimAsync(int birimId)
    {
        return await _ctx.RezervasyonTarifeler
            .Where(k => k.IsActive && k.BirimId == birimId)
            .FirstOrDefaultAsync();
    }

    public async Task<RezervasyonTarife?> GetGenelTarifeAsync(int birimTuruId, int yil)
    {
        return await _ctx.RezervasyonTarifeler
            .Where(g => g.BirimId == null && g.BirimTuruId == birimTuruId && g.IsActive && g.Yil == yil)
            .FirstOrDefaultAsync();
    }

    public async Task<List<RezervasyonTarife>> GetUcretKurallariAsync()
    {
        return await _ctx.RezervasyonTarifeler
            .Include(k => k.Birim!).ThenInclude(b => b!.Tasinmaz)
            .Where(k => k.BirimId != null)
            .OrderBy(k => k.Id)
            .ToListAsync();
    }

    public async Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id)
    {
        return await _ctx.RezervasyonTarifeler
            .Include(k => k.Birim)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task AddUcretKuralAsync(RezervasyonTarife kural)
    {
        await _ctx.RezervasyonTarifeler.AddAsync(kural);
    }

    public async Task AddTahakkukAsync(Tahakkuk tahakkuk)
    {
        await _ctx.Tahakkuklar.AddAsync(tahakkuk);
    }

    public async Task<BorcTipi?> ResolveRezervasyonBorcTipiAsync(int? preferredBorcTipiId)
    {
        if (preferredBorcTipiId.HasValue)
        {
            var bt = await _ctx.BorcTipleri
                .FirstOrDefaultAsync(b => b.Id == preferredBorcTipiId.Value && b.Aktif);
            if (bt != null) return bt;
        }

        return await _ctx.BorcTipleri
            .FirstOrDefaultAsync(b => b.Davranis == BorcTipiDavranisi.RezervasyonOzel && b.Aktif);
    }
}
