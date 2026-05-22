using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TasinmazService : ITasinmazService
{
    private readonly ITasinmazRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IUserTasinmazYetkiService _yetkiService;
    private readonly IIstatistikService _istatistikService;

    public TasinmazService(
        ITasinmazRepository repo,
        IUnitOfWork uow,
        IUserTasinmazYetkiService yetkiService,
        IIstatistikService istatistikService)
    {
        _repo = repo;
        _uow = uow;
        _yetkiService = yetkiService;
        _istatistikService = istatistikService;
    }

    public async Task<List<TasinmazListItemDto>> GetAllAsync(string? userId = null)
    {
        var yetkiliIds = userId == null ? null : await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
        return await _repo.GetListAsync(yetkiliIds);
    }

    public async Task<TasinmazDetayDto?> GetByIdAsync(int id)
    {
        var dto = await _repo.GetDetayAsync(id);
        if (dto == null) return null;

        // Birimlerin aktif sözleşmelerinin aylık bedellerini hesapla
        foreach (var b in dto.Birimler)
        {
            if (b.AktifSozlesmeId.HasValue)
            {
                var dummySozlesme = new KiraSozlesmesi
                {
                    Id = b.AktifSozlesmeId.Value,
                    KiraciId = b.AktifSozlesmeKiraciId ?? 0,
                    BirimId = b.Id,
                    Birim = new Birim { Id = b.Id, Yuzolcumu = b.Yuzolcumu }
                };
                b.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
            }
        }

        // Sözleşme geçmişindeki sözleşmelerin aylık bedellerini hesapla
        foreach (var s in dto.SozlesmeGecmisi)
        {
            var birimYuzolcumu = dto.Birimler.FirstOrDefault(b => b.Id == s.BirimId)?.Yuzolcumu ?? 0m;
            var dummySozlesme = new KiraSozlesmesi
            {
                Id = s.Id,
                KiraciId = s.KiraciId,
                BirimId = s.BirimId,
                Birim = new Birim { Id = s.BirimId, Yuzolcumu = birimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }

        return dto;
    }

    public async Task<Tasinmaz> CreateAsync(Tasinmaz t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null)
    {
        t.KayitTarihi = DateTime.Now;

        if (t.KiralamaSekli == KiralamaSekli.BirimBazli && birimler != null && birimler.Count > 0)
        {
            foreach (var b in birimler)
            {
                var ad = string.IsNullOrWhiteSpace(b.Ad) ? $"Birim {b.BirimNo}" : b.Ad;
                t.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = b.BirimNo,
                    KatNo = b.KatNo,
                    Ad = ad,
                    Yuzolcumu = b.Yuzolcumu,
                    Aciklama = b.Aciklama,
                    BirimTuruId = b.BirimTuruId
                });
            }
        }
        else
        {
            t.Birimler.Add(new Birim
            {
                BirimTipi = BirimTipi.Komple,
                Ad = "Komple",
                Yuzolcumu = t.KapaliYuzolcumu > 0 ? t.KapaliYuzolcumu : t.AcikYuzolcumu
            });
        }

        if (rezervasyonAlanlari != null && rezervasyonAlanlari.Count > 0)
        {
            foreach (var r in rezervasyonAlanlari)
            {
                var birim = new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    Ad = string.IsNullOrWhiteSpace(r.Ad) ? "Rezervasyon Alanı" : r.Ad,
                    Yuzolcumu = r.Yuzolcumu,
                    Aciklama = r.Aciklama,
                    BirimTuruId = r.BirimTuruId
                };
                t.Birimler.Add(birim);

                // Ücret kuralını ekle
                await _repo.AddRezervasyonTarifeAsync(new RezervasyonTarife
                {
                    Birim = birim,
                    UcretsizSureDakika = r.UcretsizSureDakika,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti = r.SaatlikUcret,
                    KdvOrani = r.KdvOrani,
                    Aktif = true,
                    OlusturmaTarihi = DateTime.Now,
                    Aciklama = $"{r.Ad} için otomatik oluşturuldu"
                });
            }
        }

        await _repo.AddAsync(t);
        await _uow.SaveChangesAsync();
        return t;
    }

    public async Task UpdateAsync(Tasinmaz t)
    {
        await _repo.UpdateAsync(t);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<BirimLookupDto>> GetBosBirimlerAsync(string? userId = null)
    {
        var yetkiliIds = userId == null ? null : await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
        return await _repo.GetBosBirimlerAsync(yetkiliIds);
    }
}
