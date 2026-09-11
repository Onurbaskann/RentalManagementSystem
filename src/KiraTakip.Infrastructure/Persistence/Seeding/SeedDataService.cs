using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Pricing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Infrastructure.Seeding;

public class SeedDataService(
    ApplicationDbContext ctx,
    IChargeGenerationService tahakkukUretim,
    IRateResolverService rateResolver,
    UserManager<ApplicationUser> userManager,
    IUserRoleService userRoleService,
    IStoreAccountCredentialProtector credentialProtector)
{
    public async Task SeedEnumDegerleriAsync()
    {
        var enumTypes = typeof(LeaseStatus).Assembly.GetTypes()
            .Where(t => t.IsEnum && t.Namespace == "KiraTakip.Models")
            .ToList();

        var existing = await ctx.LookupValues
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

                ctx.LookupValues.Add(new LookupValue
                {
                    EnumName = enumAdi,
                    Value = intVal,
                    Name = Enum.GetName(enumType, value)!
                });
            }
        }
        await ctx.SaveChangesAsync();
    }

    public async Task SeedBorcTipleriAsync()
    {
        var existingCodes = await ctx.ChargeTypes.Select(b => b.Code).ToListAsync();
        var toAdd = new List<ChargeType>();

        if (!existingCodes.Contains("ORTAK")) toAdd.Add(new ChargeType { Name = "Ortak Gider", Code = "ORTAK", IsActive = true, SortOrder = 2, Behavior = ChargeTypeBehavior.MonthlyFixed, IsSystem = false });
        if (!existingCodes.Contains("PORTAL")) toAdd.Add(new ChargeType { Name = "Portal Gideri", Code = "PORTAL", IsActive = true, SortOrder = 3, Behavior = ChargeTypeBehavior.MonthlyFixed, IsSystem = false });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new ChargeType { Name = "Toplantı Salonu Kullanım Bedeli", Code = "TOPLANTI", IsActive = true, SortOrder = 4, Behavior = ChargeTypeBehavior.ReservationSpecific, IsSystem = false });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new ChargeType { Name = "Etkinlik Alanı Kullanım Bedeli", Code = "ETKINLIK", IsActive = true, SortOrder = 5, Behavior = ChargeTypeBehavior.ReservationSpecific, IsSystem = false });

        if (toAdd.Any())
        {
            ctx.ChargeTypes.AddRange(toAdd);
            await ctx.SaveChangesAsync();
        }

        // Mevcut kayıtların davranışlarını doğrula (Idempotency)
        await ctx.ChargeTypes.Where(b => b.Code == "TOPLANTI").ExecuteUpdateAsync(s => s.SetProperty(b => b.Behavior, ChargeTypeBehavior.ReservationSpecific));
        await ctx.ChargeTypes.Where(b => b.Code == "ETKINLIK").ExecuteUpdateAsync(s => s.SetProperty(b => b.Behavior, ChargeTypeBehavior.ReservationSpecific));
    }

    /// <summary>
    /// Faz 20 kalem bazlı ödeme çekirdeği için gereken seed mağaza/hesap ve her borç tipi
    /// için genel ödeme yönlendirmesini oluşturur. Idempotent — "SEED" mağazası zaten varsa
    /// yeniden oluşturulmaz, ama <see cref="ClearDomainDataAsync"/> sonrası sistem-dışı borç
    /// tipleri yeniden oluşturulup yeni Id aldığı için eksik kalan genel yönlendirmeler her
    /// çağrıda tamamlanır (yalnız mağaza varlığına bakıp tamamen çıkmaz).
    /// </summary>
    public async Task SeedOdemeMagazalariAsync()
    {
        var store = await ctx.Stores.FirstOrDefaultAsync(s => s.Code == "SEED");
        if (store == null)
        {
            store = new Store
            {
                Code = "SEED",
                Name = "Seed Mağaza",
                Description = "Geliştirme ortamı için otomatik oluşturulan varsayılan mağaza.",
                IsActive = true
            };
            store.Accounts.Add(new StoreAccount
            {
                ProviderCode = PaymentProviderCodes.Paratika,
                Currency = CurrencyCodes.Try,
                MerchantId = "SEED-MERCHANT",
                MerchantUser = "seed-user",
                ProtectedMerchantPassword = credentialProtector.Protect("seed-dummy-password"),
                ValidFrom = DateTime.UtcNow,
                IsActive = true
            });
            ctx.Stores.Add(store);
            await ctx.SaveChangesAsync();
        }

        var chargeTypeIds = await ctx.ChargeTypes.Select(ct => ct.Id).ToListAsync();
        var routedChargeTypeIds = await ctx.PaymentStoreRoutings
            .Where(r => r.PropertyId == null && r.UnitId == null && r.IsActive)
            .Select(r => r.ChargeTypeId)
            .ToListAsync();
        var missingChargeTypeIds = chargeTypeIds.Except(routedChargeTypeIds);
        foreach (var chargeTypeId in missingChargeTypeIds)
        {
            ctx.PaymentStoreRoutings.Add(new PaymentStoreRouting
            {
                ChargeTypeId = chargeTypeId,
                PropertyId = null,
                UnitId = null,
                StoreId = store.Id,
                IsActive = true
            });
        }
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Ödeme yönlendirmesinin Birim → Taşınmaz → Genel öncelik sırasını gerçek seed
    /// verisiyle (Teknokent A Blok / Ofis 101) gösteren örnek override'lar oluşturur.
    /// Idempotent — ilgili override zaten varsa tekrar oluşturmaz. `SeedDomainDataAsync`
    /// çalışıp Teknokent/Ofis 101 kaydı oluştuktan sonra çağrılmalıdır.
    /// </summary>
    public async Task SeedOrnekMagazaYonlendirmeleriAsync()
    {
        var teknokent = await ctx.Properties.FirstOrDefaultAsync(p => p.Name == "Teknokent A Blok");
        var ofis101 = teknokent == null
            ? null
            : await ctx.Units.FirstOrDefaultAsync(u => u.PropertyId == teknokent.Id && u.UnitNo == "101");
        if (teknokent == null || ofis101 == null) return;

        var kiraTipi = await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Kira);
        var ortakTipi = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "ORTAK");

        // Senaryo 1: Ofis 101 için Kira Bedeli'nde birim özel yönlendirme (Genel'in önüne geçer).
        var unitOverrideExists = await ctx.PaymentStoreRoutings.AnyAsync(r =>
            r.ChargeTypeId == kiraTipi.Id && r.UnitId == ofis101.Id && r.IsActive);
        if (!unitOverrideExists)
        {
            var unitStore = await GetOrCreateSeedStoreAsync(
                "SEED-UNIT101", "Ofis 101 Mağazası", "SEED-MERCHANT-U101", "seed-unit101-user");
            ctx.PaymentStoreRoutings.Add(new PaymentStoreRouting
            {
                ChargeTypeId = kiraTipi.Id,
                PropertyId = null,
                UnitId = ofis101.Id,
                StoreId = unitStore.Id,
                IsActive = true
            });
        }

        // Senaryo 2: Teknokent A Blok'un tamamı için Ortak Gider'de taşınmaz özel yönlendirme
        // (birim override'ı olmayan tüm birimlerde Genel'in önüne geçer).
        if (ortakTipi != null)
        {
            var propertyOverrideExists = await ctx.PaymentStoreRoutings.AnyAsync(r =>
                r.ChargeTypeId == ortakTipi.Id && r.PropertyId == teknokent.Id && r.IsActive);
            if (!propertyOverrideExists)
            {
                var propertyStore = await GetOrCreateSeedStoreAsync(
                    "SEED-ORTAK-TEKNOKENT", "Teknokent Ortak Gider Mağazası",
                    "SEED-MERCHANT-ORTAK", "seed-ortak-user");
                ctx.PaymentStoreRoutings.Add(new PaymentStoreRouting
                {
                    ChargeTypeId = ortakTipi.Id,
                    PropertyId = teknokent.Id,
                    UnitId = null,
                    StoreId = propertyStore.Id,
                    IsActive = true
                });
            }
        }

        await ctx.SaveChangesAsync();
    }

    private async Task<Store> GetOrCreateSeedStoreAsync(
        string code, string name, string merchantId, string merchantUser)
    {
        var existing = await ctx.Stores.FirstOrDefaultAsync(s => s.Code == code);
        if (existing != null) return existing;

        var store = new Store { Code = code, Name = name, IsActive = true };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = merchantId,
            MerchantUser = merchantUser,
            ProtectedMerchantPassword = credentialProtector.Protect("seed-dummy-password"),
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });
        ctx.Stores.Add(store);
        await ctx.SaveChangesAsync();
        return store;
    }

    public async Task EnsureVarsayilanReservationRateOverrideAsync()
    {
        var cariYil = DateTime.Now.Year;
        var varsayilanUcret = 500m;
        var varsayilanUcretsizSure = 120;
        var varsayilanPeriyot = 60;
        var varsayilanKdv = 20m;

        var rezBirimTurleri = await ctx.UnitTypes
            .Where(t => t.IsActive && t.Usage == UnitTypeUsage.Reservable)
            .ToListAsync();
        if (!rezBirimTurleri.Any()) return;

        var mevcut = await ctx.RezervasyonTarifeler
            .Where(r => r.UnitId == null && r.Year == cariYil)
            .Select(r => r.UnitTypeId)
            .ToListAsync();

        foreach (var bt in rezBirimTurleri.Where(b => !mevcut.Contains(b.Id)))
        {
            ctx.RezervasyonTarifeler.Add(new ReservationRateOverride
            {
                Year = cariYil,
                UnitTypeId = bt.Id,
                FreeDurationMinutes = varsayilanUcretsizSure,
                BillingPeriodMinutes = varsayilanPeriyot,
                PeriodRate = varsayilanUcret,
                KdvRate = varsayilanKdv,
                Description = $"{cariYil} varsayılan — {bt.Name}"
            });
        }
        await ctx.SaveChangesAsync();
    }

    public async Task SeedTasinmazTipleriAsync()
    {
        var existingCodes = await ctx.TasinmazTipleri.Select(t => t.Code).ToListAsync();
        var toAdd = new List<PropertyType>();

        if (!existingCodes.Contains("BINA")) toAdd.Add(new PropertyType { Name = "Bina", Code = "BINA", IsActive = true, SortOrder = 1, SupportsSingleUnit = true, SupportsMultipleUnits = true });

        if (toAdd.Any())
        {
            ctx.TasinmazTipleri.AddRange(toAdd);
            await ctx.SaveChangesAsync();
        }
    }

    public async Task SeedBirimTurleriAsync()
    {
        var existingCodes = await ctx.UnitTypes.Select(t => t.Code).ToListAsync();

        var toplantiBorcTipiId = await ctx.ChargeTypes
            .Where(b => b.Code == "TOPLANTI")
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        var etkinliBorcTipiId = await ctx.ChargeTypes
            .Where(b => b.Code == "ETKINLIK")
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        var toAdd = new List<UnitType>();
        if (!existingCodes.Contains("OFIS")) toAdd.Add(new UnitType { Name = "Ofis", Code = "OFIS", IsActive = true, Usage = UnitTypeUsage.Rentable, SortOrder = 1 });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new UnitType { Name = "Toplantı Salonu", Code = "TOPLANTI", IsActive = true, Usage = UnitTypeUsage.Reservable, SortOrder = 10, ChargeTypeId = toplantiBorcTipiId });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new UnitType { Name = "Etkinlik Alanı", Code = "ETKINLIK", IsActive = true, Usage = UnitTypeUsage.Reservable, SortOrder = 11, ChargeTypeId = etkinliBorcTipiId });

        if (toAdd.Any())
        {
            ctx.UnitTypes.AddRange(toAdd);
            await ctx.SaveChangesAsync();
        }

        if (toplantiBorcTipiId.HasValue)
        {
            await ctx.UnitTypes
                .Where(t => t.Code == "TOPLANTI" && t.ChargeTypeId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ChargeTypeId, toplantiBorcTipiId));
        }

        if (etkinliBorcTipiId.HasValue)
        {
            await ctx.UnitTypes
                .Where(t => t.Code == "ETKINLIK" && t.ChargeTypeId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ChargeTypeId, etkinliBorcTipiId));
        }
    }

    public async Task SeedKiraciKategorileriAsync()
    {
        var existingCodes = await ctx.Kategoriler.Where(k => k.Type == CategoryType.Tenant).Select(k => k.Code).ToListAsync();
        var toAdd = new List<Category>();

        if (!existingCodes.Contains("AKADEMIK")) toAdd.Add(new Category { Type = CategoryType.Tenant, Name = "Akademik", Code = "AKADEMIK", IsActive = true, Order = 1, CreatedAt = DateTime.UtcNow });
        if (!existingCodes.Contains("AKADEMIK_OLMAYAN")) toAdd.Add(new Category { Type = CategoryType.Tenant, Name = "Akademik Olmayan", Code = "AKADEMIK_OLMAYAN", IsActive = true, Order = 2, CreatedAt = DateTime.UtcNow });

        if (toAdd.Any())
        {
            ctx.Kategoriler.AddRange(toAdd);
            await ctx.SaveChangesAsync();
        }
    }

    public async Task SeedSektorlerAsync()
    {
        var existingCodes = await ctx.Kategoriler.Where(k => k.Type == CategoryType.Sector).Select(k => k.Code).ToListAsync();
        var toAdd = new List<Category>();

        if (!existingCodes.Contains("YAZILIM")) toAdd.Add(new Category { Type = CategoryType.Sector, Name = "Yazılım", Code = "YAZILIM", IsActive = true, Order = 1, CreatedAt = DateTime.UtcNow });
        if (!existingCodes.Contains("LOJISTIK")) toAdd.Add(new Category { Type = CategoryType.Sector, Name = "Lojistik", Code = "LOJISTIK", IsActive = true, Order = 2, CreatedAt = DateTime.UtcNow });
        if (!existingCodes.Contains("GIDA")) toAdd.Add(new Category { Type = CategoryType.Sector, Name = "Gıda", Code = "GIDA", IsActive = true, Order = 3, CreatedAt = DateTime.UtcNow });
        if (!existingCodes.Contains("TARIM")) toAdd.Add(new Category { Type = CategoryType.Sector, Name = "Tarım", Code = "TARIM", IsActive = true, Order = 4, CreatedAt = DateTime.UtcNow });
        if (!existingCodes.Contains("FINANS")) toAdd.Add(new Category { Type = CategoryType.Sector, Name = "Finans", Code = "FINANS", IsActive = true, Order = 5, CreatedAt = DateTime.UtcNow });

        if (toAdd.Any())
        {
            ctx.Kategoriler.AddRange(toAdd);
            await ctx.SaveChangesAsync();
        }
    }

    public async Task SeedTarifelerAsync()
    {
        var cariYil = DateTime.Now.Year;
        if (await ctx.GenelTarifeler.AnyAsync(k => k.Year == cariYil)) return;

        var kategoriler = await ctx.Kategoriler
            .Where(k => k.Type == CategoryType.Tenant && k.IsActive)
            .OrderBy(k => k.Order)
            .ToListAsync();

        var borcTipleri = await ctx.ChargeTypes
            .Where(b => b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

        if (!kategoriler.Any() || !borcTipleri.Any()) return;

        foreach (var kat in kategoriler)
        {
            foreach (var bt in borcTipleri)
            {
                ctx.GenelTarifeler.Add(new RateSchedule
                {
                    Year = cariYil,
                    TenantCategoryId = kat.Id,
                    ChargeTypeId = bt.Id,
                    CalculationMethod = (bt.Code == BorcTipiConsts.Kira || bt.Code == "ORTAK") ? CalculationMethod.M2 : CalculationMethod.Fixed,
                    UnitValue = bt.Code switch
                    {
                        BorcTipiConsts.Kira => kat.Code == "AKADEMIK" ? 300m : 450m,
                        "ORTAK" => kat.Code == "AKADEMIK" ? 100m : 150m,
                        "PORTAL" => kat.Code == "AKADEMIK" ? 300m : 500m,
                        BorcTipiConsts.Depozito => kat.Code == "AKADEMIK" ? 8000m : 15000m,
                        _ => 0m
                    },
                    KdvRate = 20m
                });
            }
        }

        await ctx.SaveChangesAsync();
    }

    public async Task SeedDomainDataAsync()
    {
        if (await ctx.Properties.AnyAsync()) return;

        var now = DateTime.Now;
        var tipiMap = await ctx.TasinmazTipleri.ToDictionaryAsync(t => t.Code, k => k.Id);
        var birimTuruMap = await ctx.UnitTypes.ToDictionaryAsync(t => t.Code, t => t.Id);
        var katMap = await ctx.Kategoriler.Where(k => k.Type == CategoryType.Tenant).ToDictionaryAsync(k => k.Code, k => k.Id);
        var sekMap = await ctx.Kategoriler.Where(k => k.Type == CategoryType.Sector).ToDictionaryAsync(k => k.Code, k => k.Id);

        // --- Kiracılar ---
        var yzCozum = Tenant("KRC-000001", katMap["AKADEMIK_OLMAYAN"], sekMap["YAZILIM"], "Yapay Zeka Çözümleri A.Ş.",
            vergiNo: "1234567890", ticaretSicilNo: "İZM-123", telefon: "0232 444 5566", email: "info@yz.com", adres: "Teknokent");
        var megaFinans = Tenant("KRC-000002", katMap["AKADEMIK_OLMAYAN"], sekMap["FINANS"], "Mega Finans Hizmetleri A.Ş.",
            vergiNo: "9876543210", ticaretSicilNo: "İZM-456", telefon: "0232 555 6677", email: "info@megafinans.com", adres: "Teknokent");
        var biotech = Tenant("KRC-000003", katMap["AKADEMIK"], sekMap["YAZILIM"], "BiyoTek Akademik Arge Ltd.",
            vergiNo: "5556667770", ticaretSicilNo: "İZM-789", telefon: "0232 666 7788", email: "iletisim@biotech.com", adres: "Teknokent");

        ctx.Tenants.AddRange(yzCozum, megaFinans, biotech);

        // --- Taşınmaz (Teknokent A Blok) ---
        var ofisTuruId = birimTuruMap["OFIS"];
        var toplantiTuruId = birimTuruMap["TOPLANTI"];

        var teknokent = new Property
        {
            Name = "Teknokent A Blok",
            PropertyTypeId = tipiMap.GetValueOrDefault("BINA"),
            UnitStructure = UnitStructure.MultipleUnits,
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
            UnitNo = "Z01",
            FloorNo = 0,
            Name = "Toplantı Salonu Z01",
            Area = 80,
            UnitTypeId = toplantiTuruId,
            Description = "Ortak kullanıma açık ana toplantı salonu."
        };
        var toplantiZ02 = new Unit
        {
            UnitNo = "Z02",
            FloorNo = 0,
            Name = "Toplantı Odası Z02",
            Area = 40,
            UnitTypeId = toplantiTuruId,
            Description = "Ortak kullanıma açık küçük toplantı odası."
        };
        teknokent.Units.Add(toplantiZ01);
        teknokent.Units.Add(toplantiZ02);

        ctx.Properties.Add(teknokent);
        await ctx.SaveChangesAsync();

        // --- Tarifelerin Oluşturulması ---
        await SeedTasinmazFiyatlarAsync();

        var btKiraId = (await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Kira)).Id;
        var btDepozitoId = (await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Depozito)).Id;

        var birim101 = teknokent.Units.First(b => b.UnitNo == "101");
        var birim102 = teknokent.Units.First(b => b.UnitNo == "102");
        var birim103 = teknokent.Units.First(b => b.UnitNo == "103");
        var birim104 = teknokent.Units.First(b => b.UnitNo == "104");

        // Unit Tarifesi Örneği (Hiyerarşide Matrisin Üstündedir)
        // Ofis 101 için Akademik kategorisinde özel unit fiyatı tanımlayalım
        ctx.UnitRates.Add(new UnitRate
        {
            UnitId = birim101.Id,
            TenantCategoryId = katMap["AKADEMIK"],
            ChargeTypeId = btKiraId,
            CalculationMethod = CalculationMethod.M2,
            UnitValue = 400, // Genel Tarife 300 / Matris 320 yerine unit bazlı 400
            KdvRate = 20
        });

        // Reservation Tarifesi - Genel (UnitId = null)
        var mevcutGenelRez = await ctx.RezervasyonTarifeler
            .FirstOrDefaultAsync(r => r.UnitId == null && r.UnitTypeId == toplantiTuruId && r.Year == now.Year);
        if (mevcutGenelRez != null)
        {
            mevcutGenelRez.FreeDurationMinutes = 60;
            mevcutGenelRez.BillingPeriodMinutes = 60;
            mevcutGenelRez.PeriodRate = 400m;
            mevcutGenelRez.KdvRate = 20m;
            mevcutGenelRez.Description = "Genel Toplantı Salonu fiyatlandırma kuralı";
        }
        else
        {
            ctx.RezervasyonTarifeler.Add(new ReservationRateOverride
            {
                Year = now.Year,
                UnitTypeId = toplantiTuruId,
                UnitId = null,
                FreeDurationMinutes = 60,
                BillingPeriodMinutes = 60,
                PeriodRate = 400m,
                KdvRate = 20m,
                Description = "Genel Toplantı Salonu fiyatlandırma kuralı"
            });
        }

        // Reservation Tarifesi - Unit (UnitId = Z01.Id)
        ctx.RezervasyonTarifeler.Add(new ReservationRateOverride
        {
            Year = now.Year,
            UnitTypeId = toplantiTuruId,
            UnitId = toplantiZ01.Id,
            FreeDurationMinutes = 30,
            BillingPeriodMinutes = 60,
            PeriodRate = 600m,
            KdvRate = 20m,
            Description = "Toplantı Salonu Z01 için özel fiyatlandırma kuralı"
        });

        // Kullanıcı tanımlı Belge Türlerini ekle
        var btKimlik = new DocumentType
        {
            Code = "KIMLIK_FOTOKOPISI",
            Name = "Kimlik Fotokopisi",
            TargetEntity = DocumentOwnerType.Tenant,
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
            TargetEntity = DocumentOwnerType.Tenant,
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
            TargetEntity = DocumentOwnerType.Lease,
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
            TargetEntity = DocumentOwnerType.Tenant,
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
            TargetEntity = DocumentOwnerType.Lease,
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
            TargetEntity = DocumentOwnerType.Lease,
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

        ctx.DocumentTypes.AddRange(btKimlik, btSozlesmeEvrak, btImzaliSozlesme, btKvkk, btTeslim, btTeminat);
        await ctx.SaveChangesAsync();

        // Kiracılar için belgeleri ekle
        var belgeler = new List<Document>
        {
            // Kimlik Fotokopisi belgeleri
            new Document
            {
                DocumentTypeId = btKimlik.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = yzCozum.Id,
                FileName = "kimlik_yz.pdf",
                MimeType = "application/pdf",
                FileSize = 1024,
                Description = "YZ Çözüm Yetkili Kimlik Fotokopisi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 1, 2, 3, 4 } }
            },
            new Document
            {
                DocumentTypeId = btKimlik.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = megaFinans.Id,
                FileName = "kimlik_mega.pdf",
                MimeType = "application/pdf",
                FileSize = 1024,
                Description = "Mega Finans Yetkili Kimlik Fotokopisi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 1, 2, 3, 4 } }
            },
            new Document
            {
                DocumentTypeId = btKimlik.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = biotech.Id,
                FileName = "kimlik_biotech.pdf",
                MimeType = "application/pdf",
                FileSize = 1024,
                Description = "BiyoTek Yetkili Kimlik Fotokopisi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 1, 2, 3, 4 } }
            },

            // KVKK Onay Belgesi belgeleri
            new Document
            {
                DocumentTypeId = btKvkk.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = yzCozum.Id,
                FileName = "kvkk_yz.pdf",
                MimeType = "application/pdf",
                FileSize = 1024,
                Description = "YZ Çözüm Yetkili KVKK Belgesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 1, 2, 3, 4 } }
            },
            new Document
            {
                DocumentTypeId = btKvkk.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = megaFinans.Id,
                FileName = "kvkk_mega.pdf",
                MimeType = "application/pdf",
                FileSize = 1024,
                Description = "Mega Finans Yetkili KVKK Belgesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 1, 2, 3, 4 } }
            },
            new Document
            {
                DocumentTypeId = btKvkk.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = biotech.Id,
                FileName = "kvkk_biotech.pdf",
                MimeType = "application/pdf",
                FileSize = 1024,
                Description = "BiyoTek Yetkili KVKK Belgesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 1, 2, 3, 4 } }
            },

            // Sözleşme Evrakı belgeleri
            new Document
            {
                DocumentTypeId = btSozlesmeEvrak.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = yzCozum.Id,
                FileName = "sozlesme_yz.pdf",
                MimeType = "application/pdf",
                FileSize = 1024,
                Description = "YZ Çözüm Sözleşme Evrakı",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 1, 2, 3, 4 } }
            },
            new Document
            {
                DocumentTypeId = btSozlesmeEvrak.Id,
                OwnerType = DocumentOwnerType.Tenant,
                OwnerId = megaFinans.Id,
                FileName = "sozlesme_mega.pdf",
                MimeType = "application/pdf",
                FileSize = 2048,
                Description = "Mega Finans Sözleşme Evrakı",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 5, 6, 7, 8 } }
            }
        };
        ctx.Belgeler.AddRange(belgeler);
        await ctx.SaveChangesAsync();

        // Yardımcı fonksiyon: Dinamik m2 unit bedeli çözünürlüğü
        async Task<decimal> ResolveKiraM2Rate(Unit b, Tenant k)
        {
            var res = await rateResolver.ResolveAsync(null, k.Id, b.Id, btKiraId, now);
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

        ctx.Leases.AddRange(sozlesmeler);
        await ctx.SaveChangesAsync();

        // Sözleşmeler için İmzalı Sözleşme Metni belgelerini ekle
        var sozlesmeBelgeleri = new List<Document>
        {
            new Document
            {
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = DocumentOwnerType.Lease,
                OwnerId = sozlesmeler[0].Id,
                FileName = "imzali_sozlesme_101.pdf",
                MimeType = "application/pdf",
                FileSize = 4096,
                Description = "Ofis 101 İmzalı Kira Sözleşmesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 10, 11, 12, 13 } }
            },
            new Document
            {
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = DocumentOwnerType.Lease,
                OwnerId = sozlesmeler[1].Id,
                FileName = "imzali_sozlesme_102.pdf",
                MimeType = "application/pdf",
                FileSize = 4096,
                Description = "Ofis 102 İmzalı Kira Sözleşmesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 14, 15, 16, 17 } }
            },
            new Document
            {
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = DocumentOwnerType.Lease,
                OwnerId = sozlesmeler[2].Id,
                FileName = "imzali_sozlesme_103.pdf",
                MimeType = "application/pdf",
                FileSize = 4096,
                Description = "Ofis 103 İmzalı Kira Sözleşmesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 18, 19, 20, 21 } }
            },
            new Document
            {
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = DocumentOwnerType.Lease,
                OwnerId = sozlesmeler[3].Id,
                FileName = "imzali_sozlesme_104.pdf",
                MimeType = "application/pdf",
                FileSize = 4096,
                Description = "Ofis 104 İmzalı Kira Sözleşmesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 22, 23, 24, 25 } }
            },
            new Document
            {
                DocumentTypeId = btTeslim.Id,
                OwnerType = DocumentOwnerType.Lease,
                OwnerId = sozlesmeler[0].Id,
                FileName = "teslim_tutanagi_101.pdf",
                MimeType = "application/pdf",
                FileSize = 2048,
                Description = "Ofis 101 Teslim Tesellüm Tutanağı",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 30, 31, 32, 33 } }
            },
            new Document
            {
                DocumentTypeId = btTeminat.Id,
                OwnerType = DocumentOwnerType.Lease,
                OwnerId = sozlesmeler[0].Id,
                FileName = "teminat_mektubu_101.pdf",
                MimeType = "application/pdf",
                FileSize = 8192,
                Description = "Ofis 101 Teminat Mektubu",
                IsInvalid = false,
                Content = new DocumentContent { Content = new byte[] { 40, 41, 42, 43 } }
            }
        };
        ctx.Belgeler.AddRange(sozlesmeBelgeleri);
        await ctx.SaveChangesAsync();

        // --- 6. Sözleşme Tarifesi (Özel Oran) Uygulaması ---
        ctx.SozlesmeTarifeler.AddRange(
            new LeaseRateOverride { LeaseId = sozlesmeler[0].Id, ChargeTypeId = btKiraId, UnitValue = rate101, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new LeaseRateOverride { LeaseId = sozlesmeler[1].Id, ChargeTypeId = btKiraId, UnitValue = rate102, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new LeaseRateOverride { LeaseId = sozlesmeler[2].Id, ChargeTypeId = btKiraId, UnitValue = rate103, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new LeaseRateOverride { LeaseId = sozlesmeler[3].Id, ChargeTypeId = btKiraId, UnitValue = rate104, CalculationMethod = CalculationMethod.M2, KdvRate = 20 }
        );
        await ctx.SaveChangesAsync();

        // --- 7. Charge Üretimi ---
        foreach (var s in sozlesmeler)
        {
            await tahakkukUretim.GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));
        }

        // --- 8. Diğer Seed İşlemleri ---
        await SeedRezervasyonlarAsync();
        await SeedBankaHareketleriAsync();
        await SeedTahakkuklarVeOdemelerAsync(sozlesmeler);

        // --- 9. Kiracı Rol ve Kullanıcı Seed İşlemleri ---
        var seededKiraciler = await ctx.Tenants.ToListAsync();
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
            var mehmetUser = await userManager.FindByEmailAsync("mehmet.yildiz@yz.com");
            if (mehmetUser != null)
            {
                // Ofis 101'i bulup kapsam ekleyelim
                var ofis101 = await ctx.Units.FirstOrDefaultAsync(b => b.UnitNo == "101");
                if (ofis101 != null)
                {
                    var hasScope = await ctx.KullaniciYetkiKapsamlari.AnyAsync(s => s.UserId == mehmetUser.Id);
                    if (!hasScope)
                    {
                        ctx.KullaniciYetkiKapsamlari.Add(new UserPermissionScope
                        {
                            UserId = mehmetUser.Id,
                            ScopeType = ScopeType.Unit,
                            ScopeId = ofis101.Id
                        });
                        await ctx.SaveChangesAsync();
                    }
                }
            }
        }
    }

    public async Task SeedTasinmazFiyatlarAsync()
    {
        var teknokent = await ctx.Properties.FirstOrDefaultAsync(t => t.Name == "Teknokent A Blok");
        if (teknokent != null && !await ctx.TasinmazTarifeler.AnyAsync(f => f.PropertyId == teknokent.Id))
        {
            var katAkademik = await ctx.Kategoriler.FirstAsync(k => k.Type == CategoryType.Tenant && k.Code == "AKADEMIK");
            var katAkadOlmayan = await ctx.Kategoriler.FirstAsync(k => k.Type == CategoryType.Tenant && k.Code == "AKADEMIK_OLMAYAN");

            var btKira = await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Kira);
            var btOrtak = await ctx.ChargeTypes.FirstAsync(b => b.Code == "ORTAK");
            var btPortal = await ctx.ChargeTypes.FirstAsync(b => b.Code == "PORTAL");
            var btDepozito = await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Depozito);

            ctx.TasinmazTarifeler.AddRange(
                // Akademik için (m2 bazlı kira ve ortak gider) - Taşınmaz Tarifesi
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btKira.Id, UnitValue = 320, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btOrtak.Id, UnitValue = 95, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btPortal.Id, UnitValue = 480, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btDepozito.Id, UnitValue = 9000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },

                // Akademik Olmayan için - Taşınmaz Tarifesi
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btKira.Id, UnitValue = 430, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btOrtak.Id, UnitValue = 140, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btPortal.Id, UnitValue = 700, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btDepozito.Id, UnitValue = 22000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 }
            );

            await ctx.SaveChangesAsync();
        }
    }

    public async Task SeedTahakkuklarAsync()
    {
        // Geriye dönük uyumluluk için (UretSozlesmeIcinAsync SeedDomainDataAsync içinde çağrılıyor)
        if (await ctx.Charges.AnyAsync()) return;
        var aktifSozlesmeler = await ctx.Leases.Where(s => s.Status == LeaseStatus.Active).ToListAsync();
        foreach (var s in aktifSozlesmeler)
            await tahakkukUretim.GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));
    }

    private async Task SeedTahakkuklarVeOdemelerAsync(List<Lease> sozlesmeler)
    {
        try
        {
            var adminUser = await ctx.Users.FirstOrDefaultAsync();
            var adminId = adminUser?.Id ?? "admin-id-missing";
            var seedStoreAccountId = await ctx.Stores
                .Where(s => s.Code == "SEED")
                .SelectMany(s => s.Accounts)
                .Select(a => a.Id)
                .FirstAsync();

            // 1. Manuel Borçlar ve İptaller
            var manuelBorcTipi = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == BorcTipiConsts.Diger);
            if (manuelBorcTipi != null)
            {
                var targetSozlesme = sozlesmeler.First();
                ctx.Charges.Add(new Charge
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
                ctx.Charges.Add(new Charge
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

            await ctx.SaveChangesAsync();

            // 2. Geçmiş Yıl Ödemeleri (%90 ve %95 oranları)
            var currentYear = DateTime.Today.Year;
            await SeedGecmisYilOdemeleriAsync(currentYear - 1, 0.90, adminId, seedStoreAccountId);
            await SeedGecmisYilOdemeleriAsync(currentYear, 0.60, adminId, seedStoreAccountId);
            await SeedGecmisYilOdemeleriAsync(currentYear + 1, 0.05, adminId, seedStoreAccountId);

            // 4. Kısmi Ödemeler
            await SeedKismiOdemelerAsync(adminId, seedStoreAccountId);

            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in SeedTahakkuklarVeOdemelerAsync: {ex.Message}");
            throw;
        }
    }

    private async Task SeedGecmisYilOdemeleriAsync(int yil, double oran, string adminId, int storeAccountId)
    {
        var query = ctx.Charges
            .Include(t => t.LineItems)
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

            var paymentDate = gecikmis ? t.DueDate.AddDays(Random.Shared.Next(15, 45)) : t.DueDate.AddDays(Random.Shared.Next(-5, 5));
            var paymentChannel = (PaymentChannel)Random.Shared.Next(1, 5);
            var description = (kismiMi ? "Kısmi " : "") + (gecikmis ? "gecikmeli seed ödemesi" : "zamanında seed ödemesi");

            DagitKalemBazliOdeme(t, odemeTutari, storeAccountId, adminId, paymentDate, paymentChannel, description);

            t.PaidAmount = odemeTutari;
            t.Status = kismiMi ? ChargeStatus.PartiallyPaid : ChargeStatus.Paid;
        }
    }

    private async Task SeedKismiOdemelerAsync(string adminId, int storeAccountId)
    {
        var bekleyenler = await ctx.Charges
            .Include(t => t.LineItems)
            .Where(t => t.Status == ChargeStatus.Pending)
            .Take(3)
            .ToListAsync();

        foreach (var t in bekleyenler)
        {
            var kismiTutar = Math.Round(t.TotalAmount / 2, 2);

            DagitKalemBazliOdeme(
                t, kismiTutar, storeAccountId, adminId,
                DateTime.Today.AddDays(-2), PaymentChannel.Eft, "Seed kısmi ödeme");

            t.PaidAmount = kismiTutar;
            t.Status = ChargeStatus.PartiallyPaid;
        }
    }

    /// <summary>
    /// Bir tahakkuk için toplam ödeme tutarını, kalemlerin ToplamTutar oranına göre her
    /// kaleme bir PaymentAllocation olarak dağıtır. Kalem bazlı ödeme çekirdeğinde her
    /// PaymentAllocation tek bir ChargeLineItem'a bağlı olmak zorunda olduğu için seed
    /// verisi de bu invariant'a uymalıdır; tahmine dayalı tek-kalem ataması yapılmaz.
    /// </summary>
    private void DagitKalemBazliOdeme(
        Charge charge,
        decimal totalToPay,
        int storeAccountId,
        string adminId,
        DateTime paymentDate,
        PaymentChannel paymentChannel,
        string description)
    {
        var lineItems = charge.LineItems.ToList();
        if (lineItems.Count == 0 || totalToPay <= 0) return;

        decimal remaining = totalToPay;
        for (int i = 0; i < lineItems.Count; i++)
        {
            var lineItem = lineItems[i];
            var isLast = i == lineItems.Count - 1;
            var share = isLast
                ? remaining
                : Math.Round(totalToPay * (lineItem.TotalAmount / charge.TotalAmount), 2);
            remaining -= share;
            if (share <= 0) continue;

            ctx.PaymentAllocations.Add(new PaymentAllocation
            {
                LeaseId = charge.LeaseId,
                ChargeId = charge.Id,
                ChargeLineItemId = lineItem.Id,
                StoreAccountId = storeAccountId,
                PaymentDate = paymentDate,
                Amount = share,
                PaymentChannel = paymentChannel,
                Status = PaymentStatus.Approved,
                Description = description,
                CreatedByUserId = adminId
            });
            lineItem.PaidAmount = share;
        }
    }

    private async Task SeedRezervasyonlarAsync()
    {
        var salon = await ctx.Units.Include(b => b.Property).FirstOrDefaultAsync(b => b.Name == "Toplantı Salonu Z01");
        var salonB = await ctx.Units.Include(b => b.Property).FirstOrDefaultAsync(b => b.Name == "Toplantı Odası Z02");
        var tenant = await ctx.Tenants.FirstOrDefaultAsync();
        var lease = await ctx.Leases.FirstOrDefaultAsync(s => s.TenantId == tenant.Id);

        if (salon == null || tenant == null) return;

        var btRezervasyon = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "TOPLANTI");

        // 1. Geçmiş ve tahakkuku bulunan onaylı rezervasyon
        var rezervasyon1 = new Reservation
        {
            UnitId = salon.Id,
            TenantId = tenant.Id,
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
            Status = ReservationStatus.Confirmed,
        };
        ctx.Reservations.Add(rezervasyon1);
        await ctx.SaveChangesAsync();

        if (btRezervasyon != null)
        {
            var charge = new Charge
            {
                TenantId = tenant.Id,
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
            ctx.Charges.Add(charge);
            await ctx.SaveChangesAsync();
        }

        // 2. Gelecek Reservation (Planlandı)
        ctx.Reservations.Add(new Reservation
        {
            UnitId = salon.Id,
            TenantId = tenant.Id,
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
            Status = ReservationStatus.Confirmed,
        });

        // 3. Z02 Rezervasyonu (Gelecek - Planlandı)
        var kiraciVeri = await ctx.Tenants.FirstOrDefaultAsync(k => k.Email == "iletisim@biotech.com");
        if (salonB != null && kiraciVeri != null)
        {
            ctx.Reservations.Add(new Reservation
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
                Status = ReservationStatus.Confirmed
            });
        }

        await ctx.SaveChangesAsync();
    }

    private async Task SeedBankaHareketleriAsync()
    {
        // Eşleşmiş Hareket
        ctx.BankTransactions.Add(new BankTransaction
        {
            TransactionDate = DateTime.Today.AddDays(-1),
            TransactionAmount = 1500,
            Description = "KİRA ÖDEMESİ - TEKNOKENT",
            SenderInfo = "Yapay Zeka Çözümleri A.Ş.",
            BankCode = "TR01",
            MatchStatus = BankMatchStatus.Matched,
        });

        // Eşleşmemiş (Açıkta) Hareket
        ctx.BankTransactions.Add(new BankTransaction
        {
            TransactionDate = DateTime.Today.AddDays(-2),
            TransactionAmount = 5000,
            Description = "HAVALE - BİLİNMEYEN",
            SenderIban = "TR123456789...",
            BankCode = "TR01",
            MatchStatus = BankMatchStatus.Unmatched,
        });

        await ctx.SaveChangesAsync();
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

    private static Lease MakeSozlesme(Unit unit, Tenant tenant,
        DateTime baslangic, DateTime bitis,
        bool kdv, decimal kdvOrani = 20, string? notlar = null,
        DueDateRuleType vadeKuraliTipi = DueDateRuleType.FixedDayOfMonth,
        int vadeGunu = 1) => new()
        {
            Unit = unit,
            UnitId = unit.Id,
            Tenant = tenant,
            TenantId = tenant.Id,
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
        ctx.KullaniciYetkiKapsamlari.RemoveRange(ctx.KullaniciYetkiKapsamlari.IgnoreQueryFilters());
        ctx.Davetiyeler.RemoveRange(ctx.Davetiyeler.IgnoreQueryFilters());
        ctx.SifreSifirlamaTalepleri.RemoveRange(ctx.SifreSifirlamaTalepleri.IgnoreQueryFilters());

        // Temizlik sırası önemlidir (FK kısıtlamaları nedeniyle)
        ctx.PaymentMatches.RemoveRange(ctx.PaymentMatches.IgnoreQueryFilters());
        ctx.PaymentAllocations.RemoveRange(ctx.PaymentAllocations.IgnoreQueryFilters());
        ctx.BankTransactions.RemoveRange(ctx.BankTransactions.IgnoreQueryFilters());

        ctx.Reservations.RemoveRange(ctx.Reservations.IgnoreQueryFilters());
        ctx.RezervasyonTarifeler.RemoveRange(ctx.RezervasyonTarifeler.IgnoreQueryFilters());

        ctx.ChargeLineItems.RemoveRange(ctx.ChargeLineItems.IgnoreQueryFilters());
        ctx.Charges.RemoveRange(ctx.Charges.IgnoreQueryFilters());

        ctx.SozlesmeTarifeler.RemoveRange(ctx.SozlesmeTarifeler.IgnoreQueryFilters());
        ctx.SozlesmeIncelemeGecmisleri.RemoveRange(ctx.SozlesmeIncelemeGecmisleri.IgnoreQueryFilters());
        ctx.SozlesmeIslemGecmisleri.RemoveRange(ctx.SozlesmeIslemGecmisleri.IgnoreQueryFilters());
        ctx.Leases.RemoveRange(ctx.Leases.IgnoreQueryFilters());

        ctx.PaymentStoreRoutings.RemoveRange(ctx.PaymentStoreRoutings.IgnoreQueryFilters());
        ctx.StoreAccounts.RemoveRange(ctx.StoreAccounts.IgnoreQueryFilters());
        ctx.Stores.RemoveRange(ctx.Stores.IgnoreQueryFilters());

        ctx.UnitRates.RemoveRange(ctx.UnitRates.IgnoreQueryFilters());
        ctx.Units.RemoveRange(ctx.Units.IgnoreQueryFilters());

        ctx.TasinmazTarifeler.RemoveRange(ctx.TasinmazTarifeler.IgnoreQueryFilters());
        ctx.Properties.RemoveRange(ctx.Properties.IgnoreQueryFilters());

        ctx.GenelTarifeler.RemoveRange(ctx.GenelTarifeler.IgnoreQueryFilters());

        // Belgeleri sil (DocumentTypes temizlenmeden önce silinmelidir)
        ctx.Belgeler.RemoveRange(ctx.Belgeler.IgnoreQueryFilters());
        await ctx.SaveChangesAsync();

        // Kiracı kullanıcılarını ve rollerini temizle (Referans veren tüm charge ödemeleri silindikten sonra güvenle silinebilir)
        var kiraciUsers = await userManager.Users.Where(u => u.UserType == UserType.Tenant).ToListAsync();
        foreach (var ku in kiraciUsers)
        {
            await userRoleService.RemoveAllRolesAsync(ku.Id);
            await userManager.DeleteAsync(ku);
        }

        var kiraciRoller = await ctx.Roller.IgnoreQueryFilters().Where(r => r.Scope == RoleScope.Tenant && r.TenantId != null).ToListAsync();
        ctx.Roller.RemoveRange(kiraciRoller);
        await ctx.SaveChangesAsync();

        // Artık üzerinde hiçbir referans kalmayan Tenants tablosunu silebiliriz
        ctx.Tenants.RemoveRange(ctx.Tenants.IgnoreQueryFilters());
        await ctx.SaveChangesAsync();

        // Sistem-dışı borç tipleri silinmeden önce onlara referans veren ödeme yönlendirmeleri
        // kaldırılmalıdır (FK Restrict); Magazalar/MagazaHesapBilgileri kasıtlı olarak
        // silinmez — SeedOdemeMagazalariAsync/SeedOrnekMagazaYonlendirmeleriAsync eksik
        // yönlendirmeleri bir sonraki seed çalışmasında yeniden oluşturur.
        ctx.PaymentStoreRoutings.RemoveRange(ctx.PaymentStoreRoutings.IgnoreQueryFilters());
        await ctx.SaveChangesAsync();

        // Sistem Tanımları (Baştan seed edileceği için temizlenebilir)
        ctx.Kategoriler.RemoveRange(ctx.Kategoriler.IgnoreQueryFilters());
        ctx.UnitTypes.RemoveRange(ctx.UnitTypes.IgnoreQueryFilters());
        ctx.ChargeTypes.RemoveRange(ctx.ChargeTypes.IgnoreQueryFilters().Where(b => !b.IsSystem));
        ctx.DocumentTypes.RemoveRange(ctx.DocumentTypes.IgnoreQueryFilters().Where(b => !b.IsSystem));

        await ctx.SaveChangesAsync();
    }

    private async Task EnsureKiraciUserAsync(string email, string password, string adSoyad, int tenantId)
    {
        var user = await userManager.FindByEmailAsync(email);
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
                TenantId = tenantId
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Kiracı kullanıcısı '{email}' oluşturulamadı: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            user.UserType = UserType.Tenant;
            user.TenantId = tenantId;
            user.IsActive = true;
            await userManager.UpdateAsync(user);
        }

        var firmaRol = await ctx.Roller.FirstOrDefaultAsync(r => r.TenantId == null && r.Name == RoleNames.KiraciYoneticisi);
        if (firmaRol != null)
        {
            var hasRole = await ctx.UserRoller.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == firmaRol.Id);
            if (!hasRole)
            {
                await userRoleService.AddRoleByRolIdAsync(user.Id, firmaRol.Id, "system");
            }
        }
    }
}
