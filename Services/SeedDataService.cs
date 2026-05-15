using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class SeedDataService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ITahakkukUretimService _tahakkukUretim;
    private readonly IRateResolverService _rateResolver;

    public SeedDataService(ApplicationDbContext ctx, ITahakkukUretimService tahakkukUretim, IRateResolverService rateResolver)
    {
        _ctx = ctx;
        _tahakkukUretim = tahakkukUretim;
        _rateResolver = rateResolver;
    }

    public async Task SeedBorcTipleriAsync()
    {
        var existingCodes = await _ctx.BorcTipleri.Select(b => b.Kod).ToListAsync();
        var toAdd = new List<BorcTipi>();

        if (!existingCodes.Contains("KIRA")) toAdd.Add(new BorcTipi { Ad = "Kira Bedeli", Kod = "KIRA", Aktif = true, Sira = 1, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = true });
        if (!existingCodes.Contains("DIGER")) toAdd.Add(new BorcTipi { Ad = "Diğer", Kod = "DIGER", Aktif = true, Sira = 100, Davranis = BorcTipiDavranisi.KullaniciManuel, Sistem = true });
        if (!existingCodes.Contains("ORTAK")) toAdd.Add(new BorcTipi { Ad = "Ortak Gider", Kod = "ORTAK", Aktif = true, Sira = 2, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = false });
        if (!existingCodes.Contains("PORTAL")) toAdd.Add(new BorcTipi { Ad = "Portal Gideri", Kod = "PORTAL", Aktif = true, Sira = 3, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = false });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BorcTipi { Ad = "Toplantı Salonu Kullanım Bedeli", Kod = "TOPLANTI", Aktif = true, Sira = 4, Davranis = BorcTipiDavranisi.RezervasyonOzel, Sistem = false });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new BorcTipi { Ad = "Etkinlik Alanı Kullanım Bedeli", Kod = "ETKINLIK", Aktif = true, Sira = 5, Davranis = BorcTipiDavranisi.RezervasyonOzel, Sistem = false });
        if (!existingCodes.Contains("DEPOZITO")) toAdd.Add(new BorcTipi { Ad = "Depozito", Kod = "DEPOZITO", Aktif = true, Sira = 99, Davranis = BorcTipiDavranisi.IlkAyTekSeferlik, Sistem = false });

        if (toAdd.Any())
        {
            _ctx.BorcTipleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }

        // Mevcut kayıtların sistem bayraklarını ve davranışlarını doğrula (Idempotency)
        await _ctx.BorcTipleri.Where(b => b.Kod == "KIRA").ExecuteUpdateAsync(s => s.SetProperty(b => b.Sistem, true).SetProperty(b => b.Davranis, BorcTipiDavranisi.AylikSabit));
        await _ctx.BorcTipleri.Where(b => b.Kod == "DIGER").ExecuteUpdateAsync(s => s.SetProperty(b => b.Sistem, true).SetProperty(b => b.Davranis, BorcTipiDavranisi.KullaniciManuel));
        await _ctx.BorcTipleri.Where(b => b.Kod == "TOPLANTI").ExecuteUpdateAsync(s => s.SetProperty(b => b.Davranis, BorcTipiDavranisi.RezervasyonOzel));
        await _ctx.BorcTipleri.Where(b => b.Kod == "ETKINLIK").ExecuteUpdateAsync(s => s.SetProperty(b => b.Davranis, BorcTipiDavranisi.RezervasyonOzel));
    }

    public async Task EnsureVarsayilanRezervasyonGenelTarifeAsync()
    {
        var cariYil = DateTime.Now.Year;
        // Hardcode Değerler - Değişken üzerinden yönetim
        var varsayilanUcret = 500m;
        var varsayilanUcretsizSure = 120;
        var varsayilanPeriyot = 60;
        var varsayilanKdv = 20m;

        var tarife = await _ctx.Tarifeler
            .FirstOrDefaultAsync(t => t.Yil == cariYil);
        if (tarife == null) return;

        var rezBirimTurleri = await _ctx.BirimTurleri
            .Where(t => t.Aktif && t.RezervasyonYapilabilirMi)
            .ToListAsync();
        if (!rezBirimTurleri.Any()) return;

        var mevcut = await _ctx.RezervasyonGenelTarifeleri
            .Where(r => r.TarifeId == tarife.Id)
            .Select(r => r.BirimTuruId)
            .ToListAsync();

        foreach (var bt in rezBirimTurleri.Where(b => !mevcut.Contains(b.Id)))
        {
            _ctx.RezervasyonGenelTarifeleri.Add(new RezervasyonGenelTarife
            {
                TarifeId                    = tarife.Id,
                BirimTuruId                 = bt.Id,
                UcretsizSureDakika          = varsayilanUcretsizSure,
                UcretlendirmePeriyoduDakika = varsayilanPeriyot,
                PeriyotUcreti               = varsayilanUcret,
                KdvOrani                    = varsayilanKdv,
                Aciklama                    = $"{cariYil} varsayılan — {bt.Ad}",
                OlusturmaTarihi             = DateTime.UtcNow
            });
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedTasinmazTipleriAsync()
    {
        var existingCodes = await _ctx.TasinmazTipleri.Select(t => t.Kod).ToListAsync();
        var toAdd = new List<TasinmazTipi>();

        if (!existingCodes.Contains("BINA")) toAdd.Add(new TasinmazTipi { Ad = "Bina", Kod = "BINA", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("OTOMAT")) toAdd.Add(new TasinmazTipi { Ad = "Otomat", Kod = "OTOMAT", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("BANKAMATIK")) toAdd.Add(new TasinmazTipi { Ad = "Bankamatik", Kod = "BANKAMATIK", Aktif = true, Sira = 3, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.TasinmazTipleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }

        await SeedTasinmazTipiKiralamaSekilleriAsync();
    }

    private async Task SeedTasinmazTipiKiralamaSekilleriAsync()
    {
        var defaults = new Dictionary<string, KiralamaSekli[]>
        {
            ["BINA"] = new[] { KiralamaSekli.TekParca, KiralamaSekli.BirimBazli },
            ["OTOMAT"] = new[] { KiralamaSekli.TekParca },
            ["BANKAMATIK"] = new[] { KiralamaSekli.TekParca }
        };

        var tipler = await _ctx.TasinmazTipleri
            .Where(t => defaults.Keys.Contains(t.Kod))
            .Select(t => new { t.Id, t.Kod })
            .ToListAsync();

        var existing = await _ctx.TasinmazTipiKiralamaSekilleri
            .Select(x => new { x.TasinmazTipiId, x.KiralamaSekli })
            .ToListAsync();

        var toAdd = new List<TasinmazTipiKiralamaSekli>();
        foreach (var tip in tipler)
        {
            foreach (var sekli in defaults[tip.Kod])
            {
                if (!existing.Any(e => e.TasinmazTipiId == tip.Id && e.KiralamaSekli == sekli))
                    toAdd.Add(new TasinmazTipiKiralamaSekli { TasinmazTipiId = tip.Id, KiralamaSekli = sekli });
            }
        }

        if (toAdd.Any())
        {
            _ctx.TasinmazTipiKiralamaSekilleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedBirimTurleriAsync()
    {
        var existingCodes = await _ctx.BirimTurleri.Select(t => t.Kod).ToListAsync();

        var toplantiBorcTipiId = await _ctx.BorcTipleri
            .Where(b => b.Kod == "TOPLANTI")
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        var etkinliBorcTipiId = await _ctx.BorcTipleri
            .Where(b => b.Kod == "ETKINLIK")
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        var toAdd = new List<BirimTuru>();
        if (!existingCodes.Contains("OFIS"))     toAdd.Add(new BirimTuru { Ad = "Ofis",            Kod = "OFIS",     Aktif = true, KiralanabilirMi = true,  RezervasyonYapilabilirMi = false, Sira = 1,  OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BirimTuru { Ad = "Toplantı Salonu", Kod = "TOPLANTI", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true,  Sira = 10, OlusturmaTarihi = DateTime.UtcNow, BorcTipiId = toplantiBorcTipiId });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new BirimTuru { Ad = "Etkinlik Alanı",  Kod = "ETKINLIK", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true,  Sira = 11, OlusturmaTarihi = DateTime.UtcNow, BorcTipiId = etkinliBorcTipiId });

        if (toAdd.Any())
        {
            _ctx.BirimTurleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }

        if (toplantiBorcTipiId.HasValue)
        {
            await _ctx.BirimTurleri
                .Where(t => t.Kod == "TOPLANTI" && t.BorcTipiId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.BorcTipiId, toplantiBorcTipiId));
        }

        if (etkinliBorcTipiId.HasValue)
        {
            await _ctx.BirimTurleri
                .Where(t => t.Kod == "ETKINLIK" && t.BorcTipiId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.BorcTipiId, etkinliBorcTipiId));
        }
    }

    public async Task SeedKiraciKategorileriAsync()
    {
        var existingCodes = await _ctx.KiraciKategorileri.Select(k => k.Kod).ToListAsync();
        var toAdd = new List<KiraciKategori>();

        if (!existingCodes.Contains("AKADEMISYEN")) toAdd.Add(new KiraciKategori { Ad = "Akademisyen", Kod = "AKADEMISYEN", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("AKAD_OLMAYAN")) toAdd.Add(new KiraciKategori { Ad = "Akademisyen Olmayan", Kod = "AKAD_OLMAYAN", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.KiraciKategorileri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedSektorlerAsync()
    {
        var existingCodes = await _ctx.Sektorler.Select(s => s.Kod).ToListAsync();
        var toAdd = new List<Sektor>();

        if (!existingCodes.Contains("YAZILIM")) toAdd.Add(new Sektor { Ad = "Yazılım", Kod = "YAZILIM", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("LOJISTIK")) toAdd.Add(new Sektor { Ad = "Lojistik", Kod = "LOJISTIK", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("GIDA")) toAdd.Add(new Sektor { Ad = "Gıda", Kod = "GIDA", Aktif = true, Sira = 3, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TARIM")) toAdd.Add(new Sektor { Ad = "Tarım", Kod = "TARIM", Aktif = true, Sira = 4, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("FINANS")) toAdd.Add(new Sektor { Ad = "Finans", Kod = "FINANS", Aktif = true, Sira = 5, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("EGITIM")) toAdd.Add(new Sektor { Ad = "Eğitim", Kod = "EGITIM", Aktif = true, Sira = 6, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("KAMU")) toAdd.Add(new Sektor { Ad = "Kamu", Kod = "KAMU", Aktif = true, Sira = 7, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.Sektorler.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedTarifelerAsync()
    {
        var cariYil = DateTime.Now.Year;
        if (await _ctx.Tarifeler.AnyAsync(t => t.Yil == cariYil)) return;

        var kategoriler = await _ctx.KiraciKategorileri
            .Where(k => k.Aktif)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        var borcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
            .OrderBy(b => b.Sira)
            .ToListAsync();

        if (!kategoriler.Any() || !borcTipleri.Any()) return;

        var tarife = new Tarife
        {
            Yil             = cariYil,
            Aciklama        = $"{cariYil} Yılı Genel Tarifesi",
            Aktif           = true,
            OlusturmaTarihi = DateTime.Now
        };

        foreach (var kat in kategoriler)
        {
            foreach (var bt in borcTipleri)
            {
                tarife.Kalemler.Add(new TarifeKalemi
                {
                    KiraciKategoriId = kat.Id,
                    BorcTipiId       = bt.Id,
                    HesaplamaYontemi = (bt.Kod == "KIRA" || bt.Kod == "ORTAK") ? HesaplamaYontemi.M2 : HesaplamaYontemi.Sabit,
                    BirimDeger       = bt.Kod switch
                    {
                        "KIRA"     => kat.Kod == "AKADEMISYEN" ? 300m : 400m,
                        "ORTAK"    => kat.Kod == "AKADEMISYEN" ? 100m : 150m, // m2 fiyatı olarak güncellendi
                        "PORTAL"   => kat.Kod == "AKADEMISYEN" ? 300m : 500m,
                        "DEPOZITO" => kat.Kod == "AKADEMISYEN" ? 8000m : 15000m,
                        _          => 0m
                    },
                    KdvOrani = bt.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik ? 0m : 20m
                });
            }
        }

        _ctx.Tarifeler.Add(tarife);
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedDomainDataAsync()
    {
        if (await _ctx.Tasinmazlar.AnyAsync()) return;

        var now = DateTime.Now;
        var tipiMap = await _ctx.TasinmazTipleri.ToDictionaryAsync(t => t.Kod, t => t.Id);
        var birimTuruMap = await _ctx.BirimTurleri.ToDictionaryAsync(t => t.Kod, t => t.Id);
        var katMap = await _ctx.KiraciKategorileri.ToDictionaryAsync(k => k.Kod, k => k.Id);
        var sekMap = await _ctx.Sektorler.ToDictionaryAsync(s => s.Kod, s => s.Id);

        // --- Kiracılar ---
        var ahmet = Kiraci("KRC-001", KiraciTuru.Gercek, katMap["AKADEMISYEN"], sekMap["EGITIM"], "Ahmet", "Yılmaz",
            tcNo: "12345678901", telefon: "0532 111 2233", email: "ahmet@ege.edu.tr", adres: "İzmir, Bornova");
        var ayse = Kiraci("KRC-002", KiraciTuru.Gercek, katMap["AKADEMISYEN"], sekMap["YAZILIM"], "Ayşe", "Demir",
            tcNo: "98765432100", telefon: "0533 222 3344", email: "ayse@ege.edu.tr", adres: "İzmir, Karşıyaka");
        var yzCozum = Kiraci("KRC-003", KiraciTuru.Tuzel, katMap["AKAD_OLMAYAN"], sekMap["YAZILIM"], "Yapay Zeka Çözümleri A.Ş.", null,
            vergiNo: "1234567890", ticaretSicilNo: "İZM-123", telefon: "0232 444 5566", email: "info@yz.com", adres: "Teknokent");
        var biyoLab = Kiraci("KRC-004", KiraciTuru.Tuzel, katMap["AKADEMISYEN"], sekMap["YAZILIM"], "BiyoTek Laboratuvarları Ltd.", null,
            vergiNo: "9876543210", ticaretSicilNo: "İZM-456", telefon: "0232 555 6677", email: "info@biyotek.com", adres: "Teknokent");
        var veriBilisim = Kiraci("KRC-005", KiraciTuru.Tuzel, katMap["AKAD_OLMAYAN"], sekMap["YAZILIM"], "Veri Bilişim A.Ş.", null,
            vergiNo: "5556667770", ticaretSicilNo: "İZM-789", telefon: "0232 666 7788", email: "iletisim@veribilisim.com", adres: "Teknokent");

        _ctx.Kiraciler.AddRange(ahmet, ayse, yzCozum, biyoLab, veriBilisim);

        // --- Taşınmaz (Teknokent A Blok) ---
        var ofisTuruId = birimTuruMap["OFIS"];
        var toplantiTuruId = birimTuruMap["TOPLANTI"];

        var teknokent = new Tasinmaz
        {
            Ad = "Teknokent A Blok", TasinmazTipiId = tipiMap.GetValueOrDefault("BINA"), KiralamaSekli = KiralamaSekli.BirimBazli,
            Il = "İzmir", Ilce = "Bornova", Mahalle = "Ege Üniversitesi", AcikAdres = "Ege Üniversitesi Teknokent Kampüsü",
            AcikYuzolcumu = 500, KapaliYuzolcumu = 4500, KatSayisi = 4, Aciklama = "Ofis bazlı kiralanabilir teknokent binası",
            KayitTarihi = now.AddMonths(-36)
        };

        // 16 Ofis Ekleme
        for (int kat = 1; kat <= 4; kat++)
        {
            for (int ofis = 1; ofis <= 4; ofis++)
            {
                var ofisNo = $"{kat}0{ofis}";
                teknokent.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Birim, BirimNo = ofisNo, KatNo = kat,
                    Ad = $"Ofis {ofisNo}", Yuzolcumu = 60 + (ofis * 10), BirimTuruId = ofisTuruId
                });
            }
        }
        
        // 1 Ana Toplantı Salonu
        var toplantiZ01 = new Birim
        {
            BirimTipi = BirimTipi.Birim, BirimNo = "Z01", KatNo = 0,
            Ad = "Ana Toplantı Salonu", Yuzolcumu = 150, BirimTuruId = toplantiTuruId, Aciklama = "Ortak kullanıma açık ana rezervasyon alanı."
        };
        teknokent.Birimler.Add(toplantiZ01);

        _ctx.Tasinmazlar.Add(teknokent);
        await _ctx.SaveChangesAsync();

        // Toplantı salonu için ücret kuralı ekle (Değişken üzerinden yönetim)
        var salonUcret = 600m;
        var salonUcretsizSure = 120;
        var salonPeriyot = 60;
        var salonKdv = 20m;

        _ctx.RezervasyonUcretKurallari.Add(new RezervasyonUcretKural
        {
            BirimId = toplantiZ01.Id, // Doğrudan nesne referansı üzerinden Id
            UcretsizSureDakika = salonUcretsizSure,
            UcretlendirmePeriyoduDakika = salonPeriyot,
            PeriyotUcreti = salonUcret,
            KdvOrani = salonKdv,
            Aktif = true,
            OlusturmaTarihi = now,
            Aciklama = "Seed — Ana Toplantı Salonu için varsayılan kural"
        });
        await _ctx.SaveChangesAsync();

        // --- 4. Tarifelerin Oluşturulması (Hiyerarşik Sıralama İçin Önce Bunlar Gelmeli) ---
        await SeedTasinmazFiyatlarAsync();

        var btKiraId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "KIRA")).Id;
        var btDepozitoId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "DEPOZITO")).Id;

        var birim101 =teknokent.Birimler.First(b => b.BirimNo == "101");
        var birim102 =teknokent.Birimler.First(b => b.BirimNo == "102");
        var birim201 =teknokent.Birimler.First(b => b.BirimNo == "201");
        var birim301 =teknokent.Birimler.First(b => b.BirimNo == "301");
        var birim302 =teknokent.Birimler.First(b => b.BirimNo == "302");
        var birim401 =teknokent.Birimler.First(b => b.BirimNo == "401");

        // 4.3 Birim Tarifesi Örneği (Hiyerarşide Matrisin Üstündedir)
        // Ofis 201 için Akademisyen kategorisinde özel birim fiyatı tanımlayalım
        _ctx.BirimRateler.Add(new BirimRate
        {
            BirimId = birim201.Id,
            KiraciKategoriId = katMap["AKADEMISYEN"],
            BorcTipiId = btKiraId,
            HesaplamaYontemi = HesaplamaYontemi.M2,
            BirimDeger = 400, // Matris 350 yerine birim bazlı 400
            KdvOrani = 20
        });
        await _ctx.SaveChangesAsync();

        // Yardımcı fonksiyon: Dinamik bedel çözünürlüğü
        async Task<decimal> ResolveKiraBedeli(Birim b, Kiraci k)
        {
            var res = await _rateResolver.ResolveAsync(null, k.Id, b.Id, btKiraId, now);
            if (res == null) return 0;
            return res.HesaplamaYontemi == HesaplamaYontemi.M2 
                ? Math.Round(res.BirimDeger * b.Yuzolcumu, 2) 
                : res.BirimDeger;
        }

        // --- 5. Sözleşmelerin Oluşturulması ---
        // Tarih aralığı: Mevcut yıl - 1'den başlar
        var startYearMinus1 = new DateTime(now.Year - 1, 1, 1);

        // Bedelleri önceden çöz (SozlesmeRate seed'inde kullanmak için)
        var bedel201 = await ResolveKiraBedeli(birim201, ahmet);
        var bedel301 = await ResolveKiraBedeli(birim301, biyoLab);
        var bedel401 = await ResolveKiraBedeli(birim401, veriBilisim);
        var bedel102 = await ResolveKiraBedeli(birim102, ayse);
        var bedel302 = await ResolveKiraBedeli(birim302, yzCozum);

        var sozlesmeler = new List<KiraSozlesmesi>
        {
            // Ofis 101: Matris/Birim üzerinden bedel alacak, aşağıda Sözleşme Tarifesi ile ezilecek
            MakeSozlesme(birim101, yzCozum, startYearMinus1, startYearMinus1.AddYears(2), 50000, true),

            // Diğerleri tamamen hiyerarşiyi (Birim -> Matris -> Genel) takip edecek
            MakeSozlesme(birim201, ahmet, startYearMinus1.AddMonths(3), startYearMinus1.AddMonths(24), 25000, false),
            MakeSozlesme(birim301, biyoLab, startYearMinus1.AddMonths(6), startYearMinus1.AddMonths(18), 60000, true),
            MakeSozlesme(birim401, veriBilisim, startYearMinus1.AddMonths(1), startYearMinus1.AddMonths(13), 44000, true),

            // Süresi dolan/dolmak üzere olanlar
            MakeSozlesme(birim102, ayse, startYearMinus1.AddMonths(2), now.AddDays(15), 16000, false),
            MakeSozlesme(birim302, yzCozum, startYearMinus1.AddMonths(0), now.AddDays(-5), 40000, true),
        };

        foreach (var s in sozlesmeler)
        {
            if (s.BitisTarihi < now) s.Durum = SozlesmeDurumu.SonaErdi;
        }

        _ctx.Sozlesmeler.AddRange(sozlesmeler);
        await _ctx.SaveChangesAsync();

        // --- 6. Sözleşme Tarifesi (Özel Oran) Uygulaması ---
        // Ofis 101 / YZ Çözüm için Sözleşme Tarifesi ekleyelim
        var targetSozlesme = sozlesmeler[0];

        _ctx.SozlesmeRateler.AddRange(
            new SozlesmeRate
            {
                SozlesmeId = targetSozlesme.Id,
                BorcTipiId = btKiraId,
                BirimDeger = 360, // 25200 / 70m2 = 360
                HesaplamaYontemi = HesaplamaYontemi.M2,
                KdvOrani = 20
            },
            new SozlesmeRate
            {
                SozlesmeId = targetSozlesme.Id,
                BorcTipiId = btDepozitoId,
                BirimDeger = 40000,
                HesaplamaYontemi = HesaplamaYontemi.Sabit,
                KdvOrani = 0
            }
        );

        // Diğer sözleşmeler için Sabit kira rate'i ekle
        _ctx.SozlesmeRateler.AddRange(
            new SozlesmeRate { SozlesmeId = sozlesmeler[1].Id, BorcTipiId = btKiraId, BirimDeger = bedel201, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },
            new SozlesmeRate { SozlesmeId = sozlesmeler[2].Id, BorcTipiId = btKiraId, BirimDeger = bedel301, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new SozlesmeRate { SozlesmeId = sozlesmeler[3].Id, BorcTipiId = btKiraId, BirimDeger = bedel401, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new SozlesmeRate { SozlesmeId = sozlesmeler[4].Id, BorcTipiId = btKiraId, BirimDeger = bedel102, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },
            new SozlesmeRate { SozlesmeId = sozlesmeler[5].Id, BorcTipiId = btKiraId, BirimDeger = bedel302, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 }
        );
        await _ctx.SaveChangesAsync();

        // --- 7. Tahakkuk Üretimi ---
        // Tüm aktif sözleşmeler için tahakkukları üret
        foreach (var s in sozlesmeler.Where(s => s.Durum != SozlesmeDurumu.Feshedildi))
        {
            await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
        }

        // --- 8. Diğer Seed İşlemleri ---
        await SeedRezervasyonlarAsync();
        await SeedBankaHareketleriAsync();
        await SeedTahakkuklarVeOdemelerAsync(sozlesmeler);
    }

    public async Task SeedTasinmazFiyatlarAsync()
    {
        var teknokent = await _ctx.Tasinmazlar.FirstOrDefaultAsync(t => t.Ad == "Teknokent A Blok");
        if (teknokent == null) return;

        if (await _ctx.TasinmazKiraciKategoriFiyatlari.AnyAsync(f => f.TasinmazId == teknokent.Id)) return;

        var katAkademisyen = await _ctx.KiraciKategorileri.FirstAsync(k => k.Kod == "AKADEMISYEN");
        var katAkadOlmayan = await _ctx.KiraciKategorileri.FirstAsync(k => k.Kod == "AKAD_OLMAYAN");

        var btKira = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "KIRA");
        var btOrtak = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "ORTAK");
        var btPortal = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "PORTAL");
        var btDepozito = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "DEPOZITO");

        _ctx.TasinmazKiraciKategoriFiyatlari.AddRange(
            // Akademisyen için (m2 bazlı kira ve ortak gider)
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btKira.Id, BirimDeger = 350, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btOrtak.Id, BirimDeger = 100, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btPortal.Id, BirimDeger = 500, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btDepozito.Id, BirimDeger = 10000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },

            // Akademisyen Olmayan için (m2 bazlı kira ve ortak gider)
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btKira.Id, BirimDeger = 450, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btOrtak.Id, BirimDeger = 150, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btPortal.Id, BirimDeger = 750, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btDepozito.Id, BirimDeger = 25000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 }
        );

        await _ctx.SaveChangesAsync();
    }

    public async Task SeedTahakkuklarAsync()
    {
        // Geriye dönük uyumluluk için (UretSozlesmeIcinAsync SeedDomainDataAsync içinde çağrılıyor)
        if (await _ctx.KiraTahakkuklar.AnyAsync()) return;
        var aktifSozlesmeler = await _ctx.Sozlesmeler.Where(s => s.Durum == SozlesmeDurumu.Aktif).ToListAsync();
        foreach (var s in aktifSozlesmeler) await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
    }

    private async Task SeedTahakkuklarVeOdemelerAsync(List<KiraSozlesmesi> sozlesmeler)
    {
        try
        {
            var adminUser = await _ctx.Users.FirstOrDefaultAsync();
            var adminId = adminUser?.Id ?? "admin-id-missing";

            // 1. Manuel Borçlar ve İptaller
            var manuelBorcTipi = await _ctx.BorcTipleri.FirstOrDefaultAsync(b => b.Kod == "DIGER");
            if (manuelBorcTipi != null)
            {
                var targetSozlesme = sozlesmeler.First();
                _ctx.KiraTahakkuklar.Add(new KiraTahakkuk
                {
                    KiraSozlesmesiId = targetSozlesme.Id,
                    DonemBaslangic = DateTime.Today.AddDays(-5), DonemBitis = DateTime.Today, VadeTarihi = DateTime.Today.AddDays(15),
                    BeklenenTutar = 2500m, KdvTutari = 500m, ToplamTutar = 3000m, OdenenTutar = 0m,
                    Durum = TahakkukDurumu.Bekleniyor, KaynakTipi = TahakkukKaynakTipi.Manuel, OlusturmaTarihi = DateTime.Now,
                    Kalemler = new List<TahakkukKalemi> { new TahakkukKalemi { BorcTipiId = manuelBorcTipi.Id, Aciklama = "Ekstra Temizlik Bedeli", BirimDeger = 2500m, Carpan = 1m, Tutar = 2500m, KdvOrani = 20m, KdvTutari = 500m, ToplamTutar = 3000m, KaynakTipi = KalemKaynakTipi.ManuelGiris } }
                });

                // İptal Edilen Kayıt
                _ctx.KiraTahakkuklar.Add(new KiraTahakkuk
                {
                    KiraSozlesmesiId = targetSozlesme.Id,
                    DonemBaslangic = DateTime.Today.AddMonths(-1), DonemBitis = DateTime.Today.AddMonths(-1), VadeTarihi = DateTime.Today.AddMonths(-1),
                    BeklenenTutar = 500m, KdvTutari = 100m, ToplamTutar = 600m, OdenenTutar = 0m,
                    Durum = TahakkukDurumu.IptalEdildi, KaynakTipi = TahakkukKaynakTipi.Manuel, OlusturmaTarihi = DateTime.Now, IptalNotu = "Hatalı giriş nedeniyle iptal edildi.",
                    Kalemler = new List<TahakkukKalemi> { new TahakkukKalemi { BorcTipiId = manuelBorcTipi.Id, Aciklama = "Yanlış Borç Kaydı", BirimDeger = 500m, Tutar = 500m, KdvOrani = 20m, ToplamTutar = 600m, KaynakTipi = KalemKaynakTipi.ManuelGiris } }
                });
            }

            await _ctx.SaveChangesAsync();

            // 2. Geçmiş Yıl Ödemeleri (%90 ve %95 oranları)
            var currentYear = DateTime.Today.Year;
            await SeedGecmisYilOdemeleriAsync(currentYear - 1, 0.90, adminId);
            await SeedGecmisYilOdemeleriAsync(currentYear, 0.60, adminId);
            await SeedGecmisYilOdemeleriAsync(currentYear + 1, 0.05, adminId);

            // 4. Kısmi Ödemeler
            await SeedKismiOdemelerAsync(adminId);

            await _ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in SeedTahakkuklarVeOdemelerAsync: {ex.Message}");
            throw;
        }
    }

    private async Task SeedGecmisYilOdemeleriAsync(int yil, double oran, string adminId)
    {
        var query = _ctx.KiraTahakkuklar
            .Where(t => t.DonemBaslangic.Year == yil && t.Durum == TahakkukDurumu.Bekleniyor);

        // Eğer cari yıl ise (2026), sadece bugünü ve geçmiş ayları öde (Gerçekçilik için)
        if (yil == DateTime.Today.Year)
        {
            query = query.Where(t => t.DonemBaslangic <= DateTime.Today);
        }

        var tahakkuklar = await query.ToListAsync();

        if (!tahakkuklar.Any()) return;

        int odenecekAdet = (int)Math.Round(tahakkuklar.Count * oran);
        var secilenler = tahakkuklar.OrderBy(x => Guid.NewGuid()).Take(odenecekAdet).ToList();

        foreach (var t in secilenler)
        {
            bool gecikmis = Random.Shared.Next(1, 10) > 7; // %30 ihtimalle gecikmiş ödeme
            bool kismiMi = Random.Shared.Next(1, 100) <= 30; // %30 ihtimalle kısmi ödeme
            
            decimal odemeTutari = t.ToplamTutar;
            if (kismiMi)
            {
                // %25 ile %75 arasında rastgele bir tutar ödensin
                var kismiOran = Random.Shared.Next(25, 76) / 100m;
                odemeTutari = Math.Round(t.ToplamTutar * kismiOran, 2);
            }

            var odeme = new KiraOdeme
            {
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                KiraTahakkukId = t.Id,
                OdemeTarihi = gecikmis ? t.VadeTarihi.AddDays(Random.Shared.Next(15, 45)) : t.VadeTarihi.AddDays(Random.Shared.Next(-5, 5)),
                Tutar = odemeTutari,
                OdemeKanali = (OdemeKanali)Random.Shared.Next(1, 5),
                Durum = OdemeDurumu.Onaylandi,
                Aciklama = (kismiMi ? "Kısmi " : "") + (gecikmis ? "gecikmeli seed ödemesi" : "zamanında seed ödemesi"),
                GirenUserId = adminId
            };

            t.OdenenTutar = odemeTutari;
            t.Durum = kismiMi ? TahakkukDurumu.KismenOdendi : TahakkukDurumu.TamOdendi;
            _ctx.KiraOdemeler.Add(odeme);
        }
    }

    private async Task SeedKismiOdemelerAsync(string adminId)
    {
        var bekleyenler = await _ctx.KiraTahakkuklar
            .Where(t => t.Durum == TahakkukDurumu.Bekleniyor)
            .Take(3)
            .ToListAsync();

        foreach (var t in bekleyenler)
        {
            var kismiTutar = Math.Round(t.ToplamTutar / 2, 2);
            var odeme = new KiraOdeme
            {
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                KiraTahakkukId = t.Id,
                OdemeTarihi = DateTime.Today.AddDays(-2),
                Tutar = kismiTutar,
                OdemeKanali = OdemeKanali.EFT,
                Durum = OdemeDurumu.Onaylandi,
                Aciklama = "Seed kısmi ödeme",
                GirenUserId = adminId
            };
            t.OdenenTutar = kismiTutar;
            t.Durum = TahakkukDurumu.KismenOdendi;
            _ctx.KiraOdemeler.Add(odeme);
        }
    }

    private async Task SeedRezervasyonlarAsync()
    {
        var salon = await _ctx.Birimler.Include(b => b.Tasinmaz).FirstOrDefaultAsync(b => b.Ad == "Ana Toplantı Salonu");
        var kiraci = await _ctx.Kiraciler.FirstOrDefaultAsync();
        var sozlesme = await _ctx.Sozlesmeler.FirstOrDefaultAsync(s => s.KiraciId == kiraci.Id);
        
        if (salon == null || kiraci == null) return;

        var btRezervasyon = await _ctx.BorcTipleri.FirstOrDefaultAsync(b => b.Kod == "TOPLANTI");

        // 1. Geçmiş Rezervasyon (Tahakkuka Aktarıldı)
        var rezervasyon1 = new ToplantiSalonuRezervasyon
        {
            BirimId = salon.Id,
            KiraciId = kiraci.Id,
            KiraSozlesmesiId = sozlesme?.Id,
            BaslangicTarihi = DateTime.Today.AddDays(-10).AddHours(10),
            BitisTarihi = DateTime.Today.AddDays(-10).AddHours(13),
            ToplamSureDakika = 180,
            UcretsizSureDakika = 60,
            UcretliSureDakika = 120,
            BirimUcret = 500,
            UcretTutar = 1000,
            KdvOrani = 20,
            KdvTutari = 200,
            ToplamTutar = 1200,
            Durum = RezervasyonDurumu.TahakkukaAktarildi,
            OlusturmaTarihi = DateTime.Now.AddDays(-15)
        };
        _ctx.ToplantiSalonuRezervasyonlari.Add(rezervasyon1);
        await _ctx.SaveChangesAsync();

        if (btRezervasyon != null)
        {
            var tahakkuk = new KiraTahakkuk
            {
                KiraSozlesmesiId = sozlesme?.Id,
                DonemBaslangic = rezervasyon1.BaslangicTarihi.Date,
                DonemBitis = rezervasyon1.BitisTarihi.Date,
                VadeTarihi = rezervasyon1.BitisTarihi.Date,
                BeklenenTutar = 1000,
                KdvTutari = 200,
                ToplamTutar = 1200,
                OdenenTutar = 0,
                Durum = TahakkukDurumu.Bekleniyor,
                KaynakTipi = TahakkukKaynakTipi.Rezervasyon,
                OlusturmaTarihi = DateTime.Now.AddDays(-10),
                Kalemler = new List<TahakkukKalemi>
                {
                    new TahakkukKalemi
                    {
                        BorcTipiId = btRezervasyon.Id,
                        Aciklama = $"Toplantı salonu: {salon.Ad} ({rezervasyon1.BaslangicTarihi:dd.MM.yyyy HH:mm} – {rezervasyon1.BitisTarihi:HH:mm})",
                        HesaplamaYontemi = HesaplamaYontemi.Sabit,
                        BirimDeger = 1000,
                        Carpan = 1,
                        Tutar = 1000,
                        KdvOrani = 20,
                        KdvTutari = 200,
                        ToplamTutar = 1200,
                        KaynakTipi = KalemKaynakTipi.RezervasyonKurali
                    }
                }
            };
            _ctx.KiraTahakkuklar.Add(tahakkuk);
            await _ctx.SaveChangesAsync();

            rezervasyon1.KiraTahakkukId = tahakkuk.Id;
            await _ctx.SaveChangesAsync();
        }

        // 2. Gelecek Rezervasyon (Planlandı)
        _ctx.ToplantiSalonuRezervasyonlari.Add(new ToplantiSalonuRezervasyon
        {
            BirimId = salon.Id,
            KiraciId = kiraci.Id,
            KiraSozlesmesiId = sozlesme?.Id,
            BaslangicTarihi = DateTime.Today.AddDays(3).AddHours(14),
            BitisTarihi = DateTime.Today.AddDays(3).AddHours(17),
            ToplamSureDakika = 180,
            UcretsizSureDakika = 60,
            UcretliSureDakika = 120,
            BirimUcret = 500,
            UcretTutar = 1000,
            KdvOrani = 20,
            KdvTutari = 200,
            ToplamTutar = 1200,
            Durum = RezervasyonDurumu.Planlandi,
            OlusturmaTarihi = DateTime.Now
        });

        await _ctx.SaveChangesAsync();
    }

    private async Task SeedBankaHareketleriAsync()
    {
        var admin = await _ctx.Users.FirstOrDefaultAsync();
        var adminId = admin?.Id ?? "";

        // Eşleşmiş Hareket
        _ctx.BankaHareketleri.Add(new BankaHareketi
        {
            HareketTarihi = DateTime.Today.AddDays(-1), Tutar = 1500, Aciklama = "KİRA ÖDEMESİ - TEKNOKENT",
            KarsiUnvan = "Yapay Zeka Çözümleri A.Ş.", BankaKodu = "TR01", EslesmeDurumu = BankaEslesmeDurumu.Eslesti,
            ImportBatchId = Guid.NewGuid(), ImportEdenUserId = adminId
        });

        // Eşleşmemiş (Açıkta) Hareket
        _ctx.BankaHareketleri.Add(new BankaHareketi
        {
            HareketTarihi = DateTime.Today.AddDays(-2), Tutar = 5000, Aciklama = "HAVALE - BİLİNMEYEN",
            KarsiHesap = "TR123456789...", BankaKodu = "TR01", EslesmeDurumu = BankaEslesmeDurumu.Eslestirilmedi,
            ImportBatchId = Guid.NewGuid(), ImportEdenUserId = adminId
        });

        await _ctx.SaveChangesAsync();
    }


    private static Kiraci Kiraci(string kiraciNo, KiraciTuru tur, int kategoriId, int sektorId, string ad, string? soyad = null,
        string? tcNo = null, string? vergiNo = null, string? vergiDairesi = null,
        string? ticaretSicilNo = null, string? mersisNo = null,
        string telefon = "", string email = "", string? adres = null) => new()
    {
        KiraciNo = kiraciNo, KiraciTuru = tur, KiraciKategoriId = kategoriId, SektorId = sektorId,
        Ad = ad, Soyad = soyad, TcKimlikNo = tcNo, VergiNo = vergiNo, VergiDairesi = vergiDairesi,
        TicaretSicilNo = ticaretSicilNo, MersisNo = mersisNo, Telefon = telefon, Email = email, Adres = adres,
        KayitTarihi = DateTime.Now.AddMonths(-Random.Shared.Next(6, 36))
    };

    private static KiraSozlesmesi MakeSozlesme(Birim birim, Kiraci kiraci,
        DateTime baslangic, DateTime bitis,
        decimal? depozito, bool kdv, decimal kdvOrani = 20, string? notlar = null) => new()
    {
        Birim = birim, BirimId = birim.Id, Kiraci = kiraci, KiraciId = kiraci.Id,
        BaslangicTarihi = baslangic, BitisTarihi = bitis,
        Depozito = depozito, Notlar = notlar, Durum = SozlesmeDurumu.Aktif,
        KdvUygulanacakMi = kdv, KdvOrani = kdv ? kdvOrani : 0
    };

    public async Task ClearDomainDataAsync()
    {
        // Temizlik sırası önemlidir (FK kısıtlamaları nedeniyle)
        _ctx.OdemeBankaEslesmeleri.RemoveRange(_ctx.OdemeBankaEslesmeleri);
        _ctx.KiraOdemeler.RemoveRange(_ctx.KiraOdemeler);
        _ctx.Dekontlar.RemoveRange(_ctx.Dekontlar);
        _ctx.BankaHareketleri.RemoveRange(_ctx.BankaHareketleri);
        
        _ctx.ToplantiSalonuRezervasyonlari.RemoveRange(_ctx.ToplantiSalonuRezervasyonlari);
        _ctx.RezervasyonUcretKurallari.RemoveRange(_ctx.RezervasyonUcretKurallari);
        _ctx.RezervasyonGenelTarifeleri.RemoveRange(_ctx.RezervasyonGenelTarifeleri);
        
        _ctx.TahakkukKalemleri.RemoveRange(_ctx.TahakkukKalemleri);
        _ctx.KiraTahakkuklar.RemoveRange(_ctx.KiraTahakkuklar);
        
        _ctx.SozlesmeRateler.RemoveRange(_ctx.SozlesmeRateler);
        _ctx.SozlesmeIslemGecmisleri.RemoveRange(_ctx.SozlesmeIslemGecmisleri);
        _ctx.Sozlesmeler.RemoveRange(_ctx.Sozlesmeler);
        _ctx.Kiraciler.RemoveRange(_ctx.Kiraciler);
        
        _ctx.BirimRateler.RemoveRange(_ctx.BirimRateler);
        _ctx.Birimler.RemoveRange(_ctx.Birimler);
        
        _ctx.TasinmazKiraciKategoriFiyatlari.RemoveRange(_ctx.TasinmazKiraciKategoriFiyatlari);
        _ctx.Tasinmazlar.RemoveRange(_ctx.Tasinmazlar);

        // Tarifeler ve Kalemleri (Cascade delete genellikle açıktır ama garanti olsun)
        _ctx.TarifeKalemleri.RemoveRange(_ctx.TarifeKalemleri);
        _ctx.Tarifeler.RemoveRange(_ctx.Tarifeler);

        // Sistem Tanımları (Baştan seed edileceği için temizlenebilir)
        _ctx.Sektorler.RemoveRange(_ctx.Sektorler);
        _ctx.KiraciKategorileri.RemoveRange(_ctx.KiraciKategorileri);
        _ctx.BirimTurleri.RemoveRange(_ctx.BirimTurleri);
        _ctx.TasinmazTipleri.RemoveRange(_ctx.TasinmazTipleri);
        _ctx.TasinmazTipiKiralamaSekilleri.RemoveRange(_ctx.TasinmazTipiKiralamaSekilleri);
        _ctx.BorcTipleri.RemoveRange(_ctx.BorcTipleri);

        await _ctx.SaveChangesAsync();
    }
}
