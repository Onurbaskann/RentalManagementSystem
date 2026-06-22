using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class SozlesmeService : ISozlesmeService
{
    private readonly ISozlesmeRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IIstatistikService _istatistikService;

    public SozlesmeService(
        ISozlesmeRepository repo,
        IUnitOfWork uow,
        IIstatistikService istatistikService)
    {
        _repo = repo;
        _uow = uow;
        _istatistikService = istatistikService;
    }

    public async Task<List<SozlesmeListItemDto>> GetAllAsync(string? filtre = null, IReadOnlyList<int>? tasinmazIds = null)
    {
        var yetkiliIds = tasinmazIds?.ToList();
        var list = await _repo.GetListAsync(filtre, yetkiliIds);
        foreach (var s in list)
        {
            var dummySozlesme = new KiraSozlesmesi
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                BirimId = s.BirimId,
                Birim = new Birim { Id = s.BirimId, Yuzolcumu = s.BirimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }
        return list;
    }

    public async Task<SozlesmeDetayDto?> GetByIdAsync(int id)
    {
        return await _repo.GetDetayAsync(id);
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

        await _repo.AddAsync(s);
        await _uow.SaveChangesAsync();
        return s;
    }

    public async Task UzatAsync(int id, DateTime yeniBitis, decimal eskiBedel, decimal yeniBedel,
        bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama)
    {
        var s = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.IslemGecmisi))
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

        await _uow.SaveChangesAsync();
    }

    public async Task FeshetAsync(int id, DateTime fesihTarihi, string fesihNedeni, string? aciklama)
    {
        var s = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.IslemGecmisi))
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

        await _uow.SaveChangesAsync();
    }

    public async Task VadeGuncelleAsync(int id, VadeKuraliTipi tip, int gun, string? aciklama)
    {
        if (gun < 1 || gun > 31)
            throw new ArgumentOutOfRangeException(nameof(gun), "Vade günü 1-31 arasında olmalıdır.");

        var s = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.IslemGecmisi))
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        var eskiTip = s.VadeKuraliTipi;
        var eskiGun = s.VadeGunu;

        if (eskiTip == tip && eskiGun == gun) return;

        s.VadeKuraliTipi = tip;
        s.VadeGunu = gun;

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            KiraSozlesmesiId = id,
            IslemTipi = SozlesmeIslemTipi.TahakkukYenidenUretim,
            IslemTarihi = DateTime.Now,
            Aciklama = aciklama ?? $"Vade kuralı güncellendi: {eskiTip}({eskiGun}) → {tip}({gun})"
        });

        await _uow.SaveChangesAsync();
    }

    public async Task<List<SozlesmeListItemDto>> GetByKiraciIdAsync(int kiraciId)
    {
        var list = await _repo.GetByKiraciIdAsync(kiraciId);
        foreach (var s in list)
        {
            var dummySozlesme = new KiraSozlesmesi
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                BirimId = s.BirimId,
                Birim = new Birim { Id = s.BirimId, Yuzolcumu = s.BirimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }
        return list;
    }

    public async Task<List<SozlesmeListItemDto>> GetByBirimIdAsync(int birimId)
    {
        var list = await _repo.GetByBirimIdAsync(birimId);
        foreach (var s in list)
        {
            var dummySozlesme = new KiraSozlesmesi
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                BirimId = s.BirimId,
                Birim = new Birim { Id = s.BirimId, Yuzolcumu = s.BirimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }
        return list;
    }

    public async Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> sozlesmeIds)
    {
        return await _repo.GetDepozitoTutarlariAsync(sozlesmeIds);
    }
}
