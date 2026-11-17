using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Services.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Tests;

internal sealed class NoopPropertyPricingService : IPropertyPricingService
{
    public Task<PropertyPricingMatrixDto> GetMatrixAsync(GetPropertyPricingMatrixInput input)
        => Task.FromResult(new PropertyPricingMatrixDto());

    public Task SaveMatrixAsync(SavePropertyPricingMatrixInput input) => Task.CompletedTask;
}

public abstract class PropertyUnitTestBase : IDisposable
{
    protected readonly ApplicationDbContext Ctx;
    private readonly IDbContextTransaction _tx;
    protected readonly string Suffix = Guid.NewGuid().ToString("N")[..8];
    protected readonly DateTime Today = DateTime.Today;

    protected PropertyUnitTestBase(DatabaseFixture fixture)
    {
        Ctx = fixture.CreateContext();
        _tx = Ctx.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _tx.Rollback();
        _tx.Dispose();
        Ctx.Dispose();
    }

    protected PropertyService CreateService()
        => new(new PropertyRepository(Ctx),
               new PropertyTypeRepository(Ctx),
               new UnitRepository(Ctx),
               new UnitTypeRepository(Ctx),
               new ReservationRepository(Ctx),
               new ReservationRateOverrideRepository(Ctx),
               new NoopPropertyPricingService(),
               new UnitOfWork(Ctx),
               null!,
               new TestOperationalPolicyProvider(),
               TimeProvider.System);

    protected async Task<PropertyType> SeedPropertyTypeAsync()
    {
        var propertyType = new PropertyType
        {
            Name = $"Test Tipi {Suffix}",
            Code = $"TT_{Suffix}",
            SupportsSingleUnit = true,
            SupportsMultipleUnits = true
        };
        Ctx.TasinmazTipleri.Add(propertyType);
        await Ctx.SaveChangesAsync();
        return propertyType;
    }

    protected async Task<UnitType> SeedUnitTypeAsync(string label, UnitRentalMode mode)
    {
        var unitType = new UnitType
        {
            Name = $"{label} {Suffix}",
            Code = $"{label.ToUpperInvariant()}_{Suffix}",
            Usage = UnitTypeUsage.Rentable,
            RentalMode = mode
        };
        Ctx.UnitTypes.Add(unitType);
        await Ctx.SaveChangesAsync();
        return unitType;
    }

    protected async Task<Property> SeedPropertyAsync(
        PropertyType propertyType,
        UnitStructure structure,
        decimal closedArea = 0m,
        decimal openArea = 0m)
    {
        var property = new Property
        {
            Name = $"Test Taşınmaz {Suffix}",
            PropertyTypeId = propertyType.Id,
            UnitStructure = structure,
            ClosedArea = closedArea,
            OpenArea = openArea,
            City = "İstanbul",
            District = "Test",
            Neighborhood = "Test",
            Address = "Test"
        };
        Ctx.Properties.Add(property);
        await Ctx.SaveChangesAsync();
        return property;
    }

