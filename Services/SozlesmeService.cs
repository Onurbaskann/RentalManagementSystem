using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class SozlesmeService : ISozlesmeService
{
    private readonly ApplicationDbContext _ctx;

    public SozlesmeService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<KiraSozlesmesi>> GetAllAsync(string? filtre = null, string? userId = null)
    {
        var now = DateTime.Now;
        var query = _ctx.Sozlesmeler
            .Include(s => s.Birim)
                .ThenInclude(b => b.Tasinmaz)
            .Include(s => s.Kiraci)
                .ThenInclude(k => k.KiraciKategori)
            .Include(s => s.IslemGecmisi)
            .AsQueryable();

        if (userId != null)
        {
            var yetkiliIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();
            query = query.Where(s => yetkiliIds.Contains(s.Birim.TasinmazId));
        }

        query = filtre switch
        {
            "aktif"      => query.Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now),
            "surek"      => query.Where(s => s.Durum == SozlesmeDurumu.Aktif && s.BaslangicTarihi <= now && s.BitisTarihi >= now && s.BitisTarihi <= now.AddDays(30)),
            "gecmis"     => query.Where(s => s.Durum == SozlesmeDurumu.SonaErdi),
            "feshedildi" => query.Where(s => s.Durum == SozlesmeDurumu.Feshedildi),
            _            => query
        };

        return await query.OrderByDescending(s => s.BaslangicTarihi).ToListAsync();
    }

    public async Task<KiraSozlesmesi?> GetByIdAsync(int id)
    {
        return await _ctx.Sozlesmeler
            .Include(s => s.Birim)
                .ThenInclude(b => b.Tasinmaz)
            .Include(s => s.Birim)
                .ThenInclude(b => b.Sozlesmeler)
                    .ThenInclude(x => x.Kiraci)
            .Include(s => s.Kiraci)
                .ThenInclude(k => k.KiraciKategori)
            .Include(s => s.IslemGecmisi)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<KiraSozlesmesi> CreateAsync(KiraSozlesmesi s)
    {
        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            IslemTipi = SozlesmeIslemTipi.Olusturma,
            IslemTarihi = DateTime.Now,
            Aciklama = "Sözleşme oluşturuldu.",
            YeniKiraBedeli = s.KiraBedeli
        });

        _ctx.Sozlesmeler.Add(s);
        await _ctx.SaveChangesAsync();
        return s;
    }

    public async Task UzatAsync(int id, DateTime yeniBitis, decimal yeniKiraBedeli,
        bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama)
    {
        var s = await _ctx.Sozlesmeler
            .Include(x => x.IslemGecmisi)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        var eskiBitis = s.BitisTarihi;
        var eskiBedel = s.KiraBedeli;

        s.BitisTarihi = yeniBitis;
        s.KiraBedeli = yeniKiraBedeli;
        s.KdvUygulanacakMi = kdvUygulanacakMi;
        if (kdvUygulanacakMi) s.KdvOrani = kdvOrani;

        decimal? kdvTutari = kdvUygulanacakMi ? yeniKiraBedeli * kdvOrani / 100 : null;
        decimal? kdvDahil = kdvUygulanacakMi ? yeniKiraBedeli + kdvTutari : null;

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            KiraSozlesmesiId = id,
            IslemTipi = SozlesmeIslemTipi.SureUzatma,
            IslemTarihi = DateTime.Now,
            Aciklama = aciklama ?? "Sözleşme süresi uzatıldı.",
            EskiBitisTarihi = eskiBitis,
            YeniBitisTarihi = yeniBitis,
            EskiKiraBedeli = eskiBedel,
            YeniKiraBedeli = yeniKiraBedeli,
            TufeOrani = tufeOrani,
            KdvUygulandiMi = kdvUygulanacakMi,
            KdvOrani = kdvUygulanacakMi ? kdvOrani : null,
            KdvTutari = kdvTutari,
            KdvDahilTutar = kdvDahil
        });

        await _ctx.SaveChangesAsync();
    }

    public async Task FeshetAsync(int id, DateTime fesihTarihi, string fesihNedeni, string? aciklama)
    {
        var s = await _ctx.Sozlesmeler
            .Include(x => x.IslemGecmisi)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        s.Durum = SozlesmeDurumu.Feshedildi;
        s.FesihTarihi = fesihTarihi;
        s.FesihNedeni = fesihNedeni;

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            KiraSozlesmesiId = id,
            IslemTipi = SozlesmeIslemTipi.Fesih,
            IslemTarihi = DateTime.Now,
            Aciklama = aciklama ?? fesihNedeni
        });

        await _ctx.SaveChangesAsync();
    }

    public async Task<List<KiraSozlesmesi>> GetByKiraciIdAsync(int kiraciId)
    {
        return await _ctx.Sozlesmeler
            .Include(s => s.Birim)
                .ThenInclude(b => b.Tasinmaz)
            .Include(s => s.IslemGecmisi)
            .Where(s => s.KiraciId == kiraciId)
            .OrderByDescending(s => s.BaslangicTarihi)
            .ToListAsync();
    }

    public async Task<List<KiraSozlesmesi>> GetByBirimIdAsync(int birimId)
    {
        return await _ctx.Sozlesmeler
            .Include(s => s.Kiraci)
            .Include(s => s.IslemGecmisi)
            .Where(s => s.BirimId == birimId)
            .OrderByDescending(s => s.BaslangicTarihi)
            .ToListAsync();
    }
}
