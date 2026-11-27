using KiraTakip.Data;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseAreaSnapshotTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly LeaseServiceTestHarness _harness;

    public LeaseAreaSnapshotTests(DatabaseFixture fixture)
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

    private Task<LeaseArea> GetAreaAsync(int leaseId)
        => _context.LeaseAreas.AsNoTracking().SingleAsync(area => area.LeaseId == leaseId);

    [Fact]
    public async Task CreateDraft_ShouldWriteSnapshotFromCurrentSpace()
    {
        var seed = await _harness.SeedAsync(area: 100m);
        var root = await _context.Spaces.SingleAsync(space => space.Id == seed.SpaceId);
        var child = await SpaceTestSeed.AddChildAsync(_context, root, $"Alt Alan {seed.Suffix}", 62.5m);
        var childType = await _context.SpaceTypes.SingleAsync(type => type.Id == child.SpaceTypeId);

        var lease = await _harness.CreateDraftAsync(
            seed,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 10, 31),
            spaceId: child.Id);

        var area = await GetAreaAsync(lease.Id);
        Assert.Equal(child.Id, area.SpaceId);
        Assert.Equal(child.Code, area.SnapshotCode);
        Assert.Equal(child.Name, area.SnapshotName);
        Assert.Equal(childType.Name, area.SnapshotTypeName);
        Assert.Equal(SpaceUsage.Rentable, area.SnapshotUsage);
        Assert.Equal(62.5m, area.SnapshotArea);
        Assert.Equal($"{root.Name} / {child.Name}", area.SnapshotPath);
    }

    [Fact]
    public async Task UpdateDraft_ShouldRewriteSnapshotWhenSpaceChanges()
    {
        var seed = await _harness.SeedAsync(area: 40);
        var oldSpace = await _context.Spaces.SingleAsync(space => space.Id == seed.SpaceId);
        var newSpace = await _harness.AddSpaceAsync(seed, 75);
        var newType = await _context.SpaceTypes.SingleAsync(type => type.Id == newSpace.SpaceTypeId);
        var lease = await _harness.CreateDraftAsync(
            seed,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 10, 31));
        var before = await GetAreaAsync(lease.Id);
        Assert.Equal(oldSpace.Name, before.SnapshotName);
        Assert.Equal(40, before.SnapshotArea);

        await _harness.CreateService(seed.ApplicantId).UpdateDraftAsync(new UpdateLeaseDraftInput(
            lease.Id,
            newSpace.Id,
            seed.TenantId,
            lease.StartDate,
            lease.EndDate,
            lease.DueDateRuleType,
            lease.DueDay,
            lease.Description,
            _harness.Rates(seed),
            lease.RowVersion,
            seed.ApplicantId,
            LeaseServiceTestHarness.Scope()));

        var after = await GetAreaAsync(lease.Id);
        Assert.Equal(newSpace.Id, after.SpaceId);
        Assert.Equal(newSpace.Name, after.SnapshotName);
        Assert.Equal(newType.Name, after.SnapshotTypeName);
        Assert.Equal(75, after.SnapshotArea);
    }

    [Fact]
    public async Task Approve_ShouldKeepSnapshotWhenSpaceAndTypeChangeLater()
    {
        var seed = await _harness.SeedAsync(area: 50);
        var space = await _context.Spaces.SingleAsync(item => item.Id == seed.SpaceId);
        var spaceType = await _context.SpaceTypes.SingleAsync(type => type.Id == space.SpaceTypeId);
        var originalName = spaceType.Name;
        var originalSpaceName = space.Name;
        var lease = await _harness.CreateDraftAsync(
            seed,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 10, 31));
        await _harness.AddAllRequiredDocumentsAsync(lease.Id);
        await _context.Entry(lease).ReloadAsync();

        await _harness.CreateService(seed.ReviewerId).ApproveAsync(new ApproveLeaseInput(
            lease.Id,
            "Onaylandı",
            lease.RowVersion,
            seed.ReviewerId,
            LeaseServiceTestHarness.Scope()));

        spaceType.Name = $"Değişti {seed.Suffix}";
        space.Name = $"Yeni Ad {seed.Suffix}";
        space.Area = 99;
        await _context.SaveChangesAsync();
        await _context.Entry(lease).ReloadAsync();

        var area = await GetAreaAsync(lease.Id);
        Assert.Equal(LeaseStatus.Active, lease.Status);
        Assert.Equal(originalName, area.SnapshotTypeName);
        Assert.Equal(originalSpaceName, area.SnapshotName);
        Assert.Equal(originalSpaceName, area.SnapshotPath);
        Assert.Equal(50, area.SnapshotArea);
    }
}
