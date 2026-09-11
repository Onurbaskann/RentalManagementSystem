using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Pricing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Property;

namespace KiraTakip.Tests;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

public class DatabaseFixture : IDisposable
{
    public string ConnectionString { get; }

    public DatabaseFixture()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        var configuredConnectionString = config.GetConnectionString("DefaultConnection")!;
        var connectionBuilder = new SqlConnectionStringBuilder(configuredConnectionString);
        var databaseName = connectionBuilder.InitialCatalog;
        if (!string.Equals(databaseName, "KiraTakipDb_Test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Entegrasyon testleri yalnızca KiraTakipDb_Test veritabanında çalıştırılabilir. Yapılandırılan veritabanı: {databaseName}");
        }

        // Uzak test SQL Server'ın eski TLS yapılandırması yalnız test çalıştırıcısında
        // Microsoft.Data.SqlClient'in varsayılan şifreleme davranışıyla uyuşmuyor.
        // Uygulama connection string'i değiştirilmeden yalnız doğrulanmış test DB bağlantısı uyarlanır.
        connectionBuilder.Encrypt = false;
        ConnectionString = connectionBuilder.ConnectionString;

        using var ctx = CreateContext();
        ctx.Database.Migrate();
    }

    public ApplicationDbContext CreateContext(ICurrentUserContext? currentUser = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(
            options,
            new DummyHttpContextAccessor(),
            currentUser ?? new DummyCurrentUserContext());
    }

    public void Dispose() { }
}

