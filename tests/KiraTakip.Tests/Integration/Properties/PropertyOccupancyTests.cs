using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Properties;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class PropertyOccupancyTests(DatabaseFixture fixture) : PropertyUnitTestBase(fixture)
{
    private async Task<KiraTakip.Models.Entities.Unit> SeedOpenUnitAsync(
        KiraTakip.Models.Entities.Property property,
        KiraTakip.Models.Entities.UnitType capacityType,
        int occupied,
        int capacity,
        decimal area,
        decimal allocatedArea,
        DateTime? leaseEnd = null)
    {
        var unit = await SeedUnitAsync(property, capacityType, "A1", area, capacity);
        for (var i = 0; i < occupied; i++)
            await SeedLeaseAsync(unit, LeaseStatus.Active, allocatedArea, end: leaseEnd);
        return unit;
    }

    [Fact]
    public async Task GetListAsync_ShouldCountNormalAndOpenUnitsSeparately()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var normalType = await SeedUnitTypeAsync("Norm", UnitRentalMode.UnitBased);
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        for (var i = 0; i < 3; i++)
        {
            var normal = await SeedUnitAsync(property, normalType, $"N{i}", 50m);
            await SeedLeaseAsync(normal, LeaseStatus.Active);
        }
        await SeedOpenUnitAsync(property, capacityType, 6, 10, 70m, 7m);

        var item = (await new PropertyRepository(Ctx).GetListAsync(null)).Single(p => p.Id == property.Id);

        Assert.Equal(3, item.LeasedUnitCount);
        Assert.Equal(1, item.PartiallyLeasedUnitCount);
        Assert.Equal(0, item.ExpiringSoonUnitCount);
        Assert.Equal(0, item.VacantUnitCount);
        Assert.Equal(10, item.TotalCapacity);
        Assert.Equal(6, item.OccupiedCapacity);
    }

    [Fact]
    public async Task GetListAsync_ShouldCountFullOpenUnitAsLeased_AndReportExpiringLeaseCount()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "A1", 70m, 10);
        for (var i = 0; i < 10; i++)
            await SeedLeaseAsync(unit, LeaseStatus.Active, 7m, end: i < 2 ? Today.AddDays(10) : Today.AddDays(90));

        var repository = new PropertyRepository(Ctx);
        var item = (await repository.GetListAsync(null)).Single(p => p.Id == property.Id);
        var details = await repository.GetDetailsAsync(property.Id);

        Assert.Equal(1, item.LeasedUnitCount);
        Assert.Equal(0, item.ExpiringSoonUnitCount);
        var detailUnit = Assert.Single(details!.Units);
        Assert.Equal(OccupancyStatus.Leased, detailUnit.Status);
        Assert.Equal(10, detailUnit.OccupiedCount);
        Assert.Equal(2, detailUnit.ExpiringLeaseCount);
    }

    [Fact]
    public async Task GetListAsync_ShouldUseFractionalCountAndCapAreaAtUnitArea_ForOpenUnit()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        await SeedOpenUnitAsync(property, capacityType, 6, 10, 70m, 20m);

        var item = (await new PropertyRepository(Ctx).GetListAsync(null)).Single(p => p.Id == property.Id);

        Assert.Equal(0.6m, item.OccupiedUnitEquivalent);
        Assert.Equal(70m, item.LeasedUnitArea);
        Assert.Equal(0m, item.VacantUnitArea);
        Assert.True(item.LeasedUnitArea / (item.LeasedUnitArea + item.VacantUnitArea) <= 1m);
    }
}
