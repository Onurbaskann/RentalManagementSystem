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
using KiraTakip.Domain.Leases;
using KiraTakip.Domain.Reservations;
using System.IO;
using KiraTakip.Domain.Payments;
using KiraTakip.Domain.Charges;
using KiraTakip.Models.Dtos.Role;

namespace KiraTakip.Infrastructure.Seeding;

public class SeedDataService(
    ApplicationDbContext ctx,
    IChargeGenerationService tahakkukUretim,
    IRateResolverService rateResolver,
    UserManager<ApplicationUser> userManager,
    IUserRoleService userRoleService,
    IStoreAccountCredentialProtector credentialProtector,
    IRoleService roleService)
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
        if (!existingCodes.Contains("REKLAM_TABELA")) toAdd.Add(new ChargeType { Name = "Tabela ve Reklam Alanı Tahsis Bedeli", Code = "REKLAM_TABELA", IsActive = true, SortOrder = 6, Behavior = ChargeTypeBehavior.MonthlyFixed, IsSystem = false });

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
    /// <summary>
    /// Faz 20 kalem bazlı ödeme çekirdeği için kurumsal mağazaları ve her borç tipi
    /// için genel ödeme yönlendirmesini oluşturur. Idempotent — mağazalar zaten varsa
    /// yeniden oluşturulmaz, eksik kalan genel yönlendirmeler her çağrıda tamamlanır.
    /// </summary>
    public async Task SeedOdemeMagazalariAsync()
    {
        // 1. Ana Kira ve Finans Mağazası (STR-KIRA) - Hesap Geçmişi Örneği ile
        var storeKira = await GetOrCreateStoreWithAccountsAsync(
            "STR-KIRA",
            "Ana Kira ve Finans Mağazası",
            "Kira, depozito ve ana kurumsal gelirlerin tahsil edildiği birincil sanal POS mağazası.",
            isStoreActive: true,
            primaryMerchantId: "PRT-KIRA-01",
            primaryMerchantUser: "teknokent-kira-user",
            primaryValidFrom: DateTime.UtcNow.AddMonths(-3),
            hasPreviousInactiveAccount: true,
            previousMerchantId: "PRT-KIRA-ESKI",
            previousMerchantUser: "kira-old-user",
            previousValidFrom: DateTime.UtcNow.AddYears(-2),
            previousValidUntil: DateTime.UtcNow.AddMonths(-3));

        // 2. Tesis ve Ortak Alan İşletme Mağazası (STR-TESIS)
        var storeTesis = await GetOrCreateStoreWithAccountsAsync(
            "STR-TESIS",
            "Tesis ve Ortak Alan İşletme Mağazası",
            "Ortak alan giderleri, aidat, iklimlendirme ve bakım bedelleri için işletme mağazası.",
            isStoreActive: true,
            primaryMerchantId: "PRT-TESIS-01",
            primaryMerchantUser: "teknokent-tesis-user",
            primaryValidFrom: DateTime.UtcNow.AddYears(-1));

        // 3. Ticari Alanlar ve Kafeterya Mağazası (STR-TICARI)
        var storeTicari = await GetOrCreateStoreWithAccountsAsync(
            "STR-TICARI",
            "Ticari Alanlar ve Kafeterya Mağazası",
            "Kafeterya, ticari alan ve kiosk gelirlerinin yönetildiği ticari tahsilat hesabı.",
            isStoreActive: true,
            primaryMerchantId: "PRT-TICARI-01",
            primaryMerchantUser: "teknokent-ticari-user",
            primaryValidFrom: DateTime.UtcNow.AddYears(-1));

        // 4. Etkinlik ve Rezervasyon Mağazası (STR-REZERVASYON)
        var storeRezervasyon = await GetOrCreateStoreWithAccountsAsync(
            "STR-REZERVASYON",
            "Etkinlik ve Rezervasyon Mağazası",
            "Toplantı salonları ve konferans/etkinlik alanları saatlik rezervasyon tahsilat hesabı.",
            isStoreActive: true,
            primaryMerchantId: "PRT-REZ-01",
            primaryMerchantUser: "teknokent-rez-user",
            primaryValidFrom: DateTime.UtcNow.AddMonths(-6));

        // 5. Ofis 101 Özel Tahsilat Mağazası (STR-UNIT101)
        var storeUnit101 = await GetOrCreateStoreWithAccountsAsync(
            "STR-UNIT101",
            "Ofis 101 Özel Tahsilat Mağazası",
            "Ofis 101 birim özel kira ve operasyon tahsilat hesabı.",
            isStoreActive: true,
            primaryMerchantId: "PRT-U101-01",
            primaryMerchantUser: "u101-user",
            primaryValidFrom: DateTime.UtcNow.AddMonths(-8));

        // 6. Eski Kampüs Hizmetleri Mağazası (STR-ESKI - Pasif Mağaza Örneği)
        var storeEski = await GetOrCreateStoreWithAccountsAsync(
            "STR-ESKI",
            "Eski Kampüs Hizmetleri Mağazası (Kapatıldı)",
            "Geçmiş dönemde kullanılan, sözleşmesi feshedilmiş pasif mağaza örneği.",
            isStoreActive: false,
            primaryMerchantId: "PRT-ESKI-00",
            primaryMerchantUser: "eski-user",
            primaryValidFrom: DateTime.UtcNow.AddYears(-3),
            primaryValidUntil: DateTime.UtcNow.AddMonths(-6),
            isPrimaryAccountActive: false);

        // Geriye dönük uyumluluk için varsayılan SEED mağazası
        var storeSeed = await GetOrCreateStoreWithAccountsAsync(
            "SEED",
            "Seed Varsayılan Mağazası",
            "Geliştirme ve test ortamı için varsayılan sanal POS mağazası.",
            isStoreActive: true,
            primaryMerchantId: "SEED-MERCHANT",
            primaryMerchantUser: "seed-user",
            primaryValidFrom: DateTime.UtcNow.AddYears(-1));

        // =========================================================================
        // Seviye 1: Genel / Varsayılan Yönlendirmeler (General - Tüm Taşınmazlar)
        // =========================================================================
        var chargeTypes = await ctx.ChargeTypes.ToListAsync();

        async Task EnsureGeneralRoutingAsync(string chargeTypeCode, int storeId)
        {
            var ct = chargeTypes.FirstOrDefault(c => c.Code == chargeTypeCode);
            if (ct == null) return;

            var exists = await ctx.PaymentStoreRoutings.AnyAsync(r =>
                r.ChargeTypeId == ct.Id && r.PropertyId == null && r.UnitId == null && r.IsActive)
                || ctx.PaymentStoreRoutings.Local.Any(r =>
                r.ChargeTypeId == ct.Id && r.PropertyId == null && r.UnitId == null && r.IsActive);
            if (!exists)
            {
                ctx.PaymentStoreRoutings.Add(new PaymentStoreRouting
                {
                    ChargeTypeId = ct.Id,
                    PropertyId = null,
                    UnitId = null,
                    StoreId = storeId,
                    IsActive = true
                });
            }
        }

        await EnsureGeneralRoutingAsync(BorcTipiConsts.Kira, storeKira.Id);
        await EnsureGeneralRoutingAsync(BorcTipiConsts.Depozito, storeKira.Id);
        await EnsureGeneralRoutingAsync("PORTAL", storeKira.Id);
        await EnsureGeneralRoutingAsync("ORTAK", storeTesis.Id);
        await EnsureGeneralRoutingAsync(BorcTipiConsts.Diger, storeTesis.Id);
        await EnsureGeneralRoutingAsync("TOPLANTI", storeRezervasyon.Id);
        await EnsureGeneralRoutingAsync("ETKINLIK", storeRezervasyon.Id);
        await EnsureGeneralRoutingAsync("REKLAM_TABELA", storeTicari.Id);
        await ctx.SaveChangesAsync();

        // Geriye kalan veya dinamik eklenmiş diğer borç tipleri varsa fallback olarak storeKira
        var routedChargeTypeIds = (await ctx.PaymentStoreRoutings
            .Where(r => r.PropertyId == null && r.UnitId == null && r.IsActive)
            .Select(r => r.ChargeTypeId)
            .ToListAsync())
            .Union(ctx.PaymentStoreRoutings.Local.Where(r => r.PropertyId == null && r.UnitId == null && r.IsActive).Select(r => r.ChargeTypeId))
            .ToHashSet();

        var missingChargeTypeIds = chargeTypes.Select(ct => ct.Id).Where(id => !routedChargeTypeIds.Contains(id)).ToList();
        foreach (var missingId in missingChargeTypeIds)
        {
            ctx.PaymentStoreRoutings.Add(new PaymentStoreRouting
            {
                ChargeTypeId = missingId,
                PropertyId = null,
                UnitId = null,
                StoreId = storeKira.Id,
                IsActive = true
            });
        }
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Ödeme yönlendirmesinin Birim → Taşınmaz → Genel öncelik sırasını ve geçmiş yönlendirmeleri
    /// gerçek seed verisiyle (Teknokent A Blok / B Blok / Otopark / Ofisler) gösterir.
    /// Idempotent — `SeedDomainDataAsync` çalışıp taşınmazlar oluştuktan sonra çağrılmalıdır.
    /// </summary>
    public async Task SeedOrnekMagazaYonlendirmeleriAsync()
    {
        var teknokentA = await ctx.Properties.FirstOrDefaultAsync(p => p.Name == "Teknokent A Blok");
        var teknokentB = await ctx.Properties.FirstOrDefaultAsync(p => p.Name == "Teknokent B Blok");
        var otopark = await ctx.Properties.FirstOrDefaultAsync(p => p.Name == "Teknokent Açık Otoparkı");

        if (teknokentA == null) return;

        var ofis101 = await ctx.Units.FirstOrDefaultAsync(u => u.PropertyId == teknokentA.Id && u.UnitNo == "101");
        var ofis102 = await ctx.Units.FirstOrDefaultAsync(u => u.PropertyId == teknokentA.Id && u.UnitNo == "102");
        var birimKafe = await ctx.Units.FirstOrDefaultAsync(u => u.PropertyId == teknokentA.Id && u.UnitNo == "K01");
        var birimBEA01 = teknokentB == null
            ? null
            : await ctx.Units.FirstOrDefaultAsync(u => u.PropertyId == teknokentB.Id && u.UnitNo == "B-EA01");

        var kiraTipi = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == BorcTipiConsts.Kira);
        var ortakTipi = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "ORTAK");
        var etkinlikTipi = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "ETKINLIK");
        var reklamTipi = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "REKLAM_TABELA");

        if (kiraTipi == null || ortakTipi == null) return;

        var storeKira = await ctx.Stores.FirstAsync(s => s.Code == "STR-KIRA");
        var storeTesis = await ctx.Stores.FirstAsync(s => s.Code == "STR-TESIS");
        var storeTicari = await ctx.Stores.FirstAsync(s => s.Code == "STR-TICARI");
        var storeRezervasyon = await ctx.Stores.FirstAsync(s => s.Code == "STR-REZERVASYON");
        var storeUnit101 = await ctx.Stores.FirstAsync(s => s.Code == "STR-UNIT101");

        async Task EnsurePropertyOverrideAsync(int chargeTypeId, int propertyId, int storeId, bool isActive = true)
        {
            var exists = await ctx.PaymentStoreRoutings.AnyAsync(r =>
                r.ChargeTypeId == chargeTypeId && r.PropertyId == propertyId && r.UnitId == null && r.IsActive == isActive);
            if (!exists)
            {
                ctx.PaymentStoreRoutings.Add(new PaymentStoreRouting
                {
                    ChargeTypeId = chargeTypeId,
                    PropertyId = propertyId,
                    UnitId = null,
                    StoreId = storeId,
                    IsActive = isActive
                });
            }
        }

        async Task EnsureUnitOverrideAsync(int chargeTypeId, int unitId, int storeId, bool isActive = true)
        {
            var exists = await ctx.PaymentStoreRoutings.AnyAsync(r =>
                r.ChargeTypeId == chargeTypeId && r.PropertyId == null && r.UnitId == unitId && r.IsActive == isActive);
            if (!exists)
            {
                ctx.PaymentStoreRoutings.Add(new PaymentStoreRouting
                {
                    ChargeTypeId = chargeTypeId,
                    PropertyId = null,
                    UnitId = unitId,
                    StoreId = storeId,
                    IsActive = isActive
                });
            }
        }

        // =========================================================================
        // Seviye 2: Taşınmaz Seviyesi Özel Yönlendirmeleri (Property Override)
        // =========================================================================
        // A. Teknokent A Blok için Ortak Gider -> STR-TESIS
        await EnsurePropertyOverrideAsync(ortakTipi.Id, teknokentA.Id, storeTesis.Id);

        // B. Teknokent B Blok için Etkinlik Bedeli -> STR-REZERVASYON
        if (teknokentB != null && etkinlikTipi != null)
        {
            await EnsurePropertyOverrideAsync(etkinlikTipi.Id, teknokentB.Id, storeRezervasyon.Id);
        }

        // C. Teknokent Açık Otoparkı için Kira Bedeli -> STR-TESIS
        if (otopark != null)
        {
            await EnsurePropertyOverrideAsync(kiraTipi.Id, otopark.Id, storeTesis.Id);
        }


        // D. Teknokent A Blok iÃ§in Tabela ve Reklam Bedeli -> STR-KIRA
        if (reklamTipi != null && teknokentA != null)
        {
            await EnsurePropertyOverrideAsync(reklamTipi.Id, teknokentA.Id, storeKira.Id);
        }

        // =========================================================================
        // Seviye 3: Birim Seviyesi Özel Yönlendirmeleri (Unit Override - En Yüksek Öncelik)
        // =========================================================================
        // A. Ofis 101 için Kira -> STR-UNIT101
        if (ofis101 != null)
        {
            await EnsureUnitOverrideAsync(kiraTipi.Id, ofis101.Id, storeUnit101.Id);
        }

        // B. Tekno Kafe (K01) için Kira -> STR-TICARI
        if (birimKafe != null)
        {
            await EnsureUnitOverrideAsync(kiraTipi.Id, birimKafe.Id, storeTicari.Id);
            // C. Tekno Kafe (K01) için Ortak Gider -> STR-TICARI
            await EnsureUnitOverrideAsync(ortakTipi.Id, birimKafe.Id, storeTicari.Id);
        }

        // D. B Blok Konferans Salonu (B-EA01) için Etkinlik -> STR-REZERVASYON
        if (birimBEA01 != null && etkinlikTipi != null)
        {
            await EnsureUnitOverrideAsync(etkinlikTipi.Id, birimBEA01.Id, storeRezervasyon.Id);
        }


        // E. Tekno Kafe (K01) iÃ§in Tabela ve Reklam Bedeli -> STR-TICARI
        if (reklamTipi != null && birimKafe != null)
        {
            await EnsureUnitOverrideAsync(reklamTipi.Id, birimKafe.Id, storeTicari.Id);
        }

        // =========================================================================
        // Seviye 4: Pasif / Geçmiş Yönlendirmeler (History / Inactive Override)
        // =========================================================================
        // Örnek 1: Ofis 102 için eskiden STR-UNIT101'e yönlendirilmiş ancak sonradan pasife alınmış
        if (ofis102 != null)
        {
            await EnsureUnitOverrideAsync(kiraTipi.Id, ofis102.Id, storeUnit101.Id, isActive: false);
        }

        // Örnek 2: Teknokent Açık Otoparkı için eski Ortak Gider yönlendirmesi pasife alınmış
        if (otopark != null)
        {
            await EnsurePropertyOverrideAsync(ortakTipi.Id, otopark.Id, storeKira.Id, isActive: false);
        }

        await ctx.SaveChangesAsync();
    }

    private async Task<Store> GetOrCreateStoreWithAccountsAsync(
        string code,
        string name,
        string description,
        bool isStoreActive,
        string primaryMerchantId,
        string primaryMerchantUser,
        DateTime primaryValidFrom,
        DateTime? primaryValidUntil = null,
        bool isPrimaryAccountActive = true,
        bool hasPreviousInactiveAccount = false,
        string? previousMerchantId = null,
        string? previousMerchantUser = null,
        DateTime? previousValidFrom = null,
        DateTime? previousValidUntil = null)
    {
        var existing = await ctx.Stores
            .Include(s => s.Accounts)
            .FirstOrDefaultAsync(s => s.Code == code);

        if (existing == null)
        {
            existing = new Store
            {
                Code = code,
                Name = name,
                Description = description,
                IsActive = isStoreActive
            };

            if (hasPreviousInactiveAccount && previousMerchantId != null)
            {
                existing.Accounts.Add(new StoreAccount
                {
                    ProviderCode = PaymentProviderCodes.Paratika,
                    Currency = CurrencyCodes.Try,
                    MerchantId = previousMerchantId,
                    MerchantUser = previousMerchantUser ?? previousMerchantId,
                    ProtectedMerchantPassword = credentialProtector.Protect("seed-dummy-password"),
                    ValidFrom = previousValidFrom ?? DateTime.UtcNow.AddYears(-2),
                    ValidUntil = previousValidUntil ?? DateTime.UtcNow.AddMonths(-3),
                    IsActive = false
                });
            }

            existing.Accounts.Add(new StoreAccount
            {
                ProviderCode = PaymentProviderCodes.Paratika,
                Currency = CurrencyCodes.Try,
                MerchantId = primaryMerchantId,
                MerchantUser = primaryMerchantUser,
                ProtectedMerchantPassword = credentialProtector.Protect("seed-dummy-password"),
                ValidFrom = primaryValidFrom,
                ValidUntil = primaryValidUntil,
                IsActive = isPrimaryAccountActive
            });

            ctx.Stores.Add(existing);
            await ctx.SaveChangesAsync();
        }

        return existing;
    }

    private async Task<Store> GetOrCreateSeedStoreAsync(
        string code, string name, string merchantId, string merchantUser)
        => await GetOrCreateStoreWithAccountsAsync(
            code, name, "Seed Mağaza", true, merchantId, merchantUser, DateTime.UtcNow);


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
        if (!existingCodes.Contains("OTOPARK")) toAdd.Add(new PropertyType { Name = "Otopark", Code = "OTOPARK", IsActive = true, SortOrder = 2, SupportsSingleUnit = true, SupportsMultipleUnits = false });
        if (!existingCodes.Contains("DEPO")) toAdd.Add(new PropertyType { Name = "Depo", Code = "DEPO", IsActive = true, SortOrder = 3, SupportsSingleUnit = true, SupportsMultipleUnits = true });

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
        if (!existingCodes.Contains("CAFE")) toAdd.Add(new UnitType { Name = "Kafe", Code = "CAFE", IsActive = true, Usage = UnitTypeUsage.Rentable, SortOrder = 2 });
        if (!existingCodes.Contains("OTOPARK")) toAdd.Add(new UnitType { Name = "Otopark Alanı", Code = "OTOPARK", IsActive = true, Usage = UnitTypeUsage.Rentable, SortOrder = 3 });
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
                    CalculationMethod = (bt.Code == BorcTipiConsts.Kira || bt.Code == "ORTAK" || bt.Code == BorcTipiConsts.Depozito) ? CalculationMethod.M2 : CalculationMethod.Fixed,
                    UnitValue = bt.Code switch
                    {
                        BorcTipiConsts.Kira => kat.Code == "AKADEMIK" ? 300m : 450m,
                        "ORTAK" => kat.Code == "AKADEMIK" ? 100m : 150m,
                        "PORTAL" => kat.Code == "AKADEMIK" ? 300m : 500m,
                        BorcTipiConsts.Depozito => kat.Code == "AKADEMIK" ? 900m : 1350m, // 3 Kira Bedeli (3x300, 3x450 TL/m2)
                        "REKLAM_TABELA" => kat.Code == "AKADEMIK" ? 500m : 750m,
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

        var egeLojistik = Tenant("KRC-000004", katMap["AKADEMIK_OLMAYAN"], sekMap["LOJISTIK"], "Ege Lojistik ve Dağıtım A.Ş.",
            vergiNo: "2345678901", ticaretSicilNo: "İZM-888", telefon: "0232 777 8899", email: "info@egelojistik.com", adres: "Teknokent");
        var agroGida = Tenant("KRC-000005", katMap["AKADEMIK"], sekMap["TARIM"], "AgroGıda İnovasyon Arge A.Ş.",
            vergiNo: "3456789012", ticaretSicilNo: "İZM-999", telefon: "0232 888 9900", email: "iletisim@agrogida.com", adres: "Teknokent");

        ctx.Tenants.AddRange(yzCozum, megaFinans, biotech, egeLojistik, agroGida);

        // --- Taşınmaz (Teknokent A Blok) ---
        var ofisTuruId = birimTuruMap["OFIS"];
        var etkinlikTuruId = birimTuruMap["ETKINLIK"];
        var cafeTuruId = birimTuruMap["CAFE"];
        var otoparkBirimTuruId = birimTuruMap["OTOPARK"];
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

        // Zemin Kat Kafe Ekleme
        teknokent.Units.Add(new Unit
        {
            UnitNo = "K01",
            FloorNo = 0,
            Name = "Tekno Kafe",
            Area = 120,
            UnitTypeId = cafeTuruId,
            Description = "Zemin kat ana kafeterya ve dinlenme alanı."
        });

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

        // 2 Rezerve Edilebilir Etkinlik Alanı Ekleme
        var etkinlikA01 = new Unit
        {
            UnitNo = "EA01",
            FloorNo = 0,
            Name = "A Blok Konferans Salonu",
            Area = 200,
            UnitTypeId = etkinlikTuruId,
            Description = "120 kişilik çok amaçlı konferans ve etkinlik salonu."
        };
        var etkinlikA02 = new Unit
        {
            UnitNo = "EA02",
            FloorNo = 0,
            Name = "A Blok Fuaye ve Sergi Alanı",
            Area = 150,
            UnitTypeId = etkinlikTuruId,
            Description = "Lansman, kokteyl ve sergi organizasyonları için fuaye alanı."
        };
        teknokent.Units.Add(etkinlikA01);
        teknokent.Units.Add(etkinlikA02);

        // 2. Taşınmaz: Teknokent B Blok (6 Kiralanabilir Ofis, 3 Rezervasyon Alanı)
        var teknokentB = new Property
        {
            Name = "Teknokent B Blok",
            PropertyTypeId = tipiMap.GetValueOrDefault("BINA"),
            UnitStructure = UnitStructure.MultipleUnits,
            City = "İzmir",
            District = "Bornova",
            Neighborhood = "Ege Üniversitesi",
            Address = "Ege Üniversitesi Teknokent Kampüsü B Blok",
            OpenArea = 400,
            ClosedArea = 5200,
            FloorCount = 5,
            Description = "Yazılım ve Ar-Ge firmaları için kiralanabilir modern teknopark binası"
        };

        // 6 Kiralanabilir Ofis Ekleme
        var bOfisler = new[]
        {
            (No: "201", Kat: 2, Alan: 75m),
            (No: "202", Kat: 2, Alan: 85m),
            (No: "203", Kat: 2, Alan: 95m),
            (No: "301", Kat: 3, Alan: 65m),
            (No: "302", Kat: 3, Alan: 110m),
            (No: "303", Kat: 3, Alan: 120m)
        };
        foreach (var ofis in bOfisler)
        {
            teknokentB.Units.Add(new Unit
            {
                UnitNo = ofis.No,
                FloorNo = ofis.Kat,
                Name = $"Ofis B-{ofis.No}",
                Area = ofis.Alan,
                UnitTypeId = ofisTuruId,
                Description = $"{ofis.Kat}. Kat güney cephe ofisi."
            });
        }

        // 3 Rezerve Edilebilir Alan Ekleme (2 Toplantı Salonu + 1 Etkinlik Alanı)
        teknokentB.Units.Add(new Unit
        {
            UnitNo = "B-Z01",
            FloorNo = 0,
            Name = "B Blok Toplantı Salonu 1",
            Area = 50,
            UnitTypeId = toplantiTuruId,
            Description = "12 kişilik toplantı salonu."
        });
        teknokentB.Units.Add(new Unit
        {
            UnitNo = "B-Z02",
            FloorNo = 0,
            Name = "B Blok Toplantı Salonu 2",
            Area = 70,
            UnitTypeId = toplantiTuruId,
            Description = "20 kişilik toplantı salonu."
        });
        teknokentB.Units.Add(new Unit
        {
            UnitNo = "B-EA01",
            FloorNo = 0,
            Name = "B Blok Konferans ve Etkinlik Salonu",
            Area = 220,
            UnitTypeId = etkinlikTuruId,
            Description = "150 kişilik seminer, lansman ve konferans salonu."
        });

        // 3. Taşınmaz: Teknokent Açık Otoparkı (SingleUnit Yapısı)
        var otopark = new Property
        {
            Name = "Teknokent Açık Otoparkı",
            PropertyTypeId = tipiMap.GetValueOrDefault("OTOPARK"),
            UnitStructure = UnitStructure.SingleUnit,
            City = "İzmir",
            District = "Bornova",
            Neighborhood = "Ege Üniversitesi",
            Address = "Ege Üniversitesi Teknokent Kampüsü Açık Otopark Alanı",
            OpenArea = 2500,
            ClosedArea = 0,
            FloorCount = 0,
            Description = "100 araç kapasiteli kampüs açık otopark alanı"
        };
        otopark.Units.Add(new Unit
        {
            Name = "Teknokent Açık Otoparkı",
            Area = 2500,
            UnitTypeId = otoparkBirimTuruId,
            Description = "Tekil taşınmaz kiralama birimi."
        });

        // 4. Taşınmaz: Teknokent C Blok (Yeni Ar-Ge Merkezi - Boş/Kiralanabilir Birimler)
        var teknokentC = new Property
        {
            Name = "Teknokent C Blok (Ar-Ge Merkezi)",
            PropertyTypeId = tipiMap.GetValueOrDefault("BINA"),
            UnitStructure = UnitStructure.MultipleUnits,
            City = "İzmir",
            District = "Bornova",
            Neighborhood = "Ege Üniversitesi",
            Address = "Ege Üniversitesi Teknokent Kampüsü C Blok",
            OpenArea = 600,
            ClosedArea = 6000,
            FloorCount = 6,
            Description = "Yeni Ar-Ge binası (kiralanmaya hazır boş ofis ve laboratuvar birimleri)."
        };

        // Kiralanabilir Boş Birimler (İş kuralı: Çoklu birim yapısında en az 1 birim bulunmalıdır)
        teknokentC.Units.Add(new Unit
        {
            UnitNo = "C-101",
            FloorNo = 1,
            Name = "Ofis C-101 (Ar-Ge Ofisi)",
            Area = 120,
            UnitTypeId = ofisTuruId,
            Description = "1. Kat geniş Ar-Ge ofis birimi."
        });
        teknokentC.Units.Add(new Unit
        {
            UnitNo = "C-102",
            FloorNo = 1,
            Name = "Ofis C-102 (Yazılım Laboratuvarı)",
            Area = 150,
            UnitTypeId = ofisTuruId,
            Description = "1. Kat altyapısı hazır laboratuvar birimi."
        });
        teknokentC.Units.Add(new Unit
        {
            UnitNo = "C-201",
            FloorNo = 2,
            Name = "Ofis C-201 (Açık İnovasyon Ofisi)",
            Area = 200,
            UnitTypeId = ofisTuruId,
            Description = "2. Kat açık ofis konseptli Ar-Ge birimi."
        });
        teknokentC.Units.Add(new Unit
        {
            UnitNo = "C-Z01",
            FloorNo = 0,
            Name = "C Blok Toplantı Salonu",
            Area = 60,
            UnitTypeId = toplantiTuruId,
            Description = "C Blok zemin kat toplantı salonu."
        });

        // 5. Taşınmaz: Teknokent D Blok (Kuluçka ve Girişimcilik Merkezi)
        var teknokentD = new Property
        {
            Name = "Teknokent D Blok (Kuluçka ve Girişimcilik Merkezi)",
            PropertyTypeId = tipiMap.GetValueOrDefault("BINA"),
            UnitStructure = UnitStructure.MultipleUnits,
            City = "İzmir",
            District = "Bornova",
            Neighborhood = "Ege Üniversitesi",
            Address = "Ege Üniversitesi Teknokent Kampüsü D Blok",
            OpenArea = 350,
            ClosedArea = 3200,
            FloorCount = 3,
            Description = "Erken aşama girişimler ve kuluçka ekipleri için tasarlanmış ortak çalışma ve inovasyon merkezi."
        };

        teknokentD.Units.Add(new Unit
        {
            UnitNo = "D-101",
            FloorNo = 1,
            Name = "Kuluçka Ofisi D-101",
            Area = 45,
            UnitTypeId = ofisTuruId,
            Description = "1. Kat kuluçka girişim ofisi."
        });
        teknokentD.Units.Add(new Unit
        {
            UnitNo = "D-102",
            FloorNo = 1,
            Name = "Kuluçka Ofisi D-102",
            Area = 55,
            UnitTypeId = ofisTuruId,
            Description = "1. Kat kuluçka girişim ofisi."
        });
        teknokentD.Units.Add(new Unit
        {
            UnitNo = "D-201",
            FloorNo = 2,
            Name = "Girişim Ofisi D-201",
            Area = 75,
            UnitTypeId = ofisTuruId,
            Description = "2. Kat ölçeklenme aşaması girişim ofisi."
        });
        teknokentD.Units.Add(new Unit
        {
            UnitNo = "D-Z01",
            FloorNo = 0,
            Name = "D Blok Seminer ve Toplantı Odası",
            Area = 50,
            UnitTypeId = toplantiTuruId,
            Description = "D Blok zemin kat kuluçka seminer ve toplantı odası."
        });

        ctx.Properties.AddRange(teknokent, teknokentB, otopark, teknokentC, teknokentD);
        await ctx.SaveChangesAsync();

        // --- Kiracı Kullanıcılarının ve Yetkilerinin Oluşturulması (Sözleşme iş akışında ActorUserId için) ---
        var (adminId, yzUserId, megaUserId, biotechUserId, egeUserId, agroUserId) =
            await SeedKiraciKullanicilariAsync([yzCozum, megaFinans, biotech, egeLojistik, agroGida]);

        // --- Tarifelerin Oluşturulması ---
        await SeedTasinmazFiyatlarAsync();

        var btKiraId = (await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Kira)).Id;
        var btDepozitoId = (await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Depozito)).Id;
        var btOrtakId = (await ctx.ChargeTypes.FirstAsync(b => b.Code == "ORTAK")).Id;

        var birim101 = teknokent.Units.First(b => b.UnitNo == "101");
        var birim102 = teknokent.Units.First(b => b.UnitNo == "102");
        var birim103 = teknokent.Units.First(b => b.UnitNo == "103");
        var birim104 = teknokent.Units.First(b => b.UnitNo == "104");
        var birim105 = teknokent.Units.First(b => b.UnitNo == "105");
        var birimKafe = teknokent.Units.First(b => b.UnitNo == "K01");
        var birimOtopark = otopark.Units.First();
        var birimB201 = teknokentB.Units.First(b => b.UnitNo == "201");
        var birimB202 = teknokentB.Units.First(b => b.UnitNo == "202");
        var birimB203 = teknokentB.Units.First(b => b.UnitNo == "203");
        var birimB301 = teknokentB.Units.First(b => b.UnitNo == "301");
        var birimB302 = teknokentB.Units.First(b => b.UnitNo == "302");
        var birimB303 = teknokentB.Units.First(b => b.UnitNo == "303");
        var birimToplantiZ01 = teknokent.Units.First(b => b.UnitNo == "Z01");
        var birimToplantiZ02 = teknokent.Units.First(b => b.UnitNo == "Z02");
        var birimEtkinlikEA01 = teknokent.Units.First(b => b.UnitNo == "EA01");
        var birimEtkinlikEA02 = teknokent.Units.First(b => b.UnitNo == "EA02");
        var birimToplantiBZ01 = teknokentB.Units.First(b => b.UnitNo == "B-Z01");
        var birimToplantiBZ02 = teknokentB.Units.First(b => b.UnitNo == "B-Z02");
        var birimEtkinlikBEA01 = teknokentB.Units.First(b => b.UnitNo == "B-EA01");

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

        // Diğer Birim Tarifeleri (Kafe, B-203, B-303)
        ctx.UnitRates.AddRange(
            new UnitRate { UnitId = birimKafe.Id, TenantCategoryId = katMap["AKADEMIK"], ChargeTypeId = btKiraId, CalculationMethod = CalculationMethod.M2, UnitValue = 650, KdvRate = 20 },
            new UnitRate { UnitId = birimKafe.Id, TenantCategoryId = katMap["AKADEMIK"], ChargeTypeId = btOrtakId, CalculationMethod = CalculationMethod.M2, UnitValue = 180, KdvRate = 20 },
            new UnitRate { UnitId = birimKafe.Id, TenantCategoryId = katMap["AKADEMIK"], ChargeTypeId = btDepozitoId, CalculationMethod = CalculationMethod.M2, UnitValue = 1950, KdvRate = 20 },
            new UnitRate { UnitId = birimKafe.Id, TenantCategoryId = katMap["AKADEMIK_OLMAYAN"], ChargeTypeId = btKiraId, CalculationMethod = CalculationMethod.M2, UnitValue = 650, KdvRate = 20 },
            new UnitRate { UnitId = birimKafe.Id, TenantCategoryId = katMap["AKADEMIK_OLMAYAN"], ChargeTypeId = btOrtakId, CalculationMethod = CalculationMethod.M2, UnitValue = 180, KdvRate = 20 },
            new UnitRate { UnitId = birimKafe.Id, TenantCategoryId = katMap["AKADEMIK_OLMAYAN"], ChargeTypeId = btDepozitoId, CalculationMethod = CalculationMethod.M2, UnitValue = 1950, KdvRate = 20 },
            // B-203 için sadece Kira tanımlandı, Ortak Gider tanımlanmadı (Taşınmaz 110 TL'den düşecek)
            new UnitRate { UnitId = birimB203.Id, TenantCategoryId = katMap["AKADEMIK"], ChargeTypeId = btKiraId, CalculationMethod = CalculationMethod.M2, UnitValue = 380, KdvRate = 20 },
            // B-303 için sadece Kira tanımlandı, Ortak Gider tanımlanmadı (Taşınmaz 160 TL'den düşecek)
            new UnitRate { UnitId = birimB303.Id, TenantCategoryId = katMap["AKADEMIK_OLMAYAN"], ChargeTypeId = btKiraId, CalculationMethod = CalculationMethod.M2, UnitValue = 550, KdvRate = 20 }
        );

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
            UnitId = birimToplantiZ01.Id,
            FreeDurationMinutes = 30,
            BillingPeriodMinutes = 60,
            PeriodRate = 600m,
            KdvRate = 20m,
            Description = "Toplantı Salonu Z01 için özel fiyatlandırma kuralı"
        });

        // 1. Ozel Etkinlik Alani Tarifesi: B-EA01
        ctx.RezervasyonTarifeler.Add(new ReservationRateOverride
        {
            Year = now.Year,
            UnitTypeId = etkinlikTuruId,
            UnitId = birimEtkinlikBEA01.Id,
            FreeDurationMinutes = 0,
            BillingPeriodMinutes = 60,
            PeriodRate = 2500m,
            KdvRate = 20m,
            Description = "B Blok Konferans ve Etkinlik Salonu ozel saatlik fiyatlandirma kuralı"
        });

        // 2. Tamamen Ucretsiz Rezervasyon Tarifeleri:
        // Ucretsiz Tarife 1: A Blok Toplanti Odasi Z02
        ctx.RezervasyonTarifeler.Add(new ReservationRateOverride
        {
            Year = now.Year,
            UnitTypeId = toplantiTuruId,
            UnitId = birimToplantiZ02.Id,
            FreeDurationMinutes = 480,
            BillingPeriodMinutes = 60,
            PeriodRate = 0m,
            KdvRate = 20m,
            Description = "Toplanti Odasi Z02 - Tamamen Ucretsiz Tahsis Kurali"
        });

        // Ucretsiz Tarife 2: B Blok Toplanti Salonu 1 (B-Z01)
        ctx.RezervasyonTarifeler.Add(new ReservationRateOverride
        {
            Year = now.Year,
            UnitTypeId = toplantiTuruId,
            UnitId = birimToplantiBZ01.Id,
            FreeDurationMinutes = 480,
            BillingPeriodMinutes = 60,
            PeriodRate = 0m,
            KdvRate = 20m,
            Description = "B Blok Toplanti Salonu 1 (B-Z01) - Tamamen Ucretsiz Tahsis Kurali"
        });

        // 3. Genel Etkinlik Alani Tarifesi
        var mevcutGenelEtkinlik = await ctx.RezervasyonTarifeler
            .FirstOrDefaultAsync(r => r.UnitId == null && r.UnitTypeId == etkinlikTuruId && r.Year == now.Year);
        if (mevcutGenelEtkinlik != null)
        {
            mevcutGenelEtkinlik.FreeDurationMinutes = 0;
            mevcutGenelEtkinlik.BillingPeriodMinutes = 60;
            mevcutGenelEtkinlik.PeriodRate = 1500m;
            mevcutGenelEtkinlik.KdvRate = 20m;
            mevcutGenelEtkinlik.Description = "Genel Etkinlik Alani saatlik fiyatlandirma kuralı";
        }
        else
        {
            ctx.RezervasyonTarifeler.Add(new ReservationRateOverride
            {
                Year = now.Year,
                UnitTypeId = etkinlikTuruId,
                UnitId = null,
                FreeDurationMinutes = 0,
                BillingPeriodMinutes = 60,
                PeriodRate = 1500m,
                KdvRate = 20m,
                Description = "Genel Etkinlik Alani saatlik fiyatlandirma kuralı"
            });
        }

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

        var pdfBytes = await GetDummyPdfBytesAsync();

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
                Content = new DocumentContent { Content = pdfBytes }
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
                Content = new DocumentContent { Content = pdfBytes }
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
                Content = new DocumentContent { Content = pdfBytes }
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
                Content = new DocumentContent { Content = pdfBytes }
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
                Content = new DocumentContent { Content = pdfBytes }
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
                Content = new DocumentContent { Content = pdfBytes }
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
                Content = new DocumentContent { Content = pdfBytes }
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
                Content = new DocumentContent { Content = pdfBytes }
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

        // --- 5. Sözleşmelerin Oluşturulması ve Onay İş Akışı ---
        var startYearMinus1 = new DateTime(now.Year - 1, 1, 1);

        var rate101 = await ResolveKiraM2Rate(birim101, yzCozum);
        var rate102 = await ResolveKiraM2Rate(birim102, megaFinans);
        var rate103 = await ResolveKiraM2Rate(birim103, biotech);
        var rate104 = await ResolveKiraM2Rate(birim104, yzCozum);
        var rateKafe = await ResolveKiraM2Rate(birimKafe, agroGida);
        var rateOtopark = await ResolveKiraM2Rate(birimOtopark, egeLojistik);
        var rateB201 = await ResolveKiraM2Rate(birimB201, egeLojistik);
        var rateB202 = await ResolveKiraM2Rate(birimB202, megaFinans);
        var rateB203 = await ResolveKiraM2Rate(birimB203, agroGida);
        var rate105 = await ResolveKiraM2Rate(birim105, biotech);

        // SZL-000001: Ofis 101 - yzCozum (Aktif, Doğrudan Onaylanmış)
        var sozlesme1 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(1), birim101, yzCozum,
            startYearMinus1, startYearMinus1.AddYears(2).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 5,
            notlar: "Yapay Zeka Çözümleri ana ofis kiralama sözleşmesi");
        sozlesme1.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = yzUserId,
            ActionDate = startYearMinus1.AddDays(-20)
        });
        sozlesme1.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddDays(-5),
            Explanation = "Başvuru ve sözleşme şartları uygun görüldü, onaylandı."
        });
        sozlesme1.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddDays(-5),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000002: Ofis 102 - megaFinans (Aktif, Revizyon Sürecinden Geçmiş)
        var sozlesme2 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(2), birim102, megaFinans,
            startYearMinus1.AddMonths(3), startYearMinus1.AddMonths(24).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 10,
            notlar: "Mega Finans operasyon merkezi sözleşmesi");
        sozlesme2.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = megaUserId,
            ActionDate = startYearMinus1.AddMonths(3).AddDays(-30)
        });
        sozlesme2.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.RevisionRequested,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.RevisionRequested,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddMonths(3).AddDays(-20),
            Explanation = "Teminat mektubu tutarı ve yetkili imza sirküleri eksik, kira başlangıç tarihi aybaşı olarak güncellenmelidir."
        });
        sozlesme2.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Resubmitted,
            FromStatus = LeaseStatus.RevisionRequested,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = megaUserId,
            ActionDate = startYearMinus1.AddMonths(3).AddDays(-10),
            Explanation = "Eksik belgeler sisteme yüklendi, kira başlangıç tarihi 1 Nisan olarak revize edildi."
        });
        sozlesme2.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddMonths(3).AddDays(-3),
            Explanation = "Revizyonlar incelendi, şartlar uygun görülerek onaylandı."
        });
        sozlesme2.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddMonths(3).AddDays(-3),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000003: Ofis 103 - biotech (Aktif, Akademik Firma)
        var sozlesme3 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(3), birim103, biotech,
            startYearMinus1.AddMonths(6), startYearMinus1.AddYears(2).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 15,
            notlar: "BiyoTek Ar-Ge laboratuvar ve ofis sözleşmesi");
        sozlesme3.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = biotechUserId,
            ActionDate = startYearMinus1.AddMonths(6).AddDays(-15)
        });
        sozlesme3.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddMonths(6).AddDays(-2),
            Explanation = "Akademik firma Ar-Ge ofis kiralama başvurusu onaylandı."
        });
        sozlesme3.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddMonths(6).AddDays(-2),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000004: Ofis 104 - yzCozum (Aktif, Aynı Kiracının 2. Ofisi)
        var sozlesme4 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(4), birim104, yzCozum,
            startYearMinus1.AddMonths(1), startYearMinus1.AddYears(2).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.PeriodStartOffset, vadeGunu: 5,
            notlar: "Yapay Zeka Çözümleri ek çalışma alanı sözleşmesi");
        sozlesme4.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = yzUserId,
            ActionDate = startYearMinus1.AddMonths(1).AddDays(-20)
        });
        sozlesme4.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddMonths(1).AddDays(-5),
            Explanation = "Ek ofis genişleme kiralama başvurusu onaylandı."
        });
        sozlesme4.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddMonths(1).AddDays(-5),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000005: Tekno Kafe K01 - agroGida (Aktif, Ticari Kafe Alanı)
        var sozlesme5 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(5), birimKafe, agroGida,
            startYearMinus1.AddMonths(2), startYearMinus1.AddYears(3).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 1,
            notlar: "Tekno Kafe işletme ve kafeterya kiralama sözleşmesi");
        sozlesme5.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = agroUserId,
            ActionDate = startYearMinus1.AddMonths(2).AddDays(-25)
        });
        sozlesme5.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddMonths(2).AddDays(-5),
            Explanation = "Tekno Kafe ticari alan işletme sözleşmesi onaylandı."
        });
        sozlesme5.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddMonths(2).AddDays(-5),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000006: Açık Otopark - egeLojistik (Aktif, Tekil Taşınmaz Kiralama)
        var sozlesme6 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(6), birimOtopark, egeLojistik,
            startYearMinus1.AddMonths(4), startYearMinus1.AddYears(2).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 5,
            notlar: "Açık otopark alanı filo kiralama sözleşmesi");
        sozlesme6.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = egeUserId,
            ActionDate = startYearMinus1.AddMonths(4).AddDays(-20)
        });
        sozlesme6.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddMonths(4).AddDays(-5),
            Explanation = "Açık otopark alanı lojistik filo kiralama başvurusu onaylandı."
        });
        sozlesme6.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddMonths(4).AddDays(-5),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000007: B Blok Ofis 201 - egeLojistik (Sona Ermiş / Ended)
        var sozlesme7 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(7), birimB201, egeLojistik,
            startYearMinus1.AddMonths(-12), now.AddMonths(-2), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 10,
            status: LeaseStatus.Ended,
            notlar: "Süre bitimi nedeniyle yenilenmeyerek sona erdi.");
        sozlesme7.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = egeUserId,
            ActionDate = startYearMinus1.AddMonths(-13)
        });
        sozlesme7.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddMonths(-12).AddDays(-5),
            Explanation = "Sözleşme onaylandı."
        });
        sozlesme7.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddMonths(-12).AddDays(-5),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000008: B Blok Ofis 202 - megaFinans (Erken Feshedilmiş / Terminated)
        var sozlesme8 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(8), birimB202, megaFinans,
            startYearMinus1, now.AddMonths(6), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 10,
            status: LeaseStatus.Terminated,
            terminationDate: now.AddMonths(-1),
            terminationReason: "Kiracının operasyonel küçülme talebi ve karşılıklı protokol ile erken fesih.",
            notlar: "Karşılıklı anlaşma ile erken feshedilmiştir.");
        sozlesme8.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = megaUserId,
            ActionDate = startYearMinus1.AddDays(-20)
        });
        sozlesme8.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = startYearMinus1.AddDays(-5),
            Explanation = "Sözleşme onaylandı."
        });
        sozlesme8.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = startYearMinus1.AddDays(-5),
            Description = "Sözleşme başvurusu onaylandı."
        });
        sozlesme8.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Termination,
            TransactionDate = now.AddMonths(-1),
            Description = "Kiracının operasyonel küçülme talebi ve karşılıklı protokol ile erken fesih."
        });

        // SZL-000009: B Blok Ofis 203 - agroGida (Aktif, 3 Seviyeli Tarife Düşüşü Örneği)
        var sozlesme9 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(9), birimB203, agroGida,
            now.AddMonths(-3), now.AddMonths(21), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 10,
            notlar: "AgroGıda Ar-Ge ofis sözleşmesi");
        sozlesme9.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = agroUserId,
            ActionDate = now.AddMonths(-3).AddDays(-15)
        });
        sozlesme9.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = now.AddMonths(-3).AddDays(-3),
            Explanation = "AgroGıda Ar-Ge ofis kiralama başvurusu onaylandı."
        });
        sozlesme9.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = now.AddMonths(-3).AddDays(-3),
            Description = "Sözleşme başvurusu onaylandı."
        });

        // SZL-000010: B Blok Ofis 301 - biotech (Revizyon Bekleyen Taslak / RevisionRequested)
        var sozlesme10 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(10), birimB301, biotech,
            now.AddMonths(1), now.AddMonths(13).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 1,
            status: LeaseStatus.RevisionRequested,
            notlar: "B-301 Ar-Ge ofisi kiralama başvurusu");
        sozlesme10.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = biotechUserId,
            ActionDate = now.AddDays(-10)
        });
        sozlesme10.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.RevisionRequested,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.RevisionRequested,
            ActorUserId = adminId,
            ActionDate = now.AddDays(-2),
            Explanation = "Vergi levhası güncel değil ve depozito taahhütnamesi eksik. Lütfen revize ederek yeniden iletiniz."
        });

        // SZL-000011: B Blok Ofis 302 - megaFinans (Yönetici Onayı Bekleyen Taslak / Draft)
        var sozlesme11 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(11), birimB302, megaFinans,
            now.AddMonths(2), now.AddMonths(14).AddDays(-1), true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 1,
            status: LeaseStatus.Draft,
            notlar: "B-302 nolu büyük ofis için yeni kiralama başvurusu.");
        sozlesme11.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = megaUserId,
            ActionDate = now.AddDays(-3)
        });

        // SZL-000012: Ofis 105 - biotech (Aktif, Süresi Dolmak Üzere / Expiring Soon)
        // Kural: Kalan gün sayısı <= 30 gün olan aktif sözleşmeler "Süresi Dolmak Üzere" kabul edilir.
        // getdate (DateTime.Today) referans alınarak bitiş tarihi dinamik hesaplanır.
        var yakindaBitisTarihi = DateTime.Today.AddDays(18); // getdate üzerinden dinamik 18 gün kalmış
        var yakindaBaslangicTarihi = yakindaBitisTarihi.AddYears(-1).AddDays(1); // 1 yıllık sözleşme

        var sozlesme12 = MakeSozlesme(LeaseNumberPolicy.FormatLeaseNo(12), birim105, biotech,
            yakindaBaslangicTarihi, yakindaBitisTarihi, true,
            vadeKuraliTipi: DueDateRuleType.FixedDayOfMonth, vadeGunu: 5,
            notlar: "BiyoTek Ar-Ge ek ofis kiralama sözleşmesi (Süresi 18 gün sonra dolacak)");
        sozlesme12.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.DraftCreated,
            ToStatus = LeaseStatus.Draft,
            ActorUserId = biotechUserId,
            ActionDate = yakindaBaslangicTarihi.AddDays(-20)
        });
        sozlesme12.ReviewHistory.Add(new LeaseReviewHistory
        {
            ActionType = LeaseReviewActionType.Approved,
            FromStatus = LeaseStatus.Draft,
            ToStatus = LeaseStatus.Active,
            ActorUserId = adminId,
            ActionDate = yakindaBaslangicTarihi.AddDays(-5),
            Explanation = "Ek ofis kiralama başvurusu onaylandı."
        });
        sozlesme12.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = yakindaBaslangicTarihi.AddDays(-5),
            Description = "Sözleşme başvurusu onaylandı."
        });

        var sozlesmeler = new List<Lease>
        {
            sozlesme1, sozlesme2, sozlesme3, sozlesme4, sozlesme5,
            sozlesme6, sozlesme7, sozlesme8, sozlesme9, sozlesme10, sozlesme11, sozlesme12
        };

        ctx.Leases.AddRange(sozlesmeler);
        await ctx.SaveChangesAsync();

        // Sözleşmeler için İmzalı Sözleşme Metni ve Ek Belgeleri ekle
        var sozlesmeBelgeleri = new List<Document>();
        foreach (var s in sozlesmeler)
        {
            sozlesmeBelgeleri.Add(new Document
            {
                DocumentTypeId = btImzaliSozlesme.Id,
                OwnerType = DocumentOwnerType.Lease,
                OwnerId = s.Id,
                FileName = $"imzali_sozlesme_{s.LeaseNo}.pdf",
                MimeType = "application/pdf",
                FileSize = pdfBytes.Length,
                Description = $"{s.LeaseNo} İmzalı Kira Sözleşmesi",
                IsInvalid = false,
                Content = new DocumentContent { Content = pdfBytes }
            });
        }

        sozlesmeBelgeleri.Add(new Document
        {
            DocumentTypeId = btTeslim.Id,
            OwnerType = DocumentOwnerType.Lease,
            OwnerId = sozlesme1.Id,
            FileName = "teslim_tutanagi_101.pdf",
            MimeType = "application/pdf",
            FileSize = pdfBytes.Length,
            Description = "Ofis 101 Teslim Tesellüm Tutanağı",
            IsInvalid = false,
            Content = new DocumentContent { Content = pdfBytes }
        });

        sozlesmeBelgeleri.Add(new Document
        {
            DocumentTypeId = btTeminat.Id,
            OwnerType = DocumentOwnerType.Lease,
            OwnerId = sozlesme1.Id,
            FileName = "teminat_mektubu_101.pdf",
            MimeType = "application/pdf",
            FileSize = pdfBytes.Length,
            Description = "Ofis 101 Teminat Mektubu",
            IsInvalid = false,
            Content = new DocumentContent { Content = pdfBytes }
        });

        ctx.Belgeler.AddRange(sozlesmeBelgeleri);
        await ctx.SaveChangesAsync();

        // --- 6. Sözleşme Tarifesi (Özel Oran ve Depozito Kalemleri) Uygulaması ---
        // Kiralanabilir sözleşmelerin %90'ında depozito kalemi tanımlanır.
        // Açık Otopark sözleşmesinde (SZL-000006) filo kiralama kapsamında depozito muafiyeti vardır.
        ctx.SozlesmeTarifeler.AddRange(
            // Ofis 101: Özel kira indirimi (390 TL vs 400 TL birim tarifesi) ve depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme1.Id, ChargeTypeId = btKiraId, UnitValue = 390, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new LeaseRateOverride { LeaseId = sozlesme1.Id, ChargeTypeId = btDepozitoId, UnitValue = 1290, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // Ofis 102: Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme2.Id, ChargeTypeId = btDepozitoId, UnitValue = 1290, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // Ofis 103: Kira ve depozito sözleşme tarifesi (ortak ve portal taşınmazdan düşer)
            new LeaseRateOverride { LeaseId = sozlesme3.Id, ChargeTypeId = btKiraId, UnitValue = rate103, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
            new LeaseRateOverride { LeaseId = sozlesme3.Id, ChargeTypeId = btDepozitoId, UnitValue = 960, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // Ofis 104: Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme4.Id, ChargeTypeId = btDepozitoId, UnitValue = 1290, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // Tekno Kafe: Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme5.Id, ChargeTypeId = btDepozitoId, UnitValue = 1950, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // Açık Otopark: Özel filo kiralama bedeli (110 TL), filo kapsamında depozito muafiyeti uygulanmıştır (depozito kalemi bulunmaz)
            new LeaseRateOverride { LeaseId = sozlesme6.Id, ChargeTypeId = btKiraId, UnitValue = 110, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // B-201: Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme7.Id, ChargeTypeId = btDepozitoId, UnitValue = 30000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },

            // B-202: Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme8.Id, ChargeTypeId = btDepozitoId, UnitValue = 30000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },

            // B-203: Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme9.Id, ChargeTypeId = btDepozitoId, UnitValue = 1080, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // B-301 (Revizyon İstenen Başvuru): Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme10.Id, ChargeTypeId = btDepozitoId, UnitValue = 1080, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

            // B-302 (Onay Bekleyen Taslak Başvuru): Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme11.Id, ChargeTypeId = btDepozitoId, UnitValue = 30000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },

            // Ofis 105 (Süresi Dolmak Üzere): Depozito kalemi
            new LeaseRateOverride { LeaseId = sozlesme12.Id, ChargeTypeId = btDepozitoId, UnitValue = 960, CalculationMethod = CalculationMethod.M2, KdvRate = 20 }
        );
        await ctx.SaveChangesAsync();

        // --- 7. Charge Üretimi ---
        // Yalnız Aktif, Sona Ermiş veya Feshedilmiş sözleşmeler için tahakkuk üretilir (Taslak ve revizyon bekleyenler hariç)
        sozlesme7.Status = LeaseStatus.Active;
        sozlesme8.Status = LeaseStatus.Active;
        await ctx.SaveChangesAsync();

        var uretilecekSozlesmeler = sozlesmeler
            .Where(s => s.Status == LeaseStatus.Active)
            .ToList();

        foreach (var s in uretilecekSozlesmeler)
        {
            await tahakkukUretim.GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));
        }

        sozlesme7.Status = LeaseStatus.Ended;
        sozlesme8.Status = LeaseStatus.Terminated;
        await ctx.SaveChangesAsync();


        // --- 8. Diğer Seed İşlemleri ---
        await SeedRezervasyonlarAsync();
        await SeedBankaHareketleriAsync();
        await SeedTahakkuklarVeOdemelerAsync(sozlesmeler);
    }

    private async Task<(string adminId, string yzUserId, string megaUserId, string biotechUserId, string egeUserId, string agroUserId)> SeedKiraciKullanicilariAsync(List<Tenant> seededKiraciler)
    {
        const string defaultPassword = "Admin123!";
        var admin = await userManager.FindByEmailAsync(IdentitySeedService.AdminEmail);
        if (admin == null || !admin.IsActive || !admin.IsSuperAdmin || admin.UserType != UserType.Internal)
            throw new InvalidOperationException("Kiracı seed işleminden önce iç süper admin oluşturulmalıdır.");

        var adminId = admin.Id;

        foreach (var k in seededKiraciler)
        {
            if (!string.IsNullOrWhiteSpace(k.Email))
            {
                var (userEmail, adSoyad) = k.Email switch
                {
                    "info@yz.com" => ("ahmet.yilmaz@yz.com", "Ahmet Yılmaz"),
                    "info@megafinans.com" => ("mehmet.demir@megafinans.com", "Mehmet Demir"),
                    "iletisim@biotech.com" => ("ayse.kaya@biotech.com", "Ayşe Kaya"),
                    "info@egelojistik.com" => ("kemal.sahin@egelojistik.com", "Kemal Şahin"),
                    "iletisim@agrogida.com" => ("fatma.celik@agrogida.com", "Fatma Çelik"),
                    _ => (k.Email, k.DisplayName)
                };

                await EnsureKiraciUserAsync(userEmail, defaultPassword, adSoyad, k.Id);
            }
        }

        var yzCozumEntity = seededKiraciler.FirstOrDefault(k => k.Email == "info@yz.com");
        if (yzCozumEntity != null)
        {
            var yzCozumId = yzCozumEntity.Id;

            await EnsureKiraciUserAsync("mehmet.yildiz@yz.com", defaultPassword, "Mehmet Yıldız", yzCozumId, tumTasinmazlaraErisim: false);
            var mehmetUser = await userManager.FindByEmailAsync("mehmet.yildiz@yz.com");
            if (mehmetUser != null)
            {
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

            await EnsureKiraciPersoneliRoleAsync(adminId, yzCozumId);
            await EnsureKiraciUserAsync("canan.oz@yz.com", defaultPassword, "Canan Öz", yzCozumId, roleName: "Kiracı Personeli");
        }

        var yzUser = await userManager.FindByEmailAsync("ahmet.yilmaz@yz.com");
        var megaUser = await userManager.FindByEmailAsync("mehmet.demir@megafinans.com");
        var biotechUser = await userManager.FindByEmailAsync("ayse.kaya@biotech.com");
        var egeUser = await userManager.FindByEmailAsync("kemal.sahin@egelojistik.com");
        var agroUser = await userManager.FindByEmailAsync("fatma.celik@agrogida.com");

        return (
            adminId,
            yzUser?.Id ?? adminId,
            megaUser?.Id ?? adminId,
            biotechUser?.Id ?? adminId,
            egeUser?.Id ?? adminId,
            agroUser?.Id ?? adminId
        );
    }

    public async Task SeedTasinmazFiyatlarAsync()
    {
        var katAkademik = await ctx.Kategoriler.FirstAsync(k => k.Type == CategoryType.Tenant && k.Code == "AKADEMIK");
        var katAkadOlmayan = await ctx.Kategoriler.FirstAsync(k => k.Type == CategoryType.Tenant && k.Code == "AKADEMIK_OLMAYAN");

        var btKira = await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Kira);
        var btOrtak = await ctx.ChargeTypes.FirstAsync(b => b.Code == "ORTAK");
        var btPortal = await ctx.ChargeTypes.FirstAsync(b => b.Code == "PORTAL");
        var btDepozito = await ctx.ChargeTypes.FirstAsync(b => b.Code == BorcTipiConsts.Depozito);

        var teknokent = await ctx.Properties.FirstOrDefaultAsync(t => t.Name == "Teknokent A Blok");
        if (teknokent != null && !await ctx.TasinmazTarifeler.AnyAsync(f => f.PropertyId == teknokent.Id))
        {








            ctx.TasinmazTarifeler.AddRange(
                // Akademik için (m2 bazlı kira ve ortak gider) - Taşınmaz Tarifesi
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btKira.Id, UnitValue = 320, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btOrtak.Id, UnitValue = 95, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btPortal.Id, UnitValue = 480, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btDepozito.Id, UnitValue = 960, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

                // Akademik Olmayan için - Taşınmaz Tarifesi
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btKira.Id, UnitValue = 430, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btOrtak.Id, UnitValue = 140, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btPortal.Id, UnitValue = 700, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokent.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btDepozito.Id, UnitValue = 1290, CalculationMethod = CalculationMethod.M2, KdvRate = 20 }
            );

            await ctx.SaveChangesAsync();
        }

        // 2. Teknokent B Blok Taşınmaz Tarifesi
        var teknokentB = await ctx.Properties.FirstOrDefaultAsync(t => t.Name == "Teknokent B Blok");
        if (teknokentB != null && !await ctx.TasinmazTarifeler.AnyAsync(f => f.PropertyId == teknokentB.Id))
        {
            ctx.TasinmazTarifeler.AddRange(
                // Akademik için — Not: Portal tanımlanmadı, böylece Portal gideri bir üst seviye olan Genel Tarife'den düşecek!
                new PropertyRateOverride { PropertyId = teknokentB.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btKira.Id, UnitValue = 360, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentB.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btOrtak.Id, UnitValue = 110, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentB.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btDepozito.Id, UnitValue = 1080, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

                // Akademik Olmayan için
                new PropertyRateOverride { PropertyId = teknokentB.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btKira.Id, UnitValue = 490, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentB.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btOrtak.Id, UnitValue = 160, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentB.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btPortal.Id, UnitValue = 800, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentB.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btDepozito.Id, UnitValue = 30000, CalculationMethod = CalculationMethod.Fixed, KdvRate = 20 }
            );

            await ctx.SaveChangesAsync();
        }

        // 3. Teknokent Açık Otoparkı Taşınmaz Tarifesi
        var otopark = await ctx.Properties.FirstOrDefaultAsync(t => t.Name == "Teknokent Açık Otoparkı");
        if (otopark != null && !await ctx.TasinmazTarifeler.AnyAsync(f => f.PropertyId == otopark.Id))
        {
            ctx.TasinmazTarifeler.AddRange(
                // Otopark için sadece Kira ve Ortak Gider tanımlandı (Portal ve Depozito tanımlanmadı)
                new PropertyRateOverride { PropertyId = otopark.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btKira.Id, UnitValue = 80, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = otopark.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btOrtak.Id, UnitValue = 20, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

                new PropertyRateOverride { PropertyId = otopark.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btKira.Id, UnitValue = 120, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = otopark.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btOrtak.Id, UnitValue = 30, CalculationMethod = CalculationMethod.M2, KdvRate = 20 }
            );

            await ctx.SaveChangesAsync();
        }

        // 4. Teknokent D Blok Taşınmaz Tarifesi
        var teknokentD = await ctx.Properties.FirstOrDefaultAsync(t => t.Name == "Teknokent D Blok (Kuluçka ve Girişimcilik Merkezi)");
        if (teknokentD != null && !await ctx.TasinmazTarifeler.AnyAsync(f => f.PropertyId == teknokentD.Id))
        {
            ctx.TasinmazTarifeler.AddRange(
                // Akademik için
                new PropertyRateOverride { PropertyId = teknokentD.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btKira.Id, UnitValue = 280, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentD.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btOrtak.Id, UnitValue = 85, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentD.Id, TenantCategoryId = katAkademik.Id, ChargeTypeId = btDepozito.Id, UnitValue = 840, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },

                // Akademik Olmayan için
                new PropertyRateOverride { PropertyId = teknokentD.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btKira.Id, UnitValue = 380, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentD.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btOrtak.Id, UnitValue = 120, CalculationMethod = CalculationMethod.M2, KdvRate = 20 },
                new PropertyRateOverride { PropertyId = teknokentD.Id, TenantCategoryId = katAkadOlmayan.Id, ChargeTypeId = btDepozito.Id, UnitValue = 1140, CalculationMethod = CalculationMethod.M2, KdvRate = 20 }
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

    private static int _paymentSeq = 100;

    private async Task SeedTahakkuklarVeOdemelerAsync(List<Lease> sozlesmeler)
    {
        try
        {
            var adminUser = await userManager.FindByEmailAsync(IdentitySeedService.AdminEmail);
            var adminId = adminUser?.Id ?? "admin-id";
            var yzUser = await userManager.FindByEmailAsync("ahmet.yilmaz@yz.com");
            var megaUser = await userManager.FindByEmailAsync("mehmet.demir@megafinans.com");
            var biotechUser = await userManager.FindByEmailAsync("ayse.kaya@biotech.com");
            var egeUser = await userManager.FindByEmailAsync("kemal.sahin@egelojistik.com");
            var agroUser = await userManager.FindByEmailAsync("fatma.celik@agrogida.com");

            var seedStoreAccountId = await ctx.Stores
                .Where(s => s.Code == "STR-KIRA" || s.Code == "SEED")
                .SelectMany(s => s.Accounts)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();
            if (seedStoreAccountId == 0)
                seedStoreAccountId = await ctx.StoreAccounts.Select(a => a.Id).FirstOrDefaultAsync();

            var today = DateTime.Today;
            var currentYear = today.Year;
            var currentMonth = today.Month;

            var manuelBorcTipi = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == BorcTipiConsts.Diger)
                                ?? await ctx.ChargeTypes.FirstAsync();
            var btOrtak = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "ORTAK") ?? manuelBorcTipi;

            var dekontDocType = await ctx.DocumentTypes.FirstOrDefaultAsync(d => d.Code == "ODEME_DEKONT" && d.IsSystem)
                                ?? await ctx.DocumentTypes.FirstAsync(d => d.TargetEntity == DocumentOwnerType.Payment);

            var pdfBytes = await GetDummyPdfBytesAsync();

            var yzCozum = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000001");
            var megaFinans = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000002");
            var biotech = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000003");
            var egeLojistik = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000004");
            var agroGida = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000005");

            var ofis101 = await ctx.Units.FirstAsync(u => u.UnitNo == "101");
            var ofis102 = await ctx.Units.FirstAsync(u => u.UnitNo == "102");
            var ofis103 = await ctx.Units.FirstAsync(u => u.UnitNo == "103");
            var ofis104 = await ctx.Units.FirstAsync(u => u.UnitNo == "104");
            var birimKafe = await ctx.Units.FirstAsync(u => u.UnitNo == "K01");
            var sozlesme1 = sozlesmeler.First(s => s.LeaseNo == LeaseNumberPolicy.FormatLeaseNo(1));
            var sozlesme2 = sozlesmeler.First(s => s.LeaseNo == LeaseNumberPolicy.FormatLeaseNo(2));
            var sozlesme3 = sozlesmeler.First(s => s.LeaseNo == LeaseNumberPolicy.FormatLeaseNo(3));
            var sozlesme4 = sozlesmeler.First(s => s.LeaseNo == LeaseNumberPolicy.FormatLeaseNo(4));
            var sozlesme5 = sozlesmeler.First(s => s.LeaseNo == LeaseNumberPolicy.FormatLeaseNo(5));
            var sozlesme6 = sozlesmeler.First(s => s.LeaseNo == LeaseNumberPolicy.FormatLeaseNo(6));
            var sozlesme9 = sozlesmeler.First(s => s.LeaseNo == LeaseNumberPolicy.FormatLeaseNo(9));

            var ofisOtopark = sozlesme6.Unit ?? await ctx.Units.FirstAsync(u => u.Id == sozlesme6.UnitId);
            var birimB203 = sozlesme9.Unit ?? await ctx.Units.FirstAsync(u => u.Id == sozlesme9.UnitId);

            // =========================================================================
            // 1. Manuel Operasyonel ve İptal Edilen Tahakkukların Tanımlanması (> 10 adet)
            // =========================================================================
            Charge CreateManual(int seq, Tenant tenant, Unit unit, Lease lease, ChargeType cType,
                string desc, decimal netAmount, int dueDayOffset, ChargeStatus status = ChargeStatus.Pending, string? cancelNote = null, DateTime? periodStart = null)
            {
                var kdv = Math.Round(netAmount * 0.20m, 2);
                var total = netAmount + kdv;
                var pStart = periodStart ?? new DateTime(currentYear, currentMonth, 1);
                var pEnd = pStart.AddMonths(1).AddDays(-1);
                var dDate = pStart.AddDays(dueDayOffset);
                if (pStart.Year == currentYear && pStart.Month == currentMonth && dDate > today) dDate = today;

                return new Charge
                {
                    ChargeNo = ChargeNumberPolicy.FormatChargeNo(900000 + seq),
                    TenantId = tenant.Id,
                    UnitId = unit.Id,
                    LeaseId = lease.Id,
                    PeriodStart = pStart,
                    PeriodEnd = pEnd,
                    DueDate = dDate,
                    ExpectedAmount = netAmount,
                    KdvAmount = kdv,
                    TotalAmount = total,
                    PaidAmount = 0m,
                    Status = status,
                    SourceType = ChargeSourceType.Manual,
                    CancellationNote = cancelNote,
                    LineItems = new List<ChargeLineItem>
                    {
                        new ChargeLineItem
                        {
                            ChargeTypeId = cType.Id,
                            Description = desc,
                            UnitValue = netAmount,
                            Multiplier = 1m,
                            Amount = netAmount,
                            KdvRate = 20m,
                            KdvAmount = kdv,
                            TotalAmount = total,
                            PaidAmount = 0m,
                            SourceType = LineItemSourceType.ManualInput
                        }
                    }
                };
            }

            // --- Cari Ay (Eylül 2026) Manuel Tahakkukları ---
            var manuel01 = CreateManual(1, yzCozum, ofis101, sozlesme1, btOrtak, "Yüksek Kapasiteli Fiber İnternet Altyapı ve Kablolama Bedeli", 4000m, 5);
            var manuel02 = CreateManual(2, megaFinans, ofis102, sozlesme2, manuelBorcTipi, "Bina İçi Güvenlik Geçiş Kartları ve Turnike Entegrasyon Bedeli", 2500m, 7);
            var manuel03 = CreateManual(3, biotech, ofis103, sozlesme3, btOrtak, "Laboratuvar Atık Yönetimi ve Özel Arıtma Bertaraf Hizmeti", 6000m, 10);
            var manuel04 = CreateManual(4, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Ağır Vasıta ve Filo Zemin Bakım ve Çizgi Boyama Masrafı", 5000m, 6);
            var manuel05 = CreateManual(5, agroGida, birimKafe, sozlesme5, btOrtak, "Yağ Tutucu ve Havalandırma Bacası Periyodik Temizlik Bedeli", 3500m, 4);
            var manuel06 = CreateManual(6, agroGida, birimB203, sozlesme9, manuelBorcTipi, "Ofis Cam Cephe Dış Temizlik ve Dağcı Vinç Hizmet Bedeli", 2000m, 8);
            var manuel07 = CreateManual(7, yzCozum, ofis104, sozlesme4, btOrtak, "Ek Çalışma Alanı Elektrik Alt Sayaç Tüketim Fark Bedeli", 1800m, 9);
            var manuel08 = CreateManual(8, megaFinans, ofis102, sozlesme2, manuelBorcTipi, "Ortak Teras Alanı Kurumsal Ağırlama ve Organizasyon Payı", 3000m, 11);
            var manuel09 = CreateManual(9, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Ekstra Otopark Güvenlik ve Gece Devriye Hizmeti", 2500m, 5);
            var manuel10 = CreateManual(10, megaFinans, ofis102, sozlesme2, btOrtak, "Yıllık İklimlendirme ve Filtre Bakım Payı", 1500m, 8);
            var manuel11 = CreateManual(11, agroGida, birimKafe, sozlesme5, manuelBorcTipi, "Kafe Önü Bahçe Sundurması Onarım ve Tadilat Bedeli", 4500m, 12);
            var manuel12 = CreateManual(12, biotech, ofis103, sozlesme3, btOrtak, "Biyolojik Güvenlik Kabini Kalibrasyon ve Sertifikasyon Katkısı", 3200m, 10);
            var manuel13 = CreateManual(13, yzCozum, ofis101, sozlesme1, manuelBorcTipi, "Akıllı Giriş Sistemi İlave Manyetik Kart ve Token Tedarik Bedeli", 1200m, 6);

            // İptal Edilen Cari Ay Manuel Tahakkuklar
            var manuel14 = CreateManual(14, yzCozum, ofis101, sozlesme1, manuelBorcTipi, "Mükerrer Girilen Sunucu Odası Soğutma Ekstra Bedeli", 2000m, 3,
                status: ChargeStatus.Cancelled, cancelNote: "Mükerrer muhasebe kaydı ve sözleşme kapsamı dahilinde olduğu tespitiyle iptal edildi.");
            var manuel15 = CreateManual(15, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Hatalı Giriş Yapılan Otopark Aydınlatma Direk Onarım Payı", 1500m, 4,
                status: ChargeStatus.Cancelled, cancelNote: "Sigorta şirketi tarafından tazmin edildiği için iptal edilmiştir.");

            // --- Geçmiş Aylar (Ocak - Ağustos 2026) Manuel Tahakkukları ---
            var pastManualCharges = new List<Charge>
            {
                // Ocak 2026
                CreateManual(16, yzCozum, ofis101, sozlesme1, manuelBorcTipi, "Yıllık Sunucu Odası Yangın Söndürme Tüpleri Dolum ve Kontrolü", 2800m, 10, periodStart: new DateTime(2026, 1, 1)),
                CreateManual(17, megaFinans, ofis102, sozlesme2, btOrtak, "Ofis Zemin Halı Yıkama ve Dezenfeksiyon Hizmeti", 1900m, 12, periodStart: new DateTime(2026, 1, 1)),
                CreateManual(18, biotech, ofis103, sozlesme3, btOrtak, "Hassas Terazi ve Ölçüm Cihazları Yıllık Kalibrasyon Katkı Bedeli", 3200m, 15, periodStart: new DateTime(2026, 1, 1)),

                // Şubat 2026
                CreateManual(19, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Otopark Zemin Buzlanma Önleme ve Tuzlama Hizmet Payı", 1500m, 8, periodStart: new DateTime(2026, 2, 1)),
                CreateManual(20, agroGida, birimKafe, sozlesme5, btOrtak, "Mutfak Alanı Yağ Ayırıcı Filtre ve Pompa Bakım Masrafı", 2400m, 10, periodStart: new DateTime(2026, 2, 1)),
                CreateManual(21, yzCozum, ofis104, sozlesme4, manuelBorcTipi, "İlave Ofis İçi Network Switch ve Patch Panel Montaj Bedeli", 3100m, 14, periodStart: new DateTime(2026, 2, 1)),

                // Mart 2026
                CreateManual(22, megaFinans, ofis102, sozlesme2, manuelBorcTipi, "Toplantı Odası Ses Yalıtım Panelleri ve Akustik Düzenleme", 4200m, 9, periodStart: new DateTime(2026, 3, 1)),
                CreateManual(23, biotech, ofis103, sozlesme3, btOrtak, "Kimyasal Atık Depolama Alanı Periyodik İlaçlama ve Bertarafı", 2900m, 11, periodStart: new DateTime(2026, 3, 1)),
                CreateManual(24, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Açık Otopark Güvenlik Kamerası Lens ve Gece Görüş Bakımı", 1800m, 14, periodStart: new DateTime(2026, 3, 1)),
                CreateManual(25, yzCozum, ofis101, sozlesme1, manuelBorcTipi, "Mükerrer Girilen Sunucu Raf Montaj Bedeli", 1600m, 5,
                    status: ChargeStatus.Cancelled, cancelNote: "Muhasebe departmanı tarafından sehven mükerrer fatura edildiği için iptal edilmiştir.", periodStart: new DateTime(2026, 3, 1)),

                // Nisan 2026
                CreateManual(26, agroGida, birimKafe, sozlesme5, btOrtak, "Bina Terası Bahçe Oturma Grupları Ahşap Koruma ve Vernikleme", 2500m, 8, periodStart: new DateTime(2026, 4, 1)),
                CreateManual(27, yzCozum, ofis101, sozlesme1, manuelBorcTipi, "Yüksek Hızlı Metro Ethernet Altyapı Bağlantı ve Hat Test Payı", 3800m, 10, periodStart: new DateTime(2026, 4, 1)),
                CreateManual(28, megaFinans, ofis102, sozlesme2, btOrtak, "Bina İçi Acil Aydınlatma ve Yönlendirme Armatürleri Yenileme", 2100m, 12, periodStart: new DateTime(2026, 4, 1)),

                // Mayıs 2026
                CreateManual(29, biotech, ofis103, sozlesme3, btOrtak, "Temiz Oda Hava Basınç Fark Sensörleri Değişim Bedeli", 4400m, 7, periodStart: new DateTime(2026, 5, 1)),
                CreateManual(30, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Lojistik Sevkiyat Girişi Otomatik Sarmal Kepenk Motor Bakımı", 3300m, 10, periodStart: new DateTime(2026, 5, 1)),
                CreateManual(31, agroGida, birimKafe, sozlesme5, btOrtak, "Kafe Açık Alan Gölgelik Tente Kumaş Yenileme ve Mekanizma Tamiri", 3600m, 14, periodStart: new DateTime(2026, 5, 1)),

                // Haziran 2026
                CreateManual(32, yzCozum, ofis104, sozlesme4, btOrtak, "Yaz Dönemi Merkezi Chiller Klima Soğutma Gaz Dolumu Payı", 4100m, 9, periodStart: new DateTime(2026, 6, 1)),
                CreateManual(33, megaFinans, ofis102, sozlesme2, manuelBorcTipi, "Veri Merkezi UPS Güç Kaynağı Akü Grubu Test ve Değişim Masrafı", 5000m, 11, periodStart: new DateTime(2026, 6, 1)),
                CreateManual(34, biotech, ofis103, sozlesme3, btOrtak, "Biyolojik Güvenlik Kabinleri Yıllık Validasyon ve Sertifikasyon Katkısı", 3900m, 15, periodStart: new DateTime(2026, 6, 1)),

                // Temmuz 2026
                CreateManual(35, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Yaz Dönemi Otopark Alanı Asfalt Çatlak Dolgu ve Çizgi Yenileme", 2800m, 8, periodStart: new DateTime(2026, 7, 1)),
                CreateManual(36, agroGida, birimB203, sozlesme9, btOrtak, "Soğuk Hava Deposu Kompresör Revizyon ve Fan Temizlik Bedeli", 4500m, 10, periodStart: new DateTime(2026, 7, 1)),
                CreateManual(37, yzCozum, ofis101, sozlesme1, manuelBorcTipi, "Ofis İçi Akıllı Aydınlatma Otomasyonu Kart Değişimi", 1600m, 12, periodStart: new DateTime(2026, 7, 1)),
                CreateManual(38, megaFinans, ofis102, sozlesme2, btOrtak, "Sehven Açılan Ortak Koridor İklimlendirme Katkı Payı", 2200m, 6,
                    status: ChargeStatus.Cancelled, cancelNote: "Taşınmaz genel bütçesinden karşılandığı için tahakkuk iptal edilmiştir.", periodStart: new DateTime(2026, 7, 1)),

                // Ağustos 2026
                CreateManual(39, megaFinans, ofis102, sozlesme2, btOrtak, "Yıllık Yangın ve Tahliye Tatbikatı Dış Danışmanlık ve Raporlama Payı", 2300m, 8, periodStart: new DateTime(2026, 8, 1)),
                CreateManual(40, biotech, ofis103, sozlesme3, btOrtak, "Saf Su Arıtma Sistemi Reçine ve Membran Filtre Değişim Bedeli", 3700m, 10, periodStart: new DateTime(2026, 8, 1)),
                CreateManual(41, agroGida, birimKafe, sozlesme5, manuelBorcTipi, "Gıda Analiz Laboratuvarı Havalandırma Kanalı Dezenfeksiyon Hizmeti", 2900m, 12, periodStart: new DateTime(2026, 8, 1)),
                CreateManual(42, egeLojistik, ofisOtopark, sozlesme6, manuelBorcTipi, "Giriş Güvenlik Kulübesi Ekstra Kamera ve Monitör Bağlantısı", 1750m, 14, periodStart: new DateTime(2026, 8, 1))
            };

            ctx.Charges.AddRange(manuel01, manuel02, manuel03, manuel04, manuel05, manuel06, manuel07,
                manuel08, manuel09, manuel10, manuel11, manuel12, manuel13, manuel14, manuel15);
            ctx.Charges.AddRange(pastManualCharges);
            await ctx.SaveChangesAsync();

            // =========================================================================
            // 2. Cari Ay Vadesi Gelmiş Tahakkukların Dinamik Oranlara Göre Dağıtılması
            // =========================================================================
            // Cari ay içinde vadesi gelmiş ve iptal edilmemiş tahakkukları çekiyoruz
            var cariAyVadesiGelmisTahakkuklar = await ctx.Charges
                .Include(t => t.LineItems)
                .Where(t => t.SourceType != ChargeSourceType.Reservation
                    && t.PeriodStart.Year == currentYear
                    && t.PeriodStart.Month == currentMonth
                    && t.DueDate <= today
                    && t.Status != ChargeStatus.Cancelled)
                .OrderBy(t => t.SourceType)
                .ThenBy(t => t.Id)
                .ToListAsync();

            int totalCharges = cariAyVadesiGelmisTahakkuklar.Count;
            int countPaid = (int)Math.Round(totalCharges * 0.60);
            int countPartial = (int)Math.Round(totalCharges * 0.10);
            int countOverdue = (int)Math.Round(totalCharges * 0.20);
            int countPending = totalCharges - countPaid - countPartial - countOverdue;

            if (countPaid < 1) countPaid = 1;
            if (countPartial < 1) countPartial = 1;
            if (countOverdue < 1) countOverdue = 1;
            if (countPending < 1) countPending = 1;

            while (countPaid + countPartial + countOverdue + countPending > totalCharges)
            {
                if (countPaid > 1) countPaid--;
                else break;
            }

            var leaseCharges = cariAyVadesiGelmisTahakkuklar.Where(t => t.SourceType == ChargeSourceType.Lease).ToList();
            var manualCharges = cariAyVadesiGelmisTahakkuklar.Where(t => t.SourceType == ChargeSourceType.Manual).ToList();

            var paidCharges = new List<Charge>();
            var partialCharges = new List<Charge>();
            var overdueCharges = new List<Charge>();
            var pendingCharges = new List<Charge>();

            if (leaseCharges.Count >= 7 && manualCharges.Count == 13)
            {
                // Dengeli dağıtım:
                // Paid: 5 kira + 7 manuel
                paidCharges.AddRange(leaseCharges.Take(5));
                paidCharges.AddRange(manualCharges.Take(7));

                // Partial (2 adet): 1 kira + 1 manuel
                partialCharges.AddRange(leaseCharges.Skip(5).Take(1));
                partialCharges.AddRange(manualCharges.Skip(7).Take(1));

                // Overdue: kalan kiralar + 3 manuel
                overdueCharges.AddRange(leaseCharges.Skip(6));
                overdueCharges.AddRange(manualCharges.Skip(8).Take(3));

                // Pending (2 adet): 2 manuel
                pendingCharges.AddRange(manualCharges.Skip(11).Take(2));
            }
            else
            {
                paidCharges = cariAyVadesiGelmisTahakkuklar.Take(countPaid).ToList();
                partialCharges = cariAyVadesiGelmisTahakkuklar.Skip(countPaid).Take(countPartial).ToList();
                overdueCharges = cariAyVadesiGelmisTahakkuklar.Skip(countPaid + countPartial).Take(countOverdue).ToList();
                pendingCharges = cariAyVadesiGelmisTahakkuklar.Skip(countPaid + countPartial + countOverdue).Take(countPending).ToList();
            }

            // --- A. Tam Ödenmiş Tahakkuklar (%60) & Onaylı Ödemeler ---
            // Toplam ödeme kayıtlarında %80 onaylı oranını (16 adet) tam yakalamak için
            // ilk 2 tahakkuk 2 taksit halinde onaylı ödenir, diğer 10 tahakkuk tek parça ödenir (2*2 + 10*1 = 14 ödeme).
            for (int i = 0; i < paidCharges.Count; i++)
            {
                var t = paidCharges[i];
                var payerId = GetTenantUserId(t.TenantId, yzCozum.Id, yzUser?.Id, megaFinans.Id, megaUser?.Id,
                    biotech.Id, biotechUser?.Id, egeLojistik.Id, egeUser?.Id, agroGida.Id, agroUser?.Id, adminId);

                if (i < 2)
                {
                    var parca1 = Math.Round(t.TotalAmount * 0.70m, 2);
                    var parca2 = t.TotalAmount - parca1;

                    DagitKalemBazliOdeme(t, parca1, seedStoreAccountId, payerId,
                        t.DueDate.AddDays(-3), PaymentChannel.BankTransfer,
                        $"{t.ChargeNo} cari ay 1. taksit ödemesi",
                        status: PaymentStatus.Approved, approvedByUserId: adminId, approvalDate: t.DueDate.AddDays(-3));

                    DagitKalemBazliOdeme(t, parca2, seedStoreAccountId, payerId,
                        t.DueDate.AddDays(-1), PaymentChannel.BankTransfer,
                        $"{t.ChargeNo} cari ay 2. taksit bakiye ödemesi",
                        status: PaymentStatus.Approved, approvedByUserId: adminId, approvalDate: t.DueDate.AddDays(-1));
                }
                else
                {
                    DagitKalemBazliOdeme(t, t.TotalAmount, seedStoreAccountId, payerId,
                        t.DueDate.AddDays(-1), PaymentChannel.BankTransfer,
                        $"{t.ChargeNo} tam tutar tahakkuk ödemesi teyidi",
                        status: PaymentStatus.Approved, approvedByUserId: adminId, approvalDate: t.DueDate);
                }

                t.PaidAmount = t.TotalAmount;
                t.Status = ChargeStatus.Paid;
            }

            // --- B. Kısmi Ödenmiş Tahakkuklar (%10) & Onaylı Ödemeler ---
            // 2 tahakkuk kısmi ödenir (2 ödeme -> toplam onaylı = 14 + 2 = 16 ödeme, tam %80!).
            foreach (var t in partialCharges)
            {
                var payerId = GetTenantUserId(t.TenantId, yzCozum.Id, yzUser?.Id, megaFinans.Id, megaUser?.Id,
                    biotech.Id, biotechUser?.Id, egeLojistik.Id, egeUser?.Id, agroGida.Id, agroUser?.Id, adminId);

                var kismiTutar = Math.Round(t.TotalAmount * 0.50m, 2);
                DagitKalemBazliOdeme(t, kismiTutar, seedStoreAccountId, payerId,
                    today.AddDays(-2), PaymentChannel.Eft,
                    $"{t.ChargeNo} cari ay kısmi ödeme bildirimi teyidi",
                    status: PaymentStatus.Approved, approvedByUserId: adminId, approvalDate: today.AddDays(-1));

                t.PaidAmount = kismiTutar;
                t.Status = ChargeStatus.PartiallyPaid;
            }

            await ctx.SaveChangesAsync();

            // --- C. Ödeme Onayı Bekleyen Tahakkuklar (%10) & Onay Bekleyen Ödemeler (%10) ---
            // Kiracılar ödeme bildiriminde bulunmuş, sistemde dekontları ekli, onay bekliyor (2 ödeme = %10).
            foreach (var t in pendingCharges)
            {
                var payerId = GetTenantUserId(t.TenantId, yzCozum.Id, yzUser?.Id, megaFinans.Id, megaUser?.Id,
                    biotech.Id, biotechUser?.Id, egeLojistik.Id, egeUser?.Id, agroGida.Id, agroUser?.Id, adminId);

                var pendingAllocs = DagitKalemBazliOdeme(t, t.TotalAmount, seedStoreAccountId, payerId,
                    today.AddDays(-1), PaymentChannel.BankTransfer,
                    $"{t.ChargeNo} tahakkukuna istinaden kiracı havale/EFT ödeme bildirimi",
                    status: PaymentStatus.PendingApproval);

                await ctx.SaveChangesAsync();

                if (dekontDocType != null && pendingAllocs.Count > 0)
                {
                    AttachPaymentReceiptDocument(pendingAllocs[0], dekontDocType, pdfBytes, $"dekont_{t.ChargeNo}.pdf");
                    await ctx.SaveChangesAsync();
                }

                t.PaidAmount = 0m;
                t.Status = ChargeStatus.Pending;
            }

            // --- D. Gecikmiş Tahakkuklar (%20) & Reddedilen Ödemeler (%10) ---
            // 4 gecikmiş tahakkukun 2 tanesinde kiracı hatalı bildirim yapmış, yönetici reddetmiş (2 ödeme = %10).
            // Kalan 2 tanesinde kiracı henüz hiçbir ödeme yapmamış.
            for (int i = 0; i < overdueCharges.Count; i++)
            {
                var t = overdueCharges[i];
                var payerId = GetTenantUserId(t.TenantId, yzCozum.Id, yzUser?.Id, megaFinans.Id, megaUser?.Id,
                    biotech.Id, biotechUser?.Id, egeLojistik.Id, egeUser?.Id, agroGida.Id, agroUser?.Id, adminId);

                if (i == 0)
                {
                    var rejAllocs = DagitKalemBazliOdeme(t, t.TotalAmount, seedStoreAccountId, payerId,
                        today.AddDays(-4), PaymentChannel.BankTransfer,
                        $"{t.ChargeNo} ödeme bildirimi",
                        status: PaymentStatus.Rejected,
                        rejectionReason: "Banka dekontundaki tutar ve açıklama tahakkuk kalemi ile uyuşmuyor.");

                    await ctx.SaveChangesAsync();

                    if (dekontDocType != null && rejAllocs.Count > 0)
                    {
                        AttachPaymentReceiptDocument(rejAllocs[0], dekontDocType, pdfBytes, $"dekont_red_{t.ChargeNo}.pdf");
                        await ctx.SaveChangesAsync();
                    }
                }
                else if (i == 1)
                {
                    var rejAllocs = DagitKalemBazliOdeme(t, t.TotalAmount, seedStoreAccountId, payerId,
                        today.AddDays(-3), PaymentChannel.Eft,
                        $"{t.ChargeNo} ödeme bildirimi",
                        status: PaymentStatus.Rejected,
                        rejectionReason: "Geçersiz veya okunamayan dekont görseli yüklendiği için bildirim onaylanmadı.");

                    await ctx.SaveChangesAsync();

                    if (dekontDocType != null && rejAllocs.Count > 0)
                    {
                        AttachPaymentReceiptDocument(rejAllocs[0], dekontDocType, pdfBytes, $"dekont_red_{t.ChargeNo}.pdf");
                        await ctx.SaveChangesAsync();
                    }
                }

                t.PaidAmount = 0m;
                t.Status = ChargeStatus.Overdue;
            }

            await ctx.SaveChangesAsync();

            // =========================================================================
            // 3. Geçmiş Yıl ve Cari Yıl Geçmiş Ayların Ödemeleri (%90 Oran)
            // =========================================================================
            await SeedGecmisYilOdemeleriAsync(currentYear - 1, 0.90, adminId, seedStoreAccountId);
            await SeedGecmisAylarOdemeleriAsync(currentYear, currentMonth, 0.90, adminId, seedStoreAccountId);

            await ctx.SaveChangesAsync();

            // Tüm ödemeler için eksik dekont belgelerini Blank.pdf ile ekle
            if (dekontDocType != null)
            {
                var allPayments = await ctx.PaymentAllocations.ToListAsync();
                var existingDocPaymentIds = (await ctx.Belgeler
                    .Where(b => b.OwnerType == DocumentOwnerType.Payment)
                    .Select(b => b.OwnerId)
                    .ToListAsync()).ToHashSet();

                var missingDocPayments = allPayments.Where(p => !existingDocPaymentIds.Contains(p.Id)).ToList();
                foreach (var p in missingDocPayments)
                {
                    var fileName = p.Status == PaymentStatus.Rejected
                        ? $"dekont_red_{p.PaymentNo}.pdf"
                        : $"dekont_{p.PaymentNo}.pdf";

                    ctx.Belgeler.Add(new Document
                    {
                        DocumentTypeId = dekontDocType.Id,
                        OwnerType = DocumentOwnerType.Payment,
                        OwnerId = p.Id,
                        FileName = fileName,
                        MimeType = "application/pdf",
                        FileSize = pdfBytes.Length,
                        CreatedAt = p.PaymentDate,
                        CreatedBy = p.CreatedByUserId,
                        IsActive = true,
                        IsDeleted = false,
                        Content = new DocumentContent
                        {
                            Content = pdfBytes
                        }
                    });
                }
                await ctx.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in SeedTahakkuklarVeOdemelerAsync: {ex.Message}");
            throw;
        }
    }

    private static string GetTenantUserId(int tenantId,
        int yzId, string? yzUserId,
        int megaId, string? megaUserId,
        int biotechId, string? biotechUserId,
        int egeId, string? egeUserId,
        int agroId, string? agroUserId,
        string adminId)
    {
        if (tenantId == yzId) return yzUserId ?? adminId;
        if (tenantId == megaId) return megaUserId ?? adminId;
        if (tenantId == biotechId) return biotechUserId ?? adminId;
        if (tenantId == egeId) return egeUserId ?? adminId;
        if (tenantId == agroId) return agroUserId ?? adminId;
        return adminId;
    }


    private async Task SeedGecmisYilOdemeleriAsync(int yil, double oran, string adminId, int storeAccountId)
    {
        var query = ctx.Charges
            .Include(t => t.LineItems)
            .Where(t => t.PeriodStart.Year == yil && t.Status == ChargeStatus.Pending);

        var tahakkuklar = await query.ToListAsync();
        if (!tahakkuklar.Any()) return;

        int odenecekAdet = (int)Math.Round(tahakkuklar.Count * oran);
        var secilenler = tahakkuklar.OrderBy(x => Guid.NewGuid()).Take(odenecekAdet).ToList();

        foreach (var t in secilenler)
        {
            var paymentDate = t.DueDate.AddDays(Random.Shared.Next(-5, 5));
            var paymentChannel = (PaymentChannel)Random.Shared.Next(1, 5);
            var description = $"{yil} dönemi onaylı tahakkuk ödemesi";

            DagitKalemBazliOdeme(t, t.TotalAmount, storeAccountId, adminId, paymentDate, paymentChannel, description,
                status: PaymentStatus.Approved, approvedByUserId: adminId, approvalDate: paymentDate);

            t.PaidAmount = t.TotalAmount;
            t.Status = ChargeStatus.Paid;
        }

        var unpaidGecmisYil = tahakkuklar.Except(secilenler).ToList();
        foreach (var t in unpaidGecmisYil)
        {
            t.Status = ChargeStatus.Overdue;
        }
    }

    private async Task SeedGecmisAylarOdemeleriAsync(int yil, int cariAy, double oran, string adminId, int storeAccountId)
    {
        var query = ctx.Charges
            .Include(t => t.LineItems)
            .Where(t => t.PeriodStart.Year == yil
                && t.PeriodStart.Month < cariAy
                && t.Status == ChargeStatus.Pending);

        var tahakkuklar = await query.ToListAsync();
        if (!tahakkuklar.Any()) return;

        int odenecekAdet = (int)Math.Round(tahakkuklar.Count * oran);
        var secilenler = tahakkuklar.OrderBy(x => Guid.NewGuid()).Take(odenecekAdet).ToList();

        foreach (var t in secilenler)
        {
            var paymentDate = t.DueDate.AddDays(Random.Shared.Next(-3, 3));
            var paymentChannel = (PaymentChannel)Random.Shared.Next(1, 5);
            var description = $"{t.PeriodStart:MMMM yyyy} dönemi onaylı tahakkuk ödemesi";

            DagitKalemBazliOdeme(t, t.TotalAmount, storeAccountId, adminId, paymentDate, paymentChannel, description,
                status: PaymentStatus.Approved, approvedByUserId: adminId, approvalDate: paymentDate);

            t.PaidAmount = t.TotalAmount;
            t.Status = ChargeStatus.Paid;
        }

        var unpaidGecmisAylar = tahakkuklar.Except(secilenler).ToList();
        foreach (var t in unpaidGecmisAylar)
        {
            if (t.DueDate < DateTime.Today)
            {
                t.Status = ChargeStatus.Overdue;
            }
        }
    }

    private void AttachPaymentReceiptDocument(
        PaymentAllocation payment,
        DocumentType docType,
        byte[] content,
        string fileName)
    {
        var doc = new Document
        {
            DocumentTypeId = docType.Id,
            OwnerType = DocumentOwnerType.Payment,
            OwnerId = payment.Id,
            FileName = fileName,
            MimeType = "application/pdf",
            FileSize = content.Length,
            CreatedAt = payment.PaymentDate,
            CreatedBy = payment.CreatedByUserId,
            IsActive = true,
            IsDeleted = false,
            Content = new DocumentContent
            {
                Content = content
            }
        };
        ctx.Belgeler.Add(doc);
    }

    /// <summary>
    /// Bir tahakkuk için toplam ödeme tutarını, kalemlerin ToplamTutar oranına göre her
    /// kaleme bir PaymentAllocation olarak dağıtır. Kalem bazlı ödeme çekirdeğinde her
    /// PaymentAllocation tek bir ChargeLineItem'a bağlı olmak zorunda olduğu için seed
    /// verisi de bu invariant'a uymalıdır; tahmine dayalı tek-kalem ataması yapılmaz.
    /// </summary>
    private List<PaymentAllocation> DagitKalemBazliOdeme(
        Charge charge,
        decimal totalToPay,
        int storeAccountId,
        string adminId,
        DateTime paymentDate,
        PaymentChannel paymentChannel,
        string description,
        PaymentStatus status = PaymentStatus.Approved,
        string? approvedByUserId = null,
        DateTime? approvalDate = null,
        string? rejectionReason = null)
    {
        var result = new List<PaymentAllocation>();
        var lineItems = charge.LineItems.ToList();
        if (lineItems.Count == 0 || totalToPay <= 0) return result;

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

            var alloc = new PaymentAllocation
            {
                PaymentNo = PaymentNumberPolicy.FormatPaymentNo(++_paymentSeq),
                LeaseId = charge.LeaseId,
                ChargeId = charge.Id,
                ChargeLineItemId = lineItem.Id,
                StoreAccountId = storeAccountId,
                PaymentDate = paymentDate,
                Amount = share,
                PaymentChannel = paymentChannel,
                PaymentSourceType = PaymentSourceType.Manual,
                Status = status,
                Description = description,
                CreatedByUserId = adminId,
                ApprovedByUserId = status == PaymentStatus.Approved ? (approvedByUserId ?? adminId) : null,
                ApprovalDate = status == PaymentStatus.Approved ? (approvalDate ?? paymentDate) : null,
                RejectionReason = rejectionReason
            };
            ctx.PaymentAllocations.Add(alloc);
            result.Add(alloc);

            if (status == PaymentStatus.Approved)
            {
                lineItem.PaidAmount += share;
            }
        }
        return result;
    }


    private async Task SeedRezervasyonlarAsync()
    {
        var salonZ01 = await ctx.Units.FirstAsync(u => u.UnitNo == "Z01");
        var salonZ02 = await ctx.Units.FirstAsync(u => u.UnitNo == "Z02");
        var salonEA01 = await ctx.Units.FirstAsync(u => u.UnitNo == "EA01");
        var salonEA02 = await ctx.Units.FirstAsync(u => u.UnitNo == "EA02");
        var salonBZ01 = await ctx.Units.FirstAsync(u => u.UnitNo == "B-Z01");
        var salonBZ02 = await ctx.Units.FirstAsync(u => u.UnitNo == "B-Z02");
        var salonBEA01 = await ctx.Units.FirstAsync(u => u.UnitNo == "B-EA01");
        var salonDZ01 = await ctx.Units.FirstOrDefaultAsync(u => u.UnitNo == "D-Z01") ?? salonZ01;

        var yzCozum = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000001");
        var megaFinans = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000002");
        var biotech = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000003");
        var egeLojistik = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000004");
        var agroGida = await ctx.Tenants.FirstAsync(t => t.TenantNo == "KRC-000005");

        var admin = await userManager.FindByEmailAsync(IdentitySeedService.AdminEmail);
        var adminId = admin?.Id ?? "admin-id";
        var yzUser = await userManager.FindByEmailAsync("ahmet.yilmaz@yz.com");
        var megaUser = await userManager.FindByEmailAsync("mehmet.demir@megafinans.com");
        var biotechUser = await userManager.FindByEmailAsync("ayse.kaya@biotech.com");
        var egeUser = await userManager.FindByEmailAsync("kemal.sahin@egelojistik.com");
        var agroUser = await userManager.FindByEmailAsync("fatma.celik@agrogida.com");

        var btToplanti = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "TOPLANTI");
        var btEtkinlik = await ctx.ChargeTypes.FirstOrDefaultAsync(b => b.Code == "ETKINLIK") ?? btToplanti;
        if (btToplanti == null) return;

        var seedStoreAccountId = await ctx.Stores
            .Where(s => s.Code == "STR-REZERVASYON" || s.Code == "SEED")
            .SelectMany(s => s.Accounts)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();
        if (seedStoreAccountId == 0)
            seedStoreAccountId = await ctx.StoreAccounts.Select(a => a.Id).FirstOrDefaultAsync();

        var today = DateTime.Today;

        // Local helper to create and add a reservation with an owner attendee
        Reservation CreateReservation(
            int seq,
            Unit unit,
            Tenant tenant,
            ApplicationUser? reqUser,
            DateTime start,
            DateTime end,
            int totalMin,
            int freeMin,
            int paidMin,
            decimal unitRate,
            decimal rateAmount,
            decimal kdvRate,
            decimal kdvAmount,
            decimal totalAmount,
            ReservationStatus status,
            string title,
            string desc,
            string? approvedBy = null,
            DateTime? approvedAt = null,
            string? rejectedBy = null,
            DateTime? rejectedAt = null,
            string? rejectionReason = null,
            string? cancelledBy = null,
            DateTime? cancelledAt = null,
            string? cancellationReason = null,
            DateTime? completedAt = null)
        {
            var rzv = new Reservation
            {
                ReservationNo = ReservationNumberPolicy.FormatReservationNo(seq),
                UnitId = unit.Id,
                TenantId = tenant.Id,
                StartDate = start,
                EndDate = end,
                TotalDurationMinutes = totalMin,
                FreeDurationMinutes = freeMin,
                PaidDurationMinutes = paidMin,
                UnitRate = unitRate,
                RateAmount = rateAmount,
                KdvRate = kdvRate,
                KdvAmount = kdvAmount,
                TotalAmount = totalAmount,
                Status = status,
                Title = title,
                Description = desc,
                RequestedByUserId = reqUser?.Id,
                RequestedByDisplayNameSnapshot = reqUser?.AdSoyad ?? tenant.Name,
                RequestedByEmailSnapshot = reqUser?.Email ?? tenant.Email,
                ApprovedByUserId = approvedBy,
                ApprovedAt = approvedAt,
                RejectedByUserId = rejectedBy,
                RejectedAt = rejectedAt,
                RejectionReason = rejectionReason,
                CancelledByUserId = cancelledBy,
                CancelledAt = cancelledAt,
                CancellationReason = cancellationReason,
                CompletedAt = completedAt
            };

            // Owner attendee
            rzv.Attendees.Add(new ReservationAttendee
            {
                DisplayName = reqUser?.AdSoyad ?? tenant.Name,
                EmailAddress = reqUser?.Email ?? tenant.Email,
                NormalizedEmailAddress = (reqUser?.Email ?? tenant.Email).Trim().ToUpperInvariant(),
                IsReservationOwner = true
            });

            return rzv;
        }

        // Local helper to add extra attendee
        void AddGuestAttendee(Reservation rzv, string name, string email)
        {
            rzv.Attendees.Add(new ReservationAttendee
            {
                DisplayName = name,
                EmailAddress = email,
                NormalizedEmailAddress = email.Trim().ToUpperInvariant(),
                IsReservationOwner = false
            });
        }

        // Local helper for reservation charge
        Charge CreateCharge(int seq, Reservation rzv, Unit unit, Tenant tenant, ChargeType chargeType, decimal expectedAmount, decimal kdvRate, decimal kdvAmount, decimal totalAmount, decimal paidAmount, ChargeStatus status, DateTime dueDate)
        {
            return new Charge
            {
                ChargeNo = ChargeNumberPolicy.FormatChargeNo(800000 + seq),
                TenantId = tenant.Id,
                UnitId = unit.Id,
                ReservationId = rzv.Id,
                PeriodStart = rzv.StartDate,
                PeriodEnd = rzv.EndDate,
                DueDate = dueDate,
                ExpectedAmount = expectedAmount,
                KdvAmount = kdvAmount,
                TotalAmount = totalAmount,
                PaidAmount = paidAmount,
                Status = status,
                SourceType = ChargeSourceType.Reservation,
                LineItems = new List<ChargeLineItem>
                {
                    new ChargeLineItem
                    {
                        ChargeTypeId = chargeType.Id,
                        Description = $"{unit.Name} Rezervasyon Bedeli ({rzv.StartDate:dd.MM.yyyy HH:mm} – {rzv.EndDate:HH:mm})",
                        CalculationMethod = CalculationMethod.Fixed,
                        UnitValue = expectedAmount,
                        Multiplier = 1,
                        Amount = expectedAmount,
                        KdvRate = kdvRate,
                        KdvAmount = kdvAmount,
                        TotalAmount = totalAmount,
                        PaidAmount = paidAmount,
                        SourceType = LineItemSourceType.ReservationRule
                    }
                }
            };
        }

        // =========================================================================
        // SENARYO 1: Geçmiş, Tamamlanmış ve Tahakkuku Ödenmiş (3 Kayıt)
        // =========================================================================
        // 1. RZV-000001: Toplantı Salonu Z01 (Özel 600 TL, 30 dk ücretsiz, 150 dk ücretli -> 1.800 TL + 360 TL KDV = 2.160 TL)
        var r1 = CreateReservation(1, salonZ01, yzCozum, yzUser,
            today.AddDays(-15).AddHours(10), today.AddDays(-15).AddHours(13), 180, 30, 150, 600m, 1800m, 20m, 360m, 2160m,
            ReservationStatus.Completed, "Sprint Kapanış ve Retrospektif Toplantısı", "YZ Ar-Ge ekibi sprint tamamlama oturumu",
            approvedBy: adminId, approvedAt: today.AddDays(-16).AddHours(9), completedAt: today.AddDays(-15).AddHours(13));
        AddGuestAttendee(r1, "Sarp Mühendis", "sarp@yz.com");
        AddGuestAttendee(r1, "Ceyda Tasarımcı", "ceyda@yz.com");

        // 2. RZV-000002: A Blok Konferans Salonu EA01 (Genel 1.500 TL, 0 dk ücretsiz, 240 dk = 4 periyot -> 6.000 TL + 1.200 TL KDV = 7.200 TL)
        var r2 = CreateReservation(2, salonEA01, megaFinans, megaUser,
            today.AddDays(-12).AddHours(13), today.AddDays(-12).AddHours(17), 240, 0, 240, 1500m, 6000m, 20m, 1200m, 7200m,
            ReservationStatus.Completed, "Finansal Teknolojiler Çeyrek Değerlendirme Konferansı", "Yatırımcı ve danışmanlara özel sunum",
            approvedBy: adminId, approvedAt: today.AddDays(-14).AddHours(10), completedAt: today.AddDays(-12).AddHours(17));
        AddGuestAttendee(r2, "Sibel Finans", "sibel@megafinans.com");

        // 3. RZV-000003: B Blok Toplantı Salonu 2 B-Z02 (Genel 400 TL, 60 dk ücretsiz, 60 dk ücretli -> 400 TL + 80 TL KDV = 480 TL)
        var r3 = CreateReservation(3, salonBZ02, biotech, biotechUser,
            today.AddDays(-8).AddHours(9), today.AddDays(-8).AddHours(11), 120, 60, 60, 400m, 400m, 20m, 80m, 480m,
            ReservationStatus.Completed, "Klinik Deney Faz-1 Sonuçları Brifingi", "Araştırma hekimleri koordinasyon toplantısı",
            approvedBy: adminId, approvedAt: today.AddDays(-10).AddHours(14), completedAt: today.AddDays(-8).AddHours(11));

        // =========================================================================
        // SENARYO 2: Geçmiş, Tamamlanmış ve Tahakkuku Beklemede (Ödenmemiş) (3 Kayıt)
        // =========================================================================
        // 4. RZV-000004: B Blok Konferans ve Etkinlik Salonu B-EA01 (Özel 2.500 TL, 0 dk ücretsiz, 240 dk = 4 periyot -> 10.000 TL + 2.000 TL KDV = 12.000 TL)
        var r4 = CreateReservation(4, salonBEA01, agroGida, agroUser,
            today.AddDays(-5).AddHours(14), today.AddDays(-5).AddHours(18), 240, 0, 240, 2500m, 10000m, 20m, 2000m, 12000m,
            ReservationStatus.Completed, "Akıllı Tarım ve Biyoteknoloji Fuar Ön Lansmanı", "Bölgesel üretici birlikleri tanıtımı",
            approvedBy: adminId, approvedAt: today.AddDays(-7).AddHours(11), completedAt: today.AddDays(-5).AddHours(18));
        AddGuestAttendee(r4, "Hasan Ziraat", "hasan@agrogida.com");

        // 5. RZV-000005: A Blok Konferans Salonu EA01 (Genel 1.500 TL, 0 dk ücretsiz, 240 dk = 4 periyot -> 6.000 TL + 1.200 TL KDV = 7.200 TL)
        var r5 = CreateReservation(5, salonEA01, egeLojistik, egeUser,
            today.AddDays(-3).AddHours(10), today.AddDays(-3).AddHours(14), 240, 0, 240, 1500m, 6000m, 20m, 1200m, 7200m,
            ReservationStatus.Completed, "Uluslararası Tedarik Zinciri ve Soğuk Hava Lojistiği Paneli", "Sektör paydaşları çalıştayı",
            approvedBy: adminId, approvedAt: today.AddDays(-5).AddHours(16), completedAt: today.AddDays(-3).AddHours(14));

        // 6. RZV-000006: Toplantı Salonu Z01 (Özel 600 TL, 30 dk ücretsiz, 120 dk ücretli -> 1.200 TL + 240 TL KDV = 1.440 TL)
        var r6 = CreateReservation(6, salonZ01, megaFinans, megaUser,
            today.AddDays(-2).AddHours(15), today.AddDays(-2).AddHours(17).AddMinutes(30), 150, 30, 120, 600m, 1200m, 20m, 240m, 1440m,
            ReservationStatus.Completed, "Yeni ERP Entegrasyonu Güvenlik İncelemesi", "Dış sızma testi ve denetim toplantısı",
            approvedBy: adminId, approvedAt: today.AddDays(-4).AddHours(9), completedAt: today.AddDays(-2).AddHours(17).AddMinutes(30));

        // =========================================================================
        // SENARYO 3: Gelecek, Onaylanmış / Takvimde Aktif Rezervasyonlar (5 Kayıt)
        // =========================================================================
        // 7. RZV-000007: Toplantı Salonu Z01 (Özel 600 TL, 30 dk ücretsiz, 90 dk = 2 periyot -> 1.200 TL + 240 TL KDV = 1.440 TL)
        var r7 = CreateReservation(7, salonZ01, yzCozum, yzUser,
            today.AddDays(2).AddHours(10), today.AddDays(2).AddHours(12), 120, 30, 90, 600m, 1200m, 20m, 240m, 1440m,
            ReservationStatus.Confirmed, "Büyük Dil Modelleri Eğitimi ve Altyapı İncelemesi", "Mühendislik ekibi teknik semineri",
            approvedBy: adminId, approvedAt: today.AddDays(-1).AddHours(11));

        // 8. RZV-000008: A Blok Toplantı Odası Z02 (Ücretsiz Salon 1 - 0 TL)
        var r8 = CreateReservation(8, salonZ02, biotech, biotechUser,
            today.AddDays(3).AddHours(14), today.AddDays(3).AddHours(16).AddMinutes(30), 150, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Confirmed, "TÜBİTAK 1501 Proje İlerleme Raporu Hazırlığı", "Ücretsiz tahsis edilen toplantı salonunda çalışma",
            approvedBy: adminId, approvedAt: today.AddDays(-1).AddHours(14));

        // 9. RZV-000009: B Blok Konferans Salonu B-EA01 (Özel 2.500 TL, 0 dk ücretsiz, 240 dk = 4 periyot -> 10.000 TL + 2.000 TL KDV = 12.000 TL)
        var r9 = CreateReservation(9, salonBEA01, megaFinans, megaUser,
            today.AddDays(4).AddHours(13), today.AddDays(4).AddHours(17), 240, 0, 240, 2500m, 10000m, 20m, 2000m, 12000m,
            ReservationStatus.Confirmed, "Fintek Çözümleri Bayi ve Müşteri Zirvesi", "100+ katılımcılı kurumsal zirve",
            approvedBy: adminId, approvedAt: today.AddDays(-1).AddHours(16));

        // 10. RZV-000010: B Blok Toplantı Salonu 1 B-Z01 (Ücretsiz Salon 2 - 0 TL)
        var r10 = CreateReservation(10, salonBZ01, egeLojistik, egeUser,
            today.AddDays(5).AddHours(9).AddMinutes(30), today.AddDays(5).AddHours(12), 150, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Confirmed, "Sürücü ve Filo Yöneticileri Güvenlik Bilgilendirmesi", "Ücretsiz toplantı salonu kota kullanımı",
            approvedBy: adminId, approvedAt: today.AddDays(-1).AddHours(17));

        // 11. RZV-000011: B Blok Toplantı Salonu 2 B-Z02 (Genel 400 TL, 60 dk ücretsiz, 60 dk = 1 periyot -> 400 TL + 80 TL KDV = 480 TL)
        var r11 = CreateReservation(11, salonBZ02, agroGida, agroUser,
            today.AddDays(6).AddHours(15), today.AddDays(6).AddHours(17), 120, 60, 60, 400m, 400m, 20m, 80m, 480m,
            ReservationStatus.Confirmed, "Organik Tohum Islahı Yıllık Değerlendirme Oturumu", "Ar-Ge araştırmacıları koordinasyonu",
            approvedBy: adminId, approvedAt: today.AddDays(-1).AddHours(18));

        // =========================================================================
        // SENARYO 4: Gelecek, Yönetici Onayı Bekleyen Rezervasyon Talepleri (3 Kayıt)
        // =========================================================================
        // 12. RZV-000012: B-EA01 (Özel 2.500 TL, 300 dk = 5 periyot -> 12.500 TL + 2.500 TL KDV = 15.000 TL)
        var r12 = CreateReservation(12, salonBEA01, biotech, biotechUser,
            today.AddDays(7).AddHours(10), today.AddDays(7).AddHours(15), 300, 0, 300, 2500m, 12500m, 20m, 2500m, 15000m,
            ReservationStatus.PendingApproval, "Biyoteknoloji Uluslararası Bilim ve İnovasyon Zirvesi", "Onay bekleyen etkinlik salonu tahsis talebi");

        // 13. RZV-000013: A Blok EA01 (Genel 1.500 TL, 180 dk = 3 periyot -> 4.500 TL + 900 TL KDV = 5.400 TL)
        var r13 = CreateReservation(13, salonEA01, yzCozum, yzUser,
            today.AddDays(8).AddHours(13), today.AddDays(8).AddHours(16), 180, 0, 180, 1500m, 4500m, 20m, 900m, 5400m,
            ReservationStatus.PendingApproval, "Yapay Zeka ve Büyük Veri Kampüs Hackathonu", "Üniversite-sanayi iş birliği etkinliği talebi");

        // 14. RZV-000014: Toplantı Salonu Z01 (Özel 600 TL, 30 dk ücretsiz, 90 dk = 2 periyot -> 1.200 TL + 240 TL KDV = 1.440 TL)
        var r14 = CreateReservation(14, salonZ01, agroGida, agroUser,
            today.AddDays(9).AddHours(11), today.AddDays(9).AddHours(13), 120, 30, 90, 600m, 1200m, 20m, 240m, 1440m,
            ReservationStatus.PendingApproval, "Topraksız Tarım Otomasyonu Proje Onay Toplantısı", "Teknokent yönetimi onayına sunulan talep");

        // =========================================================================
        // SENARYO 5: Yönetici Tarafından Reddedilmiş Rezervasyonlar (3 Kayıt)
        // =========================================================================
        // 15. RZV-000015: Toplantı Salonu Z01
        var r15 = CreateReservation(15, salonZ01, egeLojistik, egeUser,
            today.AddDays(10).AddHours(14), today.AddDays(10).AddHours(16), 120, 30, 90, 600m, 1200m, 20m, 240m, 1440m,
            ReservationStatus.Rejected, "Ege Lojistik Tedarikçi Koordinasyon Toplantısı", "Yönetici tarafından reddedilen talep",
            rejectedBy: adminId, rejectedAt: today.AddDays(-1), rejectionReason: "Aynı saat diliminde planlı bina iklimlendirme ve altyapı testi mevcuttur.");

        // 16. RZV-000016: B Blok Konferans ve Etkinlik Salonu B-EA01
        var r16 = CreateReservation(16, salonBEA01, yzCozum, yzUser,
            today.AddDays(11).AddHours(9), today.AddDays(11).AddHours(13), 240, 0, 240, 2500m, 10000m, 20m, 2000m, 12000m,
            ReservationStatus.Rejected, "Yapay Zeka Girişimcilik ve Melek Yatırımcı Zirvesi", "Yönetici tarafından reddedilen talep",
            rejectedBy: adminId, rejectedAt: today.AddDays(-1), rejectionReason: "Talep edilen saat aralığında salon Teknokent Yönetim Kurulu genel kuruluna tahsis edilmiştir.");

        // 17. RZV-000017: A Blok Fuaye ve Sergi Alanı EA02
        var r17 = CreateReservation(17, salonEA02, megaFinans, megaUser,
            today.AddDays(12).AddHours(13), today.AddDays(12).AddHours(18), 300, 0, 300, 1500m, 7500m, 20m, 1500m, 9000m,
            ReservationStatus.Rejected, "Fintek Demo Günü ve Prototip Sergisi", "Yönetici tarafından reddedilen fuaye talebi",
            rejectedBy: adminId, rejectedAt: today.AddDays(-1), rejectionReason: "Fuaye alanında aynı gün AFAD ve İtfaiye koordinasyonunda acil durum tatbikatı planlanmıştır.");

        // =========================================================================
        // SENARYO 6: Kiracı / Yönetici Tarafından İptal Edilmiş Rezervasyonlar (3 Kayıt)
        // =========================================================================
        // 18. RZV-000018: Toplantı Salonu Z01 (Kiracı İptali)
        var r18 = CreateReservation(18, salonZ01, biotech, biotechUser,
            today.AddDays(13).AddHours(10), today.AddDays(13).AddHours(12), 120, 30, 90, 600m, 1200m, 20m, 240m, 1440m,
            ReservationStatus.Cancelled, "Klinik Araştırma Heyeti Ön Değerlendirme Toplantısı", "Kiracı tarafından iptal edilen rezervasyon",
            cancelledBy: biotechUser?.Id ?? adminId, cancelledAt: today.AddDays(-1), cancellationReason: "Yurt dışı klinik denetçilerin seyahat programı değiştiği için iptal edilmiştir.");

        // 19. RZV-000019: B Blok Konferans Salonu B-EA01 (Kiracı İptali)
        var r19 = CreateReservation(19, salonBEA01, agroGida, agroUser,
            today.AddDays(14).AddHours(14), today.AddDays(14).AddHours(17), 180, 0, 180, 2500m, 7500m, 20m, 1500m, 9000m,
            ReservationStatus.Cancelled, "Akıllı Tarım Uygulamaları Lansmanı", "Kiracı tarafından iptal edilen rezervasyon",
            cancelledBy: agroUser?.Id ?? adminId, cancelledAt: today.AddDays(-1), cancellationReason: "Saha hasat çalışmalarındaki yoğunluk sebebiyle organizasyon bir sonraki aya ertelenmiştir.");

        // 20. RZV-000020: A Blok Konferans Salonu EA01 (Yönetici İptali)
        var r20 = CreateReservation(20, salonEA01, megaFinans, megaUser,
            today.AddDays(15).AddHours(9), today.AddDays(15).AddHours(12), 180, 0, 180, 1500m, 4500m, 20m, 900m, 5400m,
            ReservationStatus.Cancelled, "Fintek Hackathon Hazırlık Kampı", "Yönetici tarafından teknik sebeple iptal edildi",
            cancelledBy: adminId, cancelledAt: today.AddDays(-1), cancellationReason: "Yönetim tarafından bina genelinde trafo yenileme çalışması nedeniyle salon kapatılmıştır.");

        // =========================================================================
        // SENARYO 7: Tamamen Ücretsiz Kota Kapsamındaki Rezervasyonlar (3 Kayıt - 0 TL)
        // =========================================================================
        // 21. RZV-000021: A Blok Toplantı Odası Z02 (Ücretsiz Salon 1)
        var r21 = CreateReservation(21, salonZ02, yzCozum, yzUser,
            today.AddDays(3).AddHours(10), today.AddDays(3).AddHours(11).AddMinutes(30), 90, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Confirmed, "YZ Çözümleri Haftalık Sprint Planlama", "Tamamen ücretsiz oda tahsisi",
            approvedBy: adminId, approvedAt: today.AddDays(-1));

        // 22. RZV-000022: B Blok Toplantı Salonu 1 B-Z01 (Ücretsiz Salon 2)
        var r22 = CreateReservation(22, salonBZ01, biotech, biotechUser,
            today.AddDays(4).AddHours(11), today.AddDays(4).AddHours(12).AddMinutes(30), 90, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Confirmed, "BioTech Patent Ön İnceleme Görüşmesi", "Tamamen ücretsiz oda tahsisi",
            approvedBy: adminId, approvedAt: today.AddDays(-1));

        // 23. RZV-000023: B Blok Toplantı Salonu 2 B-Z02 (Standart salon - 45 dk < 60 dk ücretsiz kota)
        var r23 = CreateReservation(23, salonBZ02, egeLojistik, egeUser,
            today.AddDays(5).AddHours(14), today.AddDays(5).AddHours(14).AddMinutes(45), 45, 60, 0, 400m, 0m, 20m, 0m, 0m,
            ReservationStatus.Confirmed, "Ege Lojistik Hızlı Rota Senkronizasyon Toplantısı", "Ücretsiz 60 dk kota içinde kalan 45 dk oturum",
            approvedBy: adminId, approvedAt: today.AddDays(-1));

        // =========================================================================
        // SENARYO 8: Diğer Aylar İçin Rezervasyonlar ve Tahakkukları (Mayıs - Ekim 2026)
        // =========================================================================
        // --- Mayıs 2026 ---
        // 24. RZV-000024: Toplantı Salonu Z01 (Tamamlanmış, Ödenmiş)
        var r24 = CreateReservation(24, salonZ01, yzCozum, yzUser,
            new DateTime(2026, 5, 12, 10, 0, 0), new DateTime(2026, 5, 12, 13, 0, 0), 180, 30, 150, 600m, 1800m, 20m, 360m, 2160m,
            ReservationStatus.Completed, "Bahar Dönemi Yapay Zeka Ar-Ge Çıktıları Sunumu", "TÜBİTAK izleme komitesi ara değerlendirme toplantısı",
            approvedBy: adminId, approvedAt: new DateTime(2026, 5, 10, 11, 0, 0), completedAt: new DateTime(2026, 5, 12, 13, 0, 0));
        AddGuestAttendee(r24, "Prof. Dr. İlker Danışman", "ilker@ege.edu.tr");

        // 25. RZV-000025: A Blok Konferans Salonu EA01 (Tamamlanmış, Ödenmiş)
        var r25 = CreateReservation(25, salonEA01, megaFinans, megaUser,
            new DateTime(2026, 5, 18, 13, 0, 0), new DateTime(2026, 5, 18, 17, 0, 0), 240, 0, 240, 1500m, 6000m, 20m, 1200m, 7200m,
            ReservationStatus.Completed, "Açık Bankacılık ve API Ekosistemi Semineri", "Finans sektörü entegratörleri buluşması",
            approvedBy: adminId, approvedAt: new DateTime(2026, 5, 15, 14, 0, 0), completedAt: new DateTime(2026, 5, 18, 17, 0, 0));

        // 26. RZV-000026: A Blok Toplantı Odası Z02 (Tamamlanmış, Ücretsiz Kota)
        var r26 = CreateReservation(26, salonZ02, biotech, biotechUser,
            new DateTime(2026, 5, 22, 14, 0, 0), new DateTime(2026, 5, 22, 16, 0, 0), 120, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Completed, "Biyoteknoloji Laboratuvar Güvenlik Protokolü Eğitimi", "Stajyer ve araştırmacılar için ücretsiz toplantı salonu oturumu",
            approvedBy: adminId, approvedAt: new DateTime(2026, 5, 20, 10, 0, 0), completedAt: new DateTime(2026, 5, 22, 16, 0, 0));

        // 27. RZV-000027: B Blok Toplantı Salonu 1 B-Z01 (Reddedilmiş)
        var r27 = CreateReservation(27, salonBZ01, egeLojistik, egeUser,
            new DateTime(2026, 5, 28, 9, 0, 0), new DateTime(2026, 5, 28, 12, 0, 0), 180, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Rejected, "Ege Lojistik Saha Şefleri Koordinasyon Toplantısı", "Reddedilen rezervasyon talebi",
            rejectedBy: adminId, rejectedAt: new DateTime(2026, 5, 26, 15, 0, 0), rejectionReason: "Belirtilen saat aralığında salonda planlı ses ve projeksiyon sistemi yenileme çalışması bulunmaktadır.");

        // --- Haziran 2026 ---
        // 28. RZV-000028: B Blok Konferans ve Etkinlik Salonu B-EA01 (Tamamlanmış, Ödenmiş)
        var r28 = CreateReservation(28, salonBEA01, agroGida, agroUser,
            new DateTime(2026, 6, 8, 14, 0, 0), new DateTime(2026, 6, 8, 18, 0, 0), 240, 0, 240, 2500m, 10000m, 20m, 2000m, 12000m,
            ReservationStatus.Completed, "Sürdürülebilir Tarım ve Gıda Teknolojileri Zirvesi", "Ege bölgesi kooperatif temsilcileri ve üretici konferansı",
            approvedBy: adminId, approvedAt: new DateTime(2026, 6, 5, 10, 0, 0), completedAt: new DateTime(2026, 6, 8, 18, 0, 0));
        AddGuestAttendee(r28, "Dr. Mehmet Toprak", "mehmet@tarim.gov.tr");

        // 29. RZV-000029: B Blok Toplantı Salonu 2 B-Z02 (Tamamlanmış, Ödenmiş)
        var r29 = CreateReservation(29, salonBZ02, biotech, biotechUser,
            new DateTime(2026, 6, 15, 10, 0, 0), new DateTime(2026, 6, 15, 12, 0, 0), 120, 60, 60, 400m, 400m, 20m, 80m, 480m,
            ReservationStatus.Completed, "Mikrobiyoloji Analiz Ekipmanı Tedarikçi Görüşmesi", "Alman biyosensör üreticisi ile teknik toplantı",
            approvedBy: adminId, approvedAt: new DateTime(2026, 6, 12, 16, 0, 0), completedAt: new DateTime(2026, 6, 15, 12, 0, 0));

        // 30. RZV-000030: B Blok Toplantı Salonu 1 B-Z01 (Tamamlanmış, Ücretsiz Kota)
        var r30 = CreateReservation(30, salonBZ01, yzCozum, yzUser,
            new DateTime(2026, 6, 20, 11, 0, 0), new DateTime(2026, 6, 20, 12, 30, 0), 90, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Completed, "Yaz Dönemi Stajyer Oryantasyonu", "Ücretsiz oda tahsisi oturumu",
            approvedBy: adminId, approvedAt: new DateTime(2026, 6, 18, 14, 0, 0), completedAt: new DateTime(2026, 6, 20, 12, 30, 0));

        // 31. RZV-000031: Toplantı Salonu Z01 (İptal Edilmiş)
        var r31 = CreateReservation(31, salonZ01, megaFinans, megaUser,
            new DateTime(2026, 6, 25, 13, 0, 0), new DateTime(2026, 6, 25, 15, 0, 0), 120, 30, 90, 600m, 1200m, 20m, 240m, 1440m,
            ReservationStatus.Cancelled, "Denetim Kurulu Çeyrek Sonu Brifingi", "Kiracı tarafından iptal edilen toplantı",
            cancelledBy: megaUser?.Id ?? adminId, cancelledAt: new DateTime(2026, 6, 24, 11, 0, 0), cancellationReason: "Denetim kurulu üyelerinin seyahat çakışması nedeniyle ileri tarihe ertelenmiştir.");

        // --- Temmuz 2026 ---
        // 32. RZV-000032: Toplantı Salonu Z01 (Tamamlanmış, Ödenmiş)
        var r32 = CreateReservation(32, salonZ01, yzCozum, yzUser,
            new DateTime(2026, 7, 7, 9, 30, 0), new DateTime(2026, 7, 7, 12, 30, 0), 180, 30, 150, 600m, 1800m, 20m, 360m, 2160m,
            ReservationStatus.Completed, "Yapay Zeka Tabanlı Tahmin Modelleri Demo Oturumu", "Kurumsal müşterilere yeni algoritma yeteneklerinin sunumu",
            approvedBy: adminId, approvedAt: new DateTime(2026, 7, 4, 11, 0, 0), completedAt: new DateTime(2026, 7, 7, 12, 30, 0));

        // 33. RZV-000033: A Blok Konferans Salonu EA01 (Tamamlanmış, Ödenmiş)
        var r33 = CreateReservation(33, salonEA01, megaFinans, megaUser,
            new DateTime(2026, 7, 14, 13, 0, 0), new DateTime(2026, 7, 14, 17, 0, 0), 240, 0, 240, 1500m, 6000m, 20m, 1200m, 7200m,
            ReservationStatus.Completed, "Kurumsal Risk Yönetimi ve Mevzuata Uyum Sempozyumu", "Bankacılık ve sigorta temsilcileri çalıştayı",
            approvedBy: adminId, approvedAt: new DateTime(2026, 7, 11, 15, 0, 0), completedAt: new DateTime(2026, 7, 14, 17, 0, 0));

        // 34. RZV-000034: B Blok Konferans ve Etkinlik Salonu B-EA01 (Tamamlanmış, Ödenmiş)
        var r34 = CreateReservation(34, salonBEA01, agroGida, agroUser,
            new DateTime(2026, 7, 21, 14, 0, 0), new DateTime(2026, 7, 21, 18, 0, 0), 240, 0, 240, 2500m, 10000m, 20m, 2000m, 12000m,
            ReservationStatus.Completed, "Akıllı Sera ve IoT Sensör Çözümleri Lansmanı", "Zirai girişimciler ve yatırımcı buluşması",
            approvedBy: adminId, approvedAt: new DateTime(2026, 7, 18, 10, 0, 0), completedAt: new DateTime(2026, 7, 21, 18, 0, 0));

        // 35. RZV-000035: A Blok Toplantı Odası Z02 (Tamamlanmış, Ücretsiz Kota)
        var r35 = CreateReservation(35, salonZ02, egeLojistik, egeUser,
            new DateTime(2026, 7, 27, 10, 0, 0), new DateTime(2026, 7, 27, 11, 30, 0), 90, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Completed, "Ege Lojistik Yaz Dönemi Filo Rotalama Senkronizasyonu", "Ücretsiz oda kotası kullanımı",
            approvedBy: adminId, approvedAt: new DateTime(2026, 7, 25, 14, 0, 0), completedAt: new DateTime(2026, 7, 27, 11, 30, 0));

        // --- Ağustos 2026 ---
        // 36. RZV-000036: B Blok Konferans ve Etkinlik Salonu B-EA01 (Tamamlanmış, Ödenmiş)
        var r36 = CreateReservation(36, salonBEA01, biotech, biotechUser,
            new DateTime(2026, 8, 5, 10, 0, 0), new DateTime(2026, 8, 5, 14, 0, 0), 240, 0, 240, 2500m, 10000m, 20m, 2000m, 12000m,
            ReservationStatus.Completed, "Moleküler Biyoloji Yaz Semineri ve Uygulamalı Atölye", "Akademik araştırmacılar ve sanayi ortakları çalıştayı",
            approvedBy: adminId, approvedAt: new DateTime(2026, 8, 2, 9, 0, 0), completedAt: new DateTime(2026, 8, 5, 14, 0, 0));

        // 37. RZV-000037: A Blok Konferans Salonu EA01 (Tamamlanmış, Ödenmiş)
        var r37 = CreateReservation(37, salonEA01, egeLojistik, egeUser,
            new DateTime(2026, 8, 12, 14, 0, 0), new DateTime(2026, 8, 12, 17, 0, 0), 180, 0, 180, 1500m, 4500m, 20m, 900m, 5400m,
            ReservationStatus.Completed, "Uluslararası Karayolu Taşımacılığı ve Yeşil Lojistik Konferansı", "Karbon ayak izi azaltma ve filo optimizasyonu sunumları",
            approvedBy: adminId, approvedAt: new DateTime(2026, 8, 9, 14, 0, 0), completedAt: new DateTime(2026, 8, 12, 17, 0, 0));

        // 38. RZV-000038: Teknokent D Blok Seminer ve Toplantı Odası D-Z01 (Tamamlanmış, Ödenmiş)
        var r38 = CreateReservation(38, salonDZ01, yzCozum, yzUser,
            new DateTime(2026, 8, 18, 9, 0, 0), new DateTime(2026, 8, 18, 12, 0, 0), 180, 60, 120, 400m, 800m, 20m, 160m, 960m,
            ReservationStatus.Completed, "Kuluçka Girişimleri Teknik Mentorluk Oturumu", "Teknokent D Blok yeni seminer salonunda kuluçka ekipleriyle buluşma",
            approvedBy: adminId, approvedAt: new DateTime(2026, 8, 15, 11, 0, 0), completedAt: new DateTime(2026, 8, 18, 12, 0, 0));

        // 39. RZV-000039: B Blok Toplantı Salonu 2 B-Z02 (Tamamlanmış, Gecikmiş / Ödenmemiş)
        var r39 = CreateReservation(39, salonBZ02, megaFinans, megaUser,
            new DateTime(2026, 8, 24, 15, 0, 0), new DateTime(2026, 8, 24, 17, 0, 0), 120, 60, 60, 400m, 400m, 20m, 80m, 480m,
            ReservationStatus.Completed, "Yazılım Entegrasyon Ekipleri Sprint İncelemesi", "Geçmiş dönem tamamlanmış ancak tahakkuku vadesi geçmiş kayıt",
            approvedBy: adminId, approvedAt: new DateTime(2026, 8, 21, 16, 0, 0), completedAt: new DateTime(2026, 8, 24, 17, 0, 0));

        // 40. RZV-000040: B Blok Toplantı Salonu 1 B-Z01 (Tamamlanmış, Ücretsiz Kota)
        var r40 = CreateReservation(40, salonBZ01, agroGida, agroUser,
            new DateTime(2026, 8, 27, 11, 0, 0), new DateTime(2026, 8, 27, 12, 30, 0), 90, 480, 0, 0m, 0m, 20m, 0m, 0m,
            ReservationStatus.Completed, "Tarım ve Gıda İnovasyon Grubu Aylık Değerlendirme", "Ücretsiz oda tahsisi oturumu",
            approvedBy: adminId, approvedAt: new DateTime(2026, 8, 25, 13, 0, 0), completedAt: new DateTime(2026, 8, 27, 12, 30, 0));

        // --- Ekim 2026 (Gelecek Rezervasyonlar) ---
        // 41. RZV-000041: Teknokent D Blok Seminer Odası D-Z01 (Onaylanmış)
        var r41 = CreateReservation(41, salonDZ01, agroGida, agroUser,
            new DateTime(2026, 10, 8, 10, 0, 0), new DateTime(2026, 10, 8, 13, 0, 0), 180, 60, 120, 400m, 800m, 20m, 160m, 960m,
            ReservationStatus.Confirmed, "Gıda Girişimleri Hızlandırıcı Programı Tanıtımı", "D Blok seminer salonunda Ekim ayı etkinliği",
            approvedBy: adminId, approvedAt: new DateTime(2026, 9, 28, 10, 0, 0));

        // 42. RZV-000042: B Blok Konferans ve Etkinlik Salonu B-EA01 (Onaylanmış)
        var r42 = CreateReservation(42, salonBEA01, yzCozum, yzUser,
            new DateTime(2026, 10, 15, 13, 0, 0), new DateTime(2026, 10, 15, 17, 0, 0), 240, 0, 240, 2500m, 10000m, 20m, 2000m, 12000m,
            ReservationStatus.Confirmed, "Yapay Zeka ve Bulut Mimarileri Sonbahar Zirvesi", "120+ katılımcılı bölgesel teknoloji konferansı",
            approvedBy: adminId, approvedAt: new DateTime(2026, 9, 29, 14, 0, 0));

        // 43. RZV-000043: A Blok Konferans Salonu EA01 (Onay Bekliyor)
        var r43 = CreateReservation(43, salonEA01, megaFinans, megaUser,
            new DateTime(2026, 10, 22, 9, 0, 0), new DateTime(2026, 10, 22, 12, 0, 0), 180, 0, 180, 1500m, 4500m, 20m, 900m, 5400m,
            ReservationStatus.PendingApproval, "Sermaye Piyasaları ve Blokzincir Entegrasyonu Paneli", "Ekim ayı için onay bekleyen konferans salonu tahsis talebi");

        ctx.Reservations.AddRange(
            r1, r2, r3, r4, r5, r6, r7, r8, r9, r10,
            r11, r12, r13, r14, r15, r16, r17, r18, r19, r20,
            r21, r22, r23, r24, r25, r26, r27, r28, r29, r30,
            r31, r32, r33, r34, r35, r36, r37, r38, r39, r40,
            r41, r42, r43);
        await ctx.SaveChangesAsync();

        // Tahakkuklar ve Ödemeler
        // Senaryo 1: Eylül Ayı - Geçmiş, Tamamlanmış ve Tahakkuku Ödenmiş (r1, r2, r3)
        var c1 = CreateCharge(1, r1, salonZ01, yzCozum, btToplanti, 1800m, 20m, 360m, 2160m, 0m, ChargeStatus.Pending, r1.EndDate.Date);
        var c2 = CreateCharge(2, r2, salonEA01, megaFinans, btEtkinlik!, 6000m, 20m, 1200m, 7200m, 0m, ChargeStatus.Pending, r2.EndDate.Date);
        var c3 = CreateCharge(3, r3, salonBZ02, biotech, btToplanti, 400m, 20m, 80m, 480m, 0m, ChargeStatus.Pending, r3.EndDate.Date);

        // Senaryo 2: Eylül Ayı - Geçmiş, Tamamlanmış ve Tahakkuku Beklemede (Ödenmemiş) (r4, r5, r6)
        var c4 = CreateCharge(4, r4, salonBEA01, agroGida, btEtkinlik!, 10000m, 20m, 2000m, 12000m, 0m, ChargeStatus.Pending, today.AddDays(10));
        var c5 = CreateCharge(5, r5, salonEA01, egeLojistik, btEtkinlik!, 6000m, 20m, 1200m, 7200m, 0m, ChargeStatus.Pending, today.AddDays(12));
        var c6 = CreateCharge(6, r6, salonZ01, megaFinans, btToplanti, 1200m, 20m, 240m, 1440m, 0m, ChargeStatus.Pending, today.AddDays(13));

        // Diğer Aylar: Mayıs, Haziran, Temmuz, Ağustos Rezervasyon Tahakkukları
        var c7 = CreateCharge(7, r24, salonZ01, yzCozum, btToplanti, 1800m, 20m, 360m, 2160m, 0m, ChargeStatus.Pending, r24.EndDate.Date);
        var c8 = CreateCharge(8, r25, salonEA01, megaFinans, btEtkinlik!, 6000m, 20m, 1200m, 7200m, 0m, ChargeStatus.Pending, r25.EndDate.Date);
        var c9 = CreateCharge(9, r28, salonBEA01, agroGida, btEtkinlik!, 10000m, 20m, 2000m, 12000m, 0m, ChargeStatus.Pending, r28.EndDate.Date);
        var c10 = CreateCharge(10, r29, salonBZ02, biotech, btToplanti, 400m, 20m, 80m, 480m, 0m, ChargeStatus.Pending, r29.EndDate.Date);
        var c11 = CreateCharge(11, r32, salonZ01, yzCozum, btToplanti, 1800m, 20m, 360m, 2160m, 0m, ChargeStatus.Pending, r32.EndDate.Date);
        var c12 = CreateCharge(12, r33, salonEA01, megaFinans, btEtkinlik!, 6000m, 20m, 1200m, 7200m, 0m, ChargeStatus.Pending, r33.EndDate.Date);
        var c13 = CreateCharge(13, r34, salonBEA01, agroGida, btEtkinlik!, 10000m, 20m, 2000m, 12000m, 0m, ChargeStatus.Pending, r34.EndDate.Date);
        var c14 = CreateCharge(14, r36, salonBEA01, biotech, btEtkinlik!, 10000m, 20m, 2000m, 12000m, 0m, ChargeStatus.Pending, r36.EndDate.Date);
        var c15 = CreateCharge(15, r37, salonEA01, egeLojistik, btEtkinlik!, 4500m, 20m, 900m, 5400m, 0m, ChargeStatus.Pending, r37.EndDate.Date);
        var c16 = CreateCharge(16, r38, salonDZ01, yzCozum, btToplanti, 800m, 20m, 160m, 960m, 0m, ChargeStatus.Pending, r38.EndDate.Date);
        var c17 = CreateCharge(17, r39, salonBZ02, megaFinans, btToplanti, 400m, 20m, 80m, 480m, 0m, ChargeStatus.Overdue, r39.EndDate.Date);

        ctx.Charges.AddRange(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16, c17);
        await ctx.SaveChangesAsync();

        // Ödeme Dağıtımı (Eylül Ayı Senaryo 1 için)
        DagitKalemBazliOdeme(c1, 2160m, seedStoreAccountId, adminId, r1.EndDate.AddHours(1), PaymentChannel.BankTransfer, $"{r1.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c1.PaidAmount = 2160m;
        c1.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c2, 7200m, seedStoreAccountId, adminId, r2.EndDate.AddHours(2), PaymentChannel.BankTransfer, $"{r2.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c2.PaidAmount = 7200m;
        c2.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c3, 480m, seedStoreAccountId, adminId, r3.EndDate.AddHours(1), PaymentChannel.Card, $"{r3.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c3.PaidAmount = 480m;
        c3.Status = ChargeStatus.Paid;

        // Ödeme Dağıtımı (Geçmiş Aylar: Mayıs - Ağustos için)
        DagitKalemBazliOdeme(c7, 2160m, seedStoreAccountId, adminId, r24.EndDate.AddHours(1), PaymentChannel.BankTransfer, $"{r24.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c7.PaidAmount = 2160m;
        c7.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c8, 7200m, seedStoreAccountId, adminId, r25.EndDate.AddHours(2), PaymentChannel.BankTransfer, $"{r25.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c8.PaidAmount = 7200m;
        c8.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c9, 12000m, seedStoreAccountId, adminId, r28.EndDate.AddHours(1), PaymentChannel.BankTransfer, $"{r28.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c9.PaidAmount = 12000m;
        c9.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c10, 480m, seedStoreAccountId, adminId, r29.EndDate.AddHours(1), PaymentChannel.Card, $"{r29.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c10.PaidAmount = 480m;
        c10.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c11, 2160m, seedStoreAccountId, adminId, r32.EndDate.AddHours(1), PaymentChannel.BankTransfer, $"{r32.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c11.PaidAmount = 2160m;
        c11.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c12, 7200m, seedStoreAccountId, adminId, r33.EndDate.AddHours(2), PaymentChannel.BankTransfer, $"{r33.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c12.PaidAmount = 7200m;
        c12.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c13, 12000m, seedStoreAccountId, adminId, r34.EndDate.AddHours(1), PaymentChannel.BankTransfer, $"{r34.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c13.PaidAmount = 12000m;
        c13.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c14, 12000m, seedStoreAccountId, adminId, r36.EndDate.AddHours(1), PaymentChannel.BankTransfer, $"{r36.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c14.PaidAmount = 12000m;
        c14.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c15, 5400m, seedStoreAccountId, adminId, r37.EndDate.AddHours(1), PaymentChannel.BankTransfer, $"{r37.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c15.PaidAmount = 5400m;
        c15.Status = ChargeStatus.Paid;

        DagitKalemBazliOdeme(c16, 960m, seedStoreAccountId, adminId, r38.EndDate.AddHours(1), PaymentChannel.Card, $"{r38.ReservationNo} Rezervasyon Tahakkuk Ödemesi");
        c16.PaidAmount = 960m;
        c16.Status = ChargeStatus.Paid;

        await ctx.SaveChangesAsync();
    }


    private async Task SeedBankaHareketleriAsync()
    {
        var storeAccountId = await ctx.StoreAccounts.Select(a => a.Id).FirstOrDefaultAsync();
        if (storeAccountId == 0) return;

        // Eşleşmiş Hareket
        ctx.BankTransactions.Add(new BankTransaction
        {
            StoreAccountId = storeAccountId,
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
            StoreAccountId = storeAccountId,
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

    private static Lease MakeSozlesme(string leaseNo, Unit unit, Tenant tenant,
        DateTime baslangic, DateTime bitis,
        bool kdv, decimal kdvOrani = 20, string? notlar = null,
        DueDateRuleType vadeKuraliTipi = DueDateRuleType.FixedDayOfMonth,
        int vadeGunu = 1,
        LeaseStatus status = LeaseStatus.Active,
        DateTime? terminationDate = null,
        string? terminationReason = null) => new()
        {
            LeaseNo = leaseNo,
            Unit = unit,
            UnitId = unit.Id,
            Tenant = tenant,
            TenantId = tenant.Id,
            StartDate = baslangic,
            EndDate = bitis,
            Description = notlar,
            Status = status,
            TerminationDate = terminationDate,
            TerminationReason = terminationReason,
            IsKdvApplied = kdv,
            DueDateRuleType = vadeKuraliTipi,
            DueDay = vadeGunu
        };

    private static async Task<byte[]> GetDummyPdfBytesAsync()
    {
        const string pdfPath = @"C:\Users\onur.baskan\Documents\WorkItems\Blank.pdf";
        if (File.Exists(pdfPath))
        {
            try
            {
                return await File.ReadAllBytesAsync(pdfPath);
            }
            catch
            {
                // Fallback below
            }
        }
        return [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
    }

    public async Task ClearDomainDataAsync()
    {
        // Yetki kapsamlarını ve logları temizle (FK kısıtlaması nedeniyle)
        ctx.KullaniciYetkiKapsamlari.RemoveRange(ctx.KullaniciYetkiKapsamlari.IgnoreQueryFilters());
        ctx.UserPermissions.RemoveRange(ctx.UserPermissions.IgnoreQueryFilters());
        ctx.Davetiyeler.RemoveRange(ctx.Davetiyeler.IgnoreQueryFilters());
        ctx.SifreSifirlamaTalepleri.RemoveRange(ctx.SifreSifirlamaTalepleri.IgnoreQueryFilters());
        ctx.AuditLogs.RemoveRange(ctx.AuditLogs.IgnoreQueryFilters());

        // Sanal POS tabloları (FK kısıtlaması nedeniyle tahakkuk ve ödemelerden önce silinmeli)
        ctx.OnlinePaymentEvents.RemoveRange(ctx.OnlinePaymentEvents.IgnoreQueryFilters());
        ctx.OnlinePaymentTransactions.RemoveRange(ctx.OnlinePaymentTransactions.IgnoreQueryFilters());

        // Temizlik sırası önemlidir (FK kısıtlamaları nedeniyle)
        ctx.PaymentMatches.RemoveRange(ctx.PaymentMatches.IgnoreQueryFilters());
        ctx.PaymentAllocations.RemoveRange(ctx.PaymentAllocations.IgnoreQueryFilters());
        ctx.BankTransactions.RemoveRange(ctx.BankTransactions.IgnoreQueryFilters());

        ctx.ReservationAttendees.RemoveRange(ctx.ReservationAttendees.IgnoreQueryFilters());
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

        var kiraciRoller = await ctx.Roller.IgnoreQueryFilters().Where(r => (r.Scope == RoleScope.Tenant && r.TenantId != null) || r.Name == "Kiracı Personeli").ToListAsync();
        ctx.Roller.RemoveRange(kiraciRoller);
        await ctx.SaveChangesAsync();

        // Seed iç kullanıcılarını ve dinamik iç rollerini temizle (Süper admin hariç)
        var icUsers = await userManager.Users.Where(u => u.UserType == UserType.Internal && !u.IsSuperAdmin && u.Email != IdentitySeedService.AdminEmail).ToListAsync();
        foreach (var iu in icUsers)
        {
            await userRoleService.RemoveAllRolesAsync(iu.Id);
            await userManager.DeleteAsync(iu);
        }

        var icRoller = await ctx.Roller.IgnoreQueryFilters().Where(r => r.Scope == RoleScope.Internal && !r.IsSystemRole).ToListAsync();
        ctx.Roller.RemoveRange(icRoller);
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
        ctx.TasinmazTipleri.RemoveRange(ctx.TasinmazTipleri.IgnoreQueryFilters());
        ctx.ChargeTypes.RemoveRange(ctx.ChargeTypes.IgnoreQueryFilters().Where(b => !b.IsSystem));
        ctx.DocumentTypes.RemoveRange(ctx.DocumentTypes.IgnoreQueryFilters().Where(b => !b.IsSystem));

        await ctx.SaveChangesAsync();
    }

    private async Task EnsureKiraciUserAsync(
        string email,
        string password,
        string adSoyad,
        int tenantId,
        string roleName = RoleNames.KiraciYoneticisi,
        bool tumTasinmazlaraErisim = true)
    {
        var admin = await userManager.FindByEmailAsync(IdentitySeedService.AdminEmail);
        if (admin == null || !admin.IsActive || !admin.IsSuperAdmin || admin.UserType != UserType.Internal)
            throw new InvalidOperationException("Kiracı seed işleminden önce iç süper admin oluşturulmalıdır.");

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
                TenantId = tenantId,
                TumTasinmazlaraErisim = tumTasinmazlaraErisim
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
            user.TumTasinmazlaraErisim = tumTasinmazlaraErisim;
            user.AdSoyad = adSoyad;
            await userManager.UpdateAsync(user);
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, password);
        }

        var firmaRol = await ctx.Roller.FirstOrDefaultAsync(r => (r.TenantId == tenantId || r.TenantId == null) && r.Name == roleName && !r.IsDeleted);
        if (firmaRol != null)
        {
            var hasRole = await ctx.UserRoller.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == firmaRol.Id && !ur.IsDeleted);
            if (!hasRole)
            {
                await userRoleService.AddRoleByRolIdAsync(user.Id, firmaRol.Id, admin.Id);
            }
        }
    }

    private async Task<Role> EnsureKiraciPersoneliRoleAsync(string adminUserId, int? tenantId = null)
    {
        var rol = await ctx.Roller.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == "Kiracı Personeli" && r.Scope == RoleScope.Tenant && !r.IsDeleted);
        if (rol == null)
        {
            rol = new Role
            {
                Name = "Kiracı Personeli",
                Description = "Sözleşme, tahakkuk ve rezervasyon işlemlerini yürüten kiracı şirket personeli.",
                Scope = RoleScope.Tenant,
                TenantId = tenantId,
                IsSystemRole = false,
                IsActive = true,
                CreatedBy = adminUserId,
                CreatedAt = DateTime.UtcNow
            };
            ctx.Roller.Add(rol);
            await ctx.SaveChangesAsync();
        }

        var perms = new[]
        {
            PermissionCatalog.TenantPortal.Lease.Module,
            PermissionCatalog.TenantPortal.Charge.Module,
            PermissionCatalog.TenantPortal.Payment.Module,
            PermissionCatalog.TenantPortal.Reservation.Module,
            PermissionCatalog.TenantPortal.Reservation.Create,
            PermissionCatalog.TenantPortal.Reservation.Cancel
        };

        var existingPerms = await ctx.RolPermissions.Where(rp => rp.RoleId == rol.Id).ToListAsync();
        ctx.RolPermissions.RemoveRange(existingPerms);
        foreach (var p in perms)
        {
            ctx.RolPermissions.Add(new RolePermission { RoleId = rol.Id, Permission = p });
        }
        await ctx.SaveChangesAsync();

        return rol;
    }


    public async Task SeedIcRollerVeKullanicilarAsync()
    {
        const string defaultPassword = "Admin123!";
        var admin = await userManager.FindByEmailAsync(IdentitySeedService.AdminEmail);
        if (admin == null || !admin.IsActive || !admin.IsSuperAdmin || admin.UserType != UserType.Internal)
            throw new InvalidOperationException("İç roller ve kullanıcılar seed edilmeden önce sistem yöneticisi (admin) oluşturulmalıdır.");

        // 1. Dinamik Rol: Finans ve Muhasebe Sorumlusu (Veritabanında IsSystemRole = false olarak oluşturulur)
        const string finansRolAdi = "Finans ve Muhasebe Sorumlusu";
        var finansPermissions = new[]
        {
            PermissionCatalog.Payment.Module,
            PermissionCatalog.Payment.Create,
            PermissionCatalog.Payment.UploadReceipt,
            PermissionCatalog.Payment.Approve,
            PermissionCatalog.Payment.Reject,
            PermissionCatalog.Payment.ImportBankStatement,
            PermissionCatalog.Payment.MatchBankTransaction,
            PermissionCatalog.Charge.Module,
            PermissionCatalog.Charge.Regenerate,
            PermissionCatalog.ManualCharge.Module,
            PermissionCatalog.ManualCharge.Create,
            PermissionCatalog.ManualCharge.Cancel,
            PermissionCatalog.Notification.Module,
            PermissionCatalog.Notification.BorcHatirlatma,
            PermissionCatalog.Store.Module,
            PermissionCatalog.Store.Account,
            PermissionCatalog.PaymentRouting.Module,
            PermissionCatalog.RateSchedule.Module,
            PermissionCatalog.Property.Module,
            PermissionCatalog.Unit.Module,
            PermissionCatalog.Tenant.Module,
            PermissionCatalog.Lease.Module
        };

        var finansRol = await EnsureInternalRoleAsync(
            finansRolAdi,
            "Tahakkuk, manuel borçlandırma, ödeme onay/red, dekont yönetimi, banka hareketleri ve mağaza işlemlerini yönetir.",
            finansPermissions,
            admin.Id);

        // 2. Dinamik Rol: Tesis ve Operasyon Sorumlusu (Veritabanında IsSystemRole = false olarak oluşturulur)
        const string operasyonRolAdi = "Tesis ve Operasyon Sorumlusu";
        var operasyonPermissions = new[]
        {
            PermissionCatalog.Property.Module,
            PermissionCatalog.Property.Create,
            PermissionCatalog.Property.Edit,
            PermissionCatalog.Unit.Module,
            PermissionCatalog.Unit.Create,
            PermissionCatalog.Unit.Edit,
            PermissionCatalog.Unit.OverrideRate,
            PermissionCatalog.Tenant.Module,
            PermissionCatalog.Tenant.Create,
            PermissionCatalog.Tenant.Edit,
            PermissionCatalog.Lease.Module,
            PermissionCatalog.Lease.Create,
            PermissionCatalog.Lease.Edit,
            PermissionCatalog.Lease.Extend,
            PermissionCatalog.Lease.Terminate,
            PermissionCatalog.Lease.Approve,
            PermissionCatalog.Lease.RequestRevision,
            PermissionCatalog.Reservation.Module,
            PermissionCatalog.Reservation.Create,
            PermissionCatalog.Reservation.Edit,
            PermissionCatalog.Reservation.Cancel,
            PermissionCatalog.Reservation.Approve,
            PermissionCatalog.Reservation.Reject,
            PermissionCatalog.Reservation.TransferToCharge,
            PermissionCatalog.DocumentType.Module,
            PermissionCatalog.DocumentType.Create,
            PermissionCatalog.DocumentType.Edit,
            PermissionCatalog.Charge.Module,
            PermissionCatalog.Payment.Module
        };

        var operasyonRol = await EnsureInternalRoleAsync(
            operasyonRolAdi,
            "Taşınmaz, bağımsız bölüm, sözleşme, rezervasyon ve belge süreçlerini yönetir.",
            operasyonPermissions,
            admin.Id);

        // 3. İç Kullanıcı 1: Selin Özkan (Finans)
        await EnsureInternalUserAsync(
            "finans@kiratakip.local",
            defaultPassword,
            "Selin Özkan",
            finansRol.Id,
            admin.Id);

        // 4. İç Kullanıcı 2: Burak Demir (Operasyon)
        await EnsureInternalUserAsync(
            "operasyon@kiratakip.local",
            defaultPassword,
            "Burak Demir",
            operasyonRol.Id,
            admin.Id);
    }

    private async Task<Role> EnsureInternalRoleAsync(string name, string description, IReadOnlyList<string> permissions, string adminUserId)
    {
        var rol = await ctx.Roller.FirstOrDefaultAsync(r => r.Name == name && r.Scope == RoleScope.Internal && !r.IsDeleted);
        if (rol == null)
        {
            rol = await roleService.CreateRoleAsync(new CreateRoleInput(name, description, adminUserId));
        }
        else
        {
            rol.Description = description;
            await ctx.SaveChangesAsync();
        }

        await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(rol.Id, permissions, adminUserId));
        return rol;
    }

    private async Task EnsureInternalUserAsync(string email, string password, string adSoyad, int roleId, string adminUserId)
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
                UserType = UserType.Internal,
                TumTasinmazlaraErisim = true,
                IsSuperAdmin = false
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"İç kullanıcı '{email}' oluşturulamadı: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        else
        {
            user.UserType = UserType.Internal;
            user.IsActive = true;
            user.TumTasinmazlaraErisim = true;
            user.AdSoyad = adSoyad;
            await userManager.UpdateAsync(user);

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, token, password);
        }

        var hasRole = await ctx.UserRoller.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == roleId && !ur.IsDeleted);
        if (!hasRole)
        {
            await userRoleService.AddRoleByRolIdAsync(user.Id, roleId, adminUserId);
        }
    }
}

