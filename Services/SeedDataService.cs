using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class SeedDataService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ITahakkukUretimService _tahakkukUretim;
    private readonly IRateResolverService _rateResolver;
    private readonly IRolService _rolService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRolService _userRolService;

    public SeedDataService(
        ApplicationDbContext ctx,
        ITahakkukUretimService tahakkukUretim,
        IRateResolverService rateResolver,
        IRolService rolService,
        UserManager<ApplicationUser> userManager,
        IUserRolService userRolService)
    {
        _ctx = ctx;
        _tahakkukUretim = tahakkukUretim;
        _rateResolver = rateResolver;
        _rolService = rolService;
        _userManager = userManager;
        _userRolService = userRolService;
    }

    public async Task SeedEnumDegerleriAsync()
    {
        var enumTypes = typeof(SozlesmeDurumu).Assembly.GetTypes()
            .Where(t => t.IsEnum && t.Namespace == "KiraTakip.Models")
            .ToList();

        var existing = await _ctx.EnumDegerleri
            .Select(e => new { e.EnumAdi, e.Deger })
            .ToListAsync();
        var existingSet = existing.Select(e => (e.EnumAdi, e.Deger)).ToHashSet();

        foreach (var enumType in enumTypes)
        {
            foreach (var value in Enum.GetValues(enumType))
            {
                int intVal = (int)value;
                string enumAdi = enumType.Name;
                if (existingSet.Contains((enumAdi, intVal))) continue;

                _ctx.EnumDegerleri.Add(new EnumDegeri
                {
                    EnumAdi = enumAdi,
                    Deger = intVal,
                    Ad = Enum.GetName(enumType, value)!
                });
            }
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedBorcTipleriAsync()
    {
        var existingCodes = await _ctx.BorcTipleri.Select(b => b.Kod).ToListAsync();
        var toAdd = new List<BorcTipi>();

        if (!existingCodes.Contains(BorcTipiConsts.Kira)) toAdd.Add(new BorcTipi { Ad = "Kira Bedeli", Kod = BorcTipiConsts.Kira, Aktif = true, Sira = 1, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = true });
        if (!existingCodes.Contains(BorcTipiConsts.Diger)) toAdd.Add(new BorcTipi { Ad = "Diğer", Kod = BorcTipiConsts.Diger, Aktif = true, Sira = 100, Davranis = BorcTipiDavranisi.KullaniciManuel, Sistem = true });
        if (!existingCodes.Contains("ORTAK")) toAdd.Add(new BorcTipi { Ad = "Ortak Gider", Kod = "ORTAK", Aktif = true, Sira = 2, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = false });
        if (!existingCodes.Contains("PORTAL")) toAdd.Add(new BorcTipi { Ad = "Portal Gideri", Kod = "PORTAL", Aktif = true, Sira = 3, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = false });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BorcTipi { Ad = "Toplantı Salonu Kullanım Bedeli", Kod = "TOPLANTI", Aktif = true, Sira = 4, Davranis = BorcTipiDavranisi.RezervasyonOzel, Sistem = false });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new BorcTipi { Ad = "Etkinlik Alanı Kullanım Bedeli", Kod = "ETKINLIK", Aktif = true, Sira = 5, Davranis = BorcTipiDavranisi.RezervasyonOzel, Sistem = false });
        if (!existingCodes.Contains(BorcTipiConsts.Depozito)) toAdd.Add(new BorcTipi { Ad = "Depozito", Kod = BorcTipiConsts.Depozito, Aktif = true, Sira = 99, Davranis = BorcTipiDavranisi.IlkAyTekSeferlik, Sistem = true });

        if (toAdd.Any())
        {
            _ctx.BorcTipleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }

        // Mevcut kayıtların sistem bayraklarını ve davranışlarını doğrula (Idempotency)
        await _ctx.BorcTipleri.Where(b => b.Kod == BorcTipiConsts.Kira).ExecuteUpdateAsync(s => s.SetProperty(b => b.Sistem, true).SetProperty(b => b.Davranis, BorcTipiDavranisi.AylikSabit));
        await _ctx.BorcTipleri.Where(b => b.Kod == BorcTipiConsts.Diger).ExecuteUpdateAsync(s => s.SetProperty(b => b.Sistem, true).SetProperty(b => b.Davranis, BorcTipiDavranisi.KullaniciManuel));
        await _ctx.BorcTipleri.Where(b => b.Kod == BorcTipiConsts.Depozito).ExecuteUpdateAsync(s => s.SetProperty(b => b.Sistem, true).SetProperty(b => b.Davranis, BorcTipiDavranisi.IlkAyTekSeferlik));
        await _ctx.BorcTipleri.Where(b => b.Kod == "TOPLANTI").ExecuteUpdateAsync(s => s.SetProperty(b => b.Davranis, BorcTipiDavranisi.RezervasyonOzel));
        await _ctx.BorcTipleri.Where(b => b.Kod == "ETKINLIK").ExecuteUpdateAsync(s => s.SetProperty(b => b.Davranis, BorcTipiDavranisi.RezervasyonOzel));
    }

    public async Task EnsureOdemeBelgeTuruAsync()
    {
        if (!await _ctx.BelgeTurleri.AnyAsync(t => t.Kod == "ODEME_DEKONT"))
        {
            _ctx.BelgeTurleri.Add(new BelgeTuru
            {
                Kod = "ODEME_DEKONT",
                Ad = "Ödeme Dekontu",
                HedefEntite = BelgeOwnerTipi.Odeme,
                IzinVerilenUzantilar = "pdf,jpg,jpeg,png",
                MaxBoyutMb = 5,
                Sira = 1,
                IsActive = true
            });
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task EnsureVarsayilanRezervasyonTarifeAsync()
    {
        var cariYil = DateTime.Now.Year;
        var varsayilanUcret = 500m;
        var varsayilanUcretsizSure = 120;
        var varsayilanPeriyot = 60;
        var varsayilanKdv = 20m;

        var rezBirimTurleri = await _ctx.BirimTurleri
            .Where(t => t.Aktif && t.RezervasyonYapilabilirMi)
            .ToListAsync();
        if (!rezBirimTurleri.Any()) return;

        var mevcut = await _ctx.RezervasyonTarifeler
            .Where(r => r.BirimId == null && r.Yil == cariYil)
            .Select(r => r.BirimTuruId)
            .ToListAsync();

        foreach (var bt in rezBirimTurleri.Where(b => !mevcut.Contains(b.Id)))
        {
            _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
            {
                Yil = cariYil,
                BirimTuruId = bt.Id,
                UcretsizSureDakika = varsayilanUcretsizSure,
                UcretlendirmePeriyoduDakika = varsayilanPeriyot,
                PeriyotUcreti = varsayilanUcret,
                KdvOrani = varsayilanKdv,
                Aciklama = $"{cariYil} varsayılan — {bt.Ad}"
            });
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedTasinmazTipleriAsync()
    {
        var existingCodes = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tasinmaz).Select(k => k.Kod).ToListAsync();
        var toAdd = new List<Kategori>();

        if (!existingCodes.Contains("BINA")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Tasinmaz, Ad = "Bina", Kod = "BINA", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow, TekParcaDestekli = true, BirimBazliDestekli = true });
        if (!existingCodes.Contains("OTOMAT")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Tasinmaz, Ad = "Otomat", Kod = "OTOMAT", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow, TekParcaDestekli = true, BirimBazliDestekli = false });
        if (!existingCodes.Contains("BANKAMATIK")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Tasinmaz, Ad = "Bankamatik", Kod = "BANKAMATIK", Aktif = true, Sira = 3, OlusturmaTarihi = DateTime.UtcNow, TekParcaDestekli = true, BirimBazliDestekli = false });

        if (toAdd.Any())
        {
            _ctx.Kategoriler.AddRange(toAdd);
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
        if (!existingCodes.Contains("OFIS")) toAdd.Add(new BirimTuru { Ad = "Ofis", Kod = "OFIS", Aktif = true, KiralanabilirMi = true, RezervasyonYapilabilirMi = false, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BirimTuru { Ad = "Toplantı Salonu", Kod = "TOPLANTI", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true, Sira = 10, OlusturmaTarihi = DateTime.UtcNow, BorcTipiId = toplantiBorcTipiId });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new BirimTuru { Ad = "Etkinlik Alanı", Kod = "ETKINLIK", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true, Sira = 11, OlusturmaTarihi = DateTime.UtcNow, BorcTipiId = etkinliBorcTipiId });

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
        var existingCodes = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Kiraci).Select(k => k.Kod).ToListAsync();
        var toAdd = new List<Kategori>();

        if (!existingCodes.Contains("AKADEMISYEN")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Kiraci, Ad = "Akademisyen", Kod = "AKADEMISYEN", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("AKAD_OLMAYAN")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Kiraci, Ad = "Akademisyen Olmayan", Kod = "AKAD_OLMAYAN", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.Kategoriler.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedSektorlerAsync()
    {
        var existingCodes = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Sektor).Select(k => k.Kod).ToListAsync();
        var toAdd = new List<Kategori>();

        if (!existingCodes.Contains("YAZILIM")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Yazılım", Kod = "YAZILIM", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("LOJISTIK")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Lojistik", Kod = "LOJISTIK", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("GIDA")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Gıda", Kod = "GIDA", Aktif = true, Sira = 3, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TARIM")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Tarım", Kod = "TARIM", Aktif = true, Sira = 4, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("FINANS")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Finans", Kod = "FINANS", Aktif = true, Sira = 5, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("EGITIM")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Eğitim", Kod = "EGITIM", Aktif = true, Sira = 6, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("KAMU")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Kamu", Kod = "KAMU", Aktif = true, Sira = 7, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.Kategoriler.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedTarifelerAsync()
    {
        var cariYil = DateTime.Now.Year;
        if (await _ctx.GenelTarifeler.AnyAsync(k => k.Yil == cariYil)) return;

        var kategoriler = await _ctx.Kategoriler
            .Where(k => k.Tipi == KategoriTipi.Kiraci && k.Aktif)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        var borcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
            .OrderBy(b => b.Sira)
            .ToListAsync();

        if (!kategoriler.Any() || !borcTipleri.Any()) return;

        foreach (var kat in kategoriler)
        {
            foreach (var bt in borcTipleri)
            {
                _ctx.GenelTarifeler.Add(new GenelTarife
                {
                    Yil = cariYil,
                    KiraciKategoriId = kat.Id,
                    BorcTipiId = bt.Id,
                    HesaplamaYontemi = (bt.Kod == BorcTipiConsts.Kira || bt.Kod == "ORTAK") ? HesaplamaYontemi.M2 : HesaplamaYontemi.Sabit,
                    BirimDeger = bt.Kod switch
                    {
                        BorcTipiConsts.Kira => kat.Kod == "AKADEMISYEN" ? 300m : 400m,
                        "ORTAK" => kat.Kod == "AKADEMISYEN" ? 100m : 150m,
                        "PORTAL" => kat.Kod == "AKADEMISYEN" ? 300m : 500m,
                        BorcTipiConsts.Depozito => kat.Kod == "AKADEMISYEN" ? 8000m : 15000m,
                        _ => 0m
                    },
                    KdvOrani = bt.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik ? 0m : 20m
                });
            }
        }

        await _ctx.SaveChangesAsync();
    }

    public async Task SeedDomainDataAsync()
    {
        if (await _ctx.Tasinmazlar.AnyAsync()) return;

        var now = DateTime.Now;
        var tipiMap = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tasinmaz).ToDictionaryAsync(k => k.Kod, k => k.Id);
        var birimTuruMap = await _ctx.BirimTurleri.ToDictionaryAsync(t => t.Kod, t => t.Id);
        var katMap = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Kiraci).ToDictionaryAsync(k => k.Kod, k => k.Id);
        var sekMap = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Sektor).ToDictionaryAsync(k => k.Kod, k => k.Id);

        // --- Kiracılar ---
        var yzCozum = Kiraci("KRC-000001", katMap["AKAD_OLMAYAN"], sekMap["YAZILIM"], "Yapay Zeka Çözümleri A.Ş.",
            vergiNo: "1234567890", ticaretSicilNo: "İZM-123", telefon: "0232 444 5566", email: "info@yz.com", adres: "Teknokent");
        var biyoLab = Kiraci("KRC-000002", katMap["AKADEMISYEN"], sekMap["YAZILIM"], "BiyoTek Laboratuvarları Ltd.",
            vergiNo: "9876543210", ticaretSicilNo: "İZM-456", telefon: "0232 555 6677", email: "info@biyotek.com", adres: "Teknokent");
        var veriBilisim = Kiraci("KRC-000003", katMap["AKAD_OLMAYAN"], sekMap["YAZILIM"], "Veri Bilişim A.Ş.",
            vergiNo: "5556667770", ticaretSicilNo: "İZM-789", telefon: "0232 666 7788", email: "iletisim@veribilisim.com", adres: "Teknokent");

        _ctx.Kiraciler.AddRange(yzCozum, biyoLab, veriBilisim);

        // --- Taşınmaz (Teknokent A Blok) ---
        var ofisTuruId = birimTuruMap["OFIS"];
        var toplantiTuruId = birimTuruMap["TOPLANTI"];

        var teknokent = new Tasinmaz
        {
            Ad = "Teknokent A Blok",
            TasinmazTipiId = tipiMap.GetValueOrDefault("BINA"),
            KiralamaSekli = KiralamaSekli.BirimBazli,
            Il = "İzmir",
            Ilce = "Bornova",
            Mahalle = "Ege Üniversitesi",
            AcikAdres = "Ege Üniversitesi Teknokent Kampüsü",
            AcikYuzolcumu = 500,
            KapaliYuzolcumu = 4500,
            KatSayisi = 4,
            Aciklama = "Ofis bazlı kiralanabilir teknokent binası"
        };

        // 16 Ofis Ekleme
        for (int kat = 1; kat <= 4; kat++)
        {
            for (int ofis = 1; ofis <= 4; ofis++)
            {
                var ofisNo = $"{kat}0{ofis}";
                teknokent.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = ofisNo,
                    KatNo = kat,
                    Ad = $"Ofis {ofisNo}",
                    Yuzolcumu = 60 + (ofis * 10),
                    BirimTuruId = ofisTuruId
                });
            }
        }

        // 1 Ana Toplantı Salonu
        var toplantiZ01 = new Birim
        {
            BirimTipi = BirimTipi.Birim,
            BirimNo = "Z01",
            KatNo = 0,
            Ad = "Ana Toplantı Salonu",
            Yuzolcumu = 150,
            BirimTuruId = toplantiTuruId,
            Aciklama = "Ortak kullanıma açık ana rezervasyon alanı."
        };
        teknokent.Birimler.Add(toplantiZ01);

        _ctx.Tasinmazlar.Add(teknokent);
        await _ctx.SaveChangesAsync();

        // Toplantı salonu için ücret kuralı ekle (Değişken üzerinden yönetim)
        var salonUcret = 600m;
        var salonUcretsizSure = 120;
        var salonPeriyot = 60;
        var salonKdv = 20m;

        _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
        {
            BirimId = toplantiZ01.Id,
            UcretsizSureDakika = salonUcretsizSure,
            UcretlendirmePeriyoduDakika = salonPeriyot,
            PeriyotUcreti = salonUcret,
            KdvOrani = salonKdv,
            Aciklama = "Seed — Ana Toplantı Salonu için varsayılan kural"
        });
        await _ctx.SaveChangesAsync();

        // --- Taşınmaz (Teknokent B Blok) ---
        var teknokentB = new Tasinmaz
        {
            Ad = "Teknokent B Blok",
            TasinmazTipiId = tipiMap.GetValueOrDefault("BINA"),
            KiralamaSekli = KiralamaSekli.BirimBazli,
            Il = "İzmir",
            Ilce = "Bornova",
            Mahalle = "Ege Üniversitesi",
            AcikAdres = "Ege Üniversitesi Teknokent Kampüsü B Blok",
            AcikYuzolcumu = 300,
            KapaliYuzolcumu = 2000,
            KatSayisi = 2,
            Aciklama = "B Blok ofis bazlı kiralanabilir teknokent binası"
        };

        // 4 Ofis Ekleme
        for (int kat = 1; kat <= 2; kat++)
        {
            for (int ofis = 1; ofis <= 2; ofis++)
            {
                var ofisNo = $"{kat}0{ofis}";
                teknokentB.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Birim,
                    BirimNo = ofisNo,
                    KatNo = kat,
                    Ad = $"Ofis B{ofisNo}",
                    Yuzolcumu = 50 + (ofis * 10),
                    BirimTuruId = ofisTuruId
                });
            }
        }

        // 1 Toplantı Odası (B01)
        var toplantiB01 = new Birim
        {
            BirimTipi = BirimTipi.Birim,
            BirimNo = "B01",
            KatNo = 0,
            Ad = "B Blok Küçük Toplantı Odası",
            Yuzolcumu = 60,
            BirimTuruId = toplantiTuruId,
            Aciklama = "B Blok ortak kullanıma açık toplantı odası."
        };
        teknokentB.Birimler.Add(toplantiB01);

        _ctx.Tasinmazlar.Add(teknokentB);
        await _ctx.SaveChangesAsync();

        // --- 4. Tarifelerin Oluşturulması (Hiyerarşik Sıralama İçin Önce Bunlar Gelmeli) ---
        await SeedTasinmazFiyatlarAsync();

        var btKiraId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Kira)).Id;
        var btDepozitoId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Depozito)).Id;

        var birim101 = teknokent.Birimler.First(b => b.BirimNo == "101");
        var birim102 = teknokent.Birimler.First(b => b.BirimNo == "102");
        var birim201 = teknokent.Birimler.First(b => b.BirimNo == "201");
        var birim301 = teknokent.Birimler.First(b => b.BirimNo == "301");
        var birim302 = teknokent.Birimler.First(b => b.BirimNo == "302");
        var birim401 = teknokent.Birimler.First(b => b.BirimNo == "401");
        var birim402 = teknokent.Birimler.First(b => b.BirimNo == "402");

        var birimB101 = teknokentB.Birimler.First(b => b.BirimNo == "101");

        // 4.3 Birim Tarifesi Örneği (Hiyerarşide Matrisin Üstündedir)
        // Ofis 201 için Akademisyen kategorisinde özel birim fiyatı tanımlayalım
        _ctx.BirimTarifeler.Add(new BirimTarife
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

        // Bedelleri önceden çöz (SozlesmeTarife seed'inde kullanmak için)
        var bedel201 = await ResolveKiraBedeli(birim201, yzCozum);
        var bedel301 = await ResolveKiraBedeli(birim301, biyoLab);
        var bedel401 = await ResolveKiraBedeli(birim401, veriBilisim);
        var bedel102 = await ResolveKiraBedeli(birim102, biyoLab);
        var bedel302 = await ResolveKiraBedeli(birim302, yzCozum);

        var bedelB101 = await ResolveKiraBedeli(birimB101, veriBilisim);

        var sozlesmeler = new List<Sozlesme>
        {
            // Ofis 101: Matris/Birim üzerinden bedel alacak, aşağıda Sözleşme Tarifesi ile ezilecek
            MakeSozlesme(birim101, yzCozum, startYearMinus1, startYearMinus1.AddYears(2), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 5),

            // Diğerleri tamamen hiyerarşiyi (Birim -> Matris -> Genel) takip edecek
            MakeSozlesme(birim201, yzCozum, startYearMinus1.AddMonths(3), startYearMinus1.AddMonths(24), false,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 10),
            MakeSozlesme(birim301, biyoLab, startYearMinus1.AddMonths(6), startYearMinus1.AddMonths(18), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 15),
            MakeSozlesme(birim401, veriBilisim, startYearMinus1.AddMonths(1), startYearMinus1.AddMonths(13), true,
                vadeKuraliTipi: VadeKuraliTipi.DonemBasiOfset, vadeGunu: 7),

            // Süresi dolan/dolmak üzere olanlar
            MakeSozlesme(birim102, biyoLab, startYearMinus1.AddMonths(2), now.AddDays(15), false,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 18),
            MakeSozlesme(birim302, yzCozum, startYearMinus1.AddMonths(0), now.AddDays(-5), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 25),

            // B Blok sözleşmesi
            MakeSozlesme(birimB101, veriBilisim, startYearMinus1.AddMonths(2), startYearMinus1.AddMonths(20), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 5)
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

        _ctx.SozlesmeTarifeler.AddRange(
            new SozlesmeTarife
            {
                KiraSozlesmesiId = targetSozlesme.Id,
                BorcTipiId = btKiraId,
                BirimDeger = 360, // 25200 / 70m2 = 360
                HesaplamaYontemi = HesaplamaYontemi.M2,
                KdvOrani = 20
            },
            new SozlesmeTarife
            {
                KiraSozlesmesiId = targetSozlesme.Id,
                BorcTipiId = btDepozitoId,
                BirimDeger = 40000,
                HesaplamaYontemi = HesaplamaYontemi.Sabit,
                KdvOrani = 0
            }
        );

        // Diğer sözleşmeler için Sabit kira rate'i ekle
        _ctx.SozlesmeTarifeler.AddRange(
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[1].Id, BorcTipiId = btKiraId, BirimDeger = bedel201, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[2].Id, BorcTipiId = btKiraId, BirimDeger = bedel301, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[3].Id, BorcTipiId = btKiraId, BirimDeger = bedel401, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[4].Id, BorcTipiId = btKiraId, BirimDeger = bedel102, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[5].Id, BorcTipiId = btKiraId, BirimDeger = bedel302, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[6].Id, BorcTipiId = btKiraId, BirimDeger = bedelB101, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 }
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

        // --- 9. Kiracı Rol ve Kullanıcı Seed İşlemleri (Phase 16E Uyumluluğu) ---
        var seededKiraciler = await _ctx.Kiraciler.ToListAsync();
        foreach (var k in seededKiraciler)
        {

            if (!string.IsNullOrWhiteSpace(k.Email))
            {
                var rawEmailName = k.Email.Split('@')[0];
                var password = k.Email switch
                {
                    "info@yz.com" => "Yz12345!",
                    "info@biyotek.com" => "Biyo123!",
                    "iletisim@veribilisim.com" => "Veri123!",
                    _ => char.ToUpperInvariant(rawEmailName[0]) + rawEmailName.Substring(1) + (rawEmailName.Length < 4 ? "12345!" : "123!")
                };
                
                await EnsureKiraciUserAsync(k.Email, password, k.GosterimAdi, k.Id);
            }
        }


    }

    public async Task SeedTasinmazFiyatlarAsync()
    {
        var teknokent = await _ctx.Tasinmazlar.FirstOrDefaultAsync(t => t.Ad == "Teknokent A Blok");
        if (teknokent != null && !await _ctx.TasinmazTarifeler.AnyAsync(f => f.TasinmazId == teknokent.Id))
        {
            var katAkademisyen = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Kiraci && k.Kod == "AKADEMISYEN");
            var katAkadOlmayan = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Kiraci && k.Kod == "AKAD_OLMAYAN");

            var btKira = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Kira);
            var btOrtak = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "ORTAK");
            var btPortal = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "PORTAL");
            var btDepozito = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Depozito);

            _ctx.TasinmazTarifeler.AddRange(
                // Akademisyen için (m2 bazlı kira ve ortak gider)
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btKira.Id, BirimDeger = 350, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btOrtak.Id, BirimDeger = 100, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btPortal.Id, BirimDeger = 500, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btDepozito.Id, BirimDeger = 10000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },

                // Akademisyen Olmayan için (m2 bazlı kira ve ortak gider)
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btKira.Id, BirimDeger = 450, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btOrtak.Id, BirimDeger = 150, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btPortal.Id, BirimDeger = 750, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btDepozito.Id, BirimDeger = 25000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 }
            );

            await _ctx.SaveChangesAsync();
        }

        var teknokentB = await _ctx.Tasinmazlar.FirstOrDefaultAsync(t => t.Ad == "Teknokent B Blok");
        if (teknokentB != null && !await _ctx.TasinmazTarifeler.AnyAsync(f => f.TasinmazId == teknokentB.Id))
        {
            var katAkademisyen = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Kiraci && k.Kod == "AKADEMISYEN");
            var katAkadOlmayan = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Kiraci && k.Kod == "AKAD_OLMAYAN");

            var btKira = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Kira);
            var btOrtak = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "ORTAK");
            var btPortal = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "PORTAL");
            var btDepozito = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Depozito);

            _ctx.TasinmazTarifeler.AddRange(
                // Akademisyen için (m2 bazlı kira ve ortak gider)
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btKira.Id, BirimDeger = 300, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btOrtak.Id, BirimDeger = 90, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btPortal.Id, BirimDeger = 400, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btDepozito.Id, BirimDeger = 8000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },

                // Akademisyen Olmayan için (m2 bazlı kira ve ortak gider)
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btKira.Id, BirimDeger = 400, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btOrtak.Id, BirimDeger = 130, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btPortal.Id, BirimDeger = 600, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokentB.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btDepozito.Id, BirimDeger = 20000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 }
            );

            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedTahakkuklarAsync()
    {
        // Geriye dönük uyumluluk için (UretSozlesmeIcinAsync SeedDomainDataAsync içinde çağrılıyor)
        if (await _ctx.Tahakkuklar.AnyAsync()) return;
        var aktifSozlesmeler = await _ctx.Sozlesmeler.Where(s => s.Durum == SozlesmeDurumu.Aktif).ToListAsync();
        foreach (var s in aktifSozlesmeler) await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
    }

    private async Task SeedTahakkuklarVeOdemelerAsync(List<Sozlesme> sozlesmeler)
    {
        try
        {
            var adminUser = await _ctx.Users.FirstOrDefaultAsync();
            var adminId = adminUser?.Id ?? "admin-id-missing";

            // 1. Manuel Borçlar ve İptaller
            var manuelBorcTipi = await _ctx.BorcTipleri.FirstOrDefaultAsync(b => b.Kod == BorcTipiConsts.Diger);
            if (manuelBorcTipi != null)
            {
                var targetSozlesme = sozlesmeler.First();
                _ctx.Tahakkuklar.Add(new Tahakkuk
                {
                    KiraSozlesmesiId = targetSozlesme.Id,
                    DonemBaslangic = DateTime.Today.AddDays(-5),
                    DonemBitis = DateTime.Today,
                    VadeTarihi = DateTime.Today.AddDays(15),
                    BeklenenTutar = 2500m,
                    KdvTutari = 500m,
                    ToplamTutar = 3000m,
                    OdenenTutar = 0m,
                    Durum = TahakkukDurumu.Bekleniyor,
                    KaynakTipi = TahakkukKaynakTipi.Manuel,
                    Kalemler = new List<TahakkukKalemi> { new TahakkukKalemi { BorcTipiId = manuelBorcTipi.Id, Aciklama = "Ekstra Temizlik Bedeli", BirimDeger = 2500m, Carpan = 1m, Tutar = 2500m, KdvOrani = 20m, KdvTutari = 500m, ToplamTutar = 3000m, KaynakTipi = KalemKaynakTipi.ManuelGiris } }
                });

                // İptal Edilen Kayıt
                _ctx.Tahakkuklar.Add(new Tahakkuk
                {
                    KiraSozlesmesiId = targetSozlesme.Id,
                    DonemBaslangic = DateTime.Today.AddMonths(-1),
                    DonemBitis = DateTime.Today.AddMonths(-1),
                    VadeTarihi = DateTime.Today.AddMonths(-1),
                    BeklenenTutar = 500m,
                    KdvTutari = 100m,
                    ToplamTutar = 600m,
                    OdenenTutar = 0m,
                    Durum = TahakkukDurumu.IptalEdildi,
                    KaynakTipi = TahakkukKaynakTipi.Manuel,
                    IptalNotu = "Hatalı giriş nedeniyle iptal edildi.",
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
        var query = _ctx.Tahakkuklar
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

            var odeme = new TahakkukOdeme
            {
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                TahakkukId = t.Id,
                OdemeTarihi = gecikmis ? t.VadeTarihi.AddDays(Random.Shared.Next(15, 45)) : t.VadeTarihi.AddDays(Random.Shared.Next(-5, 5)),
                Tutar = odemeTutari,
                OdemeKanali = (OdemeKanali)Random.Shared.Next(1, 5),
                Durum = OdemeDurumu.Onaylandi,
                Aciklama = (kismiMi ? "Kısmi " : "") + (gecikmis ? "gecikmeli seed ödemesi" : "zamanında seed ödemesi"),
                GirenUserId = adminId
            };

            t.OdenenTutar = odemeTutari;
            t.Durum = kismiMi ? TahakkukDurumu.KismenOdendi : TahakkukDurumu.TamOdendi;
            _ctx.TahakkukOdemeler.Add(odeme);
        }
    }

    private async Task SeedKismiOdemelerAsync(string adminId)
    {
        var bekleyenler = await _ctx.Tahakkuklar
            .Where(t => t.Durum == TahakkukDurumu.Bekleniyor)
            .Take(3)
            .ToListAsync();

        foreach (var t in bekleyenler)
        {
            var kismiTutar = Math.Round(t.ToplamTutar / 2, 2);
            var odeme = new TahakkukOdeme
            {
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                TahakkukId = t.Id,
                OdemeTarihi = DateTime.Today.AddDays(-2),
                Tutar = kismiTutar,
                OdemeKanali = OdemeKanali.EFT,
                Durum = OdemeDurumu.Onaylandi,
                Aciklama = "Seed kısmi ödeme",
                GirenUserId = adminId
            };
            t.OdenenTutar = kismiTutar;
            t.Durum = TahakkukDurumu.KismenOdendi;
            _ctx.TahakkukOdemeler.Add(odeme);
        }
    }

    private async Task SeedRezervasyonlarAsync()
    {
        var salon = await _ctx.Birimler.Include(b => b.Tasinmaz).FirstOrDefaultAsync(b => b.Ad == "Ana Toplantı Salonu");
        var salonB = await _ctx.Birimler.Include(b => b.Tasinmaz).FirstOrDefaultAsync(b => b.Ad == "B Blok Küçük Toplantı Odası");
        var kiraci = await _ctx.Kiraciler.FirstOrDefaultAsync();
        var sozlesme = await _ctx.Sozlesmeler.FirstOrDefaultAsync(s => s.KiraciId == kiraci.Id);

        if (salon == null || kiraci == null) return;

        var btRezervasyon = await _ctx.BorcTipleri.FirstOrDefaultAsync(b => b.Kod == "TOPLANTI");

        // 1. Geçmiş Rezervasyon (Tahakkuka Aktarıldı)
        var rezervasyon1 = new Rezervasyon
        {
            BirimId = salon.Id,
            KiraciId = kiraci.Id,
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
        };
        _ctx.Rezervasyonlari.Add(rezervasyon1);
        await _ctx.SaveChangesAsync();

        if (btRezervasyon != null)
        {
            var tahakkuk = new Tahakkuk
            {
                KiraciId = kiraci.Id,
                DonemBaslangic = rezervasyon1.BaslangicTarihi.Date,
                DonemBitis = rezervasyon1.BitisTarihi.Date,
                VadeTarihi = rezervasyon1.BitisTarihi.Date,
                BeklenenTutar = 1000,
                KdvTutari = 200,
                ToplamTutar = 1200,
                OdenenTutar = 0,
                Durum = TahakkukDurumu.Bekleniyor,
                KaynakTipi = TahakkukKaynakTipi.Rezervasyon,
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
            _ctx.Tahakkuklar.Add(tahakkuk);
            await _ctx.SaveChangesAsync();

            rezervasyon1.TahakkukId = tahakkuk.Id;
            await _ctx.SaveChangesAsync();
        }

        // 2. Gelecek Rezervasyon (Planlandı)
        _ctx.Rezervasyonlari.Add(new Rezervasyon
        {
            BirimId = salon.Id,
            KiraciId = kiraci.Id,
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
        });

        // 3. B Blok Rezervasyonu (Gelecek - Planlandı)
        var kiraciVeri = await _ctx.Kiraciler.FirstOrDefaultAsync(k => k.Email == "iletisim@veribilisim.com");
        if (salonB != null && kiraciVeri != null)
        {
            _ctx.Rezervasyonlari.Add(new Rezervasyon
            {
                BirimId = salonB.Id,
                KiraciId = kiraciVeri.Id,
                BaslangicTarihi = DateTime.Today.AddDays(4).AddHours(10),
                BitisTarihi = DateTime.Today.AddDays(4).AddHours(12),
                ToplamSureDakika = 120,
                UcretsizSureDakika = 60,
                UcretliSureDakika = 60,
                BirimUcret = 500,
                UcretTutar = 500,
                KdvOrani = 20,
                KdvTutari = 100,
                ToplamTutar = 600,
                Durum = RezervasyonDurumu.Planlandi
            });
        }

        await _ctx.SaveChangesAsync();
    }

    private async Task SeedBankaHareketleriAsync()
    {
        // Eşleşmiş Hareket
        _ctx.BankaHareketleri.Add(new BankaHareketi
        {
            IslemTarihi = DateTime.Today.AddDays(-1),
            IslemTutari = 1500,
            Aciklama = "KİRA ÖDEMESİ - TEKNOKENT",
            GonderenBilgisi = "Yapay Zeka Çözümleri A.Ş.",
            BankaKodu = "TR01",
            EslesmeDurumu = BankaEslesmeDurumu.Eslesti,
        });

        // Eşleşmemiş (Açıkta) Hareket
        _ctx.BankaHareketleri.Add(new BankaHareketi
        {
            IslemTarihi = DateTime.Today.AddDays(-2),
            IslemTutari = 5000,
            Aciklama = "HAVALE - BİLİNMEYEN",
            GonderenIban = "TR123456789...",
            BankaKodu = "TR01",
            EslesmeDurumu = BankaEslesmeDurumu.Eslestirilmedi,
        });

        await _ctx.SaveChangesAsync();
    }


    private static Kiraci Kiraci(string kiraciNo, int kategoriId, int sektorId, string ad,
        string? vergiNo = null, string? vergiDairesi = null,
        string? ticaretSicilNo = null, string? mersisNo = null,
        string telefon = "", string email = "", string? adres = null) => new()
        {
            KiraciNo = kiraciNo,
            KiraciKategoriId = kategoriId,
            SektorId = sektorId,
            Ad = ad,
            VergiNo = vergiNo,
            VergiDairesi = vergiDairesi,
            TicaretSicilNo = ticaretSicilNo,
            MersisNo = mersisNo,
            Telefon = telefon,
            Email = email,
            Adres = adres,
            KayitTarihi = DateTime.Now.AddMonths(-Random.Shared.Next(6, 36))
        };

    private static Sozlesme MakeSozlesme(Birim birim, Kiraci kiraci,
        DateTime baslangic, DateTime bitis,
        bool kdv, decimal kdvOrani = 20, string? notlar = null,
        VadeKuraliTipi vadeKuraliTipi = VadeKuraliTipi.SabitAyGunu,
        int vadeGunu = 1) => new()
        {
            Birim = birim,
            BirimId = birim.Id,
            Kiraci = kiraci,
            KiraciId = kiraci.Id,
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis,
            Aciklama = notlar,
            Durum = SozlesmeDurumu.Aktif,
            KdvUygulanacakMi = kdv,
            VadeKuraliTipi = vadeKuraliTipi,
            VadeGunu = vadeGunu
        };

    public async Task ClearDomainDataAsync()
    {
        // Yetki kapsamlarını temizle (FK kısıtlaması nedeniyle)
        _ctx.KullaniciYetkiKapsamlari.RemoveRange(_ctx.KullaniciYetkiKapsamlari);

        // Kiracı kullanıcılarını ve rollerini temizle (FK kısıtlaması nedeniyle kiracılardan önce silinmelidir)
        var kiraciUsers = await _userManager.Users.Where(u => u.UserType == UserType.Kiraci).ToListAsync();
        foreach (var ku in kiraciUsers)
        {
            await _userRolService.RemoveAllRolesAsync(ku.Id);
            await _userManager.DeleteAsync(ku);
        }

        var kiraciRoller = await _ctx.Roller.Where(r => r.Scope == RolScope.Kiraci && r.KiraciId != null).ToListAsync();
        _ctx.Roller.RemoveRange(kiraciRoller);

        _ctx.Davetiyeler.RemoveRange(_ctx.Davetiyeler);
        _ctx.SifreSifirlamaTalepleri.RemoveRange(_ctx.SifreSifirlamaTalepleri);
        _ctx.OdemeLinkKayitlari.RemoveRange(_ctx.OdemeLinkKayitlari);

        // Temizlik sırası önemlidir (FK kısıtlamaları nedeniyle)
        _ctx.OdemeBankaEslesmeleri.RemoveRange(_ctx.OdemeBankaEslesmeleri);
        _ctx.TahakkukOdemeler.RemoveRange(_ctx.TahakkukOdemeler);
        _ctx.BankaHareketleri.RemoveRange(_ctx.BankaHareketleri);

        _ctx.Rezervasyonlari.RemoveRange(_ctx.Rezervasyonlari);
        _ctx.RezervasyonTarifeler.RemoveRange(_ctx.RezervasyonTarifeler);

        _ctx.TahakkukKalemleri.RemoveRange(_ctx.TahakkukKalemleri);
        _ctx.Tahakkuklar.RemoveRange(_ctx.Tahakkuklar);

        _ctx.SozlesmeTarifeler.RemoveRange(_ctx.SozlesmeTarifeler);
        _ctx.SozlesmeIslemGecmisleri.RemoveRange(_ctx.SozlesmeIslemGecmisleri);
        _ctx.Sozlesmeler.RemoveRange(_ctx.Sozlesmeler);
        _ctx.Kiraciler.RemoveRange(_ctx.Kiraciler);

        _ctx.BirimTarifeler.RemoveRange(_ctx.BirimTarifeler);
        _ctx.Birimler.RemoveRange(_ctx.Birimler);

        _ctx.TasinmazTarifeler.RemoveRange(_ctx.TasinmazTarifeler);
        _ctx.Tasinmazlar.RemoveRange(_ctx.Tasinmazlar);

        _ctx.GenelTarifeler.RemoveRange(_ctx.GenelTarifeler);

        // Sistem Tanımları (Baştan seed edileceği için temizlenebilir)
        _ctx.Kategoriler.RemoveRange(_ctx.Kategoriler);
        _ctx.BirimTurleri.RemoveRange(_ctx.BirimTurleri);
        _ctx.BorcTipleri.RemoveRange(_ctx.BorcTipleri);

        await _ctx.SaveChangesAsync();
    }

    private async Task EnsureKiraciUserAsync(string email, string password, string adSoyad, int kiraciId)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                AdSoyad = adSoyad,
                EmailConfirmed = true,
                IsActive = true,
                UserType = UserType.Kiraci,
                KiraciId = kiraciId
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Kiracı kullanıcısı '{email}' oluşturulamadı: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            user.UserType = UserType.Kiraci;
            user.KiraciId = kiraciId;
            user.IsActive = true;
            await _userManager.UpdateAsync(user);
        }

        var firmaRol = await _ctx.Roller.FirstOrDefaultAsync(r => r.KiraciId == null && r.Ad == RoleNames.KiraciYoneticisi);
        if (firmaRol != null)
        {
            var hasRole = await _ctx.UserRoller.AnyAsync(ur => ur.UserId == user.Id && ur.RolId == firmaRol.Id);
            if (!hasRole)
            {
                await _userRolService.AddRoleByRolIdAsync(user.Id, firmaRol.Id, "system");
            }
        }
    }
}
