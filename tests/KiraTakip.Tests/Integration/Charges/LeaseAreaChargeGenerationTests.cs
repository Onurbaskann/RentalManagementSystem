using KiraTakip.Data;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public sealed class LeaseAreaChargeGenerationTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly IDbContextTransaction _tx;

    public LeaseAreaChargeGenerationTests(DatabaseFixture fixture)
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

    private RateResolverService CreateResolver() => new(
        new LeaseRateOverrideRepository(_ctx),
        new UnitRateRepository(_ctx),
        new PropertyRateOverrideRepository(_ctx),
        new RateScheduleRepository(_ctx),
        new LeaseRepository(_ctx),
        new UnitRepository(_ctx),
        new TenantRepository(_ctx));

    private ChargeGenerationService CreateGeneration() => new(
        new ChargeRepository(_ctx),
        new ChargeTypeRepository(_ctx),
        new UnitOfWork(_ctx),
        CreateResolver(),
        new LeaseRepository(_ctx),
        new UnitRepository(_ctx),
        new TenantRepository(_ctx));

    private StatisticsService CreateStatistics() => new(
        new ChargeTypeRepository(_ctx),
        new LeaseRepository(_ctx),
        CreateResolver(),
        new BatchRateResolver(
            new LeaseRateOverrideRepository(_ctx),
            new UnitRateRepository(_ctx),
            new PropertyRateOverrideRepository(_ctx),
            new RateScheduleRepository(_ctx),
            new LeaseRepository(_ctx)),
        new TestOperationalPolicyProvider());

    private record Seed(Property Property, Unit Unit, Category Category, ChargeType Fee);

    private async Task<Seed> SeedAsync(decimal unitArea = 60m)
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var fee = new ChargeType { Name = "Aidat", Code = $"AIDAT_{uid}", Behavior = ChargeTypeBehavior.MonthlyFixed };
        var category = new Category { Type = CategoryType.Tenant, Name = "Kategori", Code = $"KAT_{uid}" };
        var property = new Property { Name = "Test Plaza", City = "Ankara", District = "Çankaya" };
        var unitType = new UnitType { Name = $"Alan_{uid}", Code = $"AL_{uid}", Usage = UnitTypeUsage.Rentable };
        _ctx.ChargeTypes.Add(fee);
        _ctx.Kategoriler.Add(category);
        _ctx.Properties.Add(property);
        _ctx.UnitTypes.Add(unitType);
        await _ctx.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, Name = "Alan 1", Area = unitArea, UnitTypeId = unitType.Id };
        _ctx.Units.Add(unit);
        await _ctx.SaveChangesAsync();
        return new Seed(property, unit, category, fee);
    }

    private async Task AddPropertyRateAsync(Seed seed, int chargeTypeId, decimal value, CalculationMethod method)
    {
        _ctx.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = seed.Property.Id,
            TenantCategoryId = seed.Category.Id,
            ChargeTypeId = chargeTypeId,
            UnitValue = value,
            CalculationMethod = method,
            KdvRate = 20
        });
        await _ctx.SaveChangesAsync();
    }

    private async Task<Lease> AddLeaseAsync(Seed seed, decimal? rentedArea)
    {
        var tenant = new Tenant { TenantNo = $"T{Guid.NewGuid():N}"[..12], Name = $"Kiracı {Guid.NewGuid():N}"[..20], TenantCategoryId = seed.Category.Id };
        _ctx.Tenants.Add(tenant);
        await _ctx.SaveChangesAsync();

        var lease = new Lease
        {
            LeaseNo = $"TEST-{Guid.NewGuid():N}"[..20],
            UnitId = seed.Unit.Id,
            TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            Status = LeaseStatus.Active
        };
        _ctx.Leases.Add(lease);
        await _ctx.SaveChangesAsync();

        if (rentedArea.HasValue)
        {
            _ctx.LeaseAreas.Add(new LeaseArea
            {
                LeaseId = lease.Id,
                RentedArea = rentedArea.Value,
                AllocatedArea = rentedArea.Value
            });
            await _ctx.SaveChangesAsync();
        }

        return lease;
    }

    private async Task<decimal> GenerateAndGetAmountAsync(Lease lease, int chargeTypeId)
    {
        await CreateGeneration().GenerateForLeaseAsync(new GenerateLeaseChargesInput(lease.Id));
        return await _ctx.Charges
            .Where(charge => charge.LeaseId == lease.Id)
            .SelectMany(charge => charge.LineItems)
            .Where(item => item.ChargeTypeId == chargeTypeId)
            .Select(item => item.Amount)
            .SingleAsync();
    }

    [Fact]
    public async Task FixedUpperLayerRate_ShouldBeProportionalToRentedArea()
    {
        var seed = await SeedAsync();
        await AddPropertyRateAsync(seed, seed.Fee.Id, 6000m, CalculationMethod.Fixed);
        var a = await AddLeaseAsync(seed, 20m);
        var b = await AddLeaseAsync(seed, 40m);

        Assert.Equal(2000m, await GenerateAndGetAmountAsync(a, seed.Fee.Id));
        Assert.Equal(4000m, await GenerateAndGetAmountAsync(b, seed.Fee.Id));
    }

    [Fact]
    public async Task FixedLeaseRate_ShouldNotBeProportioned()
    {
        var seed = await SeedAsync();
        await AddPropertyRateAsync(seed, seed.Fee.Id, 6000m, CalculationMethod.Fixed);
        var a = await AddLeaseAsync(seed, 20m);
        var b = await AddLeaseAsync(seed, 40m);
        _ctx.SozlesmeTarifeler.Add(new LeaseRateOverride
        {
            LeaseId = a.Id,
            ChargeTypeId = seed.Fee.Id,
            UnitValue = 1500m,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        });
        await _ctx.SaveChangesAsync();

        Assert.Equal(1500m, await GenerateAndGetAmountAsync(a, seed.Fee.Id));
        Assert.Equal(4000m, await GenerateAndGetAmountAsync(b, seed.Fee.Id));
    }

    [Fact]
    public async Task M2Rate_ShouldUseRentedArea()
    {
        var seed = await SeedAsync();
        await AddPropertyRateAsync(seed, seed.Fee.Id, 100m, CalculationMethod.M2);
        var a = await AddLeaseAsync(seed, 20m);

        Assert.Equal(2000m, await GenerateAndGetAmountAsync(a, seed.Fee.Id));
    }

    [Fact]
    public async Task LeaseWithoutLeaseArea_ShouldKeepExistingAmounts()
    {
        var seed = await SeedAsync();
        await AddPropertyRateAsync(seed, seed.Fee.Id, 6000m, CalculationMethod.Fixed);
        await AddPropertyRateAsync(seed, 1, 100m, CalculationMethod.M2);
        var lease = await AddLeaseAsync(seed, null);

        await CreateGeneration().GenerateForLeaseAsync(new GenerateLeaseChargesInput(lease.Id));
        var items = await _ctx.Charges
            .Where(charge => charge.LeaseId == lease.Id)
            .SelectMany(charge => charge.LineItems)
            .ToListAsync();

        Assert.Equal(6000m, items.Single(item => item.ChargeTypeId == seed.Fee.Id).Amount);
        Assert.Equal(6000m, items.Single(item => item.ChargeTypeId == 1).Amount);
    }

    [Fact]
    public async Task Statistics_ShouldUseSameMultiplier()
    {
        var seed = await SeedAsync();
        await AddPropertyRateAsync(seed, seed.Fee.Id, 6000m, CalculationMethod.Fixed);
        var a = await AddLeaseAsync(seed, 20m);
        var b = await AddLeaseAsync(seed, 40m);
        var c = await AddLeaseAsync(seed, null);
        _ctx.SozlesmeTarifeler.Add(new LeaseRateOverride
        {
            LeaseId = b.Id,
            ChargeTypeId = seed.Fee.Id,
            UnitValue = 1500m,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        });
        await _ctx.SaveChangesAsync();
        var statistics = CreateStatistics();

        var leases = await _ctx.Leases.Include(l => l.Unit)
            .Where(l => l.Id == a.Id || l.Id == b.Id || l.Id == c.Id).ToListAsync();
        var batch = await statistics.GetMonthlyAmountsAsync(leases);

        Assert.Equal(2000m, batch[a.Id]);
        Assert.Equal(1500m, batch[b.Id]);
        Assert.Equal(6000m, batch[c.Id]);
        Assert.Equal(2000m, await statistics.GetMonthlyAmountAsync(leases.Single(l => l.Id == a.Id)));
        Assert.Equal(1500m, await statistics.GetMonthlyAmountAsync(leases.Single(l => l.Id == b.Id)));
    }
}
