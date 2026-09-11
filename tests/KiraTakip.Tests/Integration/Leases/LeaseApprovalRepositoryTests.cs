using KiraTakip.Data;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Tests;

public class LeaseApprovalRepositoryRegistrationTests
{
    [Fact]
    public void RepositoryModule_ShouldRegisterReviewHistoryRepository()
    {
        var services = new ServiceCollection();

        services.AddRepositoryModule();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ILeaseReviewHistoryRepository)
            && descriptor.ImplementationType == typeof(LeaseReviewHistoryRepository)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}

[Collection("Database collection")]
public class LeaseApprovalRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public LeaseApprovalRepositoryTests(DatabaseFixture fixture)
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
    public async Task InternalFilters_ShouldReturnExpectedStatusesAndHonorPropertyScope()
    {
        var seed = await SeedAsync();
        var repository = new LeaseRepository(_context);

        var all = await repository.GetListAsync("tum", null);
        var pending = await repository.GetListAsync("onaybekliyor", null);
        var revision = await repository.GetListAsync("revizyon", null);
        var propertyScoped = await repository.GetListAsync("tum", [seed.FirstPropertyId], []);

        Assert.Contains(all, lease => lease.Id == seed.DraftLeaseId);
        Assert.Contains(all, lease => lease.Id == seed.RevisionLeaseId);
        Assert.Contains(pending, lease => lease.Id == seed.DraftLeaseId);
        Assert.Contains(pending, lease => lease.Id == seed.OtherPropertyDraftLeaseId);
        Assert.All(pending, lease => Assert.Equal(LeaseStatus.Draft, lease.Status));
        Assert.Single(revision, lease => lease.Id == seed.RevisionLeaseId);
        Assert.All(revision, lease => Assert.Equal(LeaseStatus.RevisionRequested, lease.Status));
        Assert.DoesNotContain(propertyScoped, lease => lease.Id == seed.OtherPropertyDraftLeaseId);
    }

    [Fact]
    public async Task TenantPortalQueriesAndDocuments_ShouldHideApplications()
    {
        var seed = await SeedAsync();
        var repository = new LeaseRepository(_context);

        var list = await repository.GetTenantPortalListAsync(seed.FirstTenantId);
        var draftDetail = await repository.GetTenantDetailsAsync(
            seed.DraftLeaseId,
            seed.FirstTenantId);
        var revisionDetail = await repository.GetTenantDetailsAsync(
            seed.RevisionLeaseId,
            seed.FirstTenantId);
        var activeDetail = await repository.GetTenantDetailsAsync(
            seed.ActiveLeaseId,
            seed.FirstTenantId);

        Assert.Contains(list, lease => lease.Id == seed.ActiveLeaseId);
        Assert.DoesNotContain(list, lease => lease.Id == seed.DraftLeaseId);
        Assert.DoesNotContain(list, lease => lease.Id == seed.RevisionLeaseId);
        Assert.All(list, lease => Assert.Contains(
            lease.Status,
            new[] { LeaseStatus.Active, LeaseStatus.Ended, LeaseStatus.Terminated }));
        Assert.Null(draftDetail);
        Assert.Null(revisionDetail);
        Assert.NotNull(activeDetail);
        Assert.Null(await repository.GetDocumentOwnerContextAsync(seed.DraftLeaseId, tenantPortalOnly: true));
        Assert.NotNull(await repository.GetDocumentOwnerContextAsync(seed.DraftLeaseId));
    }

    [Fact]
    public async Task DraftAndDecisionQueries_ShouldApplyScopeAndProjectEditData()
    {
        var seed = await SeedAsync();
        var repository = new LeaseRepository(_context);

        var propertyScoped = await repository.GetForDecisionAsync(
            seed.DraftLeaseId,
            [seed.FirstPropertyId]);
        var unitScoped = await repository.GetForDecisionAsync(
            seed.DraftLeaseId,
            [],
            [seed.DraftUnitId]);
        var denied = await repository.GetForDecisionAsync(
            seed.DraftLeaseId,
            [],
            []);
        var global = await repository.GetForDecisionAsync(seed.DraftLeaseId, null);
        var outsidePropertyScope = await repository.GetForDecisionAsync(
            seed.OtherPropertyDraftLeaseId,
            [seed.FirstPropertyId],
            []);
        var globalOutsideProperty = await repository.GetForDecisionAsync(
            seed.OtherPropertyDraftLeaseId,
            null);
        var edit = await repository.GetDraftForEditAsync(
            seed.DraftLeaseId,
            [seed.FirstPropertyId]);

        Assert.NotNull(propertyScoped);
        Assert.NotNull(propertyScoped!.Unit.Property);
        Assert.NotNull(unitScoped);
        Assert.Null(denied);
        Assert.NotNull(global);
        Assert.Null(outsidePropertyScope);
        Assert.NotNull(globalOutsideProperty);
        Assert.NotNull(edit);
        Assert.NotEmpty(edit!.RowVersion);
        Assert.Equal(seed.ActorDisplayName, edit.OwnerDisplayName);
        Assert.Single(edit.RateOverrides);
        Assert.Equal(seed.MonthlyChargeTypeId, edit.RateOverrides[0].ChargeTypeId);
        Assert.NotNull(edit.LatestRevision);
        Assert.Equal("Alanları kontrol edin.", edit.LatestRevision!.Explanation);
    }

    [Fact]
    public async Task TimelineAndAuditQueries_ShouldBeDeterministicAndExplicit()
    {
        var seed = await SeedAsync();
        var historyRepository = new LeaseReviewHistoryRepository(_context);
        var leaseRepository = new LeaseRepository(_context);

        var timeline = await historyRepository.GetByLeaseIdAsync(seed.DraftLeaseId);
        var latestRevision = await historyRepository.GetLatestRevisionAsync(seed.DraftLeaseId);

        Assert.Equal(3, timeline.Count);
        Assert.True(timeline[0].Id < timeline[1].Id);
        Assert.Equal(LeaseReviewActionType.RevisionRequested, latestRevision!.ActionType);
        Assert.Equal("Alanları kontrol edin.", latestRevision.Explanation);
        Assert.Equal(seed.ActorDisplayName, latestRevision.ActorDisplayName);

        var draft = await _context.Leases.SingleAsync(lease => lease.Id == seed.DraftLeaseId);
        draft.IsDeleted = true;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        Assert.Null(await leaseRepository.GetForDecisionAsync(seed.DraftLeaseId, null));
        Assert.NotNull(await leaseRepository.GetDeletedApplicationForAuditAsync(seed.DraftLeaseId));
        Assert.Equal(
            3,
            (await historyRepository.GetByLeaseIdAsync(seed.DraftLeaseId)).Count);
    }

    [Fact]
    public async Task AvailabilityOccupancyAndDropdowns_ShouldKeepApplicationsSeparate()
    {
        var seed = await SeedAsync();
        var unitRepository = new UnitRepository(_context);
        var leaseRepository = new LeaseRepository(_context);

        var available = await unitRepository.GetAvailableAsync(null);
        var editOptions = await unitRepository.GetAvailableAsync(
            null,
            includedUnitId: seed.DraftUnitId);
        var units = await unitRepository.GetByPropertyIdAsync(seed.FirstPropertyId);
        var activeDropdown = await leaseRepository.GetActiveDropdownAsync(null);
        var activeEntities = await leaseRepository.GetAktiflerAsync();
        var tenantActiveUnits = await leaseRepository.GetActiveLeaseUnitsByTenantIdAsync(
            seed.FirstTenantId);
        var tenantChargeUnits = await unitRepository.GetTenantLeaseOptionsAsync(seed.FirstTenantId);

        Assert.Contains(available, unit => unit.Id == seed.VacantUnitId);
        Assert.DoesNotContain(available, unit => unit.Id == seed.ActiveUnitId);
        Assert.DoesNotContain(available, unit => unit.Id == seed.DraftUnitId);
        Assert.DoesNotContain(available, unit => unit.Id == seed.RevisionUnitId);
        Assert.Contains(editOptions, unit => unit.Id == seed.DraftUnitId);
        Assert.Equal(
            OccupancyStatus.Vacant,
            Assert.Single(units, unit => unit.Id == seed.DraftUnitId).Status);
        Assert.Contains(activeDropdown, lease => lease.Id == seed.ActiveLeaseId);
        Assert.DoesNotContain(activeDropdown, lease => lease.Id == seed.DraftLeaseId);
        Assert.All(activeEntities, lease => Assert.Equal(LeaseStatus.Active, lease.Status));
        Assert.Contains(tenantActiveUnits, unit => unit.Id == seed.ActiveUnitId);
        Assert.DoesNotContain(tenantActiveUnits, unit => unit.Id == seed.DraftUnitId);
        Assert.Contains(tenantChargeUnits, unit => unit.Id == seed.ActiveUnitId);
        Assert.DoesNotContain(tenantChargeUnits, unit => unit.Id == seed.DraftUnitId);
    }

    [Fact]
    public async Task ApplicationInvariantQueries_ShouldDetectUnexpectedFinancialData()
    {
        var seed = await SeedAsync();
        var repository = new LeaseRepository(_context);

        Assert.True(await repository.HasOpenApplicationForUnitAsync(seed.DraftUnitId));
        Assert.False(await repository.HasOpenApplicationForUnitAsync(
            seed.DraftUnitId,
            seed.DraftLeaseId));
        Assert.True(await repository.HasChargesAsync(seed.DraftLeaseId));
        Assert.True(await repository.HasCreationActivityAsync(seed.DraftLeaseId));
        Assert.False(await repository.HasChargesAsync(seed.RevisionLeaseId));
        Assert.False(await repository.HasCreationActivityAsync(seed.RevisionLeaseId));
    }

    private async Task<RepositorySeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstProperty = new Property { Name = $"Başvuru Taşınmazı {suffix}" };
        var secondProperty = new Property { Name = $"Dış Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Başvuru Tipi {suffix}",
            Code = $"BAR_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var firstTenant = new Tenant
        {
            TenantNo = $"BAR1-{suffix}",
            Name = $"Başvuru Kiracısı {suffix}"
        };
        var secondTenant = new Tenant
        {
            TenantNo = $"BAR2-{suffix}",
            Name = $"Diğer Kiracı {suffix}"
        };
        var actorDisplayName = $"Başvuru Sahibi {suffix}";
        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"basvuru-{suffix}",
            NormalizedUserName = $"BASVURU-{suffix.ToUpperInvariant()}",
            AdSoyad = actorDisplayName,
            UserType = UserType.Internal
        };
        var monthlyChargeType = new ChargeType
        {
            Name = $"Başvuru Kirası {suffix}",
            Code = $"BARK_{suffix}",
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        _context.AddRange(
            firstProperty,
            secondProperty,
            unitType,
            firstTenant,
            secondTenant,
            actor,
            monthlyChargeType);
        await _context.SaveChangesAsync();

        var units = Enumerable.Range(1, 7)
            .Select(index => new Unit
            {
                PropertyId = index == 7 ? secondProperty.Id : firstProperty.Id,
                UnitTypeId = unitType.Id,
                Name = $"Başvuru Birimi {index} {suffix}",
                Area = 20 + index
            })
            .ToArray();
        _context.Units.AddRange(units);
        await _context.SaveChangesAsync();

        var active = CreateLease(firstTenant.Id, units[0].Id, LeaseStatus.Active, actor.Id);
        var ended = CreateLease(firstTenant.Id, units[1].Id, LeaseStatus.Ended, actor.Id);
        var terminated = CreateLease(firstTenant.Id, units[2].Id, LeaseStatus.Terminated, actor.Id);
        var draft = CreateLease(firstTenant.Id, units[3].Id, LeaseStatus.Draft, actor.Id);
        var revision = CreateLease(firstTenant.Id, units[4].Id, LeaseStatus.RevisionRequested, actor.Id);
        var otherPropertyDraft = CreateLease(
            secondTenant.Id,
            units[6].Id,
            LeaseStatus.Draft,
            actor.Id);
        _context.Leases.AddRange(
            active,
            ended,
            terminated,
            draft,
            revision,
            otherPropertyDraft);
        await _context.SaveChangesAsync();

        draft.CreatedBy = actor.Id;
        await _context.SaveChangesAsync();

        _context.SozlesmeTarifeler.Add(new LeaseRateOverride
        {
            LeaseId = draft.Id,
            ChargeTypeId = monthlyChargeType.Id,
            UnitValue = 1250,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 20
        });

        var timelineDate = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        _context.SozlesmeIncelemeGecmisleri.AddRange(
            CreateHistory(draft.Id, actor.Id, LeaseReviewActionType.DraftCreated, timelineDate),
            CreateHistory(draft.Id, actor.Id, LeaseReviewActionType.DraftUpdated, timelineDate),
            CreateHistory(
                draft.Id,
                actor.Id,
                LeaseReviewActionType.RevisionRequested,
                timelineDate.AddMinutes(1),
                "Alanları kontrol edin."));
        _context.Charges.Add(new Charge
        {
            TenantId = firstTenant.Id,
            UnitId = units[3].Id,
            LeaseId = draft.Id,
            PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30),
            DueDate = new DateTime(2026, 9, 15),
            ExpectedAmount = 100,
            TotalAmount = 100,
            SourceType = ChargeSourceType.Lease
        });
        _context.SozlesmeIslemGecmisleri.Add(new LeaseActivityLog
        {
            LeaseId = draft.Id,
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = timelineDate,
            Description = "Beklenmeyen test verisi"
        });
        await _context.SaveChangesAsync();

        return new RepositorySeed(
            firstProperty.Id,
            firstTenant.Id,
            actorDisplayName,
            monthlyChargeType.Id,
            units[0].Id,
            units[3].Id,
            units[4].Id,
            units[5].Id,
            active.Id,
            draft.Id,
            revision.Id,
            otherPropertyDraft.Id);
    }

    private static Lease CreateLease(
        int tenantId,
        int unitId,
        LeaseStatus status,
        string createdBy) => new()
        {
            TenantId = tenantId,
            UnitId = unitId,
            Status = status,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2027, 1, 1),
            DueDay = 1,
            CreatedBy = createdBy
        };

    private static LeaseReviewHistory CreateHistory(
        int leaseId,
        string actorUserId,
        LeaseReviewActionType actionType,
        DateTime actionDate,
        string? explanation = null) => new()
        {
            LeaseId = leaseId,
            ActorUserId = actorUserId,
            ActionType = actionType,
            ToStatus = LeaseStatus.Draft,
            ActionDate = actionDate,
            Explanation = explanation
        };

    private sealed record RepositorySeed(
        int FirstPropertyId,
        int FirstTenantId,
        string ActorDisplayName,
        int MonthlyChargeTypeId,
        int ActiveUnitId,
        int DraftUnitId,
        int RevisionUnitId,
        int VacantUnitId,
        int ActiveLeaseId,
        int DraftLeaseId,
        int RevisionLeaseId,
        int OtherPropertyDraftLeaseId);
}
