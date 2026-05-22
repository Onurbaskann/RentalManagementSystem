using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;

namespace KiraTakip.Services;

public class TasinmazFiyatService : Interfaces.ITasinmazFiyatService
{
    private readonly ITasinmazTarifeRepository _tarifeRepo;
    private readonly ITasinmazRepository _tasinmazRepo;
    private readonly IUnitOfWork _uow;

    public TasinmazFiyatService(
        ITasinmazTarifeRepository tarifeRepo,
        ITasinmazRepository tasinmazRepo,
        IUnitOfWork uow)
    {
        _tarifeRepo = tarifeRepo;
        _tasinmazRepo = tasinmazRepo;
        _uow = uow;
    }

    public async Task<TasinmazFiyatMatrisiViewModel> GetMatrisiAsync(int tasinmazId, int page = 1, int pageSize = 10)
    {
        Tasinmaz? tasinmaz = null;
        if (tasinmazId > 0)
        {
            tasinmaz = await _tasinmazRepo.GetByIdAsync(tasinmazId);
            if (tasinmaz == null) throw new ArgumentException("Taşınmaz bulunamadı");
        }

        var kiraciKategorileri = await _tarifeRepo.GetKiraciKategorileriAsync();
        var borcTipleri = await _tarifeRepo.GetBorcTipleriMatrisIcinAsync();
        var mevcutFiyatlar = await _tarifeRepo.GetByTasinmazIdAsync(tasinmazId);

        var vm = new TasinmazFiyatMatrisiViewModel
        {
            TasinmazId = tasinmazId,
            TasinmazAd = tasinmaz?.Ad ?? "Yeni Taşınmaz",
            Kolonlar = borcTipleri.Select(b => new BorcTipiFiyatKolonuViewModel
            {
                BorcTipiId = b.Id,
                BorcTipiAd = b.Ad,
                BorcTipiKod = b.Kod,
                BorcTipiDavranisi = b.Davranis
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
                var fiyat = mevcutFiyatlar.FirstOrDefault(f => f.KiraciKategoriId == kk.Id && f.BorcTipiId == bt.Id);
                if (fiyat != null)
                {
                    satir.Hucreler.Add(new TasinmazFiyatHucreViewModel
                    {
                        TasinmazTarifeId = fiyat.Id,
                        TasinmazId = tasinmazId,
                        KiraciKategoriId = kk.Id,
                        BorcTipiId = bt.Id,
                        BirimDeger = fiyat.BirimDeger,
                        HesaplamaYontemi = fiyat.HesaplamaYontemi,
                        KdvOrani = fiyat.KdvOrani,
                        Aktif = fiyat.Aktif,
                        Aciklama = fiyat.Aciklama,
                        RateVarMi = true
                    });
                }
                else
                {
                    satir.Hucreler.Add(new TasinmazFiyatHucreViewModel
                    {
                        TasinmazTarifeId = null,
                        TasinmazId = tasinmazId,
                        KiraciKategoriId = kk.Id,
                        BorcTipiId = bt.Id,
                        BirimDeger = 0m,
                        HesaplamaYontemi = HesaplamaYontemi.Sabit,
                        KdvOrani = bt.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik ? 0m : 20m,
                        Aktif = true,
                        Aciklama = null,
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

    public async Task SaveMatrisiAsync(int tasinmazId, TasinmazFiyatMatrisiViewModel model, string userId)
    {
        foreach (var satir in model.Satirlar)
        {
            foreach (var hucre in satir.Hucreler)
            {
                if (hucre.TasinmazTarifeId.HasValue)
                {
                    var entity = await _tarifeRepo.GetByIdAsync(hucre.TasinmazTarifeId.Value);
                    if (entity != null)
                    {
                        entity.BirimDeger = hucre.BirimDeger;
                        entity.HesaplamaYontemi = hucre.HesaplamaYontemi;
                        entity.KdvOrani = hucre.KdvOrani;
                        entity.Aktif = hucre.Aktif;
                        entity.Aciklama = hucre.Aciklama;
                    }
                }
                else
                {
                    var newEntity = new TasinmazTarife
                    {
                        TasinmazId = tasinmazId,
                        KiraciKategoriId = hucre.KiraciKategoriId,
                        BorcTipiId = hucre.BorcTipiId,
                        BirimDeger = hucre.BirimDeger,
                        HesaplamaYontemi = hucre.HesaplamaYontemi,
                        KdvOrani = hucre.KdvOrani,
                        Aktif = hucre.Aktif,
                        Aciklama = hucre.Aciklama
                    };
                    await _tarifeRepo.AddAsync(newEntity);
                }
            }
        }
        await _uow.SaveChangesAsync();
    }
}
