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
    private readonly IChargeGenerationService _chargeGeneration;
    private readonly IRateResolverService _rateResolver;
    private readonly IRoleService _rolService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRoleService _userRolService;

    public SeedDataService(
        ApplicationDbContext ctx,
        IChargeGenerationService tahakkukUretim,
        IRateResolverService rateResolver,
        IRoleService roleService,
        UserManager<ApplicationUser> userManager,
        IUserRoleService userRoleService)
    {
        _ctx = ctx;
        _chargeGeneration = tahakkukUretim;
        _rateResolver = rateResolver;
        _rolService = roleService;
        _userManager = userManager;
        _userRolService = userRoleService;
    }

    public async Task SeedEnumDegerleriAsync()
    {
        var enumTypes = typeof(LeaseStatus).Assembly.GetTypes()
            .Where(t => t.IsEnum && t.Namespace == "KiraTakip.Models")
            .ToList();

        var existing = await _ctx.LookupValues
            .Select(e => new { e.EnumName, e.Value })
            .ToListAsync();
        var existingSet = existing.Select(e => (e.EnumName, e.Value)).ToHashSet();

        foreach (var enumType in enumTypes)
        {
            foreach (var value in Enum.GetValues(enumType))
            {
                int intVal = (int)value;
                string enumAdi = enumType.Name;
                if (existingSet.Contains((enumAdi, intVal))) continue;

                _ctx.LookupValues.Add(new LookupValue
                {
                    EnumName = enumAdi,
                    Value = intVal,
                    Name = Enum.GetName(enumType, value)!
                });
            }
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedBorcTipleriAsync()
    {
        var existingCodes = await _ctx.ChargeTypes.Select(b => b.Code).ToListAsync();
        var toAdd = new List<ChargeType>();

        if (!existingCodes.Contains("ORTAK")) toAdd.Add(new ChargeType { Name = "Ortak Gider", Code = "ORTAK", IsActive = true, SortOrder = 2, Behavior = ChargeTypeBehavior.MonthlyFixed, IsSystem = false });
        if (!existingCodes.Contains("PORTAL")) toAdd.Add(new ChargeType { Name = "Portal Gideri", Code = "PORTAL", IsActive = true, SortOrder = 3, Behavior = ChargeTypeBehavior.MonthlyFixed, IsSystem = false });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new ChargeType { Name = "Toplantı Salonu Kullanım Bedeli", Code = "TOPLANTI", IsActive = true, SortOrder = 4, Behavior = ChargeTypeBehavior.ReservationSpecific, IsSystem = false });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new ChargeType { Name = "Etkinlik Alanı Kullanım Bedeli", Code = "ETKINLIK", IsActive = true, SortOrder = 5, Behavior = ChargeTypeBehavior.ReservationSpecific, IsSystem = false });

        if (toAdd.Any())
        {
            _ctx.ChargeTypes.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }

        // Mevcut kayıtların davranışlarını doğrula (Idempotency)
        await _ctx.ChargeTypes.Where(b => b.Code == "TOPLANTI").ExecuteUpdateAsync(s => s.SetProperty(b => b.Behavior, ChargeTypeBehavior.ReservationSpecific));
        await _ctx.ChargeTypes.Where(b => b.Code == "ETKINLIK").ExecuteUpdateAsync(s => s.SetProperty(b => b.Behavior, ChargeTypeBehavior.ReservationSpecific));
    }

    public async Task EnsureVarsayilanRezervasyonTarifeAsync()
    {
        var cariYil = DateTime.Now.Year;
        var varsayilanUcret = 500m;
        var varsayilanUcretsizSure = 120;
        var varsayilanPeriyot = 60;
        var varsayilanKdv = 20m;

        var rezBirimTurleri = await _ctx.BirimTurleri
            .Where(t => t.IsActive && t.RezervasyonYapilabilirMi)
            .ToListAsync();
        if (!rezBirimTurleri.Any()) return;

        var mevcut = await _ctx.RezervasyonTarifeler
            .Where(r => r.UnitId == null && r.Yil == cariYil)
            .Select(r => r.UnitTypeId)
            .ToListAsync();

        foreach (var bt in rezBirimTurleri.Where(b => !mevcut.Contains(b.Id)))
        {
            _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
            {
                Yil = cariYil,
                UnitTypeId = bt.Id,
                FreeDurationMinutes = varsayilanUcretsizSure,
                UcretlendirmePeriyoduDakika = varsayilanPeriyot,
                PeriyotUcreti = varsayilanUcret,
                KdvRate = varsayilanKdv,
                Aciklama = $"{cariYil} varsayılan — {bt.Ad}"
            });
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedTasinmazTipleriAsync()
    {
        var existingCodes = await _ctx.TasinmazTipleri.Select(k => k.Kod).ToListAsync();
        var toAdd = new List<TasinmazTipi>();

        if (!existingCodes.Contains("BINA")) toAdd.Add(new TasinmazTipi { Ad = "Bina", Kod = "BINA", IsActive = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow, TekParcaDestekli = true, BirimBazliDestekli = true });

        if (toAdd.Any())
        {
            _ctx.TasinmazTipleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedBirimTurleriAsync()
    {
        var existingCodes = await _ctx.BirimTurleri.Select(t => t.Kod).ToListAsync();

        var toplantiBorcTipiId = await _ctx.ChargeTypes
            .Where(b => b.Code == "TOPLANTI")
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        var etkinliBorcTipiId = await _ctx.ChargeTypes
            .Where(b => b.Code == "ETKINLIK")
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        var toAdd = new List<UnitType>();
        if (!existingCodes.Contains("OFIS")) toAdd.Add(new UnitType { Ad = "Ofis", Kod = "OFIS", IsActive = true, KiralanabilirMi = true, RezervasyonYapilabilirMi = false, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new UnitType { Ad = "Toplantı Salonu", Kod = "TOPLANTI", IsActive = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true, Sira = 10, OlusturmaTarihi = DateTime.UtcNow, ChargeTypeId = toplantiBorcTipiId });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new UnitType { Ad = "Etkinlik Alanı", Kod = "ETKINLIK", IsActive = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true, Sira = 11, OlusturmaTarihi = DateTime.UtcNow, ChargeTypeId = etkinliBorcTipiId });

        if (toAdd.Any())
        {
            _ctx.BirimTurleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }

        if (toplantiBorcTipiId.HasValue)
        {
            await _ctx.BirimTurleri
                .Where(t => t.Kod == "TOPLANTI" && t.ChargeTypeId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ChargeTypeId, toplantiBorcTipiId));
        }

        if (etkinliBorcTipiId.HasValue)
        {
            await _ctx.BirimTurleri
                .Where(t => t.Kod == "ETKINLIK" && t.ChargeTypeId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ChargeTypeId, etkinliBorcTipiId));
        }
    }

    public async Task SeedKiraciKategorileriAsync()
    {
        var existingCodes = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tenant).Select(k => k.Kod).ToListAsync();
        var toAdd = new List<Kategori>();

        if (!existingCodes.Contains("AKADEMIK")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Tenant, Ad = "Akademik", Kod = "AKADEMIK", IsActive = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("AKADEMIK_OLMAYAN")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Tenant, Ad = "Akademik Olmayan", Kod = "AKADEMIK_OLMAYAN", IsActive = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });

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

        if (!existingCodes.Contains("YAZILIM")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Yazılım", Kod = "YAZILIM", IsActive = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("LOJISTIK")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Lojistik", Kod = "LOJISTIK", IsActive = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("GIDA")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Gıda", Kod = "GIDA", IsActive = true, Sira = 3, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TARIM")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Tarım", Kod = "TARIM", IsActive = true, Sira = 4, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("FINANS")) toAdd.Add(new Kategori { Tipi = KategoriTipi.Sektor, Ad = "Finans", Kod = "FINANS", IsActive = true, Sira = 5, OlusturmaTarihi = DateTime.UtcNow });

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
            .Where(k => k.Tipi == KategoriTipi.Tenant && k.IsActive)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        var borcTipleri = await _ctx.ChargeTypes
            .Where(b => b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(b => b.SortOrder)
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
                    ChargeTypeId = bt.Id,
                    CalculationMethod = (bt.Code == BorcTipiConsts.Kira || bt.Code == "ORTAK") ? CalculationMethod.M2 : CalculationMethod.Fixed,
                    UnitValue = bt.Code switch
                    {
                        BorcTipiConsts.Kira => kat.Kod == "AKADEMIK" ? 300m : 450m,
                        "ORTAK" => kat.Kod == "AKADEMIK" ? 100m : 150m,
                        "PORTAL" => kat.Kod == "AKADEMIK" ? 300m : 500m,
                        BorcTipiConsts.Depozito => kat.Kod == "AKADEMIK" ? 8000m : 15000m,
                        _ => 0m
                    },
                    KdvRate = 20m
                });
            }
        }

        await _ctx.SaveChangesAsync();
    }

    public async Task SeedDomainDataAsync()
    {
        if (await _ctx.Properties.AnyAsync()) return;

        var now = DateTime.Now;
        var tipiMap = await _ctx.TasinmazTipleri.ToDictionaryAsync(k => k.Kod, k => k.Id);
        var birimTuruMap = await _ctx.BirimTurleri.ToDictionaryAsync(t => t.Kod, t => t.Id);
        var katMap = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tenant).ToDictionaryAsync(k => k.Kod, k => k.Id);
        var sekMap = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Sektor).ToDictionaryAsync(k => k.Kod, k => k.Id);

        // --- Kiracılar ---
        var yzCozum = Tenant("KRC-000001", katMap["AKADEMIK_OLMAYAN"], sekMap["YAZILIM"], "Yapay Zeka Çözümleri A.Ş.",
            vergiNo: "1234567890", ticaretSicilNo: "İZM-123", telefon: "0232 444 5566", email: "info@yz.com", adres: "Teknokent");
        var megaFinans = Tenant("KRC-000002", katMap["AKADEMIK_OLMAYAN"], sekMap["FINANS"], "Mega Finans Hizmetleri A.Ş.",
            vergiNo: "9876543210", ticaretSicilNo: "İZM-456", telefon: "0232 555 6677", email: "info@megafinans.com", adres: "Teknokent");
        var biotech = Tenant("KRC-000003", katMap["AKADEMIK"], sekMap["YAZILIM"], "BiyoTek Akademik Arge Ltd.",
            vergiNo: "5556667770", ticaretSicilNo: "İZM-789", telefon: "0232 666 7788", email: "iletisim@biotech.com", adres: "Teknokent");

        _ctx.Tenants.AddRange(yzCozum, megaFinans, biotech);

        // --- Taşınmaz (Teknokent A Blok) ---
        var ofisTuruId = birimTuruMap["OFIS"];
        var toplantiTuruId = birimTuruMap["TOPLANTI"];

        var teknokent = new Property
        {
            Name = "Teknokent A Blok",
            PropertyTypeId = tipiMap.GetValueOrDefault("BINA"),
            RentalMode = RentalMode.UnitBased,
            City = "İzmir",
            District = "Bornova",
            Neighborhood = "Ege Üniversitesi",
            Address = "Ege Üniversitesi Teknokent Kampüsü",
            OpenArea = 500,
            ClosedArea = 4500,
            FloorCount = 4,
            Description = "Ofis bazlı kiralanabilir teknokent binası"
        };

        // 5 Kiralanabilir Ofis Ekleme
        for (int ofis = 1; ofis <= 5; ofis++)
        {
            var ofisNo = $"10{ofis}";
            teknokent.Units.Add(new Unit
            {
                UnitKind = UnitKind.Unit,
                UnitNo = ofisNo,
                FloorNo = 1,
                Name = $"Ofis {ofisNo}",
                Area = 50 + (ofis * 10),
                UnitTypeId = ofisTuruId
            });
        }

        // 2 Rezerve Edilebilir Toplantı Odası Ekleme
        var toplantiZ01 = new Unit
        {
            UnitKind = UnitKind.Unit,
            UnitNo = "Z01",
            FloorNo = 0,
            Name = "Toplantı Salonu Z01",
            Area = 80,
            UnitTypeId = toplantiTuruId,
            Description = "Ortak kullanıma açık ana toplantı salonu."
        };
        var toplantiZ02 = new Unit
        {
            UnitKind = UnitKind.Unit,
            UnitNo = "Z02",
            FloorNo = 0,
            Name = "Toplantı Odası Z02",
            Area = 40,
            UnitTypeId = toplantiTuruId,
            Description = "Ortak kullanıma açık küçük toplantı odası."
        };
        teknokent.Units.Add(toplantiZ01);
        teknokent.Units.Add(toplantiZ02);

        _ctx.Properties.Add(teknokent);
        await _ctx.SaveChangesAsync();

        // --- Tarifelerin Oluşturulması ---
        await SeedTasinmazFiyatlarAsync();

        var btKiraId = (await _ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Kira)).Id;
        var btDepozitoId = (await _ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Depozito)).Id;

        var birim101 = teknokent.Units.First(b => b.UnitNo == "101");
        var birim102 = teknokent.Units.First(b => b.UnitNo == "102");
        var birim103 = teknokent.Units.First(b => b.UnitNo == "103");
        var birim104 = teknokent.Units.First(b => b.UnitNo == "104");

        // Unit Tarifesi Örneği (Hiyerarşide Matrisin Üstündedir)
        // Ofis 101 için Akademik kategorisinde özel birim fiyatı tanımlayalım
        _ctx.BirimTarifeler.Add(new BirimTarife
        {
            UnitId = birim101.Id,
            KiraciKategoriId = katMap["AKADEMIK"],
            ChargeTypeId = btKiraId,
            CalculationMethod = CalculationMethod.M2,
            UnitValue = 400, // Genel Tarife 300 / Matris 320 yerine birim bazlı 400
            KdvRate = 20
        });

        // Reservation Tarifesi - Genel (BirimId = null)
        var mevcutGenelRez = await _ctx.RezervasyonTarifeler
            .FirstOrDefaultAsync(r => r.UnitId == null && r.UnitTypeId == toplantiTuruId && r.Yil == now.Year);
        if (mevcutGenelRez != null)
        {
            mevcutGenelRez.FreeDurationMinutes = 60;
            mevcutGenelRez.UcretlendirmePeriyoduDakika = 60;
            mevcutGenelRez.PeriyotUcreti = 400m;
            mevcutGenelRez.KdvRate = 20m;
            mevcutGenelRez.Aciklama = "Genel Toplantı Salonu fiyatlandırma kuralı";
        }
        else
        {
            _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
            {
                Yil = now.Year,
                UnitTypeId = toplantiTuruId,
                UnitId = null,
                FreeDurationMinutes = 60,
                UcretlendirmePeriyoduDakika = 60,
                PeriyotUcreti = 400m,
                KdvRate = 20m,
                Aciklama = "Genel Toplantı Salonu fiyatlandırma kuralı"
            });
        }

        // Reservation Tarifesi - Unit (BirimId = Z01.Id)
        _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
        {
            Yil = now.Year,
            UnitTypeId = toplantiTuruId,
            UnitId = toplantiZ01.Id,
            FreeDurationMinutes = 30,
            UcretlendirmePeriyoduDakika = 60,
            PeriyotUcreti = 600m,
            KdvRate = 20m,
            Aciklama = "Toplantı Salonu Z01 için özel fiyatlandırma kuralı"
        });

        // Kullanıcı tanımlı Belge Türlerini ekle
        var btKimlik = new DocumentType
        {
            Code = "KIMLIK_FOTOKOPISI",
            Name = "Kimlik Fotokopisi",
            TargetEntity = BelgeOwnerTipi.Tenant,
            Required = true,
            AllowedExtensions = "pdf,jpg,png",
            MaxSizeMb = 5,
            SortOrder = 1,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btSozlesmeEvrak = new DocumentType
        {
            Code = "SOZLESME_EVRAK",
            Name = "Sözleşme Evrakı",
            TargetEntity = BelgeOwnerTipi.Tenant,
            Required = false,
            AllowedExtensions = "pdf,jpg,png",
            MaxSizeMb = 5,
            SortOrder = 2,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btImzaliSozlesme = new DocumentType
        {
            Code = "IMZALI_SOZLESME",
            Name = "İmzalı Sözleşme Metni",
            TargetEntity = BelgeOwnerTipi.Lease,
            Required = true,
            AllowedExtensions = "pdf,jpg,png",
            MaxSizeMb = 10,
            SortOrder = 3,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btKvkk = new DocumentType
        {
            Code = "KVKK_BELGESI",
            Name = "KVKK Onay Belgesi",
            TargetEntity = BelgeOwnerTipi.Tenant,
            Required = true,
            AllowedExtensions = "pdf,jpg,png",
            MaxSizeMb = 5,
            SortOrder = 4,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btTeslim = new DocumentType
        {
            Code = "TESLIM_TESELLUM",
            Name = "Teslim Tesellüm Tutanağı",
            TargetEntity = BelgeOwnerTipi.Lease,
            Required = false,
            AllowedExtensions = "pdf,jpg,png",
            MaxSizeMb = 5,
            SortOrder = 5,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        var btTeminat = new DocumentType
        {
            Code = "TEMINAT_MEKTUBU",
            Name = "Teminat Mektubu",
            TargetEntity = BelgeOwnerTipi.Lease,
            Required = false,
            AllowedExtensions = "pdf,jpg,png",
            MaxSizeMb = 5,
            SortOrder = 6,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        };

        _ctx.DocumentTypes.AddRange(btKimlik, btSozlesmeEvrak, btImzaliSozlesme, btKvkk, btTeslim, btTeminat);
        await _ctx.SaveChangesAsync();

        // Kiracılar için belgeleri ekle
        var belgeler = new List<Belge>
        {
            // Kimlik Fotokopisi belgeleri
            new Belge
            {
                DocumentTypeId = btKimlik.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
                DocumentTypeId = btKimlik.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
                DocumentTypeId = btKimlik.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
                DocumentTypeId = btKvkk.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
                DocumentTypeId = btKvkk.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
                DocumentTypeId = btKvkk.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
                DocumentTypeId = btSozlesmeEvrak.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
                DocumentTypeId = btSozlesmeEvrak.Id,
                OwnerType = BelgeOwnerTipi.Tenant,
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
        async Task<decimal> ResolveKiraM2Rate(Unit b, Tenant k)
        {
            var res = await _rateResolver.ResolveAsync(null, k.Id, b.Id, btKiraId, now);
            return res?.UnitValue ?? 0;
        }

        // --- 5. Sözleşmelerin Oluşturulması ---
        var startYearMinus1 = new DateTime(now.Year - 1, 1, 1);

        var rate101 = await ResolveKiraM2Rate(birim101, yzCozum);
        var rate102 = await ResolveKiraM2Rate(birim102, megaFinans);
        var rate103 = await ResolveKiraM2Rate(birim103, biotech);
        var rate104 = await ResolveKiraM2Rate(birim104, yzCozum);

        var sozlesmeler = new List<Lease>
        {
            MakeSozlesme(birim101, yzCozum, startYearMinus1, startYearMinus1.AddYears(2).AddDays(-1), true,
                vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 5),
            MakeSozlesme(birim102, megaFinans, startYearMinus1.AddMonths(3), startYearMinus1.AddMonths(24).AddDays(-1), true,
                vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 10),
            MakeSozlesme(birim103, biotech, startYearMinus1.AddMonths(6), startYearMinus1.AddMonths(18).AddDays(-1), true,
                vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 15),
            MakeSozlesme(birim104, yzCozum, startYearMinus1.AddMonths(1), startYearMinus1.AddYears(2).AddDays(-1), true,
                vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 5)
        };

        _ctx.Leases.AddRange(sozlesmeler);
        await _ctx.SaveChangesAsync();

        // Sözleşmeler için İmzalı Sözleşme Metni belgelerini ekle
        var sozlesmeBelgeleri = new List<Belge>
        {
            new Belge
            {
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Lease,
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
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Lease,
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
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Lease,
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
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = BelgeOwnerTipi.Lease,
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
                DocumentTypeId = btTeslim.Id,
                OwnerType = BelgeOwnerTipi.Lease,
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
                DocumentTypeId = btTeminat.Id,
                OwnerType = BelgeOwnerTipi.Lease,
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
            new SozlesmeTarife { LeaseId = sozlesmeler[0].Id, ChargeTypeId = btKiraId, UnitValue = rate101, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new SozlesmeTarife { LeaseId = sozlesmeler[1].Id, ChargeTypeId = btKiraId, UnitValue = rate102, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new SozlesmeTarife { LeaseId = sozlesmeler[2].Id, ChargeTypeId = btKiraId, UnitValue = rate103, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new SozlesmeTarife { LeaseId = sozlesmeler[3].Id, ChargeTypeId = btKiraId, UnitValue = rate104, CalculationMethod = CalculationMethod.M2, KdvRate = 20 }
        );
        await _ctx.SaveChangesAsync();

        // --- 7. Charge Üretimi ---
        foreach (var s in sozlesmeler)
        {
            await _chargeGeneration.UretSozlesmeIcinAsync(s.Id);
        }

        // --- 8. Diğer Seed İşlemleri ---
        await SeedRezervasyonlarAsync();
        await SeedBankaHareketleriAsync();
        await SeedTahakkuklarVeOdemelerAsync(sozlesmeler);

        // --- 9. Kiracı Rol ve Kullanıcı Seed İşlemleri ---
        var seededKiraciler = await _ctx.Tenants.ToListAsync();
        foreach (var k in seededKiraciler)
        {
            if (!string.IsNullOrWhiteSpace(k.Email))
            {
                var (userEmail, adSoyad, password) = k.Email switch
                {
                    "info@yz.com" => ("ahmet.yilmaz@yz.com", "Ahmet Yılmaz", "Ahmet123!"),
                    "info@megafinans.com" => ("mehmet.demir@megafinans.com", "Mehmet Demir", "Mehmet123!"),
                    "iletisim@biotech.com" => ("ayse.kaya@biotech.com", "Ayşe Kaya", "Ayse123!"),
                    _ => (k.Email, k.DisplayName, "User123!")
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
                var ofis101 = await _ctx.Units.FirstOrDefaultAsync(b => b.UnitNo == "101");
                if (ofis101 != null)
                {
                    var hasScope = await _ctx.KullaniciYetkiKapsamlari.AnyAsync(s => s.UserId == mehmetUser.Id);
                    if (!hasScope)
                    {
                        _ctx.KullaniciYetkiKapsamlari.Add(new KullaniciYetkiKapsami
                        {
                            UserId = mehmetUser.Id,
                            ScopeType = ScopeType.Unit,
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
        var teknokent = await _ctx.Properties.FirstOrDefaultAsync(t => t.Name == "Teknokent A Blok");
        if (teknokent != null && !await _ctx.TasinmazTarifeler.AnyAsync(f => f.PropertyId == teknokent.Id))
        {
            var katAkademik = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Tenant && k.Kod == "AKADEMIK");
            var katAkadOlmayan = await _ctx.Kategoriler.FirstAsync(k => k.Tipi == KategoriTipi.Tenant && k.Kod == "AKADEMIK_OLMAYAN");

            var btKira = await _ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Kira);
            var btOrtak = await _ctx.ChargeTypes.FirstAsync(b => b.Code == "ORTAK");
            var btPortal = await _ctx.ChargeTypes.FirstAsync(b => b.Code == "PORTAL");
            var btDepozito = await _ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Depozito);

            _ctx.TasinmazTarifeler.AddRange(
                // Akademik için (m2 bazlı kira ve ortak gider) - Taşınmaz Tarifesi
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkademik.Id, ChargeTypeId = btKira.Id, UnitValue = 320, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkademik.Id, ChargeTypeId = btOrtak.Id, UnitValue = 95, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkademik.Id, ChargeTypeId = btPortal.Id, UnitValue = 480, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkademik.Id, ChargeTypeId = btDepozito.Id, UnitValue = 9000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },

                // Akademik Olmayan için - Taşınmaz Tarifesi
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, ChargeTypeId = btKira.Id, UnitValue = 430, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, ChargeTypeId = btOrtak.Id, UnitValue = 140, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, ChargeTypeId = btPortal.Id, UnitValue = 700, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },
                new TasinmazTarife { PropertyId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, ChargeTypeId = btDepozito.Id, UnitValue = 22000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 }
            );

            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedTahakkuklarAsync()
    {
        // Geriye dönük uyumluluk için (UretSozlesmeIcinAsync SeedDomainDataAsync içinde çağrılıyor)
        if (await _ctx.Charges.AnyAsync()) return;
        var aktifSozlesmeler = await _ctx.Leases.Where(s => s.Status == LeaseStatus.Active).ToListAsync();
        foreach (var s in aktifSozlesmeler) await _chargeGeneration.UretSozlesmeIcinAsync(s.Id);
    }

    private async Task SeedTahakkuklarVeOdemelerAsync(List<Lease> sozlesmeler)
    {
        try
        {
            var adminUser = await _ctx.Users.FirstOrDefaultAsync();
            var adminId = adminUser?.Id ?? "admin-id-missing";

            // 1. Manuel Borçlar ve İptaller
            var manuelBorcTipi = await _ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == BorcTipiConsts.Diger);
            if (manuelBorcTipi != null)
            {
                var targetSozlesme = sozlesmeler.First();
                _ctx.Charges.Add(new Charge
                {
                    TenantId = targetSozlesme.TenantId,
                    UnitId = targetSozlesme.UnitId,
                    LeaseId = targetSozlesme.Id,
                    PeriodStart = DateTime.Today.AddDays(-5),
                    PeriodEnd = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(15),
                    ExpectedAmount = 2500m,
                    KdvAmount = 500m,
                    TotalAmount = 3000m,
                    PaidAmount = 0m,
                    Status = ChargeStatus.Pending,
                    SourceType = ChargeSourceType.Manual,
                    LineItems = new List<ChargeLineItem> { new ChargeLineItem { ChargeTypeId = manuelBorcTipi.Id, Description = "Ekstra Temizlik Bedeli", UnitValue = 2500m, Multiplier = 1m, Amount = 2500m, KdvRate = 20m, KdvAmount = 500m, TotalAmount = 3000m, SourceType = LineItemSourceType.ManualInput } }
                });

                // İptal Edilen Kayıt
                _ctx.Charges.Add(new Charge
                {
                    TenantId = targetSozlesme.TenantId,
                    UnitId = targetSozlesme.UnitId,
                    LeaseId = targetSozlesme.Id,
                    PeriodStart = DateTime.Today.AddMonths(-1),
                    PeriodEnd = DateTime.Today.AddMonths(-1).AddDays(1),
                    DueDate = DateTime.Today.AddMonths(-1),
                    ExpectedAmount = 500m,
                    KdvAmount = 100m,
                    TotalAmount = 600m,
                    PaidAmount = 0m,
                    Status = ChargeStatus.Cancelled,
                    SourceType = ChargeSourceType.Manual,
                    CancellationNote = "Hatalı giriş nedeniyle iptal edildi.",
                    LineItems = new List<ChargeLineItem> { new ChargeLineItem { ChargeTypeId = manuelBorcTipi.Id, Description = "Yanlış Borç Kaydı", UnitValue = 500m, Amount = 500m, KdvRate = 20m, TotalAmount = 600m, SourceType = LineItemSourceType.ManualInput } }
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
        var query = _ctx.Charges
            .Where(t => t.PeriodStart.Year == yil && t.Status == ChargeStatus.Pending);

        // Eğer cari yıl ise (2026), sadece bugünü ve geçmiş ayları öde (Gerçekçilik için)
        if (yil == DateTime.Today.Year)
        {
            query = query.Where(t => t.PeriodStart <= DateTime.Today);
        }

        var tahakkuklar = await query.ToListAsync();

        if (!tahakkuklar.Any()) return;

        int odenecekAdet = (int)Math.Round(tahakkuklar.Count * oran);
        var secilenler = tahakkuklar.OrderBy(x => Guid.NewGuid()).Take(odenecekAdet).ToList();

        foreach (var t in secilenler)
        {
            bool gecikmis = Random.Shared.Next(1, 10) > 7; // %30 ihtimalle gecikmiş ödeme
            bool kismiMi = Random.Shared.Next(1, 100) <= 30; // %30 ihtimalle kısmi ödeme

            decimal odemeTutari = t.TotalAmount;
            if (kismiMi)
            {
                // %25 ile %75 arasında rastgele bir tutar ödensin
                var kismiOran = Random.Shared.Next(25, 76) / 100m;
                odemeTutari = Math.Round(t.TotalAmount * kismiOran, 2);
            }

            var odeme = new PaymentAllocation
            {
                LeaseId = t.LeaseId,
                ChargeId = t.Id,
                PaymentDate = gecikmis ? t.DueDate.AddDays(Random.Shared.Next(15, 45)) : t.DueDate.AddDays(Random.Shared.Next(-5, 5)),
                Amount = odemeTutari,
                PaymentChannel = (PaymentChannel)Random.Shared.Next(1, 5),
                Status = PaymentStatus.Approved,
                Description = (kismiMi ? "Kısmi " : "") + (gecikmis ? "gecikmeli seed ödemesi" : "zamanında seed ödemesi"),
                CreatedByUserId = adminId
            };

            t.PaidAmount = odemeTutari;
            t.Status = kismiMi ? ChargeStatus.PartiallyPaid : ChargeStatus.Paid;
            _ctx.PaymentAllocations.Add(odeme);
        }
    }

    private async Task SeedKismiOdemelerAsync(string adminId)
    {
        var bekleyenler = await _ctx.Charges
            .Where(t => t.Status == ChargeStatus.Pending)
            .Take(3)
            .ToListAsync();

        foreach (var t in bekleyenler)
        {
            var kismiTutar = Math.Round(t.TotalAmount / 2, 2);
            var odeme = new PaymentAllocation
            {
                LeaseId = t.LeaseId,
                ChargeId = t.Id,
                PaymentDate = DateTime.Today.AddDays(-2),
                Amount = kismiTutar,
                PaymentChannel = PaymentChannel.Eft,
                Status = PaymentStatus.Approved,
                Description = "Seed kısmi ödeme",
                CreatedByUserId = adminId
            };
            t.PaidAmount = kismiTutar;
            t.Status = ChargeStatus.PartiallyPaid;
            _ctx.PaymentAllocations.Add(odeme);
        }
    }

    private async Task SeedRezervasyonlarAsync()
    {
        var salon = await _ctx.Units.Include(b => b.Property).FirstOrDefaultAsync(b => b.Name == "Toplantı Salonu Z01");
        var salonB = await _ctx.Units.Include(b => b.Property).FirstOrDefaultAsync(b => b.Name == "Toplantı Odası Z02");
        var kiraci = await _ctx.Tenants.FirstOrDefaultAsync();
        var sozlesme = await _ctx.Leases.FirstOrDefaultAsync(s => s.TenantId == kiraci.Id);

        if (salon == null || kiraci == null) return;

        var btRezervasyon = await _ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "TOPLANTI");

        // 1. Geçmiş Reservation (Tahakkuka Aktarıldı)
        var rezervasyon1 = new Reservation
        {
            UnitId = salon.Id,
            TenantId = kiraci.Id,
            StartDate = DateTime.Today.AddDays(-10).AddHours(10),
            EndDate = DateTime.Today.AddDays(-10).AddHours(13),
            TotalDurationMinutes = 180,
            FreeDurationMinutes = 60,
            PaidDurationMinutes = 120,
            UnitRate = 500,
            RateAmount = 1000,
            KdvRate = 20,
            KdvAmount = 200,
            TotalAmount = 1200,
            Status = ReservationStatus.TransferredToCharge,
        };
        _ctx.Reservations.Add(rezervasyon1);
        await _ctx.SaveChangesAsync();

        if (btRezervasyon != null)
        {
            var tahakkuk = new Charge
            {
                TenantId = kiraci.Id,
                UnitId = salon.Id,
                ReservationId = rezervasyon1.Id,
                PeriodStart = rezervasyon1.StartDate,
                PeriodEnd = rezervasyon1.EndDate,
                DueDate = rezervasyon1.EndDate.Date,
                ExpectedAmount = 1000,
                KdvAmount = 200,
                TotalAmount = 1200,
                PaidAmount = 0,
                Status = ChargeStatus.Pending,
                SourceType = ChargeSourceType.Reservation,
                LineItems = new List<ChargeLineItem>
                {
                    new ChargeLineItem
                    {
                        ChargeTypeId = btRezervasyon.Id,
                        Description = $"Toplantı salonu: {salon.Name} ({rezervasyon1.StartDate:dd.MM.yyyy HH:mm} – {rezervasyon1.EndDate:HH:mm})",
                        CalculationMethod = CalculationMethod.Fixed,
                        UnitValue = 1000,
                        Multiplier = 1,
                        Amount = 1000,
                        KdvRate = 20,
                        KdvAmount = 200,
                        TotalAmount = 1200,
                        SourceType = LineItemSourceType.ReservationRule
                    }
                }
            };
            _ctx.Charges.Add(tahakkuk);
            await _ctx.SaveChangesAsync();
        }

        // 2. Gelecek Reservation (Planlandı)
        _ctx.Reservations.Add(new Reservation
        {
            UnitId = salon.Id,
            TenantId = kiraci.Id,
            StartDate = DateTime.Today.AddDays(3).AddHours(14),
            EndDate = DateTime.Today.AddDays(3).AddHours(17),
            TotalDurationMinutes = 180,
            FreeDurationMinutes = 60,
            PaidDurationMinutes = 120,
            UnitRate = 500,
            RateAmount = 1000,
            KdvRate = 20,
            KdvAmount = 200,
            TotalAmount = 1200,
            Status = ReservationStatus.Planned,
        });

        // 3. Z02 Rezervasyonu (Gelecek - Planlandı)
        var kiraciVeri = await _ctx.Tenants.FirstOrDefaultAsync(k => k.Email == "iletisim@biotech.com");
        if (salonB != null && kiraciVeri != null)
        {
            _ctx.Reservations.Add(new Reservation
            {
                UnitId = salonB.Id,
                TenantId = kiraciVeri.Id,
                StartDate = DateTime.Today.AddDays(4).AddHours(10),
                EndDate = DateTime.Today.AddDays(4).AddHours(12),
                TotalDurationMinutes = 120,
                FreeDurationMinutes = 60,
                PaidDurationMinutes = 60,
                UnitRate = 500,
                RateAmount = 500,
                KdvRate = 20,
                KdvAmount = 100,
                TotalAmount = 600,
                Status = ReservationStatus.Planned
            });
        }

        await _ctx.SaveChangesAsync();
    }

    private async Task SeedBankaHareketleriAsync()
    {
        // Eşleşmiş Hareket
        _ctx.BankTransactions.Add(new BankTransaction
        {
            TransactionDate = DateTime.Today.AddDays(-1),
            TransactionAmount = 1500,
            Description = "KİRA ÖDEMESİ - TEKNOKENT",
            SenderInfo = "Yapay Zeka Çözümleri A.Ş.",
            BankCode = "TR01",
            MatchStatus = BankMatchStatus.Matched,
        });

        // Eşleşmemiş (Açıkta) Hareket
        _ctx.BankTransactions.Add(new BankTransaction
        {
            TransactionDate = DateTime.Today.AddDays(-2),
            TransactionAmount = 5000,
            Description = "HAVALE - BİLİNMEYEN",
            SenderIban = "TR123456789...",
            BankCode = "TR01",
            MatchStatus = BankMatchStatus.Unmatched,
        });

        await _ctx.SaveChangesAsync();
    }


    private static Tenant Tenant(string kiraciNo, int kategoriId, int sektorId, string ad,
        string? vergiNo = null, string? vergiDairesi = null,
        string? ticaretSicilNo = null, string? mersisNo = null,
        string telefon = "", string email = "", string? adres = null) => new()
        {
            TenantNo = kiraciNo,
            TenantCategoryId = kategoriId,
            SectorId = sektorId,
            Name = ad,
            TaxNo = vergiNo,
            TaxOffice = vergiDairesi,
            TradeRegistryNo = ticaretSicilNo,
            MersisNo = mersisNo,
            Phone = telefon,
            Email = email,
            Address = adres,
            RegistrationDate = DateTime.Now.AddMonths(-Random.Shared.Next(6, 36))
        };

    private static Lease MakeSozlesme(Unit birim, Tenant kiraci,
        DateTime baslangic, DateTime bitis,
        bool kdv, decimal kdvOrani = 20, string? notlar = null,
        DueDateRuleType vadeKuraliTipi = DueDateRuleType.FixedDayOfMonth,
        int vadeGunu = 1) => new()
        {
            Unit = birim,
            UnitId = birim.Id,
            Tenant = kiraci,
            TenantId = kiraci.Id,
            StartDate = baslangic,
            EndDate = bitis,
            Description = notlar,
            Status = LeaseStatus.Active,
            IsKdvApplied = kdv,
            DueDateRuleType = vadeKuraliTipi,
            DueDay = vadeGunu
        };

    public async Task ClearDomainDataAsync()
    {
        // Yetki kapsamlarını temizle (FK kısıtlaması nedeniyle)
        _ctx.KullaniciYetkiKapsamlari.RemoveRange(_ctx.KullaniciYetkiKapsamlari.IgnoreQueryFilters());
        _ctx.Davetiyeler.RemoveRange(_ctx.Davetiyeler.IgnoreQueryFilters());
        _ctx.SifreSifirlamaTalepleri.RemoveRange(_ctx.SifreSifirlamaTalepleri.IgnoreQueryFilters());
        _ctx.OdemeLinkKayitlari.RemoveRange(_ctx.OdemeLinkKayitlari.IgnoreQueryFilters());

        // Temizlik sırası önemlidir (FK kısıtlamaları nedeniyle)
        _ctx.PaymentMatches.RemoveRange(_ctx.PaymentMatches.IgnoreQueryFilters());
        _ctx.PaymentAllocations.RemoveRange(_ctx.PaymentAllocations.IgnoreQueryFilters());
        _ctx.BankTransactions.RemoveRange(_ctx.BankTransactions.IgnoreQueryFilters());

        _ctx.Reservations.RemoveRange(_ctx.Reservations.IgnoreQueryFilters());
        _ctx.RezervasyonTarifeler.RemoveRange(_ctx.RezervasyonTarifeler.IgnoreQueryFilters());

        _ctx.ChargeLineItems.RemoveRange(_ctx.ChargeLineItems.IgnoreQueryFilters());
        _ctx.Charges.RemoveRange(_ctx.Charges.IgnoreQueryFilters());

        _ctx.SozlesmeTarifeler.RemoveRange(_ctx.SozlesmeTarifeler.IgnoreQueryFilters());
        _ctx.SozlesmeIslemGecmisleri.RemoveRange(_ctx.SozlesmeIslemGecmisleri.IgnoreQueryFilters());
        _ctx.Leases.RemoveRange(_ctx.Leases.IgnoreQueryFilters());

        _ctx.BirimTarifeler.RemoveRange(_ctx.BirimTarifeler.IgnoreQueryFilters());
        _ctx.Units.RemoveRange(_ctx.Units.IgnoreQueryFilters());

        _ctx.TasinmazTarifeler.RemoveRange(_ctx.TasinmazTarifeler.IgnoreQueryFilters());
        _ctx.Properties.RemoveRange(_ctx.Properties.IgnoreQueryFilters());

        _ctx.GenelTarifeler.RemoveRange(_ctx.GenelTarifeler.IgnoreQueryFilters());

        // Belgeleri sil (DocumentTypes temizlenmeden önce silinmelidir)
        _ctx.Belgeler.RemoveRange(_ctx.Belgeler.IgnoreQueryFilters());
        await _ctx.SaveChangesAsync();

        // Kiracı kullanıcılarını ve rollerini temizle (Referans veren tüm tahakkuk ödemeleri silindikten sonra güvenle silinebilir)
        var kiraciUsers = await _userManager.Users.Where(u => u.UserType == UserType.Tenant).ToListAsync();
        foreach (var ku in kiraciUsers)
        {
            await _userRolService.RemoveAllRolesAsync(ku.Id);
            await _userManager.DeleteAsync(ku);
        }

        var kiraciRoller = await _ctx.Roller.IgnoreQueryFilters().Where(r => r.Scope == RoleScope.Tenant && r.KiraciId != null).ToListAsync();
        _ctx.Roller.RemoveRange(kiraciRoller);
        await _ctx.SaveChangesAsync();

        // Artık üzerinde hiçbir referans kalmayan Tenants tablosunu silebiliriz
        _ctx.Tenants.RemoveRange(_ctx.Tenants.IgnoreQueryFilters());
        await _ctx.SaveChangesAsync();

        // Sistem Tanımları (Baştan seed edileceği için temizlenebilir)
        _ctx.Kategoriler.RemoveRange(_ctx.Kategoriler.IgnoreQueryFilters());
        _ctx.BirimTurleri.RemoveRange(_ctx.BirimTurleri.IgnoreQueryFilters());
        _ctx.ChargeTypes.RemoveRange(_ctx.ChargeTypes.IgnoreQueryFilters().Where(b => !b.IsSystem));
        _ctx.DocumentTypes.RemoveRange(_ctx.DocumentTypes.IgnoreQueryFilters().Where(b => !b.IsSystem));

        await _ctx.SaveChangesAsync();
    }

    private async Task EnsureKiraciUserAsync(string email, string password, string adSoyad, int tenantId)
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
                UserType = UserType.Tenant,
                KiraciId = tenantId
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Kiracı kullanıcısı '{email}' oluşturulamadı: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            user.UserType = UserType.Tenant;
            user.KiraciId = tenantId;
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
