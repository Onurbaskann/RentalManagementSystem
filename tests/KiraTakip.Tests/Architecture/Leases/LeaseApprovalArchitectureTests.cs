using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

public class LeaseApprovalModelTests
{
    [Fact]
    public void WorkflowEnums_ShouldKeepApprovedNumericValues()
    {
        Assert.Equal(1, (int)LeaseStatus.Active);
        Assert.Equal(2, (int)LeaseStatus.Ended);
        Assert.Equal(3, (int)LeaseStatus.Terminated);
        Assert.Equal(4, (int)LeaseStatus.Draft);
        Assert.Equal(5, (int)LeaseStatus.RevisionRequested);

        Assert.Equal(1, (int)LeaseReviewActionType.DraftCreated);
        Assert.Equal(2, (int)LeaseReviewActionType.DraftUpdated);
        Assert.Equal(3, (int)LeaseReviewActionType.RevisionRequested);
        Assert.Equal(4, (int)LeaseReviewActionType.Resubmitted);
        Assert.Equal(5, (int)LeaseReviewActionType.Approved);
        Assert.Equal(6, (int)LeaseReviewActionType.Deleted);
        Assert.DoesNotContain("Comment", Enum.GetNames<LeaseReviewActionType>());
    }

    [Fact]
    public void WorkflowModel_ShouldConfigureConcurrencyRelationsAndIndexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ModelOnly;Trusted_Connection=True")
            .Options;
        using var context = new ApplicationDbContext(
            options,
            new DummyHttpContextAccessor(),
            new DummyCurrentUserContext());

        var leaseType = context.Model.FindEntityType(typeof(Lease))!;
        var rowVersion = leaseType.FindProperty(nameof(Lease.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);

        var openApplicationIndex = leaseType.GetIndexes().Single(index =>
            index.GetDatabaseName() == "UX_Sozlesmeler_BirimId_AcikBasvuru");
        Assert.True(openApplicationIndex.IsUnique);
        Assert.Equal("[IsDeleted] = 0 AND [Durum] IN (4, 5)", openApplicationIndex.GetFilter());
        Assert.Contains(leaseType.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_Sozlesmeler_BirimId");

        var historyType = context.Model.FindEntityType(typeof(LeaseReviewHistory))!;
        Assert.Equal(1000, historyType.FindProperty(nameof(LeaseReviewHistory.Explanation))!.GetMaxLength());
        Assert.NotNull(historyType.GetQueryFilter());
        Assert.Contains(historyType.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_SozlesmeIncelemeGecmisleri_SozlesmeId_IslemTarihi");
        Assert.Contains(historyType.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_SozlesmeIncelemeGecmisleri_IslemYapanKullaniciId");

        var leaseForeignKey = historyType.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Lease));
        var actorForeignKey = historyType.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ApplicationUser));
        Assert.Equal(DeleteBehavior.Restrict, leaseForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, actorForeignKey.DeleteBehavior);
    }
}

[Collection("Database collection")]
public class LeaseApprovalDatabaseTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public LeaseApprovalDatabaseTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task HistoryAndRowVersion_ShouldBePersistedForSameLease()
    {
        var seed = await SeedAsync();
        var lease = CreateDraft(seed.FirstUnitId, seed.TenantId);
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();

        Assert.NotEmpty(lease.RowVersion);
        var initialRowVersion = lease.RowVersion.ToArray();

        _context.SozlesmeIncelemeGecmisleri.AddRange(
            CreateHistory(lease.Id, seed.ActorUserId, LeaseReviewActionType.DraftCreated),
            CreateHistory(lease.Id, seed.ActorUserId, LeaseReviewActionType.DraftUpdated));
        lease.Description = "Güncellendi";
        await _context.SaveChangesAsync();

        Assert.False(initialRowVersion.SequenceEqual(lease.RowVersion));
        Assert.Equal(2, await _context.SozlesmeIncelemeGecmisleri.CountAsync(x => x.LeaseId == lease.Id));
    }

    [Fact]
    public async Task OpenApplicationIndex_ShouldRejectSecondDraftForSameUnit()
    {
        var seed = await SeedAsync();
        _context.Leases.Add(CreateDraft(seed.FirstUnitId, seed.TenantId));
        await _context.SaveChangesAsync();

        _context.Leases.Add(CreateDraft(seed.FirstUnitId, seed.TenantId));

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task OpenApplicationIndex_ShouldAllowSoftDeletedOrDifferentUnitRows()
    {
        var seed = await SeedAsync();
        var deletedDraft = CreateDraft(seed.FirstUnitId, seed.TenantId);
        _context.Leases.Add(deletedDraft);
        await _context.SaveChangesAsync();

        deletedDraft.IsDeleted = true;
        await _context.SaveChangesAsync();

        _context.Leases.AddRange(
            CreateDraft(seed.FirstUnitId, seed.TenantId),
            CreateDraft(seed.SecondUnitId, seed.TenantId));

        await _context.SaveChangesAsync();
    }

    private async Task<LeaseApprovalSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var property = new Property { Name = $"Onay Test {suffix}", City = "Ankara", District = "Çankaya" };
        var unitType = new UnitType
        {
            Name = $"Onay Tip {suffix}",
            Code = $"ONAY_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { Name = $"Onay Kiracı {suffix}", TenantNo = $"ON{suffix}" };
        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"onay-{suffix}",
            NormalizedUserName = $"ONAY-{suffix.ToUpperInvariant()}",
            UserType = UserType.Internal
        };

        _context.AddRange(property, unitType, tenant, actor);
        await _context.SaveChangesAsync();

        var firstUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Birim 1 {suffix}",
            Area = 10
        };
        var secondUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Birim 2 {suffix}",
            Area = 20
        };
        _context.Units.AddRange(firstUnit, secondUnit);
        await _context.SaveChangesAsync();

        return new LeaseApprovalSeed(firstUnit.Id, secondUnit.Id, tenant.Id, actor.Id);
    }

    private static Lease CreateDraft(int unitId, int tenantId) => new()
    {
        UnitId = unitId,
        TenantId = tenantId,
        Status = LeaseStatus.Draft,
        StartDate = new DateTime(2026, 9, 1),
        EndDate = new DateTime(2027, 9, 1),
        DueDay = 1
    };

    private static LeaseReviewHistory CreateHistory(
        int leaseId,
        string actorUserId,
        LeaseReviewActionType actionType) => new()
        {
            LeaseId = leaseId,
            ActorUserId = actorUserId,
            ActionType = actionType,
            ToStatus = LeaseStatus.Draft,
            ActionDate = DateTime.UtcNow
        };

    private sealed record LeaseApprovalSeed(
        int FirstUnitId,
        int SecondUnitId,
        int TenantId,
        string ActorUserId);
}
