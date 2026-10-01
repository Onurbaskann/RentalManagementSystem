using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Services.Pricing;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

// PropertyService.GetDetailsAsync'teki N+1 düzeltmesi (bkz. StatisticsService.GetMonthlyAmountsAsync)
// için eklenen BatchRateResolver'ın, mevcut tekil RateResolverService ile BİREBİR aynı sonucu
// ürettiğini doğrular — 4 precedence kademesi (sözleşme/birim/taşınmaz/genel tarife) + kiracı
// kategorisi olmayan kenar durumu.
[Collection("Database collection")]
public class BatchRateResolverTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public BatchRateResolverTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task ResolveManyAsync_ShouldMatchSingleResolverAcrossAllPrecedenceLevels()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var property = new Property { Name = $"Batch Rate Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Batch Rate Birim Türü {suffix}",
            Code = $"BR_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var category = new Category { Type = CategoryType.Tenant, Name = $"Batch Rate Kategori {suffix}", Code = $"BRC_{suffix}" };
        // Ayrı kategori: L4'ün taşınmaz/birim override'ı olan kategoriyle (L3'ün) paylaşılmaması için —
        // aksi halde L3 için eklenen PropertyRateOverride, aynı kategoriyi kullanan L4'ü de "gölgeler"
        // (PropertyRateOverride sözleşme değil kategori bazlı çalışır) ve genel tarife kademesine hiç inilmez.
        var scheduleOnlyCategory = new Category { Type = CategoryType.Tenant, Name = $"Batch Rate Genel Tarife Kategori {suffix}", Code = $"BRCS_{suffix}" };
        var chargeType = new ChargeType
        {
            Name = $"Batch Rate Borç Tipi {suffix}",
            Code = $"BRCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };

        _context.AddRange(property, unitType, category, scheduleOnlyCategory, chargeType);
        await _context.SaveChangesAsync();

        var units = Enumerable.Range(1, 5)
            .Select(i => new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Batch Rate Birim {i} {suffix}", Area = 10m })
            .ToList();
        _context.Units.AddRange(units);
        await _context.SaveChangesAsync();

        var tenantsWithCategory = Enumerable.Range(1, 4)
            .Select(i => new Tenant
            {
                TenantNo = $"BR{i}-{suffix}",
                Name = $"Batch Rate Kiracı {i} {suffix}",
                TenantCategoryId = i == 4 ? scheduleOnlyCategory.Id : category.Id
            })
            .ToList();
        var tenantWithoutCategory = new Tenant { TenantNo = $"BR5-{suffix}", Name = $"Batch Rate Kiracı 5 {suffix}", TenantCategoryId = null };
        _context.Tenants.AddRange(tenantsWithCategory);
        _context.Tenants.Add(tenantWithoutCategory);
        await _context.SaveChangesAsync();

        var leases = new List<Lease>
        {
            CreateLease(units[0].Id, tenantsWithCategory[0].Id, suffix), // L1 — sözleşme override
            CreateLease(units[1].Id, tenantsWithCategory[1].Id, suffix), // L2 — birim rate
            CreateLease(units[2].Id, tenantsWithCategory[2].Id, suffix), // L3 — taşınmaz rate
            CreateLease(units[3].Id, tenantsWithCategory[3].Id, suffix), // L4 — genel tarife (yıl fallback)
            CreateLease(units[4].Id, tenantWithoutCategory.Id, suffix)   // L5 — kategori yok → null
        };
        _context.Leases.AddRange(leases);
        await _context.SaveChangesAsync();

        _context.SozlesmeTarifeler.Add(new LeaseRateOverride
        {
            LeaseId = leases[0].Id, ChargeTypeId = chargeType.Id,
            CalculationMethod = CalculationMethod.Fixed, UnitValue = 100m
        });
        _context.UnitRates.Add(new UnitRate
        {
            UnitId = units[1].Id, TenantCategoryId = category.Id, ChargeTypeId = chargeType.Id,
            CalculationMethod = CalculationMethod.Fixed, UnitValue = 200m
        });
        _context.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = property.Id, TenantCategoryId = category.Id, ChargeTypeId = chargeType.Id,
            CalculationMethod = CalculationMethod.Fixed, UnitValue = 300m
        });
        _context.GenelTarifeler.Add(new RateSchedule
        {
            TenantCategoryId = scheduleOnlyCategory.Id, ChargeTypeId = chargeType.Id, Year = 2020,
            CalculationMethod = CalculationMethod.Fixed, UnitValue = 400m
        });
        await _context.SaveChangesAsync();

