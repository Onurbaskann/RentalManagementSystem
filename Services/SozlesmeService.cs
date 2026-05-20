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
                .ThenInclude(k => k.Kategori)
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
                .ThenInclude(k => k.Kategori)
            .Include(s => s.IslemGecmisi)
            .Include(s => s.SozlesmeRateler)
                .ThenInclude(r => r.BorcTipi)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<KiraSozlesmesi> CreateAsync(KiraSozlesmesi s, decimal? aylikBedel = null)
    {
        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            IslemTipi = SozlesmeIslemTipi.Olusturma,
            IslemTarihi = DateTime.Now,
            Aciklama = "Sözleşme oluşturuldu.",
            YeniKiraBedeli = aylikBedel
        });

        _ctx.Sozlesmeler.Add(s);
        await _ctx.SaveChangesAsync();
        return s;
    }

    public async Task UzatAsync(int id, DateTime yeniBitis, decimal eskiBedel, decimal yeniBedel,
        bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama)
    {
        var s = await _ctx.Sozlesmeler
            .Include(x => x.IslemGecmisi)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        var eskiBitis = s.BitisTarihi;

        s.BitisTarihi = yeniBitis;
        s.KdvUygulanacakMi = kdvUygulanacakMi;

        decimal? kdvTutari = kdvUygulanacakMi ? yeniBedel * kdvOrani / 100 : null;
        decimal? kdvDahil = kdvUygulanacakMi ? yeniBedel + kdvTutari : null;

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            KiraSozlesmesiId = id,
            IslemTipi = SozlesmeIslemTipi.SureUzatma,
            IslemTarihi = DateTime.Now,
            Aciklama = aciklama ?? "Sözleşme süresi uzatıldı.",
            EskiBitisTarihi = eskiBitis,
            YeniBitisTarihi = yeniBitis,
            EskiKiraBedeli = eskiBedel,
            YeniKiraBedeli = yeniBedel,
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

    public async Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> sozlesmeIds)
    {
        var ids = sozlesmeIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal?>();

        var kalemler = await _ctx.TahakkukKalemleri
            .Where(k => k.Tahakkuk.KiraSozlesmesiId.HasValue
                && ids.Contains(k.Tahakkuk.KiraSozlesmesiId.Value)
                && k.BorcTipi.Kod == "DEPOZITO"
                && k.Tahakkuk.Durum != TahakkukDurumu.IptalEdildi)
            .Select(k => new
            {
                SozlesmeId = k.Tahakkuk.KiraSozlesmesiId!.Value,
                Donem = k.Tahakkuk.DonemBaslangic,
                Tutar = k.ToplamTutar
            })
            .ToListAsync();

        return kalemler
            .GroupBy(x => x.SozlesmeId)
            .ToDictionary(g => g.Key, g => (decimal?)g.OrderBy(x => x.Donem).First().Tutar);
    }
}
