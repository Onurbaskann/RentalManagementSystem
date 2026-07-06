using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;

namespace KiraTakip.Services;

public class PropertyPricingService : Interfaces.IPropertyPricingService
{
    private readonly ITasinmazTarifeRepository _tarifeRepo;
    private readonly IPropertyRepository _tasinmazRepo;
    private readonly IUnitOfWork _uow;

    public PropertyPricingService(
        ITasinmazTarifeRepository tarifeRepo,
        IPropertyRepository tasinmazRepo,
        IUnitOfWork uow)
    {
        _tarifeRepo = tarifeRepo;
        _tasinmazRepo = tasinmazRepo;
        _uow = uow;
    }

    public async Task<TasinmazFiyatMatrisiViewModel> GetMatrisiAsync(int propertyId, int page = 1, int pageSize = 10)
    {
        Property? property = null;
        if (propertyId > 0)
        {
            property = await _tasinmazRepo.GetByIdAsync(propertyId);
            if (property == null) throw new ArgumentException("Taşınmaz bulunamadı");
        }

        var kiraciKategorileri = await _tarifeRepo.GetKiraciKategorileriAsync();
        var borcTipleri = await _tarifeRepo.GetBorcTipleriMatrisIcinAsync();
        var mevcutFiyatlar = await _tarifeRepo.GetByPropertyIdAsync(propertyId);

        var vm = new TasinmazFiyatMatrisiViewModel
        {
            TasinmazId = propertyId,
            TasinmazAd = property?.Name ?? "Yeni Taşınmaz",
            Kolonlar = borcTipleri.Select(b => new BorcTipiFiyatKolonuViewModel
            {
                ChargeTypeId = b.Id,
                ChargeTypeName = b.Name,
                ChargeTypeCode = b.Code,
                ChargeTypeBehavior = b.Behavior
            }).ToList()
        };

        var satirList = new List<KiraciKategoriFiyatSatiriViewModel>();
        foreach (var kk in kiraciKategorileri)
        {
            var satir = new KiraciKategoriFiyatSatiriViewModel
            {
                KiraciKategoriId = kk.Id,
                KiraciKategoriAd = kk.Ad,
                Hucreler = new List<TasinmazFiyatHucreViewModel>()
            };
            foreach (var bt in borcTipleri)
            {
                var fiyat = mevcutFiyatlar.FirstOrDefault(f => f.KiraciKategoriId == kk.Id && f.ChargeTypeId == bt.Id);
                if (fiyat != null)
                {
                    satir.Hucreler.Add(new TasinmazFiyatHucreViewModel
                    {
                        TasinmazTarifeId = fiyat.Id,
                        TasinmazId = propertyId,
                        KiraciKategoriId = kk.Id,
                        ChargeTypeId = bt.Id,
                        UnitValue = fiyat.UnitValue,
                        CalculationMethod = fiyat.CalculationMethod,
                        KdvRate = fiyat.KdvRate,
                        RateVarMi = true
                    });
                }
                else
                {
                    satir.Hucreler.Add(new TasinmazFiyatHucreViewModel
                    {
                        TasinmazTarifeId = null,
                        TasinmazId = propertyId,
                        KiraciKategoriId = kk.Id,
                        ChargeTypeId = bt.Id,
                        UnitValue = null,
                        CalculationMethod = (bt.Code == BorcTipiConsts.Kira) ? CalculationMethod.M2 : CalculationMethod.Fixed,
                        KdvRate = null,
                        RateVarMi = false
                    });
                }
            }
            satirList.Add(satir);
        }

        var totalRows = satirList.Count;
        var skip = (page - 1) * pageSize;
        vm.TotalRows = totalRows;
        vm.Satirlar = satirList.Skip(skip).Take(pageSize).ToList();

        return vm;
    }

    public async Task SaveMatrisiAsync(int propertyId, TasinmazFiyatMatrisiViewModel model, string userId)
    {
        if (model?.Satirlar == null) return;

        foreach (var satir in model.Satirlar)
        {
            foreach (var hucre in satir.Hucreler)
            {
                if (hucre.TasinmazTarifeId.HasValue)
                {
                    var entity = await _tarifeRepo.GetByIdAsync(hucre.TasinmazTarifeId.Value);
                    if (entity != null)
                    {
                        if (hucre.UnitValue.HasValue)
                        {
                            entity.UnitValue = hucre.UnitValue.Value;
                            entity.CalculationMethod = hucre.CalculationMethod;
                            entity.KdvRate = hucre.KdvRate ?? 0m;
                        }
                        else
                        {
                            await _tarifeRepo.DeleteAsync(entity.Id);
                        }
                    }
                }
                else
                {
                    if (hucre.UnitValue.HasValue)
                    {
                        var newEntity = new TasinmazTarife
                        {
                            PropertyId = propertyId,
                            KiraciKategoriId = hucre.KiraciKategoriId,
                            ChargeTypeId = hucre.ChargeTypeId,
                            UnitValue = hucre.UnitValue.Value,
                            CalculationMethod = hucre.CalculationMethod,
                            KdvRate = hucre.KdvRate ?? 0m
                        };
                        await _tarifeRepo.AddAsync(newEntity);
                    }
                }
            }
        }
        await _uow.SaveChangesAsync();
    }
}
