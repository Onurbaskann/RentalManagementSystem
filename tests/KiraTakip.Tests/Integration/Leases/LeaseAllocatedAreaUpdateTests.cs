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
public class LeaseAllocatedAreaUpdateTests(DatabaseFixture fixture) : PropertyUnitTestBase(fixture)
{
    private CapacityPlanService CreatePlan(decimal minimum = 5m)
        => new(
            new UnitRepository(Ctx),
            new LeaseRepository(Ctx),
            new UnitOfWork(Ctx),
            new TestOperationalPolicyProvider(new OperationalPolicySettings { LeaseMinimumUsageArea = minimum }),
            new DefaultAllocationSuggestionStrategy(),
            TimeProvider.System);

    private static UpdateAllocatedAreaInput Update(Lease lease, decimal area, LeaseAccessScopeInput? scope = null)
        => new(lease.Id, area, scope ?? new LeaseAccessScopeInput());

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
        LeaseAreaMode mode = LeaseAreaMode.Automatic,
        LeaseStatus status = LeaseStatus.Active)
    {
        var lease = await SeedLeaseAsync(unit, status, rented);
        lease.LeaseArea!.AllocatedArea = allocated;
        lease.LeaseArea.AllocationMode = mode;
        await Ctx.SaveChangesAsync();
        return lease;
    }

    private async Task<LeaseArea> AreaAsync(int leaseId)
    {
        Ctx.ChangeTracker.Clear();
        return await Ctx.LeaseAreas.AsNoTracking().SingleAsync(area => area.LeaseId == leaseId);
    }

    private Task<List<LeaseAreaHistory>> HistoryAsync(int leaseId)
        => Ctx.LeaseAreaHistories.AsNoTracking()
            .Where(history => history.LeaseArea.LeaseId == leaseId)
            .OrderBy(history => history.Id)
            .ToListAsync();

    private static async Task<BusinessException> FailAsync(Task action)
        => await Assert.ThrowsAnyAsync<BusinessException>(() => action);

    [Fact]
    public async Task T03_ReduceArea_ShouldBecomeManual_AndWriteManualEditHistory()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var ali = await SeedAreaLeaseAsync(unit, 7m, 6m);
        for (var i = 0; i < 9; i++)
            await SeedAreaLeaseAsync(unit, 5m, 5m);

        await CreatePlan().UpdateAllocatedAreaAsync(Update(ali, 5m));

        var area = await AreaAsync(ali.Id);
        Assert.Equal(5m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, area.AllocationMode);
        var history = Assert.Single(await HistoryAsync(ali.Id));
        Assert.Equal(LeaseAreaChangeReason.ManualEdit, history.Reason);
        Assert.Equal(5m, history.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, history.Mode);
        Assert.Equal(Today, history.EffectiveFrom);
        Assert.Equal(50m, (await Ctx.LeaseAreas.AsNoTracking()
            .Where(a => a.Lease.UnitId == unit.Id).SumAsync(a => a.AllocatedArea)));
    }

    [Fact]
    public async Task T13_AfterActivationHistory_ShouldKeepBothRows()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var ali = await SeedAreaLeaseAsync(unit, 7m, 6m);
        Ctx.LeaseAreaHistories.Add(new LeaseAreaHistory
        {
            LeaseAreaId = ali.LeaseArea!.Id,
            AllocatedArea = 6m,
            Mode = LeaseAreaMode.Automatic,
            EffectiveFrom = ali.StartDate.Date,
            Reason = LeaseAreaChangeReason.Activation
        });
        await Ctx.SaveChangesAsync();

        await CreatePlan().UpdateAllocatedAreaAsync(Update(ali, 5m));

        Assert.Equal(5m, (await AreaAsync(ali.Id)).AllocatedArea);
        var history = await HistoryAsync(ali.Id);
        Assert.Equal(2, history.Count);
        Assert.Equal(6m, history[0].AllocatedArea);
        Assert.Equal(LeaseAreaChangeReason.Activation, history[0].Reason);
        Assert.Equal(5m, history[1].AllocatedArea);
        Assert.Equal(LeaseAreaChangeReason.ManualEdit, history[1].Reason);
    }

    [Fact]
    public async Task T18_AutomaticLease_ShouldTurnManual_AndWriteHistory()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 7m);

        await CreatePlan().UpdateAllocatedAreaAsync(Update(lease, 6m));

        var area = await AreaAsync(lease.Id);
        Assert.Equal(6m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, area.AllocationMode);
        Assert.Single(await HistoryAsync(lease.Id));
    }

    [Fact]
    public async Task SameValue_ShouldNotWriteAnything()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 6m);

        await CreatePlan().UpdateAllocatedAreaAsync(Update(lease, 6m));

        Assert.Equal(LeaseAreaMode.Automatic, (await AreaAsync(lease.Id)).AllocationMode);
        Assert.Empty(await HistoryAsync(lease.Id));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public async Task OutsideRange_ShouldReturnAreaOutOfRange(int newArea)
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 6m);

        var error = await FailAsync(CreatePlan().UpdateAllocatedAreaAsync(Update(lease, newArea)));

        Assert.Equal(CapacityPlanCodes.AllocatedAreaOutOfRange, error.Code);
        Assert.Equal(6m, (await AreaAsync(lease.Id)).AllocatedArea);
    }

    [Fact]
    public async Task TotalAboveUnitArea_ShouldReturnAreaOverflow_AndKeepData()
    {
        var unit = await SeedCapacityUnitAsync(70m, 14);
        var ali = await SeedAreaLeaseAsync(unit, 10m, 5m);
        for (var i = 0; i < 13; i++)
            await SeedAreaLeaseAsync(unit, 5m, 5m);

        var error = await FailAsync(CreatePlan().UpdateAllocatedAreaAsync(Update(ali, 8m)));

        Assert.Equal(CapacityPlanCodes.AreaOverflow, error.Code);
        Assert.Contains("3", error.Message);
        var area = await AreaAsync(ali.Id);
        Assert.Equal(5m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Automatic, area.AllocationMode);
        Assert.Empty(await HistoryAsync(ali.Id));
    }

    [Fact]
    public async Task Draft_ShouldUpdateArea_WithoutHistory()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 7m, status: LeaseStatus.Draft);

        await CreatePlan().UpdateAllocatedAreaAsync(Update(lease, 6m));

        var area = await AreaAsync(lease.Id);
        Assert.Equal(6m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, area.AllocationMode);
        Assert.Empty(await HistoryAsync(lease.Id));
    }

    [Theory]
    [InlineData(LeaseStatus.Terminated)]
    [InlineData(LeaseStatus.Ended)]
    public async Task ClosedLease_ShouldReturnNotEditable(LeaseStatus status)
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 7m, status: status);

        var error = await FailAsync(CreatePlan().UpdateAllocatedAreaAsync(Update(lease, 6m)));

        Assert.IsType<BusinessValidationException>(error);
        Assert.Equal(CapacityPlanCodes.AllocatedAreaNotEditable, error.Code);
        Assert.Equal(7m, (await AreaAsync(lease.Id)).AllocatedArea);
    }

    [Fact]
    public async Task GetAllocatedAreaInfo_ShouldReturnValuesAndHistory()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 7m);
        var service = CreatePlan();
        await service.UpdateAllocatedAreaAsync(Update(lease, 6m));

        var info = await service.GetAllocatedAreaInfoAsync(lease.Id, new LeaseAccessScopeInput());

        Assert.NotNull(info);
        Assert.Equal(7m, info.RentedArea);
        Assert.Equal(6m, info.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, info.Mode);
        Assert.Equal(5m, info.MinimumUsageArea);
        Assert.True(info.IsEditable);
        var item = Assert.Single(info.History);
        Assert.Equal(LeaseAreaChangeReason.ManualEdit, item.Reason);
    }

    [Fact]
    public async Task GetAllocatedAreaInfo_OutOfScope_ShouldBeRejected()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 7m);
        var scope = new LeaseAccessScopeInput([int.MaxValue], [int.MaxValue]);

        var error = await FailAsync(CreatePlan().GetAllocatedAreaInfoAsync(lease.Id, scope));

        Assert.Equal(CapacityPlanCodes.OutOfScope, error.Code);
    }

    [Fact]
    public async Task OutOfScopeUser_ShouldBeRejected()
    {
        var unit = await SeedCapacityUnitAsync(70m, 10);
        var lease = await SeedAreaLeaseAsync(unit, 7m, 7m);
        var scope = new LeaseAccessScopeInput([int.MaxValue], [int.MaxValue]);

        var error = await FailAsync(CreatePlan().UpdateAllocatedAreaAsync(Update(lease, 6m, scope)));

        Assert.Equal(CapacityPlanCodes.OutOfScope, error.Code);
        Assert.Equal(7m, (await AreaAsync(lease.Id)).AllocatedArea);
    }
}