[Collection("Database collection")]
public class PricingArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly IDbContextTransaction _tx;

    public PricingArchitectureTests(DatabaseFixture fixture)
    {
        _ctx = fixture.CreateContext();
        _tx = _ctx.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _tx.Rollback();
        _tx.Dispose();
        _ctx.Dispose();
    }

    private RateResolverService CreateResolver()
    {
        return new RateResolverService(new LeaseRateOverrideRepository(_ctx),
                                       new UnitRateRepository(_ctx),
                                       new PropertyRateOverrideRepository(_ctx),
                                       new RateScheduleRepository(_ctx),
                                       new LeaseRepository(_ctx),
                                       new UnitRepository(_ctx),
                                       new TenantRepository(_ctx));
    }

    private ChargeGenerationService CreateProduction()
    {
        return new ChargeGenerationService(new ChargeRepository(_ctx),
                                           new ChargeTypeRepository(_ctx),
                                           new UnitOfWork(_ctx),
                                           CreateResolver(),
                                           new LeaseRepository(_ctx),
                                           new UnitRepository(_ctx),
                                           new TenantRepository(_ctx));
    }

    private record SeedData(
        Category KategoriA,
        Property Property,
        Unit Unit,
        Tenant Tenant,
        ChargeType OrtakGider);

    private async Task<SeedData> SeedAsync()
    {
        // ChargeType Id=1(KIRA), 2(DEPOZITO), 3(DIGER) zaten HasData ile mevcut.
        var uid = Guid.NewGuid().ToString("N")[..8];
        var ortakGider = new ChargeType { Name = "Ortak Gider", Code = $"ORTAK_{uid}", Behavior = ChargeTypeBehavior.MonthlyFixed };
        _ctx.ChargeTypes.Add(ortakGider);

        var kategoriA = new Category { Type = CategoryType.Tenant, Name = "Kategori A", Code = $"KATA_{uid}" };
        _ctx.Kategoriler.Add(kategoriA);

        var property = new Property { Name = "Test Plaza", City = "Ankara", District = "Çankaya" };
        _ctx.Properties.Add(property);

        var unitType = new UnitType { Name = $"Ofis_{uid}", Code = $"OF_{uid}", Usage = UnitTypeUsage.Rentable };
        _ctx.UnitTypes.Add(unitType);

        await _ctx.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, Name = "Ofis 101", Area = 100, UnitTypeId = unitType.Id };
        _ctx.Units.Add(unit);

        var tenant = new Tenant { Name = "Test Kiracı A.Ş.", TenantCategoryId = kategoriA.Id };
        _ctx.Tenants.Add(tenant);

        await _ctx.SaveChangesAsync();

        return new SeedData(kategoriA, property, unit, tenant, ortakGider);
    }

    [Fact]
    public async Task SmokeTest_Matrix_And_12MonthProduction()
    {
        var seed = await SeedAsync();

        _ctx.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = seed.Property.Id,
            TenantCategoryId = seed.KategoriA.Id,
            ChargeTypeId = 1,
            UnitValue = 5000,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        });
        _ctx.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = seed.Property.Id,
            TenantCategoryId = seed.KategoriA.Id,
            ChargeTypeId = seed.OrtakGider.Id,
            UnitValue = 5,
            CalculationMethod = CalculationMethod.M2,
            KdvRate = 20
        });
        await _ctx.SaveChangesAsync();

        var s = new Lease
        {
            UnitId = seed.Unit.Id,
            TenantId = seed.Tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        _ctx.Leases.Add(s);
        await _ctx.SaveChangesAsync();

        await CreateProduction().GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));

        var tahakkuklar = await _ctx.Charges.Include(t => t.LineItems)
            .Where(t => t.LeaseId == s.Id).ToListAsync();
        Assert.Equal(12, tahakkuklar.Count);

        foreach (var t in tahakkuklar)
        {
            Assert.Contains(t.LineItems, k => k.ChargeTypeId == 1 && k.Amount == 5000);
            Assert.Contains(t.LineItems, k => k.ChargeTypeId == seed.OrtakGider.Id && k.Amount == 500);
            Assert.DoesNotContain(t.LineItems, k => k.ChargeTypeId == 3);
        }
    }

    [Fact]
    public async Task ZeroValueTest_ShouldRender_ZeroAmountKalem()
    {
        var seed = await SeedAsync();

        _ctx.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = seed.Property.Id,
            TenantCategoryId = seed.KategoriA.Id,
            ChargeTypeId = 1,
            UnitValue = 0,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        });
        await _ctx.SaveChangesAsync();

        var s = new Lease
        {
            UnitId = seed.Unit.Id,
            TenantId = seed.Tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };
        _ctx.Leases.Add(s);
        await _ctx.SaveChangesAsync();

        await CreateProduction().GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));

        var kalem = await _ctx.Charges
            .Where(t => t.LeaseId == s.Id)
            .SelectMany(t => t.LineItems)
            .FirstOrDefaultAsync(k => k.ChargeTypeId == 1);
        Assert.NotNull(kalem);
        Assert.Equal(0, kalem.Amount);
    }

    [Fact]
    public async Task OverrideTest_ShouldPrefer_SozlesmeRate()
    {
        var seed = await SeedAsync();

        _ctx.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = seed.Property.Id,
            TenantCategoryId = seed.KategoriA.Id,
            ChargeTypeId = 1,
            UnitValue = 5000,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        });
        await _ctx.SaveChangesAsync();

        var s = new Lease
        {
            UnitId = seed.Unit.Id,
            TenantId = seed.Tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };
        _ctx.Leases.Add(s);
        await _ctx.SaveChangesAsync();

        _ctx.SozlesmeTarifeler.Add(new LeaseRateOverride
        {
            LeaseId = s.Id,
            ChargeTypeId = 1,
            UnitValue = 6000,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        });
        await _ctx.SaveChangesAsync();

        await CreateProduction().GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));

        var kalem = await _ctx.Charges
            .Where(t => t.LeaseId == s.Id)
            .SelectMany(t => t.LineItems)
            .FirstOrDefaultAsync(k => k.ChargeTypeId == 1);
        Assert.Equal(6000, kalem!.Amount);
    }

    [Fact]
    public async Task Depozito_ShouldBeOnly_InFirstMonth()
    {
        var seed = await SeedAsync();

        var s = new Lease
        {
            UnitId = seed.Unit.Id,
            TenantId = seed.Tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 2, 28)
        };
        _ctx.Leases.Add(s);
        await _ctx.SaveChangesAsync();

        _ctx.SozlesmeTarifeler.Add(new LeaseRateOverride
        {
            LeaseId = s.Id,
            ChargeTypeId = 2,
            UnitValue = 10000,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 0
        });
        await _ctx.SaveChangesAsync();

        await CreateProduction().GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));

        var t1 = await _ctx.Charges.Include(x => x.LineItems)
            .FirstOrDefaultAsync(t => t.PeriodStart == new DateTime(2026, 1, 1) && t.LeaseId == s.Id);
        var t2 = await _ctx.Charges.Include(x => x.LineItems)
            .FirstOrDefaultAsync(t => t.PeriodStart == new DateTime(2026, 2, 1) && t.LeaseId == s.Id);

        Assert.Contains(t1!.LineItems, k => k.ChargeTypeId == 2);
        Assert.DoesNotContain(t2!.LineItems, k => k.ChargeTypeId == 2);
    }

    [Fact]
    public async Task ResolverPriorityTest_ShouldFollow_DefinedOrder()
    {
        var seed = await SeedAsync();
        var resolver = CreateResolver();

        _ctx.GenelTarifeler.Add(new RateSchedule
        {
            Year = 2026,
            TenantCategoryId = seed.KategoriA.Id,
            ChargeTypeId = 1,
            UnitValue = 1000
        });
        _ctx.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = seed.Property.Id,
            TenantCategoryId = seed.KategoriA.Id,
            ChargeTypeId = 1,
            UnitValue = 2000
        });
        _ctx.UnitRates.Add(new UnitRate
        {
            UnitId = seed.Unit.Id,
            TenantCategoryId = seed.KategoriA.Id,
            ChargeTypeId = 1,
            UnitValue = 3000
        });

        var s = new Lease
        {
            UnitId = seed.Unit.Id,
            TenantId = seed.Tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        _ctx.Leases.Add(s);
        await _ctx.SaveChangesAsync();

        _ctx.SozlesmeTarifeler.Add(new LeaseRateOverride
        {
            LeaseId = s.Id,
            ChargeTypeId = 1,
            UnitValue = 4000
        });

        await _ctx.SaveChangesAsync();

        var donem = new DateTime(2026, 1, 1);

        // 1. SozlesmeRate (4000)
        var res1 = await resolver.ResolveAsync(s.Id, seed.Tenant.Id, seed.Unit.Id, 1, donem);
        Assert.Equal(4000, res1!.UnitValue);

        // 2. BirimRate (3000)
        _ctx.SozlesmeTarifeler.RemoveRange(_ctx.SozlesmeTarifeler.Where(t => t.LeaseId == s.Id));
        await _ctx.SaveChangesAsync();
        var res2 = await resolver.ResolveAsync(s.Id, seed.Tenant.Id, seed.Unit.Id, 1, donem);
        Assert.Equal(3000, res2!.UnitValue);

        // 3. Matris (2000)
        _ctx.UnitRates.RemoveRange(_ctx.UnitRates.Where(t => t.UnitId == seed.Unit.Id));
        await _ctx.SaveChangesAsync();
        var res3 = await resolver.ResolveAsync(s.Id, seed.Tenant.Id, seed.Unit.Id, 1, donem);
        Assert.Equal(2000, res3!.UnitValue);

        // 4. GenelTarife (1000)
        _ctx.TasinmazTarifeler.RemoveRange(_ctx.TasinmazTarifeler.Where(t => t.PropertyId == seed.Property.Id));
        await _ctx.SaveChangesAsync();
        var res4 = await resolver.ResolveAsync(s.Id, seed.Tenant.Id, seed.Unit.Id, 1, donem);
        Assert.Equal(1000, res4!.UnitValue);
    }

    [Fact]
    public async Task YenidenUret_ShouldNotDelete_ManualTahakkuklar()
    {
        var seed = await SeedAsync();

        var s = new Lease
        {
            UnitId = seed.Unit.Id,
            TenantId = seed.Tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };
        _ctx.Leases.Add(s);
        await _ctx.SaveChangesAsync();

        var production = CreateProduction();
        await production.GenerateForLeaseAsync(new GenerateLeaseChargesInput(s.Id));

        var manuelT = new Charge
        {
            LeaseId = s.Id,
            TenantId = seed.Tenant.Id,
            UnitId = seed.Unit.Id,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            SourceType = ChargeSourceType.Manual,
            Status = ChargeStatus.Pending,
            ExpectedAmount = 100
        };
        _ctx.Charges.Add(manuelT);
        await _ctx.SaveChangesAsync();

        await production.RegenerateAsync(new RegenerateLeaseChargesInput(s.Id, new DateTime(2026, 1, 1)));

        var allTahakkuklar = await _ctx.Charges
            .Where(t => t.LeaseId == s.Id).ToListAsync();
        Assert.Contains(allTahakkuklar, t => t.SourceType == ChargeSourceType.Manual);
        Assert.Contains(allTahakkuklar, t => t.SourceType == ChargeSourceType.Lease);
    }

    [Fact]
    public async Task MatrixIntegration_InPropertyCreation_ShouldSavePricing()
    {
        var seed = await SeedAsync();

        var pricingService = new PropertyPricingService(
            new PropertyRateOverrideRepository(_ctx),
            new UnitOfWork(_ctx));

        var newTasinmaz = new Property { Name = "New Property" };
        _ctx.Properties.Add(newTasinmaz);
        await _ctx.SaveChangesAsync();

        var input = new SavePropertyPricingMatrixInput
        {
            PropertyId = newTasinmaz.Id,
            Rows = new List<PropertyPricingRowDto>
            {
                new PropertyPricingRowDto
                {
                    TenantCategoryId = seed.KategoriA.Id,
                    Cells = new List<PropertyPricingCellDto>
                    {
                        new PropertyPricingCellDto
                        {
                            PropertyId = newTasinmaz.Id,
                            TenantCategoryId = seed.KategoriA.Id,
                            ChargeTypeId = 1,
                            UnitValue = 7500,
                            CalculationMethod = CalculationMethod.Fixed,
                            VatRate = 20
                        }
                    }
                }
            }
        };

        await pricingService.SaveMatrixAsync(input);

        var savedFiyat = await _ctx.TasinmazTarifeler
            .FirstOrDefaultAsync(f => f.PropertyId == newTasinmaz.Id && f.TenantCategoryId == seed.KategoriA.Id && f.ChargeTypeId == 1);

        Assert.NotNull(savedFiyat);
        Assert.Equal(7500, savedFiyat.UnitValue);
    }

    [Fact]
    public async Task PricingContextRepository_ShouldBuildMatrixContextFromMainEntityRepository()
    {
        var seed = await SeedAsync();
        var repository = new PropertyRateOverrideRepository(_ctx);

        var context = await repository.GetPricingContextAsync(seed.Property.Id);

        Assert.True(context.PropertyExists);
        Assert.Equal(seed.Property.Name, context.PropertyName);
        Assert.Contains(context.Categories, category => category.Id == seed.KategoriA.Id);
        Assert.Contains(context.ChargeTypes, type => type.Id == seed.OrtakGider.Id);
    }

    [Fact]
    public async Task GetMatrixAsync_ShouldRejectPropertyOutsideScope()
    {
        var seed = await SeedAsync();
        var service = new PropertyPricingService(
            new PropertyRateOverrideRepository(_ctx),
            new UnitOfWork(_ctx));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetMatrixAsync(new GetPropertyPricingMatrixInput(
                seed.Property.Id,
                AccessiblePropertyIds: [])));

        Assert.Equal(ErrorType.Forbidden, exception.ErrorType);
        Assert.Equal("PropertyPricing.OutOfScope", exception.Code);
    }

    [Fact]
    public async Task SaveMatrixAsync_ShouldRejectForeignRateOverride()
    {
        var target = await SeedAsync();
        var foreignProperty = new Property
        {
            Name = "Yabancı Taşınmaz",
            City = "Ankara",
            District = "Çankaya"
        };
        _ctx.Properties.Add(foreignProperty);
        await _ctx.SaveChangesAsync();
        var foreignRate = new PropertyRateOverride
        {
            PropertyId = foreignProperty.Id,
            TenantCategoryId = target.KategoriA.Id,
            ChargeTypeId = target.OrtakGider.Id,
            UnitValue = 100,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        };
        _ctx.TasinmazTarifeler.Add(foreignRate);
        await _ctx.SaveChangesAsync();
        var service = new PropertyPricingService(
            new PropertyRateOverrideRepository(_ctx),
            new UnitOfWork(_ctx));
        var input = new SavePropertyPricingMatrixInput
        {
            PropertyId = target.Property.Id,
            Rows =
            [
                new PropertyPricingRowDto
                {
                    TenantCategoryId = target.KategoriA.Id,
                    Cells =
                    [
                        new PropertyPricingCellDto
                        {
                            PropertyRateOverrideId = foreignRate.Id,
                            TenantCategoryId = target.KategoriA.Id,
                            ChargeTypeId = target.OrtakGider.Id,
                            UnitValue = 999,
                            CalculationMethod = CalculationMethod.Fixed,
                            VatRate = 20
                        }
                    ]
                }
            ]
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SaveMatrixAsync(input));

        Assert.Equal(ErrorType.Forbidden, exception.ErrorType);
        Assert.Equal("Property.ForeignPricingRate", exception.Code);
        Assert.Equal(100, foreignRate.UnitValue);
    }
}

internal class DummyHttpContextAccessor : Microsoft.AspNetCore.Http.IHttpContextAccessor
{
    public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; }
}

internal class DummyCurrentUserContext : ICurrentUserContext
{
    public string? UserId => null;
    public UserType? UserType => null;
    public int? TenantId => null;
    public bool IsKiraciUser => false;
}
