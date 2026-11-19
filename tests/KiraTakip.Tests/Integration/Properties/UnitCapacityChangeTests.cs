using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Properties;
using KiraTakip.Services.Properties;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class UnitCapacityChangeTests(DatabaseFixture fixture) : PropertyUnitTestBase(fixture)
{
    private CapacityPlanService CreatePlan(decimal minimum = 4m)
        => new(
            new UnitRepository(Ctx),
            new LeaseRepository(Ctx),
            new UnitOfWork(Ctx),
            new TestOperationalPolicyProvider(new OperationalPolicySettings { LeaseMinimumUsageArea = minimum }),
            new DefaultAllocationSuggestionStrategy(),
            new ProportionalReductionStrategy(),
            TimeProvider.System);

    private static CapacityPlanInput Plan(
        Unit unit,
        int capacity,
        Dictionary<int, decimal>? overrides = null)
        => new(unit.Id, capacity, new LeaseAccessScopeInput(), overrides);

    private Task<List<UnitCapacityHistory>> HistoryAsync(int unitId)
        => Ctx.UnitCapacityHistories.AsNoTracking()
            .Where(history => history.UnitId == unitId)
            .OrderBy(history => history.Id)
            .ToListAsync();

    private async Task<Unit> SeedCapacityUnitAsync(decimal area, int capacity)
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        return await SeedUnitAsync(property, capacityType, "1", area, capacity);
    }

    private async Task<Lease> SeedAreaLeaseAsync(
        Unit unit,
        decimal rented,
        decimal allocated,
        LeaseAreaMode mode,
        LeaseStatus status = LeaseStatus.Active,
        DateTime? start = null,
        DateTime? end = null)
    {
        var lease = await SeedLeaseAsync(unit, status, rented, start, end);
        lease.LeaseArea!.AllocatedArea = allocated;
        lease.LeaseArea.AllocationMode = mode;
        await Ctx.SaveChangesAsync();
        return lease;
    }

    private async Task<List<LeaseArea>> AreasAsync(int unitId)
        => await Ctx.LeaseAreas.AsNoTracking()
            .Where(area => area.Lease.UnitId == unitId)
            .OrderBy(area => area.LeaseId)
            .ToListAsync();

    private Task<int> AreaHistoryCountAsync(int unitId)
        => Ctx.LeaseAreaHistories.AsNoTracking()
            .CountAsync(history => history.LeaseArea.Lease.UnitId == unitId
                && history.Reason == LeaseAreaChangeReason.CapacityChange);

    [Fact]
    public async Task T01_CapacityIncrease_ShouldRecalculateAutomaticAreas_AndWriteHistory()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        for (var i = 0; i < 10; i++)
            await SeedAreaLeaseAsync(unit, 7m, 7m, LeaseAreaMode.Automatic);
        var service = CreatePlan();

        var preview = await service.PreviewAsync(Plan(unit, 14));

        Assert.False(preview.HasErrors);
        Assert.All(preview.Rows, row => Assert.Equal(5m, row.ProposedAllocatedArea));
        Assert.Equal(50m, preview.TotalAllocatedToday);

        await service.ApplyAsync(Plan(unit, 14), preview.Hash);

        Ctx.ChangeTracker.Clear();
        Assert.Equal(14, (await Ctx.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id)).Capacity);
        var areas = await AreasAsync(unit.Id);
        Assert.Equal(10, areas.Count);
        Assert.All(areas, area => Assert.Equal(5m, area.AllocatedArea));
        Assert.Equal(10, await AreaHistoryCountAsync(unit.Id));
        var history = Assert.Single(await HistoryAsync(unit.Id));
        Assert.Equal(14, history.Capacity);
    }

    [Fact]
    public async Task T02_ManualAreaKept_ShouldWarnNormTargetShort()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        await SeedAreaLeaseAsync(unit, 10m, 6m, LeaseAreaMode.Manual);
        for (var i = 0; i < 9; i++)
            await SeedAreaLeaseAsync(unit, 7m, 5m, LeaseAreaMode.Automatic);

        var preview = await CreatePlan().PreviewAsync(Plan(unit, 14));

        Assert.False(preview.HasErrors);
        var warning = Assert.Single(preview.Warnings, w => w.Code == CapacityPlanCodes.NormTargetShort);
        Assert.Equal(19m, warning.RemainingArea);
        Assert.DoesNotContain(preview.Warnings, w => w.Code == CapacityPlanCodes.MinimumTargetShort);
    }

    [Fact]
    public async Task T03_Override_ShouldBecomeManual_AndWriteHistory()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        var first = await SeedAreaLeaseAsync(unit, 7m, 5m, LeaseAreaMode.Automatic);
        await SeedAreaLeaseAsync(unit, 7m, 5m, LeaseAreaMode.Automatic);
        var service = CreatePlan();
        var overrides = new Dictionary<int, decimal> { [first.Id] = 6m };

        var preview = await service.PreviewAsync(Plan(unit, 14, overrides));

        Assert.False(preview.HasErrors);
        var row = preview.Rows.Single(r => r.LeaseId == first.Id);
        Assert.Equal(6m, row.ProposedAllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, row.ProposedMode);

        await service.ApplyAsync(Plan(unit, 14, overrides), preview.Hash);

        Ctx.ChangeTracker.Clear();
        var area = (await AreasAsync(unit.Id)).Single(a => a.LeaseId == first.Id);
        Assert.Equal(6m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, area.AllocationMode);
        Assert.Equal(1, await AreaHistoryCountAsync(unit.Id));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    public async Task T18_OverrideOutsideRange_ShouldReturnAreaOutOfRange(int overrideArea)
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 5m, LeaseAreaMode.Automatic);

        var preview = await CreatePlan().PreviewAsync(
            Plan(unit, 14, new Dictionary<int, decimal> { [lease.Id] = overrideArea }));

        Assert.Contains(preview.Errors, error => error.Code == CapacityPlanCodes.AreaOutOfRange);
        Assert.NotNull(preview.Rows.Single().Error);
    }

    [Fact]
    public async Task T08_AreaOverflow_ShouldBlockApply_UntilManualRowsCorrected()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        var manual = new List<Lease>();
        for (var i = 0; i < 4; i++)
            manual.Add(await SeedAreaLeaseAsync(unit, 10m, 8m, LeaseAreaMode.Manual));
        for (var i = 0; i < 6; i++)
            await SeedAreaLeaseAsync(unit, 7m, 7m, LeaseAreaMode.Automatic);
        var service = CreatePlan();

        var preview = await service.PreviewAsync(Plan(unit, 10));

        var error = Assert.Single(preview.Errors);
        Assert.Equal(CapacityPlanCodes.AreaOverflow, error.Code);
        Assert.Contains("4 m²", error.Message);
        Assert.Null(error.Day);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.ApplyAsync(Plan(unit, 10), preview.Hash));
        Assert.Equal(CapacityPlanCodes.AreaOverflow, exception.Code);
        Ctx.ChangeTracker.Clear();
        Assert.Equal(14, (await Ctx.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id)).Capacity);
        Assert.Equal(4, (await AreasAsync(unit.Id)).Count(area => area.AllocatedArea == 8m));
        Assert.Empty(await HistoryAsync(unit.Id));

        var overrides = manual.ToDictionary(lease => lease.Id, _ => 7m);
        var fixedPreview = await service.PreviewAsync(Plan(unit, 10, overrides));
        Assert.False(fixedPreview.HasErrors);

        await service.ApplyAsync(Plan(unit, 10, overrides), fixedPreview.Hash);

        Ctx.ChangeTracker.Clear();
        Assert.Equal(10, (await Ctx.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id)).Capacity);
        Assert.All(await AreasAsync(unit.Id), area => Assert.Equal(7m, area.AllocatedArea));
    }

    [Fact]
    public async Task T09_OpenLeaseCountAboveNewCapacity_ShouldReturnCountExceeded()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        for (var i = 0; i < 12; i++)
            await SeedAreaLeaseAsync(unit, 5m, 5m, LeaseAreaMode.Automatic);

        var preview = await CreatePlan().PreviewAsync(Plan(unit, 10));

        var error = Assert.Single(preview.Errors, e => e.Code == CapacityPlanCodes.CountExceeded);
        Assert.Contains("12", error.Message);
    }

    [Fact]
    public async Task T04_CapacityAboveMaximum_ShouldReturnAboveMaximum()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);

        var preview = await CreatePlan(5m).PreviewAsync(Plan(unit, 20));

        var error = Assert.Single(preview.Errors);
        Assert.Equal(CapacityPlanCodes.AboveMaximum, error.Code);
        Assert.Contains("14", error.Message);
        Assert.Equal(14, preview.MaximumCapacity);
    }

    [Fact]
    public async Task Preview_ShouldReturnInvalid_WhenCapacityNotPositive()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);

        var preview = await CreatePlan().PreviewAsync(Plan(unit, 0));

        Assert.Contains(preview.Errors, error => error.Code == CapacityPlanCodes.Invalid);
    }

    [Fact]
    public async Task T17_FutureDraft_ShouldUpdateArea_WithoutHistory()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        await SeedAreaLeaseAsync(
            unit, 7m, 7m, LeaseAreaMode.Automatic, LeaseStatus.Draft, Today.AddDays(30), Today.AddDays(90));
        var service = CreatePlan();

        var preview = await service.PreviewAsync(Plan(unit, 14));
        Assert.False(preview.HasErrors);
        await service.ApplyAsync(Plan(unit, 14), preview.Hash);

        Ctx.ChangeTracker.Clear();
        Assert.Equal(5m, Assert.Single(await AreasAsync(unit.Id)).AllocatedArea);
        Assert.Equal(0, await AreaHistoryCountAsync(unit.Id));
    }

    [Fact]
    public async Task Apply_ShouldReturnPreviewOutdated_WhenDataChangedAfterPreview()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 7m, LeaseAreaMode.Automatic);
        var service = CreatePlan();
        var preview = await service.PreviewAsync(Plan(unit, 14));

        lease.LeaseArea!.AllocatedArea = 6m;
        await Ctx.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.ApplyAsync(Plan(unit, 14), preview.Hash));
        Assert.Equal(CapacityPlanCodes.PreviewOutdated, exception.Code);
    }

    [Fact]
    public async Task Preview_ShouldReturnForbidden_WhenUnitOutOfScope()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var input = new CapacityPlanInput(unit.Id, 10, new LeaseAccessScopeInput([], []));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreatePlan().PreviewAsync(input));
        Assert.Equal(CapacityPlanCodes.OutOfScope, exception.Code);
    }

    [Fact]
    public async Task PropertyForm_ShouldRejectDifferentCapacity_ForExistingCapacityUnit()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var input = ToUpdateInput(property, unit);
        input.Units[0].Capacity = 8;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.CapacityChangeNotAllowed", exception.Code);
        Assert.Equal("Units[0].Capacity", exception.Field);
    }

    [Fact]
    public async Task PropertyForm_ShouldKeepCapacity_WhenSubmittedEmpty()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var input = ToUpdateInput(property, unit);
        input.Units[0].Capacity = null;
        input.Units[0].Name = "Yeni Ad";
        await CreateService().UpdateWithChildrenAsync(input);

        Ctx.ChangeTracker.Clear();
        Assert.Equal(5, (await Ctx.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id)).Capacity);
        Assert.Empty(await HistoryAsync(unit.Id));
    }

    [Fact]
    public async Task PropertyForm_SingleUnit_ShouldRejectDifferentCapacity_AndKeepWhenEmpty()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.SingleUnit, 70m, 0m);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 5);

        var different = ToUpdateInput(property, unit);
        different.SingleUnitCapacity = 8;
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(different));
        Assert.Equal("Property.CapacityChangeNotAllowed", exception.Code);
        Assert.Equal(nameof(UpdatePropertyInput.SingleUnitCapacity), exception.Field);

        Ctx.ChangeTracker.Clear();
        var empty = ToUpdateInput(property, unit);
        empty.SingleUnitCapacity = null;
        await CreateService().UpdateWithChildrenAsync(empty);

        Ctx.ChangeTracker.Clear();
        Assert.Equal(5, (await Ctx.Units.AsNoTracking().SingleAsync(u => u.Id == unit.Id)).Capacity);
    }

    [Fact]
    public async Task PropertyForm_NewUnit_ShouldRejectCapacityAboveMaximum()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);

        var input = NewCreateInput(propertyType, UnitStructure.MultipleUnits);
        input.Units.Add(new PropertyUnitInputDto
        {
            UnitNo = "1", FloorNo = 1, Name = "Salon", Area = 70m, UnitTypeId = capacityType.Id, Capacity = 15
        });

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().CreateAsync(input));
        Assert.Equal("Property.CapacityAboveMaximum", exception.Code);
        Assert.Equal("Units[0].Capacity", exception.Field);
        Assert.Contains("14", exception.Message);
    }

    [Fact]
    public async Task PropertyForm_SingleUnitCreate_ShouldRejectCapacityAboveMaximum()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);

        var input = NewCreateInput(propertyType, UnitStructure.SingleUnit);
        input.SingleUnitTypeId = capacityType.Id;
        input.SingleUnitCapacity = 15;
        input.ClosedArea = 70m;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().CreateAsync(input));
        Assert.Equal("Property.CapacityAboveMaximum", exception.Code);
        Assert.Equal(nameof(CreatePropertyInput.SingleUnitCapacity), exception.Field);
        Assert.Contains("14", exception.Message);
    }

    [Fact]
    public async Task PropertyForm_AreaDecrease_ShouldRejectWhenCapacityExceedsNewLimit()
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        var unit = await SeedUnitAsync(property, capacityType, "1", 70m, 14);

        var input = ToUpdateInput(property, unit);
        input.Units[0].Area = 50m;

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateService().UpdateWithChildrenAsync(input));
        Assert.Equal("Property.CapacityAboveMaximum", exception.Code);
        Assert.Contains("10", exception.Message);
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

    [Fact]
    public async Task Preview_WithOptimize_ShouldSuggestReductions_WithoutChangingRowsOrHash()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        var ali = await SeedAreaLeaseAsync(unit, 10m, 9m, LeaseAreaMode.Manual);
        var ayse = await SeedAreaLeaseAsync(unit, 10m, 7m, LeaseAreaMode.Manual);
        for (var i = 0; i < 8; i++)
            await SeedAreaLeaseAsync(unit, 7m, 5m, LeaseAreaMode.Automatic);
        var service = CreatePlan();

        var plain = await service.PreviewAsync(Plan(unit, 14));
        var optimized = await service.PreviewAsync(Plan(unit, 14) with { Optimize = true });

        Assert.Null(plain.Optimization);
        Assert.Equal(plain.Hash, optimized.Hash);
        Assert.Equal(plain.Rows, optimized.Rows);
        Assert.Equal(plain.Errors, optimized.Errors);
        Assert.Equal(plain.Warnings, optimized.Warnings);
        var optimization = optimized.Optimization!;
        Assert.True(optimization.IsNeeded);
        Assert.Empty(optimization.Shortfalls);
        Assert.Equal(2, optimization.Suggestions.Count);
        Assert.Equal(5.25m, optimization.Suggestions.Single(item => item.LeaseId == ali.Id).AllocatedArea);
        Assert.Equal(4.75m, optimization.Suggestions.Single(item => item.LeaseId == ayse.Id).AllocatedArea);
    }

    [Fact]
    public async Task Preview_WithOptimize_ShouldReturnNull_WhenCountExceeded()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        for (var i = 0; i < 4; i++)
            await SeedAreaLeaseAsync(unit, 7m, 7m, LeaseAreaMode.Manual);

        var preview = await CreatePlan().PreviewAsync(Plan(unit, 3) with { Optimize = true });

        Assert.Contains(preview.Errors, error => error.Code == CapacityPlanCodes.CountExceeded);
        Assert.Null(preview.Optimization);
    }

    [Fact]
    public async Task Preview_ShouldWarnNormTargetShort_ForFutureDayWithWorstShortage()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        var today = DateTime.Today;
        for (var i = 0; i < 2; i++)
            await SeedAreaLeaseAsync(unit, 7m, 5m, LeaseAreaMode.Automatic,
                start: today.AddDays(-10), end: today.AddDays(365));
        await SeedAreaLeaseAsync(unit, 20m, 20m, LeaseAreaMode.Manual, LeaseStatus.Draft,
            today.AddDays(30), today.AddDays(365));

        var preview = await CreatePlan().PreviewAsync(Plan(unit, 14));

        var warning = Assert.Single(preview.Warnings, w => w.Code == CapacityPlanCodes.NormTargetShort);
        Assert.Equal(40m, warning.RemainingArea);
        Assert.Equal(55m, warning.NormTarget);
        Assert.Equal(today.AddDays(30), warning.Day);
        Assert.DoesNotContain(today.AddDays(30).ToString("dd.MM.yyyy"), warning.Message);
    }

    [Fact]
    public async Task Preview_ShouldLeaveDayEmpty_WhenWarningComesFromToday()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        for (var i = 0; i < 2; i++)
            await SeedAreaLeaseAsync(unit, 30m, 30m, LeaseAreaMode.Manual);

        var preview = await CreatePlan().PreviewAsync(Plan(unit, 14));

        var warning = Assert.Single(preview.Warnings, w => w.Code == CapacityPlanCodes.NormTargetShort);
        Assert.Null(warning.Day);
    }

    [Fact]
    public async Task Preview_ShouldFillDayOnAreaOverflow_ForFutureLease()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        var today = DateTime.Today;
        for (var i = 0; i < 2; i++)
            await SeedAreaLeaseAsync(unit, 20m, 20m, LeaseAreaMode.Manual,
                start: today.AddDays(-10), end: today.AddDays(365));
        await SeedAreaLeaseAsync(unit, 35m, 35m, LeaseAreaMode.Manual, LeaseStatus.Draft,
            today.AddDays(30), today.AddDays(365));

        var preview = await CreatePlan().PreviewAsync(Plan(unit, 14));

        var error = Assert.Single(preview.Errors, e => e.Code == CapacityPlanCodes.AreaOverflow);
        Assert.Equal(today.AddDays(30), error.Day);
    }
}