    protected async Task<Unit> SeedUnitAsync(
        Property property,
        UnitType unitType,
        string unitNo,
        decimal area,
        int? capacity = null)
    {
        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            UnitNo = unitNo,
            FloorNo = 1,
            Name = $"Birim {unitNo} {Suffix}",
            Area = area,
            Capacity = capacity
        };
        Ctx.Units.Add(unit);
        await Ctx.SaveChangesAsync();
        return unit;
    }

    protected async Task<Lease> SeedLeaseAsync(
        Unit unit,
        LeaseStatus status,
        decimal? allocatedArea = null,
        DateTime? start = null,
        DateTime? end = null)
    {
        var index = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"KT-{index}", Name = $"Kiracı {index}" };
        Ctx.Tenants.Add(tenant);
        await Ctx.SaveChangesAsync();

        var lease = new Lease
        {
            LeaseNo = $"TST-{index}",
            UnitId = unit.Id,
            TenantId = tenant.Id,
            Status = status,
            StartDate = start ?? Today.AddDays(-5),
            EndDate = end ?? Today.AddDays(60)
        };
        if (allocatedArea.HasValue)
            lease.LeaseArea = new LeaseArea { RentedArea = allocatedArea.Value, AllocatedArea = allocatedArea.Value };
        Ctx.Leases.Add(lease);
        await Ctx.SaveChangesAsync();
        return lease;
    }

    protected static UpdatePropertyInput ToUpdateInput(Property property, params Unit[] units)
        => new()
        {
            PropertyId = property.Id,
            Name = property.Name,
            PropertyTypeId = property.PropertyTypeId,
            UnitStructure = property.UnitStructure,
            SingleUnitTypeId = property.UnitStructure == UnitStructure.SingleUnit
                ? units.Single().UnitTypeId
                : null,
            SingleUnitCapacity = property.UnitStructure == UnitStructure.SingleUnit
                ? units.Single().Capacity
                : null,
            City = property.City,
            District = property.District,
            Neighborhood = property.Neighborhood,
            Address = property.Address,
            OpenArea = property.OpenArea,
            ClosedArea = property.ClosedArea,
            Units = property.UnitStructure == UnitStructure.MultipleUnits
                ? units.Select(unit => new PropertyUnitInputDto
                {
                    Id = unit.Id,
                    UnitNo = unit.UnitNo!,
                    FloorNo = unit.FloorNo,
                    Name = unit.Name,
                    Area = unit.Area,
                    UnitTypeId = unit.UnitTypeId,
                    Capacity = unit.Capacity
                }).ToList()
                : []
        };

    protected CreatePropertyInput NewCreateInput(PropertyType propertyType, UnitStructure structure)
        => new()
        {
            Name = $"Yeni Taşınmaz {Suffix}",
            PropertyTypeId = propertyType.Id,
            UnitStructure = structure,
            City = "İstanbul",
            District = "Test",
            Neighborhood = "Test",
            Address = "Test"
        };
}

