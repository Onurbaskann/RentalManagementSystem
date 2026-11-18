using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Leases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseGroupLifecycleTests : IDisposable
{
    private static readonly DateTime Start = new(2030, 1, 1);
    private static readonly DateTime End = new(2030, 12, 31);
    private static readonly DateTime TerminationDate = new(2030, 6, 30);

    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly LeaseServiceTestHarness _harness;

    public LeaseGroupLifecycleTests(DatabaseFixture fixture)
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
    public async Task ExtendGroup_ShouldApplyToAllLeases()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var newEnd = End.AddYears(1);

        await _harness.CreateGroupService(seed.ReviewerId).ExtendGroupAsync(
            new ExtendLeaseGroupInput(
                group.Id, newEnd, false, 0, null, "Grup uzatıldı", false, false, [],
                seed.ReviewerId, LeaseServiceTestHarness.Scope()));
        _context.ChangeTracker.Clear();

        var leases = await LoadLeasesAsync(group.Id);
        Assert.Equal(2, leases.Count);
        foreach (var lease in leases)
        {
            Assert.Equal(LeaseStatus.Active, lease.Status);
            Assert.Equal(newEnd, lease.EndDate);
            Assert.True(await _context.SozlesmeIslemGecmisleri.AnyAsync(log =>
                log.LeaseId == lease.Id
                && log.ActivityType == LeaseActivityType.Extension
                && log.NewEndDate == newEnd));
            Assert.True(await _context.Charges.AnyAsync(charge =>
                charge.LeaseId == lease.Id && charge.PeriodStart >= new DateTime(2031, 1, 1)));
        }
    }

    [Fact]
    public async Task ExtendGroup_ShouldRejectNonExtendableEndDate()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        var exception = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateGroupService(seed.ReviewerId).ExtendGroupAsync(
                new ExtendLeaseGroupInput(
                    group.Id, End, false, 0, null, null, false, false, [],
                    seed.ReviewerId, LeaseServiceTestHarness.Scope())));

        Assert.Equal("Lease.InvalidExtensionDate", exception.Code);
    }

    [Fact]
    public async Task ExtendGroup_ShouldRejectWhenUnitAreaChangedAndExtendNoMember()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var unit = await _context.Units.SingleAsync(item => item.Id == seed.UnitId);
        unit.Area = 80;
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateGroupService(seed.ReviewerId).ExtendGroupAsync(
                new ExtendLeaseGroupInput(
                    group.Id, End.AddYears(1), false, 0, null, null, false, false, [],
                    seed.ReviewerId, LeaseServiceTestHarness.Scope())));
        _context.ChangeTracker.Clear();

        Assert.Equal("Lease.UnitAreaChanged", exception.Code);
        Assert.All(await LoadLeasesAsync(group.Id), lease => Assert.Equal(End, lease.EndDate));
    }

    [Fact]
    public async Task TerminateGroup_ShouldApplyToAllLeasesAndCancelFutureCharges()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        await _harness.CreateGroupService(seed.ReviewerId).TerminateGroupAsync(
            new TerminateLeaseGroupInput(
                group.Id, TerminationDate, "Test fesih", null,
                seed.ReviewerId, LeaseServiceTestHarness.Scope()));
        _context.ChangeTracker.Clear();

        var leases = await LoadLeasesAsync(group.Id);
        foreach (var lease in leases)
        {
            Assert.Equal(LeaseStatus.Terminated, lease.Status);
            Assert.Equal(TerminationDate, lease.TerminationDate);
            var charges = await _context.Charges
                .Where(charge => charge.LeaseId == lease.Id)
                .ToListAsync();
            Assert.Contains(charges, charge => charge.Status == ChargeStatus.Cancelled);
            Assert.All(
                charges.Where(charge => charge.PeriodStart >= new DateTime(2030, 7, 1)),
                charge => Assert.Equal(ChargeStatus.Cancelled, charge.Status));
            Assert.All(
                charges.Where(charge => charge.PeriodStart < new DateTime(2030, 7, 1)),
                charge => Assert.NotEqual(ChargeStatus.Cancelled, charge.Status));
        }
    }

    [Fact]
    public async Task RestructureGroup_ShouldTerminateOldAndOpenLinkedDraftGroup()
    {
        var (seed, tenantB) = await SeedAsync();
        var oldGroup = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        var newGroup = await RestructureAsync(seed, oldGroup.Id, (seed.TenantId, 35), (tenantB, 25));
        _context.ChangeTracker.Clear();

        Assert.Equal(oldGroup.Id, newGroup.PreviousGroupId);
        Assert.All(await LoadLeasesAsync(oldGroup.Id), lease =>
        {
            Assert.Equal(LeaseStatus.Terminated, lease.Status);
            Assert.Equal(TerminationDate, lease.TerminationDate);
        });
        var created = await _context.Leases
            .Include(lease => lease.LeaseArea)
            .Where(lease => lease.LeaseGroupId == newGroup.Id)
            .ToListAsync();
        Assert.Equal(2, created.Count);
        Assert.All(created, lease => Assert.Equal(LeaseStatus.Draft, lease.Status));
        Assert.Equal(35m, created.Single(lease => lease.TenantId == seed.TenantId).LeaseArea!.RentedArea);
        Assert.Equal(25m, created.Single(lease => lease.TenantId == tenantB).LeaseArea!.RentedArea);
    }

    [Fact]
    public async Task RestructureGroup_ShouldRejectStartNotAfterTerminationAndKeepOldActive()
    {
        var (seed, tenantB) = await SeedAsync();
        var oldGroup = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            RestructureAsync(
                seed, oldGroup.Id, [(seed.TenantId, 35), (tenantB, 25)], startDate: TerminationDate));
        _context.ChangeTracker.Clear();

        Assert.Equal("LeaseGroup.RestructureStartNotAfterTermination", exception.Code);
        Assert.All(await LoadLeasesAsync(oldGroup.Id), lease =>
            Assert.Equal(LeaseStatus.Active, lease.Status));
    }

    [Fact]
    public async Task RestructureGroup_ShouldRejectSingleTenantAndKeepOldActive()
    {
        var (seed, tenantB) = await SeedAsync();
        var oldGroup = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            RestructureAsync(seed, oldGroup.Id, (seed.TenantId, 60)));
        _context.ChangeTracker.Clear();

        Assert.Equal("LeaseGroup.TooFewLeases", exception.Code);
        Assert.All(await LoadLeasesAsync(oldGroup.Id), lease =>
            Assert.Equal(LeaseStatus.Active, lease.Status));
        Assert.False(await _context.LeaseGroups.AnyAsync(item => item.PreviousGroupId == oldGroup.Id));
    }

    [Fact]
    public async Task RestructureGroup_ShouldRequireTerminationReason()
    {
        var (seed, tenantB) = await SeedAsync();
        var oldGroup = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            RestructureAsync(
                seed, oldGroup.Id, [(seed.TenantId, 35), (tenantB, 25)], reason: " "));

        Assert.Equal("LeaseGroup.TerminationReasonRequired", exception.Code);
    }

    [Fact]
    public async Task ApproveRestructuredGroup_ShouldBlockUntilPreviousChargesPaid()
    {
        var (seed, tenantB) = await SeedAsync();
        var oldGroup = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var newGroup = await RestructureAsync(seed, oldGroup.Id, (seed.TenantId, 35), (tenantB, 25));
        _context.ChangeTracker.Clear();
        await AddDocumentsAsync(await LoadLeasesAsync(newGroup.Id));

        var blocked = await Assert.ThrowsAnyAsync<BusinessException>(() => ApproveAsync(seed, newGroup.Id));
        _context.ChangeTracker.Clear();
        Assert.Equal("LeaseGroup.PreviousGroupUnsettled", blocked.Code);
        Assert.All(await LoadLeasesAsync(newGroup.Id), lease =>
            Assert.Equal(LeaseStatus.Draft, lease.Status));

        await MarkChargesPaidAsync(oldGroup.Id);
        await ApproveAsync(seed, newGroup.Id);
        _context.ChangeTracker.Clear();

        Assert.All(await LoadLeasesAsync(newGroup.Id), lease =>
            Assert.Equal(LeaseStatus.Active, lease.Status));
    }

    [Fact]
    public async Task ApproveRestructuredGroup_ShouldIgnoreTenantDebtOutsideOldGroup()
    {
        var (seed, tenantB) = await SeedAsync();
        var (_, otherUnit) = await _harness.AddUnitAsync(seed, 40, $"Diğer Tip {seed.Suffix}");
        var other = await _harness.CreateDraftAsync(
            seed, Start, End, unitId: otherUnit.Id, tenantId: seed.TenantId);
        await _harness.AddAllRequiredDocumentsAsync(other.Id);
        await _harness.CreateService(seed.ReviewerId).ApproveAsync(
            new ApproveLeaseInput(
                other.Id, null, other.RowVersion, seed.ReviewerId, LeaseServiceTestHarness.Scope()));
        _context.ChangeTracker.Clear();
        Assert.True(await _context.Charges.AnyAsync(charge =>
            charge.LeaseId == other.Id && charge.Status != ChargeStatus.Paid));

        var oldGroup = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var newGroup = await RestructureAsync(seed, oldGroup.Id, (seed.TenantId, 35), (tenantB, 25));
        _context.ChangeTracker.Clear();
        await AddDocumentsAsync(await LoadLeasesAsync(newGroup.Id));
        await MarkChargesPaidAsync(oldGroup.Id);

        await ApproveAsync(seed, newGroup.Id);
        _context.ChangeTracker.Clear();

        Assert.All(await LoadLeasesAsync(newGroup.Id), lease =>
            Assert.Equal(LeaseStatus.Active, lease.Status));
    }

    [Fact]
    public async Task ApproveRestructuredGroup_ShouldBlockWhenDepositNotRefunded()
    {
        var (seed, tenantB) = await SeedAsync();
        var oldGroup = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var newGroup = await RestructureAsync(seed, oldGroup.Id, (seed.TenantId, 35), (tenantB, 25));
        _context.ChangeTracker.Clear();
        await AddDocumentsAsync(await LoadLeasesAsync(newGroup.Id));
        await MarkChargesPaidAsync(oldGroup.Id);

        var exception = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateGroupService(seed.ReviewerId, new NotRefundedDepositService()).ApproveGroupAsync(
                new ApproveLeaseGroupInput(
                    newGroup.Id, null, seed.ReviewerId, LeaseServiceTestHarness.Scope())));
        _context.ChangeTracker.Clear();

        Assert.Equal("LeaseGroup.PreviousDepositNotRefunded", exception.Code);
        Assert.All(await LoadLeasesAsync(newGroup.Id), lease =>
            Assert.Equal(LeaseStatus.Draft, lease.Status));
    }

    [Fact]
    public async Task SingleExtendAndTerminate_ShouldStillBeRejectedForGroupLease()
    {
        var (seed, tenantB) = await SeedAsync();
        var group = await CreateActiveGroupAsync(seed, (seed.TenantId, 30), (tenantB, 30));
        var lease = (await LoadLeasesAsync(group.Id))[0];
        var scope = LeaseServiceTestHarness.Scope();

        var extend = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ApplicantId).ExtendAsync(
                new ExtendLeaseInput(
                    lease.Id, End.AddYears(1), false, 0, null, null, false, false, [], scope)));
        var terminate = await Assert.ThrowsAnyAsync<BusinessException>(() =>
            _harness.CreateService(seed.ApplicantId).TerminateAsync(
                new TerminateLeaseInput(lease.Id, TerminationDate, "Test", null, scope)));

        Assert.Equal("Lease.GroupOperationRequired", extend.Code);
        Assert.Equal("Lease.GroupOperationRequired", terminate.Code);
    }

    private sealed class NotRefundedDepositService : IDepositRefundStatusService
    {
        public Task<bool> IsRefundedAsync(int leaseId) => Task.FromResult(false);
    }

    private async Task<(LeaseServiceTestHarness.Seed Seed, int TenantB)> SeedAsync()
    {
        var seed = await _harness.SeedAsync(area: 60);
        return (seed, await _harness.AddTenantAsync($"b{seed.Suffix}"));
    }

    private async Task<LeaseGroup> CreateActiveGroupAsync(
        LeaseServiceTestHarness.Seed seed,
        params (int TenantId, decimal Area)[] shares)
    {
        var group = await _harness.CreateGroupService(seed.ApplicantId).CreateGroupAsync(
            new CreateLeaseGroupInput(
                seed.UnitId,
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
        _context.ChangeTracker.Clear();
        await AddDocumentsAsync(await LoadLeasesAsync(group.Id));
        await ApproveAsync(seed, group.Id);
        _context.ChangeTracker.Clear();
        Assert.All(await LoadLeasesAsync(group.Id), lease => Assert.Equal(LeaseStatus.Active, lease.Status));
        return group;
    }

    private Task ApproveAsync(LeaseServiceTestHarness.Seed seed, int groupId)
        => _harness.CreateGroupService(seed.ReviewerId).ApproveGroupAsync(
            new ApproveLeaseGroupInput(groupId, null, seed.ReviewerId, LeaseServiceTestHarness.Scope()));

    private Task<LeaseGroup> RestructureAsync(
        LeaseServiceTestHarness.Seed seed,
        int groupId,
        params (int TenantId, decimal Area)[] shares)
        => RestructureAsync(seed, groupId, shares, null, "Yeniden yapılanma");

    private Task<LeaseGroup> RestructureAsync(
        LeaseServiceTestHarness.Seed seed,
        int groupId,
        (int TenantId, decimal Area)[] shares,
        DateTime? startDate = null,
        string reason = "Yeniden yapılanma")
        => _harness.CreateGroupService(seed.ReviewerId).RestructureGroupAsync(
            new RestructureLeaseGroupInput(
                groupId,
                TerminationDate,
                reason,
                null,
                startDate ?? TerminationDate.AddDays(1),
                new DateTime(2031, 6, 30),
                DueDateRuleType.FixedDayOfMonth,
                10,
                "Yeni dağılım",
                false,
                shares.Select(share => new LeaseGroupMemberInput(
                    null, share.TenantId, share.Area, _harness.Rates(seed))).ToList(),
                seed.ReviewerId,
                LeaseServiceTestHarness.Scope()));

    private async Task MarkChargesPaidAsync(int groupId)
    {
        var leaseIds = (await LoadLeasesAsync(groupId)).Select(lease => lease.Id).ToList();
        var charges = await _context.Charges
            .Where(charge => leaseIds.Contains(charge.LeaseId!.Value)
                && charge.Status != ChargeStatus.Cancelled)
            .ToListAsync();
        foreach (var charge in charges)
            charge.Status = ChargeStatus.Paid;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

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
}
