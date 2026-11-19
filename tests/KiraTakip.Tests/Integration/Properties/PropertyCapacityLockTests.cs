using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services.Properties;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class PropertyCapacityLockTests(DatabaseFixture fixture) : PropertyUnitTestBase(fixture)
{
    private static readonly TimeSpan BlockWindow = TimeSpan.FromSeconds(2);
    private readonly DatabaseFixture _fixture = fixture;

    private static PropertyService CreateService(ApplicationDbContext context)
        => new(new PropertyRepository(context),
               new PropertyTypeRepository(context),
               new UnitRepository(context),
               new UnitTypeRepository(context),
               new ReservationRepository(context),
               new ReservationRateOverrideRepository(context),
               new NoopPropertyPricingService(),
               new UnitOfWork(context),
               null!,
               new TestOperationalPolicyProvider(),
               TimeProvider.System);

    [Fact]
    public async Task Update_CapacityUnitAreaChange_ShouldWaitForUnitCapacityLock()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 3);
        var input = ToUpdateInput(property, unit);
        input.Units[0].Area = 60m;

        await using var holder = _fixture.CreateContext();
        await using var holderTx = await holder.Database.BeginTransactionAsync();
        await new UnitRepository(holder).AcquireCapacityLocksAsync(unit.Id);

        var update = Task.Run(() => CreateService().UpdateWithChildrenAsync(input));
        Assert.NotSame(update, await Task.WhenAny(update, Task.Delay(BlockWindow)));

        await holderTx.RollbackAsync();
        await update.WaitAsync(TimeSpan.FromSeconds(30));

        await Ctx.Entry(unit).ReloadAsync();
        Assert.Equal(60m, unit.Area);
    }

    [Fact]
    public async Task Create_CapacityUnit_ShouldWaitForMinimumUsageAreaChange()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var input = NewCreateInput(propertyType, UnitStructure.SingleUnit);
        input.SingleUnitTypeId = capacityType.Id;
        input.SingleUnitCapacity = 4;
        input.ClosedArea = 50m;

        await using var holder = _fixture.CreateContext();
        await using var holderTx = await holder.Database.BeginTransactionAsync();
        await new UnitRepository(holder).AcquireGlobalCapacityLockExclusiveAsync();

        var create = Task.Run(() => CreateService().CreateAsync(input));
        Assert.NotSame(create, await Task.WhenAny(create, Task.Delay(BlockWindow)));

        await holderTx.RollbackAsync();
        var created = await create.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(4, (await Ctx.Units.SingleAsync(u => u.PropertyId == created.PropertyId)).Capacity);
    }

    [Fact]
    public async Task Update_AreaChange_ShouldUseCapacityCommittedWhileWaitingForLock()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        int propertyTypeId = 0, unitTypeId = 0, propertyId = 0, unitId = 0;
        try
        {
            await using (var seed = _fixture.CreateContext())
            {
                var propertyType = new PropertyType
                {
                    Name = $"Kilit Tipi {suffix}", Code = $"KT_{suffix}",
                    SupportsSingleUnit = true, SupportsMultipleUnits = true
                };
                var unitType = new UnitType
                {
                    Name = $"Kont {suffix}", Code = $"KONT_{suffix}",
                    Usage = UnitTypeUsage.Rentable, RentalMode = UnitRentalMode.CapacityBased
                };
                seed.TasinmazTipleri.Add(propertyType);
                seed.UnitTypes.Add(unitType);
                await seed.SaveChangesAsync();
                var property = new Property
                {
                    Name = $"Kilit Taşınmaz {suffix}", PropertyTypeId = propertyType.Id,
                    UnitStructure = UnitStructure.MultipleUnits,
                    City = "İstanbul", District = "Test", Neighborhood = "Test", Address = "Test"
                };
                seed.Properties.Add(property);
                await seed.SaveChangesAsync();
                var unit = new Unit
                {
                    PropertyId = property.Id, UnitTypeId = unitType.Id, UnitNo = "1", FloorNo = 1,
                    Name = $"Birim {suffix}", Area = 100m, Capacity = 5
                };
                seed.Units.Add(unit);
                await seed.SaveChangesAsync();
                (propertyTypeId, unitTypeId, propertyId, unitId) = (propertyType.Id, unitType.Id, property.Id, unit.Id);
            }

            await using var holder = _fixture.CreateContext();
            await using var holderTx = await holder.Database.BeginTransactionAsync();
            await new UnitRepository(holder).AcquireCapacityLocksAsync(unitId);

            await using var serviceContext = _fixture.CreateContext();
            var update = Task.Run(async () =>
            {
                await using var tx = await serviceContext.Database.BeginTransactionAsync();
                var property = await serviceContext.Properties.Include(p => p.Units).SingleAsync(p => p.Id == propertyId);
                var input = ToUpdateInput(property, property.Units.Single());
                input.Units[0].Area = 60m;
                input.Units[0].Capacity = null;
                await CreateService(serviceContext).UpdateWithChildrenAsync(input);
                await tx.CommitAsync();
            });
            Assert.NotSame(update, await Task.WhenAny(update, Task.Delay(BlockWindow)));

            await holder.Units.Where(u => u.Id == unitId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Capacity, 18));
            await holderTx.CommitAsync();

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => update.WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.Equal("Property.CapacityAboveMaximum", exception.Code);
        }
        finally
        {
            await using var cleanup = _fixture.CreateContext();
            await cleanup.UnitCapacityHistories.Where(h => h.UnitId == unitId).ExecuteDeleteAsync();
            await cleanup.Units.IgnoreQueryFilters().Where(u => u.Id == unitId).ExecuteDeleteAsync();
            await cleanup.Properties.IgnoreQueryFilters().Where(p => p.Id == propertyId).ExecuteDeleteAsync();
            await cleanup.UnitTypes.Where(t => t.Id == unitTypeId).ExecuteDeleteAsync();
            await cleanup.TasinmazTipleri.Where(t => t.Id == propertyTypeId).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Create_UnitBasedOnly_ShouldNotWaitForCapacityLock()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var normalType = await SeedUnitTypeAsync("Norm", UnitRentalMode.UnitBased);
        var input = NewCreateInput(propertyType, UnitStructure.SingleUnit);
        input.SingleUnitTypeId = normalType.Id;
        input.ClosedArea = 50m;

        await using var holder = _fixture.CreateContext();
        await using var holderTx = await holder.Database.BeginTransactionAsync();
        await new UnitRepository(holder).AcquireGlobalCapacityLockExclusiveAsync();

        var created = await CreateService().CreateAsync(input).WaitAsync(BlockWindow);
        Assert.True(created.PropertyId > 0);
        await holderTx.RollbackAsync();
    }
}
