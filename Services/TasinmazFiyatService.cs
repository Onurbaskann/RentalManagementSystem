using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services
{
    public class TasinmazFiyatService : Interfaces.ITasinmazFiyatService
    {
        private readonly ApplicationDbContext _ctx;
        public TasinmazFiyatService(ApplicationDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<TasinmazFiyatMatrisiViewModel> GetMatrisiAsync(int tasinmazId, int page = 1, int pageSize = 10)
        {
            Tasinmaz? tasinmaz = null;
            if (tasinmazId > 0)
            {
                tasinmaz = await _ctx.Tasinmazlar.FirstOrDefaultAsync(t => t.Id == tasinmazId);
                if (tasinmaz == null) throw new ArgumentException("Taşınmaz bulunamadı");
            }

            // Aktif Kiracı Kategorileri (tümünü alıyoruz, pagination sonrası dilimleyeceğiz)
            var kiraciKategorileri = await _ctx.KiraciKategorileri
                .OrderBy(k => k.Ad)
                .ToListAsync();

            var borcTipleri = await _ctx.BorcTipleri
                .Where(b => b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
                .OrderBy(b => b.Sira)
                .ToListAsync();

            // Mevcut fiyat kayıtları
            var mevcutFiyatlar = await _ctx.TasinmazKiraciKategoriFiyatlari
                .Where(f => f.TasinmazId == tasinmazId)
                .ToListAsync();

            // Sütunları doldur
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

            // Satırları (Kiracı Kategorileri) oluştur
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
                            TasinmazKiraciKategoriFiyatId = fiyat.Id,
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
                            TasinmazKiraciKategoriFiyatId = null,
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

            // Pagination on KiraciKategori rows
            var totalRows = satirList.Count;
            var skip = (page - 1) * pageSize;
            vm.TotalRows = totalRows;
            vm.Satirlar = satirList.Skip(skip).Take(pageSize).ToList();

            // Store pagination data in ViewBag later in controller
            return vm;
        }

        public async Task SaveMatrisiAsync(int tasinmazId, TasinmazFiyatMatrisiViewModel model, string userId)
        {
            // Transactional upsert
            using var transaction = await _ctx.Database.BeginTransactionAsync();
            foreach (var satir in model.Satirlar)
            {
                foreach (var hucre in satir.Hucreler)
                {
                    if (hucre.TasinmazKiraciKategoriFiyatId.HasValue)
                    {
                        // Update existing record
                        var entity = await _ctx.TasinmazKiraciKategoriFiyatlari
                            .FirstOrDefaultAsync(f => f.Id == hucre.TasinmazKiraciKategoriFiyatId.Value);
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
                        // Insert new record only if user entered a value (or wants to create empty record)
                        var newEntity = new TasinmazKiraciKategoriFiyat
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
                        await _ctx.TasinmazKiraciKategoriFiyatlari.AddAsync(newEntity);
                    }
                }
            }
            await _ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }
}
