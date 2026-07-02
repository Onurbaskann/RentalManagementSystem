using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TarifeHiyerarsiService : ITarifeHiyerarsiService
{
    private readonly ITasinmazTarifeRepository _tasinmazTarifeRepo;
    private readonly IBirimTarifeRepository _birimTarifeRepo;
    private readonly IGenelTarifeRepository _genelTarifeRepo;
    private readonly IRezervasyonTarifeRepository _rezervasyonTarifeRepo;
    private readonly IBirimRepository _birimRepo;

    public TarifeHiyerarsiService(
        ITasinmazTarifeRepository tasinmazTarifeRepo,
        IBirimTarifeRepository birimTarifeRepo,
        IGenelTarifeRepository genelTarifeRepo,
        IRezervasyonTarifeRepository rezervasyonTarifeRepo,
        IBirimRepository birimRepo)
    {
        _tasinmazTarifeRepo = tasinmazTarifeRepo;
        _birimTarifeRepo = birimTarifeRepo;
        _genelTarifeRepo = genelTarifeRepo;
        _rezervasyonTarifeRepo = rezervasyonTarifeRepo;
        _birimRepo = birimRepo;
    }

    public async Task<ParentTarifeKartViewModel?> GetParentForAsync(
        TarifeHiyerarsiKatmani katman,
        int? tasinmazId = null,
        int? birimId = null,
        int? kategoriId = null,
        int? yil = null)
    {
        int hedefYil = yil ?? DateTime.Now.Year;

        // Sozlesme katmanı: önce BirimTarife'e bak
        if (katman == TarifeHiyerarsiKatmani.Sozlesme && birimId.HasValue)
        {
            var kartlar = await _birimTarifeRepo.GetByBirimForKartAsync(birimId.Value, kategoriId);
            if (kartlar.Count > 0)
                return kartlar[0];

            if (!tasinmazId.HasValue)
                tasinmazId = await _birimRepo.GetTasinmazIdAsync(birimId.Value);
        }

        // Birim veya Sozlesme katmanı: TasinmazTarife'a bak
        if (katman is TarifeHiyerarsiKatmani.Birim or TarifeHiyerarsiKatmani.Sozlesme
            && tasinmazId.HasValue)
        {
            var fiyatlar = await _tasinmazTarifeRepo.GetForHiyerarsiAsync(tasinmazId.Value, kategoriId);

            if (fiyatlar.Count > 0)
                return new ParentTarifeKartViewModel
                {
                    KaynakAdi = "Taşınmaz Tarifesi",
                    Satirlar = fiyatlar.Select(f => new ParentTarifeSatir
                    {
                        KategoriAd = f.KiraciKategori.Ad,
                        BorcTipiAd = f.BorcTipi.Ad,
                        CalculationMethod = f.CalculationMethod,
                        BirimDeger = f.BirimDeger,
                        KdvOrani = f.KdvOrani
                    }).ToList()
                };
        }

        // Her katman için sonuç: Genel Tarife
        var kalemler = await _genelTarifeRepo.GetByYilKategoriForKartAsync(hedefYil, kategoriId);

        return new ParentTarifeKartViewModel
        {
            KaynakAdi = $"Genel Tarife - {hedefYil}",
            Satirlar = kalemler
        };
    }

    public async Task<ParentRezervasyonTarifeKartViewModel?> GetRezervasyonParentForAsync(int? yil = null)
    {
        int hedefYil = yil ?? DateTime.Now.Year;

        var satirlar = await _rezervasyonTarifeRepo.GetGenelForKartAsync(hedefYil);

        return new ParentRezervasyonTarifeKartViewModel
        {
            KaynakAdi = $"Rezervasyon Tarifesi - {hedefYil}",
            Satirlar = satirlar
        };
    }
}
