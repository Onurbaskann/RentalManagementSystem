using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateHierarchyService : IRateHierarchyService
{
    private readonly ITasinmazTarifeRepository _tasinmazTarifeRepo;
    private readonly IBirimTarifeRepository _birimTarifeRepo;
    private readonly IGenelTarifeRepository _genelTarifeRepo;
    private readonly IRezervasyonTarifeRepository _rezervasyonTarifeRepo;
    private readonly IUnitRepository _birimRepo;

    public RateHierarchyService(
        ITasinmazTarifeRepository tasinmazTarifeRepo,
        IBirimTarifeRepository birimTarifeRepo,
        IGenelTarifeRepository genelTarifeRepo,
        IRezervasyonTarifeRepository rezervasyonTarifeRepo,
        IUnitRepository birimRepo)
    {
        _tasinmazTarifeRepo = tasinmazTarifeRepo;
        _birimTarifeRepo = birimTarifeRepo;
        _genelTarifeRepo = genelTarifeRepo;
        _rezervasyonTarifeRepo = rezervasyonTarifeRepo;
        _birimRepo = birimRepo;
    }

    public async Task<ParentTarifeKartViewModel?> GetParentForAsync(
        TarifeHiyerarsiKatmani katman,
        int? propertyId = null,
        int? unitId = null,
        int? kategoriId = null,
        int? yil = null)
    {
        int hedefYil = yil ?? DateTime.Now.Year;

        // Lease katmanı: önce BirimTarife'e bak
        if (katman == TarifeHiyerarsiKatmani.Lease && unitId.HasValue)
        {
            var kartlar = await _birimTarifeRepo.GetByBirimForKartAsync(unitId.Value, kategoriId);
            if (kartlar.Count > 0)
                return kartlar[0];

            if (!propertyId.HasValue)
                propertyId = await _birimRepo.GetPropertyIdAsync(unitId.Value);
        }

        // Unit veya Lease katmanı: TasinmazTarife'a bak
        if (katman is TarifeHiyerarsiKatmani.Unit or TarifeHiyerarsiKatmani.Lease
            && propertyId.HasValue)
        {
            var fiyatlar = await _tasinmazTarifeRepo.GetForHiyerarsiAsync(propertyId.Value, kategoriId);

            if (fiyatlar.Count > 0)
                return new ParentTarifeKartViewModel
                {
                    KaynakAdi = "Taşınmaz Tarifesi",
                    Satirlar = fiyatlar.Select(f => new ParentTarifeSatir
                    {
                        KategoriAd = f.KiraciKategori.Ad,
                        ChargeTypeName = f.ChargeType.Name,
                        CalculationMethod = f.CalculationMethod,
                        UnitValue = f.UnitValue,
                        KdvRate = f.KdvRate
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
            KaynakAdi = $"Reservation Tarifesi - {hedefYil}",
            Satirlar = satirlar
        };
    }
}
