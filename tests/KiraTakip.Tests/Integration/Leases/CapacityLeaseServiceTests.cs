using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class CapacityLeaseServiceTests : IDisposable
{
    private static readonly DateTime Start = new(2030, 1, 1);
    private static readonly DateTime End = new(2030, 12, 31);

    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly LeaseServiceTestHarness _harness;

    public CapacityLeaseServiceTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
        _harness = new LeaseServiceTestHarness(_context);
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task Create_ShouldRejectWhenActiveAndDraftFillCapacity()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);
        for (var i = 0; i < 8; i++)
            await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5);
        for (var i = 0; i < 2; i++)
            await SeedLeaseAsync(seed, unit, LeaseStatus.Draft, 5);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 5));

        Assert.Equal("Lease.CapacityExceeded", exception.Code);
    }

    [Fact]
    public async Task Create_ShouldAllowThreeConcurrentDraftsAndWriteLeaseArea()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var leases = new List<Lease>();
        for (var i = 0; i < 3; i++)
            leases.Add(await CreateAsync(seed, unit, 5));

        Assert.All(leases, lease => Assert.Equal(LeaseStatus.Draft, lease.Status));
        var leaseIds = leases.Select(lease => lease.Id).ToList();
        var areas = await _context.LeaseAreas
            .Where(area => leaseIds.Contains(area.LeaseId))
            .ToListAsync();
        Assert.Equal(3, areas.Count);
        Assert.All(areas, area =>
        {
            Assert.Equal(5m, area.RentedArea);
            Assert.Equal(5m, area.AllocatedArea);
        });
    }

    [Fact]
    public async Task Create_ManualShouldEnforceAllocatedAreaRange()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var low = await CreateAsync(seed, unit, 10, 5, LeaseAreaMode.Manual);
        var high = await CreateAsync(seed, unit, 10, 10, LeaseAreaMode.Manual);
        var below = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 10, 4, LeaseAreaMode.Manual));
        var above = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 6, 7, LeaseAreaMode.Manual));
        var missing = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 10, null, LeaseAreaMode.Manual));

        Assert.Equal("Lease.AllocatedAreaOutOfRange", below.Code);
        Assert.Equal("Lease.AllocatedAreaOutOfRange", above.Code);
        Assert.Equal("Lease.AllocatedAreaOutOfRange", missing.Code);
        Assert.Equal(5m, (await _context.LeaseAreas.SingleAsync(a => a.LeaseId == low.Id)).AllocatedArea);
        Assert.Equal(10m, (await _context.LeaseAreas.SingleAsync(a => a.LeaseId == high.Id)).AllocatedArea);
    }

    [Fact]
    public async Task Create_ManualShouldAllowAllocatedBelowRentedEvenWhenRentedAtOrBelowNorm()
    {
        _harness.MinimumUsageArea = 4;
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var lease = await CreateAsync(seed, unit, 5, 4, LeaseAreaMode.Manual);

        var area = await _context.LeaseAreas.SingleAsync(item => item.LeaseId == lease.Id);
        Assert.Equal(5m, area.RentedArea);
        Assert.Equal(4m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, area.AllocationMode);
    }

    [Fact]
    public async Task Create_ManualShouldRejectRentedBelowMinimum()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 4, 4, LeaseAreaMode.Manual));

        Assert.Equal("Lease.RentedAreaBelowMinimum", exception.Code);
    }

    [Fact]
    public async Task Create_AutomaticShouldCalculateAllocatedAreaAndIgnoreInput()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var above = await CreateAsync(seed, unit, 10, 9, LeaseAreaMode.Automatic);
        var below = await CreateAsync(seed, unit, 5);

        var aboveArea = await _context.LeaseAreas.SingleAsync(a => a.LeaseId == above.Id);
        var belowArea = await _context.LeaseAreas.SingleAsync(a => a.LeaseId == below.Id);
        Assert.Equal(7m, aboveArea.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Automatic, aboveArea.AllocationMode);
        Assert.Equal(5m, belowArea.AllocatedArea);
    }

    [Fact]
    public async Task Create_ShouldCountSameTenantContractsSeparately()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 2);
        await CreateAsync(seed, unit, 5);
        await CreateAsync(seed, unit, 5);
        var (seedSingle, unitSingle) = await SeedCapacityUnitAsync(area: 70, capacity: 1);
        await CreateAsync(seedSingle, unitSingle, 5);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seedSingle, unitSingle, 5));

        Assert.Equal("Lease.CapacityExceeded", exception.Code);
    }

    [Fact]
    public async Task Create_ShouldNotBlockWhenRemainingAreaIsShortAndWarnAfterwards()
    {
        _harness.MinimumUsageArea = 4;
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 14);
        var today = DateTime.Today;
        for (var i = 0; i < 10; i++)
            await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 6, today.AddDays(-5), today.AddDays(60));

        var lease = await CreateAsync(
            seed, unit, 5, 5, LeaseAreaMode.Manual, today, today.AddDays(30));
        var warning = await _harness.CreateService(seed.ApplicantId).GetCapacityWarningAsync(lease.Id);

        Assert.NotNull(warning);
        Assert.Equal(3, warning.FreeSlots);
        Assert.Equal(5m, warning.RemainingArea);
        Assert.True(warning.NormTargetShort);
        Assert.True(warning.MinimumTargetShort);
    }

    [Fact]
    public async Task GetCapacityWarning_ShouldReturnNullWhenTargetsAreMet()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);
        var today = DateTime.Today;
        var lease = await CreateAsync(seed, unit, 5, null, null, today, today.AddDays(30));

        Assert.Null(await _harness.CreateService(seed.ApplicantId).GetCapacityWarningAsync(lease.Id));
    }

    [Fact]
    public async Task Create_ShouldNotRemeasurePastDays()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 1);
        var today = DateTime.Today;
        await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5, today.AddDays(-30), today.AddDays(-1));

        var lease = await CreateAsync(
            seed, unit, 5, null, null, today.AddDays(-10), today.AddDays(30));

        Assert.Equal(LeaseStatus.Draft, lease.Status);
    }

    [Fact]
    public async Task Approve_AutomaticShouldRecalculateAllocatedAreaAndWriteHistory()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);
        var lease = await CreateAsync(seed, unit, 10);
        Assert.Equal(7m, (await _context.LeaseAreas.SingleAsync(a => a.LeaseId == lease.Id)).AllocatedArea);
        unit.Capacity = 14;
        await _context.SaveChangesAsync();
        await _harness.AddAllRequiredDocumentsAsync(lease.Id);

        await _harness.CreateService(seed.ReviewerId).ApproveAsync(
            new ApproveLeaseInput(lease.Id, "Onaylandı", lease.RowVersion, seed.ReviewerId, LeaseServiceTestHarness.Scope()));

        var area = await _context.LeaseAreas.AsNoTracking().SingleAsync(a => a.LeaseId == lease.Id);
        var history = await new LeaseRepository(_context).GetLeaseAreaHistoryAsync(lease.Id);
        Assert.Equal(5m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Automatic, area.AllocationMode);
        var row = Assert.Single(history);
        Assert.Equal(LeaseAreaChangeReason.Activation, row.Reason);
        Assert.Equal(5m, row.AllocatedArea);
    }

    [Fact]
    public async Task Approve_ManualShouldKeepAllocatedAreaAndWriteManualHistory()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);
        var lease = await CreateAsync(seed, unit, 10, 6, LeaseAreaMode.Manual);
        unit.Capacity = 14;
        await _context.SaveChangesAsync();
        await _harness.AddAllRequiredDocumentsAsync(lease.Id);

        await _harness.CreateService(seed.ReviewerId).ApproveAsync(
            new ApproveLeaseInput(lease.Id, "Onaylandı", lease.RowVersion, seed.ReviewerId, LeaseServiceTestHarness.Scope()));

        var area = await _context.LeaseAreas.AsNoTracking().SingleAsync(a => a.LeaseId == lease.Id);
        var row = Assert.Single(await new LeaseRepository(_context).GetLeaseAreaHistoryAsync(lease.Id));
        Assert.Equal(6m, area.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Manual, area.AllocationMode);
        Assert.Equal(LeaseAreaMode.Manual, row.Mode);
        Assert.Equal(6m, row.AllocatedArea);
    }

    [Fact]
    public async Task GetAvailable_ShouldSuggestRemainingAreaForLastSlot()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);
        var today = DateTime.Today;
        for (var i = 0; i < 8; i++)
            await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 6, today.AddDays(-5), today.AddDays(60));
        await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 12, today.AddDays(-5), today.AddDays(60));

        var units = await new UnitRepository(_context).GetAvailableAsync(
            null, null, null, includeCapacityBased: true);

        var option = Assert.Single(units, item => item.Id == unit.Id);
        Assert.Equal(1, option.FreeCapacity);
        Assert.Equal(10m, option.FreeArea);
        Assert.Equal(10m, option.SuggestedLastSlotArea);

        var withoutFlag = await new UnitRepository(_context).GetAvailableAsync(null, null, null);
        Assert.DoesNotContain(withoutFlag, item => item.Id == unit.Id);
    }

    [Fact]
    public async Task GetAvailable_ShouldHideFullCapacityUnitWhenFlagIsSet()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 2);
        var today = DateTime.Today;
        await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5, today.AddDays(-5), today.AddDays(60));
        await SeedLeaseAsync(seed, unit, LeaseStatus.Draft, 5, today.AddDays(-5), today.AddDays(60));

        var units = await new UnitRepository(_context).GetAvailableAsync(
            null, null, null, includeCapacityBased: true);

        Assert.DoesNotContain(units, item => item.Id == unit.Id);
    }

    [Fact]
    public async Task Extend_ShouldRejectWhenNewRangeExceedsCapacity()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 20, capacity: 2);
        var first = await SeedLeaseAsync(
            seed, unit, LeaseStatus.Active, 5, new DateTime(2030, 1, 1), new DateTime(2030, 3, 31));
        await SeedLeaseAsync(
            seed, unit, LeaseStatus.Active, 5, new DateTime(2030, 4, 1), new DateTime(2030, 6, 30));
        await SeedLeaseAsync(
            seed, unit, LeaseStatus.Active, 5, new DateTime(2030, 4, 1), new DateTime(2030, 6, 30));

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _harness.CreateService(seed.ApplicantId).ExtendAsync(new ExtendLeaseInput(
                first.Id,
                new DateTime(2030, 5, 31),
                false,
                0,
                null,
                null,
                false,
                false,
                [],
                LeaseServiceTestHarness.Scope())));

        Assert.Equal("Lease.CapacityExceeded", exception.Code);
    }

    [Fact]
    public async Task Create_ShouldRejectRentedAreaBelowMinimum()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var below = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 4));
        var missing = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, null));

        Assert.Equal("Lease.RentedAreaBelowMinimum", below.Code);
        Assert.Equal("Lease.RentedAreaBelowMinimum", missing.Code);
    }

    [Fact]
    public async Task Create_ShouldRejectWhenUnitCapacityIsMissing()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: null);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 5));

        Assert.Equal("Lease.UnitCapacityMissing", exception.Code);
    }

    [Fact]
    public async Task UnitBasedUnit_ShouldKeepSingleOpenApplicationRuleAndIgnoreAreas()
    {
        var seed = await _harness.SeedAsync();
        var service = _harness.CreateService(seed.ApplicantId);
        CreateLeaseDraftInput Input(DateTime start, DateTime end) => new(
            seed.UnitId,
            seed.TenantId,
            start,
            end,
            DueDateRuleType.FixedDayOfMonth,
            10,
            null,
            _harness.Rates(seed),
            seed.ApplicantId,
            LeaseServiceTestHarness.Scope(),
            false,
            8,
            8);

        var first = await service.CreateDraftAsync(Input(Start, End));
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.CreateDraftAsync(Input(new DateTime(2031, 1, 1), new DateTime(2031, 12, 31))));

        Assert.Equal("Lease.OpenApplicationConflict", exception.Code);
        Assert.False(await _context.LeaseAreas.AnyAsync(area => area.LeaseId == first.Id));
    }

    private async Task<(LeaseServiceTestHarness.Seed Seed, Unit Unit)> SeedCapacityUnitAsync(
        decimal area,
        int? capacity)
    {
        var seed = await _harness.SeedAsync();
        var code = $"CAP{seed.Suffix}";
        var unitType = new UnitType
        {
            Name = $"Kontenjan Tipi {seed.Suffix}",
            Code = code,
            Usage = UnitTypeUsage.Rentable,
            RentalMode = UnitRentalMode.CapacityBased
        };
        _context.UnitTypes.Add(unitType);
        await _context.SaveChangesAsync();
        var unit = new Unit
        {
            PropertyId = seed.PropertyId,
            UnitTypeId = unitType.Id,
            Name = $"Açık Birim {seed.Suffix}",
            Area = area,
            Capacity = capacity
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return (seed, unit);
    }

    private Task<Lease> CreateAsync(
        LeaseServiceTestHarness.Seed seed,
        Unit unit,
        decimal? rentedArea,
        decimal? allocatedArea = null,
        LeaseAreaMode? mode = null,
        DateTime? start = null,
        DateTime? end = null)
        => _harness.CreateService(seed.ApplicantId).CreateDraftAsync(new CreateLeaseDraftInput(
            unit.Id,
            seed.TenantId,
            start ?? Start,
            end ?? End,
            DueDateRuleType.FixedDayOfMonth,
            10,
            null,
            _harness.Rates(seed),
            seed.ApplicantId,
            LeaseServiceTestHarness.Scope(),
            false,
            rentedArea,
            allocatedArea,
            mode));

    private async Task<Lease> SeedLeaseAsync(
        LeaseServiceTestHarness.Seed seed,
        Unit unit,
        LeaseStatus status,
        decimal allocatedArea,
        DateTime? start = null,
        DateTime? end = null)
    {
        var lease = new Lease
        {
            LeaseNo = $"CL-{Guid.NewGuid().ToString("N")[..8]}",
            UnitId = unit.Id,
            TenantId = seed.TenantId,
            Status = status,
            StartDate = start ?? Start,
            EndDate = end ?? End,
            RentalModeSnapshot = UnitRentalMode.CapacityBased,
            CreatedBy = seed.ApplicantId,
            LeaseArea = new LeaseArea
            {
                RentedArea = allocatedArea,
                AllocatedArea = allocatedArea
            }
        };
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();
        return lease;
    }
}
