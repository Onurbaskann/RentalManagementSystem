using KiraTakip.Data;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseUnitSnapshotTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly LeaseServiceTestHarness _harness;

    public LeaseUnitSnapshotTests(DatabaseFixture fixture)
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
    public async Task CreateDraft_ShouldWriteSnapshotFromCurrentUnit()
    {
        var seed = await _harness.SeedAsync(area: 62.5m);
        var unitType = await _context.UnitTypes.SingleAsync(type => type.Id == seed.UnitTypeId);

        var lease = await _harness.CreateDraftAsync(
            seed,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 10, 31));

        Assert.Equal(unitType.Name, lease.UnitTypeNameSnapshot);
        Assert.Equal(UnitTypeUsage.Rentable, lease.UnitTypeUsageSnapshot);
        Assert.Equal(unitType.RentalMode, lease.RentalModeSnapshot);
        Assert.Equal(62.5m, lease.UnitAreaSnapshot);
    }

    [Fact]
    public async Task UpdateDraft_ShouldRewriteSnapshotWhenUnitChanges()
    {
        var seed = await _harness.SeedAsync(area: 40, unitTypeName: $"Eski Tip {Guid.NewGuid():N}"[..20]);
        var (newType, newUnit) = await _harness.AddUnitAsync(seed, 75, $"Yeni Tip {seed.Suffix}");
        var lease = await _harness.CreateDraftAsync(
            seed,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 10, 31));
        Assert.Equal(40, lease.UnitAreaSnapshot);

        await _harness.CreateService(seed.ApplicantId).UpdateDraftAsync(new UpdateLeaseDraftInput(
            lease.Id,
            newUnit.Id,
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
        await _context.Entry(lease).ReloadAsync();

        Assert.Equal(newUnit.Id, lease.UnitId);
        Assert.Equal(newType.Name, lease.UnitTypeNameSnapshot);
        Assert.Equal(75, lease.UnitAreaSnapshot);
    }

    [Fact]
    public async Task Approve_ShouldKeepSnapshotWhenUnitAndTypeChangeLater()
    {
        var seed = await _harness.SeedAsync(area: 50);
        var unitType = await _context.UnitTypes.SingleAsync(type => type.Id == seed.UnitTypeId);
        var originalName = unitType.Name;
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

        unitType.Name = $"Değişti {seed.Suffix}";
        var unit = await _context.Units.SingleAsync(item => item.Id == seed.UnitId);
        unit.Area = 99;
        await _context.SaveChangesAsync();
        await _context.Entry(lease).ReloadAsync();

        Assert.Equal(LeaseStatus.Active, lease.Status);
        Assert.Equal(originalName, lease.UnitTypeNameSnapshot);
        Assert.Equal(50, lease.UnitAreaSnapshot);
    }
}
