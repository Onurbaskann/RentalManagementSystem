using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
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
    public async Task Create_ShouldEnforceAllocatedAreaRangeWhenRentedAboveNorm()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var low = await CreateAsync(seed, unit, 10, 5);
        var high = await CreateAsync(seed, unit, 10, 10);
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 10, 4));
        var missing = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 10));

        Assert.Equal("Lease.AllocatedAreaOutOfRange", exception.Code);
        Assert.Equal("Lease.AllocatedAreaOutOfRange", missing.Code);
        Assert.Equal(5m, (await _context.LeaseAreas.SingleAsync(a => a.LeaseId == low.Id)).AllocatedArea);
        Assert.Equal(10m, (await _context.LeaseAreas.SingleAsync(a => a.LeaseId == high.Id)).AllocatedArea);
    }

    [Fact]
    public async Task Create_ShouldForceAllocatedAreaToRentedWhenAtOrBelowNorm()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);

        var lease = await CreateAsync(seed, unit, 6, 5.5m);

        var area = await _context.LeaseAreas.SingleAsync(item => item.LeaseId == lease.Id);
        Assert.Equal(6m, area.RentedArea);
        Assert.Equal(6m, area.AllocatedArea);
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
    public async Task Create_ShouldRejectWhenFreeSlotsFallBelowMinimumUsage()
    {
        var (seed, unit) = await SeedCapacityUnitAsync(area: 70, capacity: 10);
        for (var i = 0; i < 5; i++)
            await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 10);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateAsync(seed, unit, 15, 15));

        Assert.Equal("Lease.CapacityFreeAreaBelowMinimum", exception.Code);
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
        decimal? allocatedArea = null)
        => _harness.CreateService(seed.ApplicantId).CreateDraftAsync(new CreateLeaseDraftInput(
            unit.Id,
            seed.TenantId,
            Start,
            End,
            DueDateRuleType.FixedDayOfMonth,
            10,
            null,
            _harness.Rates(seed),
            seed.ApplicantId,
            LeaseServiceTestHarness.Scope(),
            false,
            rentedArea,
            allocatedArea));

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
