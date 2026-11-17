using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseGroupServiceTests : IDisposable
{
    private static readonly DateTime Start = new(2030, 1, 1);
    private static readonly DateTime End = new(2030, 12, 31);

    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly LeaseServiceTestHarness _harness;

    public LeaseGroupServiceTests(DatabaseFixture fixture)
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
    public async Task CreateGroup_ShouldRejectAreaSumMismatch()
    {
        var (seed, tenantB) = await SeedAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 20)));

        Assert.Equal("LeaseGroup.AreaSumMismatch", exception.Code);
    }

    [Fact]
    public async Task CreateGroup_ShouldRejectSingleTenant()
    {
        var (seed, _) = await SeedAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateGroupAsync(seed, (seed.TenantId, 60)));

        Assert.Equal("LeaseGroup.TooFewLeases", exception.Code);
    }

    [Fact]
    public async Task CreateGroup_ShouldRejectSameTenantWithTwoShares()
    {
        var (seed, _) = await SeedAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateGroupAsync(seed, (seed.TenantId, 30), (seed.TenantId, 30)));

        Assert.Equal("LeaseGroup.DuplicateTenant", exception.Code);
    }

    [Fact]
    public async Task CreateGroup_ShouldRejectCapacityBasedUnit()
    {
        var (seed, tenantB) = await SeedAsync();
        var unitType = new UnitType
        {
            Name = $"Kontenjan {seed.Suffix}",
            Code = $"GRP{seed.Suffix}",
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
            Area = 60,
            Capacity = 10
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30), unitId: unit.Id));

        Assert.Equal("Lease.GroupUnitNotUnitBased", exception.Code);
    }

    [Fact]
    public async Task CreateGroup_ShouldCreateDraftsWithAreasAndSnapshots()
    {
        var (seed, tenantB) = await SeedAsync();

        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        var leases = await _context.Leases
            .Include(lease => lease.LeaseArea)
            .Where(lease => lease.LeaseGroupId == group.Id)
            .ToListAsync();
        Assert.Equal(2, leases.Count);
        Assert.All(leases, lease =>
        {
            Assert.Equal(LeaseStatus.Draft, lease.Status);
            Assert.Equal(60m, lease.UnitAreaSnapshot);
            Assert.Equal(UnitRentalMode.UnitBased, lease.RentalModeSnapshot);
            Assert.Equal(30m, lease.LeaseArea!.RentedArea);
            Assert.Equal(30m, lease.LeaseArea.AllocatedArea);
        });
    }

    [Fact]
    public async Task CreateGroup_ShouldBlockSecondGroupAndSingleDraftOnSameUnit()
    {
        var (seed, tenantB) = await SeedAsync();
        var tenantC = await _harness.AddTenantAsync($"c{seed.Suffix}");
        await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        var secondGroup = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _harness.CreateGroupService(seed.ApplicantId).CreateGroupAsync(
                new CreateLeaseGroupInput(
                    seed.UnitId,
                    new DateTime(2031, 1, 1),
                    new DateTime(2031, 12, 31),
                    DueDateRuleType.FixedDayOfMonth,
                    10,
                    null,
                    false,
                    [
                        new LeaseGroupMemberInput(null, tenantC, 30, _harness.Rates(seed)),
                        new LeaseGroupMemberInput(null, tenantB, 30, _harness.Rates(seed))
                    ],
                    seed.ApplicantId,
                    LeaseServiceTestHarness.Scope())));
        var single = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _harness.CreateDraftAsync(seed, new DateTime(2031, 1, 1), new DateTime(2031, 12, 31)));

        Assert.Equal("Lease.OpenApplicationConflict", secondGroup.Code);
        Assert.Equal("Lease.OpenApplicationConflict", single.Code);
    }

    [Fact]
    public async Task ApproveGroup_ShouldRejectWhenAnyLeaseRevisionRequested()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var leases = await LoadLeasesAsync(group.Id);
        await AddDocumentsAsync(leases);
        await RequestRevisionAsync(seed, leases[0]);

        var exception = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateGroupService(seed.ReviewerId).ApproveGroupAsync(
                new ApproveLeaseGroupInput(group.Id, null, seed.ReviewerId, LeaseServiceTestHarness.Scope())));

        Assert.Equal("Lease.GroupRevisionRequested", exception.Code);
        Assert.All(await LoadLeasesAsync(group.Id), lease => Assert.NotEqual(LeaseStatus.Active, lease.Status));
    }

    [Fact]
    public async Task ApproveGroup_ShouldActivateAllLeasesAndGenerateCharges()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        await AddDocumentsAsync(await LoadLeasesAsync(group.Id));

        await _harness.CreateGroupService(seed.ReviewerId).ApproveGroupAsync(
            new ApproveLeaseGroupInput(group.Id, null, seed.ReviewerId, LeaseServiceTestHarness.Scope()));

        var leases = await LoadLeasesAsync(group.Id);
        Assert.All(leases, lease => Assert.Equal(LeaseStatus.Active, lease.Status));
        foreach (var lease in leases)
            Assert.True(await _context.Charges.AnyAsync(charge => charge.LeaseId == lease.Id));
    }

    [Fact]
    public async Task Creator_ShouldApproveOwnGroup()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        await AddDocumentsAsync(await LoadLeasesAsync(group.Id));

        await _harness.CreateGroupService(seed.ApplicantId).ApproveGroupAsync(
            new ApproveLeaseGroupInput(group.Id, null, seed.ApplicantId, LeaseServiceTestHarness.Scope()));

        Assert.All(await LoadLeasesAsync(group.Id), lease => Assert.Equal(LeaseStatus.Active, lease.Status));
    }

    [Fact]
    public async Task Creator_ShouldDeleteOwnGroupDraft()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        await _harness.CreateGroupService(seed.ApplicantId).DeleteGroupDraftAsync(
            new DeleteLeaseGroupDraftInput(group.Id, "Vazgeçildi", seed.ApplicantId, LeaseServiceTestHarness.Scope()));

        Assert.Empty(await LoadLeasesAsync(group.Id));
        Assert.False(await _context.LeaseGroups.AnyAsync(item => item.Id == group.Id));
        var deleted = await _context.Leases.IgnoreQueryFilters()
            .Where(lease => lease.LeaseGroupId == group.Id)
            .ToListAsync();
        Assert.Equal(2, deleted.Count);
        Assert.All(deleted, lease => Assert.True(lease.IsDeleted));
    }

    [Fact]
    public async Task SingleOperations_ShouldBeRejectedForGroupLease()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var lease = (await LoadLeasesAsync(group.Id))[0];
        var scope = LeaseServiceTestHarness.Scope();

        var approve = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ReviewerId).ApproveAsync(
                new ApproveLeaseInput(lease.Id, null, lease.RowVersion, seed.ReviewerId, scope)));
        var update = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ApplicantId).UpdateDraftAsync(
                new UpdateLeaseDraftInput(
                    lease.Id, seed.UnitId, lease.TenantId, Start, End,
                    DueDateRuleType.FixedDayOfMonth, 10, null, _harness.Rates(seed),
                    lease.RowVersion, seed.ApplicantId, scope)));
        var delete = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ReviewerId).DeleteDraftAsync(
                new DeleteLeaseDraftInput(lease.Id, "Silme", lease.RowVersion, seed.ReviewerId, scope)));

        Assert.Equal("Lease.GroupOperationRequired", approve.Code);
        Assert.Equal("Lease.GroupOperationRequired", update.Code);
        Assert.Equal("Lease.GroupOperationRequired", delete.Code);
    }

    [Fact]
    public async Task ApproveGroup_ShouldBeAllOrNothingWhenOneLeaseFailsValidation()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var leases = await LoadLeasesAsync(group.Id);
        await _harness.AddAllRequiredDocumentsAsync(leases[0].Id);

        var exception = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateGroupService(seed.ReviewerId).ApproveGroupAsync(
                new ApproveLeaseGroupInput(group.Id, null, seed.ReviewerId, LeaseServiceTestHarness.Scope())));

        Assert.Equal("Lease.RequiredDocumentsMissing", exception.Code);
        _context.ChangeTracker.Clear();
        var reloaded = await LoadLeasesAsync(group.Id);
        Assert.Equal(2, reloaded.Count);
        Assert.All(reloaded, lease => Assert.NotEqual(LeaseStatus.Active, lease.Status));
        var ids = reloaded.Select(lease => lease.Id).ToList();
        Assert.False(await _context.Charges.AnyAsync(charge => ids.Contains(charge.LeaseId!.Value)));
    }

    [Fact]
    public async Task ResubmitRevision_ShouldBeRejectedForGroupLease()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var lease = (await LoadLeasesAsync(group.Id))[0];
        await RequestRevisionAsync(seed, lease);
        var current = await _context.Leases.AsNoTracking().SingleAsync(item => item.Id == lease.Id);

        var exception = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ApplicantId).ResubmitRevisionAsync(
                new ResubmitLeaseRevisionInput(
                    lease.Id, seed.UnitId, lease.TenantId, Start, End,
                    DueDateRuleType.FixedDayOfMonth, 10, null, _harness.Rates(seed),
                    "Düzeltildi", current.RowVersion, seed.ApplicantId, LeaseServiceTestHarness.Scope())));

        Assert.Equal("Lease.GroupOperationRequired", exception.Code);
    }

    [Fact]
    public async Task ExtendAndTerminate_ShouldBeRejectedForActiveGroupLease()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        await AddDocumentsAsync(await LoadLeasesAsync(group.Id));
        await _harness.CreateGroupService(seed.ReviewerId).ApproveGroupAsync(
            new ApproveLeaseGroupInput(group.Id, null, seed.ReviewerId, LeaseServiceTestHarness.Scope()));
        _context.ChangeTracker.Clear();
        var lease = (await LoadLeasesAsync(group.Id))[0];
        Assert.Equal(LeaseStatus.Active, lease.Status);
        var scope = LeaseServiceTestHarness.Scope();

        var extend = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ApplicantId).ExtendAsync(
                new ExtendLeaseInput(
                    lease.Id, End.AddYears(1), false, 0, null, null, false, false, [], scope)));
        var terminate = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ApplicantId).TerminateAsync(
                new TerminateLeaseInput(lease.Id, Start.AddMonths(3), "Test", null, scope)));

        Assert.Equal("Lease.GroupOperationRequired", extend.Code);
        Assert.Equal("Lease.GroupOperationRequired", terminate.Code);
    }

    [Fact]
    public async Task UpdateGroup_ShouldChangeAreasAndRejectSumMismatch()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var service = _harness.CreateGroupService(seed.ApplicantId);
        var leases = await LoadLeasesAsync(group.Id);
        var a = leases.Single(lease => lease.TenantId == seed.TenantId);
        var b = leases.Single(lease => lease.TenantId == tenantB);

        var mismatch = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.UpdateGroupAsync(UpdateInput(seed, group.Id, (a.Id, seed.TenantId, 35), (b.Id, tenantB, 30))));
        await service.UpdateGroupAsync(UpdateInput(seed, group.Id, (a.Id, seed.TenantId, 35), (b.Id, tenantB, 25)));

        Assert.Equal("LeaseGroup.AreaSumMismatch", mismatch.Code);
        var areas = await _context.LeaseAreas
            .Where(area => area.LeaseId == a.Id || area.LeaseId == b.Id)
            .ToDictionaryAsync(area => area.LeaseId, area => area.RentedArea);
        Assert.Equal(35m, areas[a.Id]);
        Assert.Equal(25m, areas[b.Id]);
    }

    [Fact]
    public async Task ResubmitGroup_ShouldReturnRevisionRequestedLeasesToDraftAfterAreaFix()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var leases = await LoadLeasesAsync(group.Id);
        var a = leases.Single(lease => lease.TenantId == seed.TenantId);
        var b = leases.Single(lease => lease.TenantId == tenantB);
        await RequestRevisionAsync(seed, a);
        var service = _harness.CreateGroupService(seed.ApplicantId);

        await service.UpdateGroupAsync(UpdateInput(seed, group.Id, (a.Id, seed.TenantId, 35), (b.Id, tenantB, 25)));
        await service.ResubmitGroupAsync(new ResubmitLeaseGroupInput(
            group.Id, "Düzeltildi", seed.ApplicantId, LeaseServiceTestHarness.Scope()));

        Assert.All(await LoadLeasesAsync(group.Id), lease => Assert.Equal(LeaseStatus.Draft, lease.Status));
    }

    private async Task<(LeaseServiceTestHarness.Seed Seed, int TenantB)> SeedAsync()
    {
        var seed = await _harness.SeedAsync(area: 60);
        return (seed, await _harness.AddTenantAsync($"b{seed.Suffix}"));
    }

    private async Task<LeaseGroup> CreateGroupAsync(
        LeaseServiceTestHarness.Seed seed,
        params (int TenantId, decimal Area)[] shares)
        => await CreateGroupAsync(seed, shares, null);

    private async Task<LeaseGroup> CreateGroupAsync(
        LeaseServiceTestHarness.Seed seed,
        (int TenantId, decimal Area) first,
        (int TenantId, decimal Area) second,
        int? unitId)
        => await CreateGroupAsync(seed, [first, second], unitId);

    private async Task<LeaseGroup> CreateGroupAsync(
        LeaseServiceTestHarness.Seed seed,
        (int TenantId, decimal Area)[] shares,
        int? unitId)
    {
        var group = await _harness.CreateGroupService(seed.ApplicantId).CreateGroupAsync(
            new CreateLeaseGroupInput(
                unitId ?? seed.UnitId,
                Start,
                End,
                DueDateRuleType.FixedDayOfMonth,
                10,
                "Paylaşımlı test",
                false,
                shares.Select(share => new LeaseGroupMemberInput(
                    null, share.TenantId, share.Area, _harness.Rates(seed))).ToList(),
                seed.ApplicantId,
                LeaseServiceTestHarness.Scope()));
        // Test bağlamında CreatedBy "system" yazılır; sahiplik kontrolü için başvuru sahibine çevrilir.
        group.CreatedBy = seed.ApplicantId;
        foreach (var lease in await _context.Leases.Where(item => item.LeaseGroupId == group.Id).ToListAsync())
            lease.CreatedBy = seed.ApplicantId;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return group;
    }

    private UpdateLeaseGroupInput UpdateInput(
        LeaseServiceTestHarness.Seed seed,
        int groupId,
        params (int LeaseId, int TenantId, decimal Area)[] rows)
        => new(
            groupId,
            Start,
            End,
            DueDateRuleType.FixedDayOfMonth,
            10,
            "Paylaşımlı test",
            false,
            rows.Select(row => new LeaseGroupMemberInput(
                row.LeaseId, row.TenantId, row.Area, _harness.Rates(seed))).ToList(),
            seed.ApplicantId,
            LeaseServiceTestHarness.Scope());

    private async Task<List<Lease>> LoadLeasesAsync(int groupId)
        => await _context.Leases
            .AsNoTracking()
            .Where(lease => lease.LeaseGroupId == groupId)
            .OrderBy(lease => lease.Id)
            .ToListAsync();

    private async Task AddDocumentsAsync(IEnumerable<Lease> leases)
    {
        foreach (var lease in leases)
            await _harness.AddAllRequiredDocumentsAsync(lease.Id);
    }

    private async Task RequestRevisionAsync(LeaseServiceTestHarness.Seed seed, Lease lease)
    {
        var current = await _context.Leases.AsNoTracking().SingleAsync(item => item.Id == lease.Id);
        await _harness.CreateService(seed.ReviewerId).RequestRevisionAsync(new RequestLeaseRevisionInput(
            lease.Id,
            "Düzeltin",
            current.RowVersion,
            seed.ReviewerId,
            LeaseServiceTestHarness.Scope()));
        _context.ChangeTracker.Clear();
    }
}
