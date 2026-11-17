using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class UnitCapacityChangeTests(DatabaseFixture fixture) : PropertyUnitTestBase(fixture)
{
    private Task<List<KiraTakip.Models.Entities.UnitCapacityHistory>> HistoryAsync(int unitId)
        => Ctx.UnitCapacityHistories.AsNoTracking()
            .Where(history => history.UnitId == unitId)
            .OrderBy(history => history.Id)
            .ToListAsync();

    [Fact]
    public async Task Update_ShouldAcceptCapacityIncreaseAndDecrease_AndWriteHistory()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var increase = ToUpdateInput(property, unit);
        increase.Units[0].Capacity = 8;
        await CreateService().UpdateWithChildrenAsync(increase);

        var history = await HistoryAsync(unit.Id);
        var row = Assert.Single(history);
        Assert.Equal(8, row.Capacity);
        Assert.Equal(Today, row.EffectiveFrom);

        Ctx.ChangeTracker.Clear();
        var decrease = ToUpdateInput(property, unit);
        decrease.Units[0].Capacity = 3;
        await CreateService().UpdateWithChildrenAsync(decrease);

        history = await HistoryAsync(unit.Id);
        Assert.Equal(2, history.Count);
        Assert.Equal(3, history[1].Capacity);
        Assert.Equal(3, (await Ctx.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id)).Capacity);
    }

    [Fact]
    public async Task Update_ShouldRejectCapacityAboveMaximum_AndReportMaximum()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var input = ToUpdateInput(property, unit);
        input.Units[0].Capacity = 15;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.CapacityAboveMaximum", exception.Code);
        Assert.Contains("14", exception.Message);

        Ctx.ChangeTracker.Clear();
        var accepted = ToUpdateInput(property, unit);
        accepted.Units[0].Capacity = 14;
        await CreateService().UpdateWithChildrenAsync(accepted);
    }

    [Fact]
    public async Task Update_ShouldRejectCapacityBelowOccupied_AndReportMinimum()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 10);
        await SeedLeaseAsync(unit, LeaseStatus.Active, 20m);
        await SeedLeaseAsync(unit, LeaseStatus.Draft, 20m);
        await SeedLeaseAsync(unit, LeaseStatus.Ended, 20m, Today.AddMonths(-6), Today.AddDays(-10));

        var input = ToUpdateInput(property, unit);
        input.Units[0].Capacity = 1;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.CapacityBelowOccupied", exception.Code);
        Assert.Contains("2", exception.Message);

        Ctx.ChangeTracker.Clear();
        var accepted = ToUpdateInput(property, unit);
        accepted.Units[0].Capacity = 2;
        await CreateService().UpdateWithChildrenAsync(accepted);
    }

    [Fact]
    public async Task Create_ShouldRequireCapacity_ForCapacityBasedUnit()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);

        var input = NewCreateInput(propertyType, UnitStructure.MultipleUnits);
        input.Units.Add(new PropertyUnitInputDto
        {
            UnitNo = "1", FloorNo = 1, Name = "Salon", Area = 60m, UnitTypeId = capacityType.Id
        });

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().CreateAsync(input));
        Assert.Equal("Property.CapacityRequired", exception.Code);
        Assert.Equal("Units[0].Capacity", exception.Field);
    }

    [Fact]
    public async Task Create_ShouldWriteHistory_AndIgnoreCapacityOnUnitBasedType()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var normalType = await SeedUnitTypeAsync("Norm", UnitRentalMode.UnitBased);

        var input = NewCreateInput(propertyType, UnitStructure.MultipleUnits);
        input.Units.Add(new PropertyUnitInputDto
        {
            UnitNo = "1", FloorNo = 1, Name = "Salon", Area = 60m, UnitTypeId = capacityType.Id, Capacity = 4
        });
        input.Units.Add(new PropertyUnitInputDto
        {
            UnitNo = "2", FloorNo = 1, Name = "Ofis", Area = 30m, UnitTypeId = normalType.Id, Capacity = 9
        });

        var created = await CreateService().CreateAsync(input);

        var units = await Ctx.Units.AsNoTracking().Where(u => u.PropertyId == created.PropertyId).ToListAsync();
        var capacityUnit = units.Single(u => u.UnitNo == "1");
        var normalUnit = units.Single(u => u.UnitNo == "2");
        Assert.Equal(4, capacityUnit.Capacity);
        Assert.Null(normalUnit.Capacity);

        var row = Assert.Single(await HistoryAsync(capacityUnit.Id));
        Assert.Equal(4, row.Capacity);
        Assert.Equal(Today, row.EffectiveFrom);
        Assert.Empty(await HistoryAsync(normalUnit.Id));
    }

    [Fact]
    public async Task Update_ShouldRequireCapacity_WhenUnitSwitchesToCapacityBasedType()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var normalType = await SeedUnitTypeAsync("Norm", UnitRentalMode.UnitBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, normalType, "1", 60m);

        var input = ToUpdateInput(property, unit);
        input.Units[0].UnitTypeId = capacityType.Id;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.CapacityRequired", exception.Code);

        Ctx.ChangeTracker.Clear();
        var accepted = ToUpdateInput(property, unit);
        accepted.Units[0].UnitTypeId = capacityType.Id;
        accepted.Units[0].Capacity = 4;
        await CreateService().UpdateWithChildrenAsync(accepted);

        Assert.Single(await HistoryAsync(unit.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task SingleUnit_ShouldRequireCapacity_ForCapacityBasedType(int? capacity)
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);

        var input = NewCreateInput(propertyType, UnitStructure.SingleUnit);
        input.SingleUnitTypeId = capacityType.Id;
        input.SingleUnitCapacity = capacity;
        input.ClosedArea = 70m;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().CreateAsync(input));
        Assert.Equal("Property.CapacityRequired", exception.Code);
        Assert.Equal(nameof(CreatePropertyInput.SingleUnitCapacity), exception.Field);
    }

    [Fact]
    public async Task SingleUnit_Create_ShouldWriteCapacityHistory()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);

        var input = NewCreateInput(propertyType, UnitStructure.SingleUnit);
        input.SingleUnitTypeId = capacityType.Id;
        input.SingleUnitCapacity = 6;
        input.ClosedArea = 70m;

        var created = await CreateService().CreateAsync(input);

        var unit = await Ctx.Units.AsNoTracking().SingleAsync(u => u.PropertyId == created.PropertyId);
        Assert.Equal(6, unit.Capacity);
        var row = Assert.Single(await HistoryAsync(unit.Id));
        Assert.Equal(6, row.Capacity);
        Assert.Equal(Today, row.EffectiveFrom);
    }

    [Fact]
    public async Task SingleUnit_ShouldRejectCapacityAboveMaximum_AndReportMaximum()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.SingleUnit, 70m, 0m);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var input = ToUpdateInput(property, unit);
        input.SingleUnitCapacity = 15;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.CapacityAboveMaximum", exception.Code);
        Assert.Equal(nameof(UpdatePropertyInput.SingleUnitCapacity), exception.Field);
        Assert.Contains("14", exception.Message);
    }

    [Fact]
    public async Task SingleUnit_Update_ShouldWriteHistory_WhenCapacityChanges()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.SingleUnit, 70m, 0m);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var input = ToUpdateInput(property, unit);
        input.SingleUnitCapacity = 8;
        await CreateService().UpdateWithChildrenAsync(input);

        var row = Assert.Single(await HistoryAsync(unit.Id));
        Assert.Equal(8, row.Capacity);
        Assert.Equal(Today, row.EffectiveFrom);
    }

    [Fact]
    public async Task GetForEdit_ShouldReturnSingleUnitCapacity()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.SingleUnit, 70m, 0m);
        await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var edit = await CreateService().GetForEditAsync(new GetPropertyForEditInput(property.Id));

        Assert.Equal(5, edit!.SingleUnitCapacity);
    }

    [Fact]
    public async Task Update_ShouldNotAddHistory_WhenCapacityUnchanged()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 60m, 4);

        var input = ToUpdateInput(property, unit);
        input.Units[0].Name = "Yeni Ad";
        await CreateService().UpdateWithChildrenAsync(input);

        Assert.Empty(await HistoryAsync(unit.Id));
    }
}