[Collection("Database collection")]
public class PropertyUnitLockTests(DatabaseFixture fixture) : PropertyUnitTestBase(fixture)
{
    [Fact]
    public async Task Update_ShouldRejectAreaChange_WhenUnitHasOpenLeases()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 60m, 2);
        await SeedLeaseAsync(unit, LeaseStatus.Active, 30m);
        await SeedLeaseAsync(unit, LeaseStatus.Draft, 30m);

        var input = ToUpdateInput(property, unit);
        input.Units[0].Area = 65m;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.UnitAreaLocked", exception.Code);
    }

    [Fact]
    public async Task Update_ShouldLockSingleUnitAreas_OnlyWhileLeaseIsOpen()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var unitType = await SeedUnitTypeAsync("Tek", UnitRentalMode.UnitBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.SingleUnit, 40m, 20m);
        var unit = await SeedUnitAsync(property, unitType, "1", 60m);
        var lease = await SeedLeaseAsync(unit, LeaseStatus.Draft, null);

        var input = ToUpdateInput(property, unit);
        input.ClosedArea = 45m;
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.UnitAreaLocked", exception.Code);

        var openAreaInput = ToUpdateInput(property, unit);
        openAreaInput.OpenArea = 25m;
        exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(openAreaInput));
        Assert.Equal("Property.UnitAreaLocked", exception.Code);

        lease.Status = LeaseStatus.Ended;
        await Ctx.SaveChangesAsync();
        Ctx.ChangeTracker.Clear();

        var endedInput = ToUpdateInput(property, unit);
        endedInput.ClosedArea = 45m;
        await CreateService().UpdateWithChildrenAsync(endedInput);

        var saved = await Ctx.Properties.AsNoTracking().SingleAsync(p => p.Id == property.Id);
        Assert.Equal(45m, saved.ClosedArea);
    }

    [Theory]
    [InlineData(LeaseStatus.Ended, true)]
    [InlineData(LeaseStatus.Terminated, true)]
    [InlineData(LeaseStatus.Draft, false)]
    [InlineData(LeaseStatus.Active, false)]
    [InlineData(LeaseStatus.RevisionRequested, false)]
    public async Task Update_ShouldAllowUnitTypeChange_OnlyWithoutOpenLease(LeaseStatus status, bool allowed)
    {
        var propertyType = await SeedPropertyTypeAsync();
        var typeA = await SeedUnitTypeAsync("TurA", UnitRentalMode.UnitBased);
        var typeB = await SeedUnitTypeAsync("TurB", UnitRentalMode.UnitBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, typeA, "1", 50m);
        await SeedLeaseAsync(unit, status, 50m);

        var input = ToUpdateInput(property, unit);
        input.Units[0].UnitTypeId = typeB.Id;

        if (allowed)
        {
            await CreateService().UpdateWithChildrenAsync(input);
            var saved = await Ctx.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id);
            Assert.Equal(typeB.Id, saved.UnitTypeId);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().UpdateWithChildrenAsync(input));
            Assert.Equal("Property.UnitTypeLocked", exception.Code);
        }
    }

    [Fact]
    public async Task Update_ShouldRejectSingleUnitTypeChange_WhenLeaseIsOpen()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var typeA = await SeedUnitTypeAsync("TurA", UnitRentalMode.UnitBased);
        var typeB = await SeedUnitTypeAsync("TurB", UnitRentalMode.UnitBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.SingleUnit, 40m, 20m);
        var unit = await SeedUnitAsync(property, typeA, "1", 60m);
        await SeedLeaseAsync(unit, LeaseStatus.Active);

        var input = ToUpdateInput(property, unit);
        input.SingleUnitTypeId = typeB.Id;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.UnitTypeLocked", exception.Code);
    }

    [Fact]
    public async Task CreateAndUpdate_ShouldAcceptCapacityBasedType_ForSingleUnitWithCapacity()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var normalType = await SeedUnitTypeAsync("Norm", UnitRentalMode.UnitBased);

        var createInput = NewCreateInput(propertyType, UnitStructure.SingleUnit);
        createInput.SingleUnitTypeId = capacityType.Id;
        createInput.SingleUnitCapacity = 4;
        createInput.ClosedArea = 50m;
        var created = await CreateService().CreateAsync(createInput);
        var createdUnit = await Ctx.Units.SingleAsync(u => u.PropertyId == created.PropertyId);
        Assert.Equal(4, createdUnit.Capacity);

        var property = await SeedPropertyAsync(propertyType, UnitStructure.SingleUnit, 40m, 0m);
        var unit = await SeedUnitAsync(property, normalType, "1", 40m);
        var updateInput = ToUpdateInput(property, unit);
        updateInput.SingleUnitTypeId = capacityType.Id;
        updateInput.SingleUnitCapacity = 3;
        await CreateService().UpdateWithChildrenAsync(updateInput);
        await Ctx.Entry(unit).ReloadAsync();
        Assert.Equal(capacityType.Id, unit.UnitTypeId);
        Assert.Equal(3, unit.Capacity);
    }

    [Fact]
    public async Task GetForEdit_ShouldReturnOpenLeaseUnitIdsAndCapacity()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var openUnit = await SeedUnitAsync(property, capacityType, "1", 60m, 3);
        var endedUnit = await SeedUnitAsync(property, capacityType, "2", 60m, 4);
        await SeedLeaseAsync(openUnit, LeaseStatus.Draft, 20m);
        await SeedLeaseAsync(endedUnit, LeaseStatus.Ended, 20m);

        var edit = await CreateService().GetForEditAsync(new GetPropertyForEditInput(property.Id));

        Assert.NotNull(edit);
        Assert.Contains(openUnit.Id, edit!.OpenLeaseUnitIds);
        Assert.DoesNotContain(endedUnit.Id, edit.OpenLeaseUnitIds);
        Assert.Equal(3, edit.Units.Single(u => u.Id == openUnit.Id).Capacity);
    }
}
