using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class CapacityOccupantQueryTests : IDisposable
{
    private static readonly DateTime Start = new(2030, 1, 1);
    private static readonly DateTime End = new(2030, 12, 31);

    private readonly DatabaseFixture _fixture;
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly LeaseServiceTestHarness _harness;
    private readonly LeaseRepository _leases;
    private readonly UnitRepository _units;

    public CapacityOccupantQueryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
        _harness = new LeaseServiceTestHarness(_context);
        _leases = new LeaseRepository(_context);
        _units = new UnitRepository(_context);
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task OpenLeaseWithoutLeaseArea_ShouldCountAsOneOccupantWithZeroArea()
    {
        var (seed, unit) = await SeedUnitAsync();
        await SeedLeaseAsync(seed, unit, LeaseStatus.Draft, null);

        var open = await _leases.GetOpenLeaseAreasForUnitAsync(unit.Id, Start, End, null);
        var occupants = await _units.GetCapacityOccupantsAsync(unit.Id, Start);

        var a = Assert.Single(open);
        var b = Assert.Single(occupants);
        Assert.Equal(0m, a.AllocatedArea);
        Assert.Equal(0m, b.AllocatedArea);
        Assert.Null(b.RentedArea);
        Assert.Null(b.Mode);
    }

    [Fact]
    public async Task DeletedAndClosedLeases_ShouldBeExcluded()
    {
        var (seed, unit) = await SeedUnitAsync();
        await SeedLeaseAsync(seed, unit, LeaseStatus.Terminated, 5);
        await SeedLeaseAsync(seed, unit, LeaseStatus.Ended, 5);
        var deleted = await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5);
        deleted.IsDeleted = true;
        await _context.SaveChangesAsync();

        Assert.Empty(await _leases.GetOpenLeaseAreasForUnitAsync(unit.Id, Start, End, null));
        Assert.Empty(await _units.GetCapacityOccupantsAsync(unit.Id, Start));
    }

    [Fact]
    public async Task Projection_ShouldFillLeaseIdRentedAreaAndMode()
    {
        var (seed, unit) = await SeedUnitAsync();
        var lease = await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5, rented: 7, mode: LeaseAreaMode.Automatic);

        var occupant = Assert.Single(await _units.GetCapacityOccupantsAsync(unit.Id, Start));

        Assert.Equal(lease.Id, occupant.LeaseId);
        Assert.Equal(7m, occupant.RentedArea);
        Assert.Equal(5m, occupant.AllocatedArea);
        Assert.Equal(LeaseAreaMode.Automatic, occupant.Mode);
    }

    [Fact]
    public async Task GetCapacityOccupants_ShouldIncludeLeaseEndingSameDayWithTime()
    {
        var (seed, unit) = await SeedUnitAsync();
        var end = new DateTime(2030, 6, 15, 10, 30, 0);
        await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5, end: end);

        var sameDay = await _units.GetCapacityOccupantsAsync(unit.Id, new DateTime(2030, 6, 15, 23, 0, 0));
        var nextDay = await _units.GetCapacityOccupantsAsync(unit.Id, new DateTime(2030, 6, 16));

        Assert.Single(sameDay);
        Assert.Empty(nextDay);
    }

    [Fact]
    public async Task GetOpenLeaseAreas_ShouldHonorExcludeAndRange()
    {
        var (seed, unit) = await SeedUnitAsync();
        var first = await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5);
        var second = await SeedLeaseAsync(seed, unit, LeaseStatus.Draft, 5);
        await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5, new DateTime(2035, 1, 1), new DateTime(2035, 12, 31));

        var result = await _leases.GetOpenLeaseAreasForUnitAsync(unit.Id, Start, End, first.Id);

        var occupant = Assert.Single(result);
        Assert.Equal(second.Id, occupant.LeaseId);
    }

    [Fact]
    public async Task Locks_ShouldRequireTransaction()
    {
        await using var plain = _fixture.CreateContext();
        var repository = new UnitRepository(plain);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AcquireCapacityLocksAsync(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AcquireGlobalCapacityLockExclusiveAsync());
    }

    [Fact]
    public async Task Locks_ShouldSucceedInsideTransaction()
    {
        await _units.AcquireCapacityLocksAsync(123456);
        await _units.AcquireGlobalCapacityLockExclusiveAsync();
    }

    [Fact]
    public async Task LeaseAreaHistory_ShouldReturnInOrderWithValues()
    {
        var (seed, unit) = await SeedUnitAsync();
        var lease = await SeedLeaseAsync(seed, unit, LeaseStatus.Active, 5);
        var area = await _context.LeaseAreas.SingleAsync(item => item.LeaseId == lease.Id);

        _leases.AddLeaseAreaHistory(area, LeaseAreaMode.Manual, new DateTime(2030, 3, 1, 14, 0, 0), LeaseAreaChangeReason.ManualEdit);
        await _context.SaveChangesAsync();
        _leases.AddLeaseAreaHistory(area, LeaseAreaMode.Automatic, new DateTime(2030, 1, 1, 9, 0, 0), LeaseAreaChangeReason.Activation);
        await _context.SaveChangesAsync();

        var history = await _leases.GetLeaseAreaHistoryAsync(lease.Id);

        Assert.Equal(2, history.Count);
        Assert.Equal(new DateTime(2030, 1, 1), history[0].EffectiveFrom);
        Assert.Equal(LeaseAreaMode.Automatic, history[0].Mode);
        Assert.Equal(LeaseAreaChangeReason.Activation, history[0].Reason);
        Assert.Equal(new DateTime(2030, 3, 1), history[1].EffectiveFrom);
        Assert.Equal(LeaseAreaMode.Manual, history[1].Mode);
        Assert.Equal(LeaseAreaChangeReason.ManualEdit, history[1].Reason);
        Assert.Equal(5m, history[1].AllocatedArea);
    }

    [Fact]
    public async Task UpsertLeaseArea_ShouldWriteAndUpdateMode()
    {
        var (seed, unit) = await SeedUnitAsync();
        var lease = await SeedLeaseAsync(seed, unit, LeaseStatus.Draft, null);

        var added = await _leases.UpsertLeaseAreaAsync(lease.Id, 6, 4, LeaseAreaMode.Automatic);
        await _context.SaveChangesAsync();
        Assert.Equal(LeaseAreaMode.Automatic, (await _context.LeaseAreas.AsNoTracking().SingleAsync(a => a.Id == added.Id)).AllocationMode);

        var updated = await _leases.UpsertLeaseAreaAsync(lease.Id, 8, 8, LeaseAreaMode.Manual);
        await _context.SaveChangesAsync();

        Assert.Equal(added.Id, updated.Id);
        var stored = await _context.LeaseAreas.AsNoTracking().SingleAsync(a => a.Id == added.Id);
        Assert.Equal(LeaseAreaMode.Manual, stored.AllocationMode);
        Assert.Equal(8m, stored.RentedArea);
        Assert.Equal(8m, stored.AllocatedArea);
    }

    private async Task<(LeaseServiceTestHarness.Seed Seed, Unit Unit)> SeedUnitAsync()
    {
        var seed = await _harness.SeedAsync();
        var unitType = new UnitType
        {
            Name = $"Kontenjan Tipi {seed.Suffix}",
            Code = $"COQ{seed.Suffix}",
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
            Area = 70,
            Capacity = 10
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return (seed, unit);
    }

    private async Task<Lease> SeedLeaseAsync(
        LeaseServiceTestHarness.Seed seed,
        Unit unit,
        LeaseStatus status,
        decimal? allocatedArea,
        DateTime? start = null,
        DateTime? end = null,
        decimal? rented = null,
        LeaseAreaMode mode = LeaseAreaMode.Manual)
    {
        var lease = new Lease
        {
            LeaseNo = $"CQ-{Guid.NewGuid().ToString("N")[..8]}",
            UnitId = unit.Id,
            TenantId = seed.TenantId,
            Status = status,
            StartDate = start ?? Start,
            EndDate = end ?? End,
            RentalModeSnapshot = UnitRentalMode.CapacityBased,
            CreatedBy = seed.ApplicantId,
            LeaseArea = allocatedArea == null
                ? null
                : new LeaseArea
                {
                    RentedArea = rented ?? allocatedArea.Value,
                    AllocatedArea = allocatedArea.Value,
                    AllocationMode = mode
                }
        };
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();
        return lease;
    }
}
