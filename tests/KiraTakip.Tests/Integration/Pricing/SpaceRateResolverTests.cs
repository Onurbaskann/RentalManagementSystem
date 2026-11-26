using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Spaces;
using KiraTakip.Services.Pricing;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class SpaceRateResolverTests : IDisposable
{
    private static readonly DateTime Donem = new(2026, 5, 1);

    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public SpaceRateResolverTests(DatabaseFixture fixture)
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

    private SpaceRateResolver CreateResolver()
        => new(new SpaceRepository(_context), new SpaceRateRepository(_context), new RateScheduleRepository(_context));

    private async Task<(Category Category, ChargeType ChargeType)> SeedDefinitionsAsync()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var category = new Category { Type = CategoryType.Tenant, Name = $"Alan Tarife Kategori {s}", Code = $"SRC_{s}" };
        var chargeType = new ChargeType
        {
            Name = $"Alan Tarife Borç {s}", Code = $"SRB_{s}", IsActive = true, Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        _context.AddRange(category, chargeType);
        await _context.SaveChangesAsync();
        return (category, chargeType);
    }

    [Fact]
    public async Task OwnRate_WinsOverAncestor()
    {
        var (cat, ct) = await SeedDefinitionsAsync();
        var root = await SpaceTestSeed.AddRootAsync(_context, "Kök");
        var office = await SpaceTestSeed.AddChildAsync(_context, root, "Ofis");
        await SpaceTestSeed.AddRateAsync(_context, root, cat.Id, ct.Id, 100m);
        await SpaceTestSeed.AddRateAsync(_context, office, cat.Id, ct.Id, 200m, kdv: 20m);

        var r = await CreateResolver().ResolveAsync(office.Id, cat.Id, ct.Id, Donem);

        Assert.NotNull(r);
        Assert.Equal(200m, r!.UnitValue);
        Assert.Equal(20m, r.KdvRate);
    }

    [Fact]
    public async Task NoOwnRate_NearestAncestorWins()
    {
        var (cat, ct) = await SeedDefinitionsAsync();
        var root = await SpaceTestSeed.AddRootAsync(_context, "Kök");
        var block = await SpaceTestSeed.AddChildAsync(_context, root, "Blok");
        var office = await SpaceTestSeed.AddChildAsync(_context, block, "Ofis");
        await SpaceTestSeed.AddRateAsync(_context, root, cat.Id, ct.Id, 100m);
        await SpaceTestSeed.AddRateAsync(_context, block, cat.Id, ct.Id, 150m);

        var r = await CreateResolver().ResolveAsync(office.Id, cat.Id, ct.Id, Donem);

        Assert.Equal(150m, r!.UnitValue);
    }

    [Fact]
    public async Task NoSpaceRate_FallsBackToRateSchedule_WithYearFallback()
    {
        var (cat, ct) = await SeedDefinitionsAsync();
        var root = await SpaceTestSeed.AddRootAsync(_context, "Kök");
        var office = await SpaceTestSeed.AddChildAsync(_context, root, "Ofis");
        _context.GenelTarifeler.Add(new RateSchedule
        {
            TenantCategoryId = cat.Id, ChargeTypeId = ct.Id, Year = 2020,
            CalculationMethod = CalculationMethod.Fixed, UnitValue = 400m
        });
        await _context.SaveChangesAsync();

        var r = await CreateResolver().ResolveAsync(office.Id, cat.Id, ct.Id, Donem);

        Assert.Equal(400m, r!.UnitValue);
        Assert.Equal(LineItemSourceType.RateSchedule, r.SourceType);
    }

    [Fact]
    public async Task NothingDefined_ReturnsNull()
    {
        var (cat, ct) = await SeedDefinitionsAsync();
        var root = await SpaceTestSeed.AddRootAsync(_context, "Kök");

        Assert.Null(await CreateResolver().ResolveAsync(root.Id, cat.Id, ct.Id, Donem));
    }

    [Fact]
    public async Task OtherCategoryRate_IsSkipped()
    {
        var (cat, ct) = await SeedDefinitionsAsync();
        var (otherCat, _) = await SeedDefinitionsAsync();
        var root = await SpaceTestSeed.AddRootAsync(_context, "Kök");
        var office = await SpaceTestSeed.AddChildAsync(_context, root, "Ofis");
        await SpaceTestSeed.AddRateAsync(_context, office, otherCat.Id, ct.Id, 999m);
        await SpaceTestSeed.AddRateAsync(_context, root, cat.Id, ct.Id, 100m);

        var r = await CreateResolver().ResolveAsync(office.Id, cat.Id, ct.Id, Donem);

        Assert.Equal(100m, r!.UnitValue);
    }

    [Fact]
    public async Task InactiveOrDeletedRate_IsSkipped()
    {
        var (cat, ct) = await SeedDefinitionsAsync();
        var root = await SpaceTestSeed.AddRootAsync(_context, "Kök");
        var block = await SpaceTestSeed.AddChildAsync(_context, root, "Blok");
        var office = await SpaceTestSeed.AddChildAsync(_context, block, "Ofis");
        await SpaceTestSeed.AddRateAsync(_context, root, cat.Id, ct.Id, 100m);
        await SpaceTestSeed.AddRateAsync(_context, block, cat.Id, ct.Id, 150m, isActive: false);
        var deleted = await SpaceTestSeed.AddRateAsync(_context, office, cat.Id, ct.Id, 200m);
        deleted.IsDeleted = true;
        await _context.SaveChangesAsync();

        var r = await CreateResolver().ResolveAsync(office.Id, cat.Id, ct.Id, Donem);

        Assert.Equal(100m, r!.UnitValue);
    }
}
