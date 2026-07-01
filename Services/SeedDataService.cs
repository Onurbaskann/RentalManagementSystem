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

        if (!existingCodes.Contains("ORTAK")) toAdd.Add(new BorcTipi { Ad = "Ortak Gider", Kod = "ORTAK", Aktif = true, Sira = 2, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = false });
        if (!existingCodes.Contains("PORTAL")) toAdd.Add(new BorcTipi { Ad = "Portal Gideri", Kod = "PORTAL", Aktif = true, Sira = 3, Davranis = BorcTipiDavranisi.AylikSabit, Sistem = false });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BorcTipi { Ad = "Toplantı Salonu Kullanım Bedeli", Kod = "TOPLANTI", Aktif = true, Sira = 4, Davranis = BorcTipiDavranisi.RezervasyonOzel, Sistem = false });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new BorcTipi { Ad = "Etkinlik Alanı Kullanım Bedeli", Kod = "ETKINLIK", Aktif = true, Sira = 5, Davranis = BorcTipiDavranisi.RezervasyonOzel, Sistem = false });

        if (toAdd.Any())
        {
            _ctx.BorcTipleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }

        // Mevcut kayıtların davranışlarını doğrula (Idempotency)
        await _ctx.BorcTipleri.Where(b => b.Kod == "TOPLANTI").ExecuteUpdateAsync(s => s.SetProperty(b => b.Davranis, BorcTipiDavranisi.RezervasyonOzel));
        await _ctx.BorcTipleri.Where(b => b.Kod == "ETKINLIK").ExecuteUpdateAsync(s => s.SetProperty(b => b.Davranis, BorcTipiDavranisi.RezervasyonOzel));
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
        var existingCodes = await _ctx.TasinmazTipleri.Select(k => k.Kod).ToListAsync();
        var toAdd = new List<TasinmazTipi>();

        if (!existingCodes.Contains("BINA")) toAdd.Add(new TasinmazTipi { Ad = "Bina", Kod = "BINA", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow, TekParcaDestekli = true, BirimBazliDestekli = true });

        if (toAdd.Any())
        {
            _ctx.TasinmazTipleri.AddRange(toAdd);
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

        if (!existingCodes.Contains("AKADEMIK")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Kiraci, Ad = "Akademik", Kod = "AKADEMIK", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("AKADEMIK_OLMAYAN")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Kiraci, Ad = "Akademik Olmayan", Kod = "AKADEMIK_OLMAYAN", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });

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
                        BorcTipiConsts.Kira => kat.Kod == "AKADEMIK" ? 300m : 450m,
                        "ORTAK" => kat.Kod == "AKADEMIK" ? 100m : 150m,
                        "PORTAL" => kat.Kod == "AKADEMIK" ? 300m : 500m,
                        BorcTipiConsts.Depozito => kat.Kod == "AKADEMIK" ? 8000m : 15000m,
                        _ => 0m
                    },
                    KdvOrani = 20m
                });
            }
        }

        await _ctx.SaveChangesAsync();
    }

    public async Task SeedDomainDataAsync()
    {
        if (await _ctx.Tasinmazlar.AnyAsync()) return;

        var now = DateTime.Now;
        var tipiMap = await _ctx.TasinmazTipleri.ToDictionaryAsync(k => k.Kod, k => k.Id);
        var birimTuruMap = await _ctx.BirimTurleri.ToDictionaryAsync(t => t.Kod, t => t.Id);
        var katMap = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Kiraci).ToDictionaryAsync(k => k.Kod, k => k.Id);
        var sekMap = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Sektor).ToDictionaryAsync(k => k.Kod, k => k.Id);

        // --- Kiracılar ---
        var yzCozum = Kiraci("KRC-000001", katMap["AKADEMIK_OLMAYAN"], sekMap["YAZILIM"], "Yapay Zeka Çözümleri A.Ş.",
            vergiNo: "1234567890", ticaretSicilNo: "İZM-123", telefon: "0232 444 5566", email: "info@yz.com", adres: "Teknokent");
        var megaFinans = Kiraci("KRC-000002", katMap["AKADEMIK_OLMAYAN"], sekMap["FINANS"], "Mega Finans Hizmetleri A.Ş.",
            vergiNo: "9876543210", ticaretSicilNo: "İZM-456", telefon: "0232 555 6677", email: "info@megafinans.com", adres: "Teknokent");
        var biotech = Kiraci("KRC-000003", katMap["AKADEMIK"], sekMap["YAZILIM"], "BiyoTek Akademik Arge Ltd.",
            vergiNo: "5556667770", ticaretSicilNo: "İZM-789", telefon: "0232 666 7788", email: "iletisim@biotech.com", adres: "Teknokent");

        _ctx.Kiraciler.AddRange(yzCozum, megaFinans, biotech);

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

        // 5 Kiralanabilir Ofis Ekleme
        for (int ofis = 1; ofis <= 5; ofis++)
        {
            var ofisNo = $"10{ofis}";
            teknokent.Birimler.Add(new Birim
            {
                BirimTipi = BirimTipi.Birim,
                BirimNo = ofisNo,
                KatNo = 1,
                Ad = $"Ofis {ofisNo}",
                Yuzolcumu = 50 + (ofis * 10),
                BirimTuruId = ofisTuruId
            });
        }

        // 2 Rezerve Edilebilir Toplantı Odası Ekleme
        var toplantiZ01 = new Birim
        {
            BirimTipi = BirimTipi.Birim,
            BirimNo = "Z01",
            KatNo = 0,
            Ad = "Toplantı Salonu Z01",
            Yuzolcumu = 80,
            BirimTuruId = toplantiTuruId,
            Aciklama = "Ortak kullanıma açık ana toplantı salonu."
        };
        var toplantiZ02 = new Birim
        {
            BirimTipi = BirimTipi.Birim,
            BirimNo = "Z02",
            KatNo = 0,
            Ad = "Toplantı Odası Z02",
            Yuzolcumu = 40,
            BirimTuruId = toplantiTuruId,
            Aciklama = "Ortak kullanıma açık küçük toplantı odası."
        };
        teknokent.Birimler.Add(toplantiZ01);
        teknokent.Birimler.Add(toplantiZ02);

        _ctx.Tasinmazlar.Add(teknokent);
        await _ctx.SaveChangesAsync();

        // --- Tarifelerin Oluşturulması ---
        await SeedTasinmazFiyatlarAsync();

        var btKiraId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Kira)).Id;
        var btDepozitoId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Depozito)).Id;

        var birim101 = teknokent.Birimler.First(b => b.BirimNo == "101");
        var birim102 = teknokent.Birimler.First(b => b.BirimNo == "102");
        var birim103 = teknokent.Birimler.First(b => b.BirimNo == "103");
        var birim104 = teknokent.Birimler.First(b => b.BirimNo == "104");

        // Birim Tarifesi Örneği (Hiyerarşide Matrisin Üstündedir)
        // Ofis 101 için Akademik kategorisinde özel birim fiyatı tanımlayalım
        _ctx.BirimTarifeler.Add(new BirimTarife
        {
            BirimId = birim101.Id,
            KiraciKategoriId = katMap["AKADEMIK"],
            BorcTipiId = btKiraId,
            HesaplamaYontemi = HesaplamaYontemi.M2,
            BirimDeger = 400, // Genel Tarife 300 / Matris 320 yerine birim bazlı 400
            KdvOrani = 20
        });

        // Rezervasyon Tarifesi - Genel (BirimId = null)
        var mevcutGenelRez = await _ctx.RezervasyonTarifeler
            .FirstOrDefaultAsync(r => r.BirimId == null && r.BirimTuruId == toplantiTuruId && r.Yil == now.Year);
        if (mevcutGenelRez != null)
        {
            mevcutGenelRez.UcretsizSureDakika = 60;
            mevcutGenelRez.UcretlendirmePeriyoduDakika = 60;
            mevcutGenelRez.PeriyotUcreti = 400m;
            mevcutGenelRez.KdvOrani = 20m;
            mevcutGenelRez.Aciklama = "Genel Toplantı Salonu fiyatlandırma kuralı";
        }
        else
        {
            _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
            {
                Yil = now.Year,
                BirimTuruId = toplantiTuruId,
                BirimId = null,
                UcretsizSureDakika = 60,
                UcretlendirmePeriyoduDakika = 60,
                PeriyotUcreti = 400m,
                KdvOrani = 20m,
                Aciklama = "Genel Toplantı Salonu fiyatlandırma kuralı"
            });
        }

        // Rezervasyon Tarifesi - Birim (BirimId = Z01.Id)
        _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
        {
            Yil = now.Year,
            BirimTuruId = toplantiTuruId,
            BirimId = toplantiZ01.Id,
            UcretsizSureDakika = 30,
            UcretlendirmePeriyoduDakika = 60,
            PeriyotUcreti = 600m,
            KdvOrani = 20m,
            Aciklama = "Toplantı Salonu Z01 için özel fiyatlandırma kuralı"
        });

        // Kullanıcı tanımlı Belge Türlerini ekle
        var btKimlik = new BelgeTuru
        {
            Kod = "KIMLIK_FOTOKOPISI",
            Ad = "Kimlik Fotokopisi",
            HedefEntite = BelgeOwnerTipi.Kiraci,
            Zorunlu = true,
            IzinVerilenUzantilar = "pdf,jpg,png",
            MaxBoyutMb = 5,
            Sira = 1,
            Sistem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btSozlesmeEvrak = new BelgeTuru
        {
            Kod = "SOZLESME_EVRAK",
            Ad = "Sözleşme Evrakı",
            HedefEntite = BelgeOwnerTipi.Kiraci,
            Zorunlu = false,
            IzinVerilenUzantilar = "pdf,jpg,png",
            MaxBoyutMb = 5,
            Sira = 2,
            Sistem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btImzaliSozlesme = new BelgeTuru
        {
            Kod = "IMZALI_SOZLESME",
            Ad = "İmzalı Sözleşme Metni",
            HedefEntite = BelgeOwnerTipi.Sozlesme,
            Zorunlu = true,
            IzinVerilenUzantilar = "pdf,jpg,png",
            MaxBoyutMb = 10,
            Sira = 3,
            Sistem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btKvkk = new BelgeTuru
        {
            Kod = "KVKK_BELGESI",
            Ad = "KVKK Onay Belgesi",
            HedefEntite = BelgeOwnerTipi.Kiraci,
            Zorunlu = true,
            IzinVerilenUzantilar = "pdf,jpg,png",
            MaxBoyutMb = 5,
            Sira = 4,
            Sistem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btTeslim = new BelgeTuru
        {
            Kod = "TESLIM_TESELLUM",
            Ad = "Teslim Tesellüm Tutanağı",
            HedefEntite = BelgeOwnerTipi.Sozlesme,
            Zorunlu = false,
            IzinVerilenUzantilar = "pdf,jpg,png",
            MaxBoyutMb = 5,
            Sira = 5,
            Sistem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btTeminat = new BelgeTuru
        {
            Kod = "TEMINAT_MEKTUBU",
            Ad = "Teminat Mektubu",
            HedefEntite = BelgeOwnerTipi.Sozlesme,
            Zorunlu = false,
            IzinVerilenUzantilar = "pdf,jpg,png",
            MaxBoyutMb = 5,
            Sira = 6,
            Sistem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        _ctx.BelgeTurleri.AddRange(btKimlik, btSozlesmeEvrak, btImzaliSozlesme, btKvkk, btTeslim, btTeminat);
        await _ctx.SaveChangesAsync();

        // Kiracılar için belgeleri ekle
        var belgeler = new List<Belge>
        {
            // Kimlik Fotokopisi belgeleri
            new Belge
            {
                BelgeTuruId = btKimlik.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = yzCozum.Id,
                DosyaAdi = "kimlik_yz.pdf",
                MimeType = "application/pdf",
                BoyutByte = 1024,
                Aciklama = "YZ Çözüm Yetkili Kimlik Fotokopisi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 1, 2, 3, 4 } }
            },
            new Belge
            {
                BelgeTuruId = btKimlik.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = megaFinans.Id,
                DosyaAdi = "kimlik_mega.pdf",
                MimeType = "application/pdf",
                BoyutByte = 1024,
                Aciklama = "Mega Finans Yetkili Kimlik Fotokopisi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 1, 2, 3, 4 } }
            },
            new Belge
            {
                BelgeTuruId = btKimlik.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = biotech.Id,
                DosyaAdi = "kimlik_biotech.pdf",
                MimeType = "application/pdf",
                BoyutByte = 1024,
                Aciklama = "BiyoTek Yetkili Kimlik Fotokopisi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 1, 2, 3, 4 } }
            },

            // KVKK Onay Belgesi belgeleri
            new Belge
            {
                BelgeTuruId = btKvkk.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = yzCozum.Id,
                DosyaAdi = "kvkk_yz.pdf",
                MimeType = "application/pdf",
                BoyutByte = 1024,
                Aciklama = "YZ Çözüm Yetkili KVKK Belgesi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 1, 2, 3, 4 } }
            },
            new Belge
            {
                BelgeTuruId = btKvkk.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = megaFinans.Id,
                DosyaAdi = "kvkk_mega.pdf",
                MimeType = "application/pdf",
                BoyutByte = 1024,
                Aciklama = "Mega Finans Yetkili KVKK Belgesi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 1, 2, 3, 4 } }
            },
            new Belge
            {
                BelgeTuruId = btKvkk.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = biotech.Id,
                DosyaAdi = "kvkk_biotech.pdf",
                MimeType = "application/pdf",
                BoyutByte = 1024,
                Aciklama = "BiyoTek Yetkili KVKK Belgesi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 1, 2, 3, 4 } }
            },

            // Sözleşme Evrakı belgeleri
            new Belge
            {
                BelgeTuruId = btSozlesmeEvrak.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = yzCozum.Id,
                DosyaAdi = "sozlesme_yz.pdf",
                MimeType = "application/pdf",
                BoyutByte = 1024,
                Aciklama = "YZ Çözüm Sözleşme Evrakı",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 1, 2, 3, 4 } }
            },
            new Belge
            {
                BelgeTuruId = btSozlesmeEvrak.Id,
                OwnerType = BelgeOwnerTipi.Kiraci,
                OwnerId = megaFinans.Id,
                DosyaAdi = "sozlesme_mega.pdf",
                MimeType = "application/pdf",
                BoyutByte = 2048,
                Aciklama = "Mega Finans Sözleşme Evrakı",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 5, 6, 7, 8 } }
            }
        };
        _ctx.Belgeler.AddRange(belgeler);
        await _ctx.SaveChangesAsync();

        // Yardımcı fonksiyon: Dinamik m2 birim bedeli çözünürlüğü
        async Task<decimal> ResolveKiraM2Rate(Birim b, Kiraci k)
        {
            var res = await _rateResolver.ResolveAsync(null, k.Id, b.Id, btKiraId, now);
            return res?.BirimDeger ?? 0;
        }

        // --- 5. Sözleşmelerin Oluşturulması ---
        var startYearMinus1 = new DateTime(now.Year - 1, 1, 1);

        var rate101 = await ResolveKiraM2Rate(birim101, yzCozum);
        var rate102 = await ResolveKiraM2Rate(birim102, megaFinans);
        var rate103 = await ResolveKiraM2Rate(birim103, biotech);
        var rate104 = await ResolveKiraM2Rate(birim104, yzCozum);

        var sozlesmeler = new List<Sozlesme>
        {
            MakeSozlesme(birim101, yzCozum, startYearMinus1, startYearMinus1.AddYears(2).AddDays(-1), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 5),
            MakeSozlesme(birim102, megaFinans, startYearMinus1.AddMonths(3), startYearMinus1.AddMonths(24).AddDays(-1), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 10),
            MakeSozlesme(birim103, biotech, startYearMinus1.AddMonths(6), startYearMinus1.AddMonths(18).AddDays(-1), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 15),
            MakeSozlesme(birim104, yzCozum, startYearMinus1.AddMonths(1), startYearMinus1.AddYears(2).AddDays(-1), true,
                vadeKuraliTipi: VadeKuraliTipi.SabitAyGunu, vadeGunu: 5)
        };

        _ctx.Sozlesmeler.AddRange(sozlesmeler);
        await _ctx.SaveChangesAsync();

        // Sözleşmeler için İmzalı Sözleşme Metni belgelerini ekle
        var sozlesmeBelgeleri = new List<Belge>
        {
            new Belge
            {
                BelgeTuruId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Sozlesme,
                OwnerId = sozlesmeler[0].Id,
                DosyaAdi = "imzali_sozlesme_101.pdf",
                MimeType = "application/pdf",
                BoyutByte = 4096,
                Aciklama = "Ofis 101 İmzalı Kira Sözleşmesi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 10, 11, 12, 13 } }
            },
            new Belge
            {
                BelgeTuruId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Sozlesme,
                OwnerId = sozlesmeler[1].Id,
                DosyaAdi = "imzali_sozlesme_102.pdf",
                MimeType = "application/pdf",
                BoyutByte = 4096,
                Aciklama = "Ofis 102 İmzalı Kira Sözleşmesi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 14, 15, 16, 17 } }
            },
            new Belge
            {
                BelgeTuruId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Sozlesme,
                OwnerId = sozlesmeler[2].Id,
                DosyaAdi = "imzali_sozlesme_103.pdf",
                MimeType = "application/pdf",
                BoyutByte = 4096,
                Aciklama = "Ofis 103 İmzalı Kira Sözleşmesi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 18, 19, 20, 21 } }
            },
            new Belge
            {
                BelgeTuruId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Sozlesme,
                OwnerId = sozlesmeler[3].Id,
                DosyaAdi = "imzali_sozlesme_104.pdf",
                MimeType = "application/pdf",
                BoyutByte = 4096,
                Aciklama = "Ofis 104 İmzalı Kira Sözleşmesi",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 22, 23, 24, 25 } }
            },
            new Belge
            {
                BelgeTuruId = btTeslim.Id,
                OwnerType = BelgeOwnerTipi.Sozlesme,
                OwnerId = sozlesmeler[0].Id,
                DosyaAdi = "teslim_tutanagi_101.pdf",
                MimeType = "application/pdf",
                BoyutByte = 2048,
                Aciklama = "Ofis 101 Teslim Tesellüm Tutanağı",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 30, 31, 32, 33 } }
            },
            new Belge
            {
                BelgeTuruId = btTeminat.Id,
                OwnerType = BelgeOwnerTipi.Sozlesme,
                OwnerId = sozlesmeler[0].Id,
                DosyaAdi = "teminat_mektubu_101.pdf",
                MimeType = "application/pdf",
                BoyutByte = 8192,
                Aciklama = "Ofis 101 Teminat Mektubu",
                Gecersiz = false,
                Icerik = new BelgeIcerik { Icerik = new byte[] { 40, 41, 42, 43 } }
            }
        };
        _ctx.Belgeler.AddRange(sozlesmeBelgeleri);
        await _ctx.SaveChangesAsync();

        // --- 6. Sözleşme Tarifesi (Özel Oran) Uygulaması ---
        _ctx.SozlesmeTarifeler.AddRange(
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[0].Id, BorcTipiId = btKiraId, BirimDeger = rate101, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[1].Id, BorcTipiId = btKiraId, BirimDeger = rate102, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[2].Id, BorcTipiId = btKiraId, BirimDeger = rate103, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new SozlesmeTarife { KiraSozlesmesiId = sozlesmeler[3].Id, BorcTipiId = btKiraId, BirimDeger = rate104, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 }
        );
        await _ctx.SaveChangesAsync();

        // --- 7. Tahakkuk Üretimi ---
        foreach (var s in sozlesmeler)
        {
            await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
        }

        // --- 8. Diğer Seed İşlemleri ---
        await SeedRezervasyonlarAsync();
        await SeedBankaHareketleriAsync();
        await SeedTahakkuklarVeOdemelerAsync(sozlesmeler);

        // --- 9. Kiracı Rol ve Kullanıcı Seed İşlemleri ---
        var seededKiraciler = await _ctx.Kiraciler.ToListAsync();
        foreach (var k in seededKiraciler)
        {
            if (!string.IsNullOrWhiteSpace(k.Email))
            {
                var (userEmail, adSoyad, password) = k.Email switch
                {
                    "info@yz.com" => ("ahmet.yilmaz@yz.com", "Ahmet Yılmaz", "Ahmet123!"),
                    "info@megafinans.com" => ("mehmet.demir@megafinans.com", "Mehmet Demir", "Mehmet123!"),
                    "iletisim@biotech.com" => ("ayse.kaya@biotech.com", "Ayşe Kaya", "Ayse123!"),
                    _ => (k.Email, k.GosterimAdi, "User123!")
                };
                
                await EnsureKiraciUserAsync(userEmail, password, adSoyad, k.Id);
            }
        }

        // yzCozum kiracısının Id'sini bulalım
        var yzCozumEntity = seededKiraciler.FirstOrDefault(k => k.Email == "info@yz.com");
        if (yzCozumEntity != null)
        {
            var yzCozumId = yzCozumEntity.Id;

            // İkinci kullanıcıyı ekleyelim: mehmet.yildiz@yz.com
            await EnsureKiraciUserAsync("mehmet.yildiz@yz.com", "Mehmet123!", "Mehmet Yıldız", yzCozumId);

            // mehmet.yildiz@yz.com kullanıcısını bulalım
            var mehmetUser = await _userManager.FindByEmailAsync("mehmet.yildiz@yz.com");
            if (mehmetUser != null)
            {
                // Ofis 101'i bulup kapsam ekleyelim
                var ofis101 = await _ctx.Birimler.FirstOrDefaultAsync(b => b.BirimNo == "101");
                if (ofis101 != null)
                {
                    var hasScope = await _ctx.KullaniciYetkiKapsamlari.AnyAsync(s => s.UserId == mehmetUser.Id);
                    if (!hasScope)
                    {
                        _ctx.KullaniciYetkiKapsamlari.Add(new KullaniciYetkiKapsami
                        {
                            UserId = mehmetUser.Id,
                            KapsamTipi = KapsamTipi.Birim,
                            KapsamId = ofis101.Id
                        });
                        await _ctx.SaveChangesAsync();
                    }
                }
            }
        }
    }

    public async Task SeedTasinmazFiyatlarAsync()
    {
        var teknokent = await _ctx.Tasinmazlar.FirstOrDefaultAsync(t => t.Ad == "Teknokent A Blok");
        if (teknokent != null && !await _ctx.TasinmazTarifeler.AnyAsync(f => f.TasinmazId == teknokent.Id))
        {
            var katAkademik = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Kiraci && k.Kod == "AKADEMIK");
            var katAkadOlmayan = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Kiraci && k.Kod == "AKADEMIK_OLMAYAN");

            var btKira = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Kira);
            var btOrtak = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "ORTAK");
            var btPortal = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "PORTAL");
            var btDepozito = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == BorcTipiConsts.Depozito);

            _ctx.TasinmazTarifeler.AddRange(
                // Akademik için (m2 bazlı kira ve ortak gider) - Taşınmaz Tarifesi
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademik.Id, BorcTipiId = btKira.Id, BirimDeger = 320, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademik.Id, BorcTipiId = btOrtak.Id, BirimDeger = 95, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademik.Id, BorcTipiId = btPortal.Id, BirimDeger = 480, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademik.Id, BorcTipiId = btDepozito.Id, BirimDeger = 9000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },

                // Akademik Olmayan için - Taşınmaz Tarifesi
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btKira.Id, BirimDeger = 430, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btOrtak.Id, BirimDeger = 140, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btPortal.Id, BirimDeger = 700, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
                new TasinmazTarife { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btDepozito.Id, BirimDeger = 22000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 }
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
                    KiraciId = targetSozlesme.KiraciId,
                    BirimId = targetSozlesme.BirimId,
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
                    KiraciId = targetSozlesme.KiraciId,
                    BirimId = targetSozlesme.BirimId,
                    KiraSozlesmesiId = targetSozlesme.Id,
                    DonemBaslangic = DateTime.Today.AddMonths(-1),
                    DonemBitis = DateTime.Today.AddMonths(-1).AddDays(1),
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
        var salon = await _ctx.Birimler.Include(b => b.Tasinmaz).FirstOrDefaultAsync(b => b.Ad == "Toplantı Salonu Z01");
        var salonB = await _ctx.Birimler.Include(b => b.Tasinmaz).FirstOrDefaultAsync(b => b.Ad == "Toplantı Odası Z02");
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
                BirimId = salon.Id,
                RezervasyonId = rezervasyon1.Id,
                DonemBaslangic = rezervasyon1.BaslangicTarihi,
                DonemBitis = rezervasyon1.BitisTarihi,
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

        // 3. Z02 Rezervasyonu (Gelecek - Planlandı)
        var kiraciVeri = await _ctx.Kiraciler.FirstOrDefaultAsync(k => k.Email == "iletisim@biotech.com");
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
        _ctx.KullaniciYetkiKapsamlari.RemoveRange(_ctx.KullaniciYetkiKapsamlari.IgnoreQueryFilters());
        _ctx.Davetiyeler.RemoveRange(_ctx.Davetiyeler.IgnoreQueryFilters());
        _ctx.SifreSifirlamaTalepleri.RemoveRange(_ctx.SifreSifirlamaTalepleri.IgnoreQueryFilters());
        _ctx.OdemeLinkKayitlari.RemoveRange(_ctx.OdemeLinkKayitlari.IgnoreQueryFilters());

        // Temizlik sırası önemlidir (FK kısıtlamaları nedeniyle)
        _ctx.OdemeBankaEslesmeleri.RemoveRange(_ctx.OdemeBankaEslesmeleri.IgnoreQueryFilters());
        _ctx.TahakkukOdemeler.RemoveRange(_ctx.TahakkukOdemeler.IgnoreQueryFilters());
        _ctx.BankaHareketleri.RemoveRange(_ctx.BankaHareketleri.IgnoreQueryFilters());

        _ctx.Rezervasyonlari.RemoveRange(_ctx.Rezervasyonlari.IgnoreQueryFilters());
        _ctx.RezervasyonTarifeler.RemoveRange(_ctx.RezervasyonTarifeler.IgnoreQueryFilters());

        _ctx.TahakkukKalemleri.RemoveRange(_ctx.TahakkukKalemleri.IgnoreQueryFilters());
        _ctx.Tahakkuklar.RemoveRange(_ctx.Tahakkuklar.IgnoreQueryFilters());

        _ctx.SozlesmeTarifeler.RemoveRange(_ctx.SozlesmeTarifeler.IgnoreQueryFilters());
        _ctx.SozlesmeIslemGecmisleri.RemoveRange(_ctx.SozlesmeIslemGecmisleri.IgnoreQueryFilters());
        _ctx.Sozlesmeler.RemoveRange(_ctx.Sozlesmeler.IgnoreQueryFilters());

        _ctx.BirimTarifeler.RemoveRange(_ctx.BirimTarifeler.IgnoreQueryFilters());
        _ctx.Birimler.RemoveRange(_ctx.Birimler.IgnoreQueryFilters());

        _ctx.TasinmazTarifeler.RemoveRange(_ctx.TasinmazTarifeler.IgnoreQueryFilters());
        _ctx.Tasinmazlar.RemoveRange(_ctx.Tasinmazlar.IgnoreQueryFilters());

        _ctx.GenelTarifeler.RemoveRange(_ctx.GenelTarifeler.IgnoreQueryFilters());

        // Belgeleri sil (BelgeTurleri temizlenmeden önce silinmelidir)
        _ctx.Belgeler.RemoveRange(_ctx.Belgeler.IgnoreQueryFilters());
        await _ctx.SaveChangesAsync();

        // Kiracı kullanıcılarını ve rollerini temizle (Referans veren tüm tahakkuk ödemeleri silindikten sonra güvenle silinebilir)
        var kiraciUsers = await _userManager.Users.Where(u => u.UserType == UserType.Kiraci).ToListAsync();
        foreach (var ku in kiraciUsers)
        {
            await _userRolService.RemoveAllRolesAsync(ku.Id);
            await _userManager.DeleteAsync(ku);
        }

        var kiraciRoller = await _ctx.Roller.IgnoreQueryFilters().Where(r => r.Scope == RolScope.Kiraci && r.KiraciId != null).ToListAsync();
        _ctx.Roller.RemoveRange(kiraciRoller);
        await _ctx.SaveChangesAsync();

        // Artık üzerinde hiçbir referans kalmayan Kiraciler tablosunu silebiliriz
        _ctx.Kiraciler.RemoveRange(_ctx.Kiraciler.IgnoreQueryFilters());
        await _ctx.SaveChangesAsync();

        // Sistem Tanımları (Baştan seed edileceği için temizlenebilir)
        _ctx.Kategoriler.RemoveRange(_ctx.Kategoriler.IgnoreQueryFilters());
        _ctx.BirimTurleri.RemoveRange(_ctx.BirimTurleri.IgnoreQueryFilters());
        _ctx.BorcTipleri.RemoveRange(_ctx.BorcTipleri.IgnoreQueryFilters().Where(b => !b.Sistem));
        _ctx.BelgeTurleri.RemoveRange(_ctx.BelgeTurleri.IgnoreQueryFilters().Where(b => !b.Sistem));

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
