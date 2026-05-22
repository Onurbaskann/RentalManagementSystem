using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BirimRepository : BaseRepository<Birim>, IBirimRepository
{
    public BirimRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<BirimListItemDto>> GetByTasinmazIdAsync(int tasinmazId)
    {
        var now = DateTime.Now;
        return await _dbSet.AsNoTracking()
            .Where(b => b.TasinmazId == tasinmazId)
            .OrderBy(b => b.Ad)
            .Select(b => new BirimListItemDto
            {
                Id = b.Id,
                BirimNo = b.BirimNo,
                Ad = b.Ad,
                KatNo = b.KatNo,
                Yuzolcumu = b.Yuzolcumu,
                BirimTuruAd = b.BirimTuru != null ? b.BirimTuru.Ad : string.Empty,
                TasinmazId = b.TasinmazId,
                TasinmazAd = b.Tasinmaz.Ad,
                Durum = b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                    ? (b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now && s.BitisTarihi <= now.AddDays(30))
                        ? KiraDurumu.SuresiDolmakUzere
                        : KiraDurumu.Kirali)
                    : KiraDurumu.Bos,
                AylikBedel = 0
            })
            .ToListAsync();
    }

    public async Task<List<BirimListItemDto>> GetRezervasyonBirimleriAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(b => b.BirimTuru != null && b.BirimTuru.RezervasyonYapilabilirMi && b.BirimTuru.Aktif)
            .OrderBy(b => b.Tasinmaz.Ad).ThenBy(b => b.Ad)
            .Select(b => new BirimListItemDto
            {
                Id = b.Id,
                Ad = b.Ad,
                BirimTuruAd = b.BirimTuru != null ? b.BirimTuru.Ad : string.Empty,
                TasinmazId = b.TasinmazId,
                TasinmazAd = b.Tasinmaz.Ad,
                Yuzolcumu = b.Yuzolcumu,
                AylikBedel = 0
            })
            .ToListAsync();
    }

    public async Task<BirimDetayDto?> GetDetayAsync(int id)
    {
        var now = DateTime.Now;
        return await _dbSet.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BirimDetayDto
            {
                Id = b.Id,
                BirimNo = b.BirimNo,
                Ad = b.Ad,
                KatNo = b.KatNo,
                Yuzolcumu = b.Yuzolcumu,
                BirimTuruAd = b.BirimTuru != null ? b.BirimTuru.Ad : string.Empty,
                RezervasyonYapilabilirMi = b.BirimTuru != null ? b.BirimTuru.RezervasyonYapilabilirMi : false,
                KiralanabilirMi = b.BirimTuru != null ? b.BirimTuru.KiralanabilirMi : false,
                TasinmazId = b.TasinmazId,
                TasinmazAd = b.Tasinmaz.Ad,
                AktifSozlesmeId = b.Sozlesmeler
                    .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                    .OrderByDescending(s => s.BitisTarihi)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefault(),
                AktifSozlesmeKiraciId = b.Sozlesmeler
                    .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                    .OrderByDescending(s => s.BitisTarihi)
                    .Select(s => (int?)s.KiraciId)
                    .FirstOrDefault(),
                AktifSozlesmeKiraciGosterimAdi = b.Sozlesmeler
                    .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                    .OrderByDescending(s => s.BitisTarihi)
                    .Select(s => s.Kiraci.GosterimAdi)
                    .FirstOrDefault(),
                AktifSozlesmeBitisTarihi = b.Sozlesmeler
                    .Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                    .OrderByDescending(s => s.BitisTarihi)
                    .Select(s => (DateTime?)s.BitisTarihi)
                    .FirstOrDefault(),
                Durum = b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now)
                    ? (b.Sozlesmeler.Any(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now && s.BitisTarihi <= now.AddDays(30))
                        ? KiraDurumu.SuresiDolmakUzere
                        : KiraDurumu.Kirali)
                    : KiraDurumu.Bos,
                RezKuralId = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.BirimId == b.Id && rt.Aktif)
                    .Select(rt => (int?)rt.Id)
                    .FirstOrDefault(),
                RezKuralPeriyotUcreti = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.BirimId == b.Id && rt.Aktif)
                    .Select(rt => (decimal?)rt.PeriyotUcreti)
                    .FirstOrDefault(),
                RezKuralUcretlendirmePeriyoduDakika = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.BirimId == b.Id && rt.Aktif)
                    .Select(rt => (int?)rt.UcretlendirmePeriyoduDakika)
                    .FirstOrDefault(),
                RezKuralUcretsizSureDakika = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.BirimId == b.Id && rt.Aktif)
                    .Select(rt => (int?)rt.UcretsizSureDakika)
                    .FirstOrDefault(),
                RezKuralKdvOrani = _ctx.RezervasyonTarifeler
                    .Where(rt => rt.BirimId == b.Id && rt.Aktif)
                    .Select(rt => (decimal?)rt.KdvOrani)
                    .FirstOrDefault(),
                AylikBedel = 0
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetTasinmazIdAsync(int birimId)
        => await _dbSet.AsNoTracking()
            .Where(b => b.Id == birimId)
            .Select(b => (int?)b.TasinmazId)
            .FirstOrDefaultAsync();
}