        var leaseRateRepo = new LeaseRateOverrideRepository(_context);
        var unitRateRepo = new UnitRateRepository(_context);
        var propertyRateRepo = new PropertyRateOverrideRepository(_context);
        var rateScheduleRepo = new RateScheduleRepository(_context);
        var leaseRepo = new LeaseRepository(_context);

        var singleResolver = new RateResolverService(
            leaseRateRepo, unitRateRepo, propertyRateRepo, rateScheduleRepo,
            leaseRepo, new UnitRepository(_context), new TenantRepository(_context));
        IBatchRateResolver batchResolver = new BatchRateResolver(
            leaseRateRepo, unitRateRepo, propertyRateRepo, rateScheduleRepo, leaseRepo);

        var today = DateTime.Today;
        var requests = leases
            .Select(lease => new RateResolutionRequest(lease.Id, lease.TenantId, lease.UnitId, chargeType.Id, today))
            .ToList();

        var batchResults = await batchResolver.ResolveManyAsync(requests);

        foreach (var lease in leases)
        {
            var expected = await singleResolver.ResolveAsync(lease.Id, lease.TenantId, lease.UnitId, chargeType.Id, today);
            var actualFound = batchResults.TryGetValue((lease.Id, chargeType.Id), out var actual);

            Assert.True(actualFound, $"Lease {lease.Id} için batch sonucu yok.");
            if (expected == null)
            {
                Assert.Null(actual);
            }
            else
            {
                Assert.NotNull(actual);
                Assert.Equal(expected.CalculationMethod, actual!.CalculationMethod);
                Assert.Equal(expected.UnitValue, actual.UnitValue);
                Assert.Equal(expected.KdvRate, actual.KdvRate);
                Assert.Equal(expected.SourceType, actual.SourceType);
            }
        }

        // Beklenen kademeler açıkça de doğrulanır (yalnız "eşleşme" değil, "doğru kademe" de garanti edilir).
        Assert.Equal(100m, batchResults[(leases[0].Id, chargeType.Id)]!.UnitValue);
        Assert.Equal(LineItemSourceType.LeaseRateOverride, batchResults[(leases[0].Id, chargeType.Id)]!.SourceType);

        Assert.Equal(200m, batchResults[(leases[1].Id, chargeType.Id)]!.UnitValue);
        Assert.Equal(LineItemSourceType.UnitRateOverride, batchResults[(leases[1].Id, chargeType.Id)]!.SourceType);

        Assert.Equal(300m, batchResults[(leases[2].Id, chargeType.Id)]!.UnitValue);
        Assert.Equal(LineItemSourceType.PropertyRateOverride, batchResults[(leases[2].Id, chargeType.Id)]!.SourceType);

        Assert.Equal(400m, batchResults[(leases[3].Id, chargeType.Id)]!.UnitValue);
        Assert.Equal(LineItemSourceType.RateSchedule, batchResults[(leases[3].Id, chargeType.Id)]!.SourceType);

        Assert.Null(batchResults[(leases[4].Id, chargeType.Id)]);
    }

    private static Lease CreateLease(int unitId, int tenantId, string suffix)
        => new()
        {
            LeaseNo = $"TEST-{Guid.NewGuid():N}"[..20],
            UnitId = unitId,
            TenantId = tenantId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = LeaseStatus.Ended,
            IsRentFree = false
        };
}
