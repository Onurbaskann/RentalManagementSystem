using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseDraftOverlapTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly LeaseServiceTestHarness _harness;

    public LeaseDraftOverlapTests(DatabaseFixture fixture)
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
    public async Task Extend_ShouldRejectOverlapWithLaterDraft()
    {
        var (seed, active, draft) = await SeedActiveAndLaterDraftAsync();

        var ex = await Assert.ThrowsAnyAsync<BusinessException>(() => ExtendAsync(seed, active));

        Assert.Equal("Lease.ActiveUnitConflict", ex.Code);
        Assert.Equal(LeaseStatus.Draft, draft.Status);
    }

    [Fact]
    public async Task Extend_ShouldRejectOverlapWithLaterRevisionRequestedLease()
    {
        var (seed, active, draft) = await SeedActiveAndLaterDraftAsync();
        draft.Status = LeaseStatus.RevisionRequested;
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAnyAsync<BusinessException>(() => ExtendAsync(seed, active));

        Assert.Equal("Lease.ActiveUnitConflict", ex.Code);
    }

    [Fact]
    public async Task Extend_ShouldSucceedWhenLaterDraftIsDeleted()
    {
        var (seed, active, draft) = await SeedActiveAndLaterDraftAsync();
        draft.IsDeleted = true;
        await _context.SaveChangesAsync();

        await ExtendAsync(seed, active);
        await _context.Entry(active).ReloadAsync();

        Assert.Equal(new DateTime(2027, 6, 30), active.EndDate);
    }

    private async Task<(LeaseServiceTestHarness.Seed Seed, KiraTakip.Models.Entities.Lease Active, KiraTakip.Models.Entities.Lease Draft)>
        SeedActiveAndLaterDraftAsync()
    {
        var seed = await _harness.SeedAsync();
        var otherTenantId = await _harness.AddTenantAsync(seed.Suffix);
        var active = await _harness.CreateDraftAsync(
            seed,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));
        active.Status = LeaseStatus.Active;
        await _context.SaveChangesAsync();
        var draft = await _harness.CreateDraftAsync(
            seed,
            new DateTime(2027, 1, 1),
            new DateTime(2027, 12, 31),
            tenantId: otherTenantId);
        return (seed, active, draft);
    }

    private Task ExtendAsync(LeaseServiceTestHarness.Seed seed, KiraTakip.Models.Entities.Lease active)
        => _harness.CreateService(seed.ReviewerId).ExtendAsync(new ExtendLeaseInput(
            active.Id,
            new DateTime(2027, 6, 30),
            false,
            0,
            null,
            null,
            false,
            false,
            [],
            LeaseServiceTestHarness.Scope()));
}
