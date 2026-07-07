using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateHierarchyService : IRateHierarchyService
{
    private readonly ITasinmazTarifeRepository _tasinmazTarifeRepo;
    private readonly IUnitRateRepository _unitRateRepo;
    private readonly IGenelTarifeRepository _genelTarifeRepo;
    private readonly IRezervasyonTarifeRepository _rezervasyonTarifeRepo;
    private readonly IUnitRepository _birimRepo;

    public RateHierarchyService(
        ITasinmazTarifeRepository tasinmazTarifeRepo,
        IUnitRateRepository unitRateRepo,
        IGenelTarifeRepository genelTarifeRepo,
        IRezervasyonTarifeRepository rezervasyonTarifeRepo,
        IUnitRepository birimRepo)
    {
        _tasinmazTarifeRepo = tasinmazTarifeRepo;
        _unitRateRepo = unitRateRepo;
        _genelTarifeRepo = genelTarifeRepo;
        _rezervasyonTarifeRepo = rezervasyonTarifeRepo;
        _birimRepo = birimRepo;
    }

    public async Task<ParentTarifeKartViewModel?> GetParentForAsync(
        TarifeHiyerarsiKatmani katman,
        int? propertyId = null,
        int? unitId = null,
        int? tenantCategoryId = null,
        int? yil = null)
    {
        int hedefYil = yil ?? DateTime.Now.Year;

        // Lease katmanı: önce UnitRate'e bak
        if (katman == TarifeHiyerarsiKatmani.Lease && unitId.HasValue)
        {
            var kartlar = await _unitRateRepo.GetByBirimForKartAsync(unitId.Value, tenantCategoryId);
            if (kartlar.Count > 0)
                return kartlar[0];

            if (!propertyId.HasValue)
                propertyId = await _birimRepo.GetPropertyIdAsync(unitId.Value);
        }

        // Unit veya Lease katmanı: TasinmazTarife'a bak
        if (katman is TarifeHiyerarsiKatmani.Unit or TarifeHiyerarsiKatmani.Lease
            && propertyId.HasValue)
        {
            var fiyatlar = await _tasinmazTarifeRepo.GetForHiyerarsiAsync(propertyId.Value, tenantCategoryId);

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
        var kalemler = await _genelTarifeRepo.GetByYilKategoriForKartAsync(hedefYil, tenantCategoryId);

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
