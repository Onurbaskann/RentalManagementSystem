using Castle.DynamicProxy;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Documents;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Leases;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseApprovalServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public LeaseApprovalServiceTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateDraft_ShouldPersistRatesAndHistoryWithoutFinancialRecords()
    {
        var seed = await SeedAsync();
        var lease = await CreateDraftAsync(seed);

        Assert.Equal(LeaseStatus.Draft, lease.Status);
        Assert.False(await _context.Charges.AnyAsync(charge => charge.LeaseId == lease.Id));
        Assert.False(await _context.SozlesmeIslemGecmisleri.AnyAsync(activity =>
            activity.LeaseId == lease.Id && activity.ActivityType == LeaseActivityType.Creation));
        Assert.Single(await _context.SozlesmeTarifeler.Where(rate => rate.LeaseId == lease.Id).ToListAsync());
        var history = Assert.Single(await _context.SozlesmeIncelemeGecmisleri
            .Where(item => item.LeaseId == lease.Id)
            .ToListAsync());
        Assert.Equal(LeaseReviewActionType.DraftCreated, history.ActionType);
        Assert.Equal(seed.ApplicantId, history.ActorUserId);
    }

    [Fact]
    public async Task RevisionAndResubmit_ShouldEnforceReasonOwnershipAndHistory()
    {
        var seed = await SeedAsync();
        var lease = await CreateDraftAsync(seed);
        var reviewer = CreateService(seed.ReviewerId);

        await Assert.ThrowsAsync<BusinessException>(() => reviewer.RequestRevisionAsync(
            new RequestLeaseRevisionInput(
                lease.Id,
                "   ",
                lease.RowVersion,
                seed.ReviewerId,
                Scope())));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.RequestRevisionAsync(
            new RequestLeaseRevisionInput(
                lease.Id,
                new string('x', 1001),
                lease.RowVersion,
                seed.ReviewerId,
                Scope())));
        await reviewer.RequestRevisionAsync(new RequestLeaseRevisionInput(
            lease.Id,
            "  Tarihleri düzeltin.  ",
            lease.RowVersion,
            seed.ReviewerId,
            Scope()));
        await _context.Entry(lease).ReloadAsync();
        Assert.Equal(LeaseStatus.RevisionRequested, lease.Status);

        await Assert.ThrowsAsync<BusinessException>(() => reviewer.ApproveAsync(
            Approval(lease, seed.ReviewerId)));

        var applicant = CreateService(seed.ApplicantId);
        await applicant.ResubmitRevisionAsync(new ResubmitLeaseRevisionInput(
            lease.Id,
            seed.UnitId,
            seed.TenantId,
            lease.StartDate,
            lease.EndDate.AddDays(1),
            lease.DueDateRuleType,
            lease.DueDay,
            "Güncellendi",
            Rates(seed),
            "Revizyon tamamlandı",
            lease.RowVersion,
            seed.ApplicantId,
            Scope()));
        await _context.Entry(lease).ReloadAsync();

        Assert.Equal(LeaseStatus.Draft, lease.Status);
        var history = await _context.SozlesmeIncelemeGecmisleri
            .Where(item => item.LeaseId == lease.Id)
            .OrderBy(item => item.Id)
            .ToListAsync();
        Assert.Equal(3, history.Count);
        Assert.Equal("Tarihleri düzeltin.", history[1].Explanation);
        Assert.Equal(LeaseReviewActionType.Resubmitted, history[2].ActionType);

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(seed.ReviewerId).UpdateDraftAsync(
            new UpdateLeaseDraftInput(
                lease.Id,
                seed.UnitId,
                seed.TenantId,
                lease.StartDate,
                lease.EndDate,
                lease.DueDateRuleType,
                lease.DueDay,
                lease.Description,
                Rates(seed),
                lease.RowVersion,
                seed.ReviewerId,
                Scope())));
        await Assert.ThrowsAsync<BusinessException>(() => applicant.RequestRevisionAsync(
            new RequestLeaseRevisionInput(
                lease.Id,
                "Kendi revizyonum",
                lease.RowVersion,
                seed.ApplicantId,
                Scope())));

        await reviewer.RequestRevisionAsync(new RequestLeaseRevisionInput(
            lease.Id,
            "İkinci revizyon turu",
            lease.RowVersion,
            seed.ReviewerId,
            Scope()));
        await _context.Entry(lease).ReloadAsync();
        await applicant.ResubmitRevisionAsync(new ResubmitLeaseRevisionInput(
            lease.Id,
            seed.UnitId,
            seed.TenantId,
            lease.StartDate,
            lease.EndDate,
            lease.DueDateRuleType,
            lease.DueDay,
            lease.Description,
            Rates(seed),
            null,
            lease.RowVersion,
            seed.ApplicantId,
            Scope()));
        Assert.Equal(
            2,
            await _context.SozlesmeIncelemeGecmisleri.CountAsync(historyItem =>
                historyItem.LeaseId == lease.Id
                && historyItem.ActionType == LeaseReviewActionType.RevisionRequested));
    }

    [Fact]
    public async Task Approval_ShouldRequireDocumentsAndCreateActiveFinancialRecords()
    {
        var seed = await SeedAsync();
        var requiredType = new DocumentType
        {
            Code = $"REQ_{seed.Suffix}",
            Name = "Zorunlu Sözleşme Belgesi",
            TargetEntity = DocumentOwnerType.Lease,
            Required = true,
            IsActive = true
        };
        _context.DocumentTypes.Add(requiredType);
        await _context.SaveChangesAsync();
        var lease = await CreateDraftAsync(seed);
        var reviewer = CreateService(seed.ReviewerId);

        await Assert.ThrowsAnyAsync<BusinessException>(() => reviewer.ApproveAsync(
            Approval(lease, seed.ReviewerId)));

        var requiredTypes = await _context.DocumentTypes
            .Where(type => type.TargetEntity == DocumentOwnerType.Lease
                && type.Required
                && type.IsActive)
            .ToListAsync();
        _context.Belgeler.AddRange(requiredTypes.Select(type => new Document
        {
            DocumentTypeId = type.Id,
            OwnerType = DocumentOwnerType.Lease,
            OwnerId = lease.Id,
            FileName = $"sozlesme-{type.Id}.pdf",
            MimeType = "application/pdf",
            FileSize = 10
        }));
        await _context.SaveChangesAsync();
        await _context.Entry(lease).ReloadAsync();

        await reviewer.ApproveAsync(Approval(lease, seed.ReviewerId));
        await _context.Entry(lease).ReloadAsync();

        Assert.Equal(LeaseStatus.Active, lease.Status);
        Assert.Single(await _context.SozlesmeIslemGecmisleri.Where(activity =>
            activity.LeaseId == lease.Id && activity.ActivityType == LeaseActivityType.Creation).ToListAsync());
        Assert.Single(await _context.SozlesmeIncelemeGecmisleri.Where(history =>
            history.LeaseId == lease.Id && history.ActionType == LeaseReviewActionType.Approved).ToListAsync());
        var charges = await _context.Charges
            .Include(charge => charge.LineItems)
            .Where(charge => charge.LeaseId == lease.Id)
            .ToListAsync();
        Assert.NotEmpty(charges);
        Assert.All(charges, charge => Assert.NotEmpty(charge.LineItems));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.ApproveAsync(
            Approval(lease, seed.ReviewerId)));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.DeleteDraftAsync(
            new DeleteLeaseDraftInput(
                lease.Id,
                "Aktif kayıt silinemez",
                lease.RowVersion,
                seed.ReviewerId,
                Scope())));
    }

    [Fact]
    public async Task DeleteDraft_ShouldRequireReasonAndSoftDeleteDependentsButKeepHistory()
    {
        var seed = await SeedAsync();
        var lease = await CreateDraftAsync(seed);
        var optionalType = new DocumentType
        {
            Code = $"OPT_{seed.Suffix}",
            Name = "İsteğe Bağlı Belge",
            TargetEntity = DocumentOwnerType.Lease,
            IsActive = true
        };
        _context.DocumentTypes.Add(optionalType);
        await _context.SaveChangesAsync();
        var document = new Document
        {
            DocumentTypeId = optionalType.Id,
            OwnerType = DocumentOwnerType.Lease,
            OwnerId = lease.Id,
            FileName = "ek.pdf",
            MimeType = "application/pdf",
            FileSize = 5
        };
        _context.Belgeler.Add(document);
        await _context.SaveChangesAsync();
        await _context.Entry(lease).ReloadAsync();
        var reviewer = CreateService(seed.ReviewerId);

        await Assert.ThrowsAsync<BusinessException>(() => reviewer.DeleteDraftAsync(
            new DeleteLeaseDraftInput(
                lease.Id,
                " ",
                lease.RowVersion,
                seed.ReviewerId,
                Scope())));
        await reviewer.DeleteDraftAsync(new DeleteLeaseDraftInput(
            lease.Id,
            "  Başvuru mükerrer. ",
            lease.RowVersion,
            seed.ReviewerId,
            Scope()));

        var deletedLease = await _context.Leases.IgnoreQueryFilters().SingleAsync(item => item.Id == lease.Id);
        Assert.True(deletedLease.IsDeleted);
        Assert.All(
            await _context.SozlesmeTarifeler.IgnoreQueryFilters().Where(rate => rate.LeaseId == lease.Id).ToListAsync(),
            rate => Assert.True(rate.IsDeleted));
        Assert.True((await _context.Belgeler.IgnoreQueryFilters().SingleAsync(item => item.Id == document.Id)).IsDeleted);
        var deletedHistory = await _context.SozlesmeIncelemeGecmisleri
            .SingleAsync(history => history.LeaseId == lease.Id
                && history.ActionType == LeaseReviewActionType.Deleted);
        Assert.Equal("Başvuru mükerrer.", deletedHistory.Explanation);
    }

    [Fact]
    public async Task Draft_ShouldRejectSelfReviewScopeAndActiveOnlyOperations()
    {
        var seed = await SeedAsync();
        var lease = await CreateDraftAsync(seed);
        var applicant = CreateService(seed.ApplicantId);
        var reviewer = CreateService(seed.ReviewerId);

        await Assert.ThrowsAsync<BusinessException>(() => applicant.ApproveAsync(
            Approval(lease, seed.ApplicantId)));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.RequestRevisionAsync(
            new RequestLeaseRevisionInput(
                lease.Id,
                "Düzeltin",
                lease.RowVersion,
                seed.ReviewerId,
                new LeaseAccessScopeInput([], []))));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.ApproveAsync(
            new ApproveLeaseInput(
                lease.Id,
                null,
                lease.RowVersion,
                seed.ReviewerId,
                new LeaseAccessScopeInput([], []))));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.DeleteDraftAsync(
            new DeleteLeaseDraftInput(
                lease.Id,
                "Silinsin",
                lease.RowVersion,
                seed.ReviewerId,
                new LeaseAccessScopeInput([], []))));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.ExtendAsync(new ExtendLeaseInput(
            lease.Id,
            lease.EndDate.AddMonths(1),
            false,
            0,
            null,
            null,
            false,
            false,
            [],
            Scope())));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.TerminateAsync(new TerminateLeaseInput(
            lease.Id,
            DateTime.Today,
            "Test",
            null,
            Scope())));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.RegenerateAsync(new RegenerateLeaseInput(
            lease.Id,
            lease.StartDate,
            false,
            false,
            [],
            Scope())));
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.UpdateDueDateAsync(
            new UpdateLeaseDueDateInput(
                lease.Id,
                DueDateRuleType.FixedDayOfMonth,
                15,
                null,
                Scope())));
        await Assert.ThrowsAsync<BusinessException>(() => CreateChargeGenerationService()
            .GenerateForLeaseAsync(new GenerateLeaseChargesInput(lease.Id)));
    }

    [Fact]
    public async Task SuperAdmin_ShouldBeAllowedToReviewOwnApplication()
    {
        var revisionSeed = await SeedAsync();
        var revisionLease = await CreateDraftAsync(revisionSeed);
        var revisionAdmin = CreateService(revisionSeed.ApplicantId, isSuperAdmin: true);

        await revisionAdmin.RequestRevisionAsync(new RequestLeaseRevisionInput(
            revisionLease.Id,
            "Süper admin revizyon kontrolü",
            revisionLease.RowVersion,
            revisionSeed.ApplicantId,
            Scope()));
        Assert.Equal(LeaseStatus.RevisionRequested, revisionLease.Status);

        await revisionAdmin.DeleteDraftAsync(new DeleteLeaseDraftInput(
            revisionLease.Id,
            "Süper admin silme kontrolü",
            revisionLease.RowVersion,
            revisionSeed.ApplicantId,
            Scope()));
        Assert.True(revisionLease.IsDeleted);

        var approvalSeed = await SeedAsync();
        var approvalLease = await CreateDraftAsync(approvalSeed);
        await AddAllRequiredDocumentsAsync(approvalLease.Id);
        var approvalAdmin = CreateService(approvalSeed.ApplicantId, isSuperAdmin: true);

        await approvalAdmin.ApproveAsync(Approval(approvalLease, approvalSeed.ApplicantId));
        Assert.Equal(LeaseStatus.Active, approvalLease.Status);
        Assert.Contains(await _context.SozlesmeIncelemeGecmisleri
            .Where(history => history.LeaseId == approvalLease.Id)
            .ToListAsync(), history => history.ActionType == LeaseReviewActionType.Approved);
    }

    [Fact]
    public async Task Decisions_ShouldRejectStaleRowVersionAndActiveUnitConflict()
    {
        var seed = await SeedAsync();
        var lease = await CreateDraftAsync(seed);
        var staleVersion = lease.RowVersion.ToArray();
        lease.Description = "Başka kullanıcı güncelledi";
        await _context.SaveChangesAsync();

        var reviewer = CreateService(seed.ReviewerId);
        await Assert.ThrowsAsync<BusinessException>(() => reviewer.RequestRevisionAsync(
            new RequestLeaseRevisionInput(
                lease.Id,
                "Düzeltin",
                staleVersion,
                seed.ReviewerId,
                Scope())));

        var conflict = new Lease
        {
            UnitId = seed.UnitId,
            TenantId = seed.TenantId,
            Status = LeaseStatus.Active,
            StartDate = DateTime.Today.AddMonths(-1),
            EndDate = DateTime.Today.AddMonths(1)
        };
        _context.Leases.Add(conflict);
        await _context.SaveChangesAsync();
        await _context.Entry(lease).ReloadAsync();

        await Assert.ThrowsAnyAsync<BusinessException>(() => reviewer.ApproveAsync(
            Approval(lease, seed.ReviewerId)));
        Assert.Equal(LeaseStatus.Draft, lease.Status);
    }

    [Fact]
    public async Task DeleteDraft_ShouldRejectUnexpectedCharges()
    {
        var seed = await SeedAsync();
        var lease = await CreateDraftAsync(seed);
        _context.Charges.Add(new Charge
        {
            TenantId = seed.TenantId,
            UnitId = seed.UnitId,
            LeaseId = lease.Id,
            PeriodStart = new DateTime(2026, 9, 1),
            PeriodEnd = new DateTime(2026, 9, 30),
            DueDate = new DateTime(2026, 9, 10),
            ExpectedAmount = 10,
            TotalAmount = 10,
            SourceType = ChargeSourceType.Lease
        });
        await _context.SaveChangesAsync();
        await _context.Entry(lease).ReloadAsync();

        await Assert.ThrowsAsync<BusinessException>(() => CreateService(seed.ReviewerId)
            .DeleteDraftAsync(new DeleteLeaseDraftInput(
                lease.Id,
                "Silinsin",
                lease.RowVersion,
                seed.ReviewerId,
                Scope())));
        Assert.False(lease.IsDeleted);
    }

    [Fact]
    public async Task ApprovalFailure_ShouldRollbackStatusHistoryAndActivityThroughProxy()
    {
        var seed = await SeedAsync();
        var lease = await CreateDraftAsync(seed);
        await AddAllRequiredDocumentsAsync(lease.Id);
        await _context.Entry(lease).ReloadAsync();

        await _transaction!.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;

        try
        {
            var realGeneration = CreateChargeGenerationService();
            var target = CreateService(
                seed.ReviewerId,
                new FailingChargeGenerationService(realGeneration));
            var interceptor = new TransactionInterceptor(
                _context,
                NullLogger<TransactionInterceptor>.Instance);
            var proxy = new ProxyGenerator().CreateInterfaceProxyWithTarget<ILeaseService>(
                target,
                interceptor.ToInterceptor());

            await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.ApproveAsync(
                Approval(lease, seed.ReviewerId)));
            _context.ChangeTracker.Clear();

            var storedLease = await _context.Leases.SingleAsync(item => item.Id == lease.Id);
            Assert.Equal(LeaseStatus.Draft, storedLease.Status);
            Assert.False(await _context.SozlesmeIncelemeGecmisleri.AnyAsync(history =>
                history.LeaseId == lease.Id && history.ActionType == LeaseReviewActionType.Approved));
            Assert.False(await _context.SozlesmeIslemGecmisleri.AnyAsync(activity =>
                activity.LeaseId == lease.Id && activity.ActivityType == LeaseActivityType.Creation));
            Assert.False(await _context.Charges.AnyAsync(charge => charge.LeaseId == lease.Id));
        }
        finally
        {
            await CleanupCommittedSeedAsync(seed);
        }
    }

    private async Task<ServiceSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Servis Taşınmazı {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Servis Tipi {suffix}",
            Code = $"SRV_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { TenantNo = $"SRV-{suffix}", Name = $"Servis Kiracısı {suffix}" };
        var applicant = CreateUser($"app-{suffix}", $"Başvuran {suffix}");
        var reviewer = CreateUser($"rev-{suffix}", $"Onaylayan {suffix}");
        var chargeType = new ChargeType
        {
            Code = $"SRVK_{suffix}",
            Name = $"Servis Kirası {suffix}",
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        _context.AddRange(property, unitType, tenant, applicant, reviewer, chargeType);
        await _context.SaveChangesAsync();
        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Servis Birimi {suffix}",
            Area = 50
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return new ServiceSeed(
            suffix,
            property.Id,
            unitType.Id,
            unit.Id,
            tenant.Id,
            applicant.Id,
            reviewer.Id,
            chargeType.Id);
    }

    private async Task<Lease> CreateDraftAsync(ServiceSeed seed)
    {
        var service = CreateService(seed.ApplicantId);
        var lease = await service.CreateDraftAsync(new CreateLeaseDraftInput(
            seed.UnitId,
            seed.TenantId,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 10, 31),
            DueDateRuleType.FixedDayOfMonth,
            10,
            "Test başvurusu",
            Rates(seed),
            seed.ApplicantId,
            Scope()));
        lease.CreatedBy = seed.ApplicantId;
        await _context.SaveChangesAsync();
        await _context.Entry(lease).ReloadAsync();
        return lease;
    }

    private LeaseService CreateService(
        string actorUserId,
        IChargeGenerationService? chargeGenerationOverride = null,
        bool isSuperAdmin = false)
    {
        var leaseRepository = new LeaseRepository(_context);
        var rateRepository = new LeaseRateOverrideRepository(_context);
        var chargeTypeRepository = new ChargeTypeRepository(_context);
        var rateResolver = new RateResolverService(
            rateRepository,
            new UnitRateRepository(_context),
            new PropertyRateOverrideRepository(_context),
            new RateScheduleRepository(_context),
            leaseRepository,
            new UnitRepository(_context),
            new TenantRepository(_context));
        var chargeGeneration = new ChargeGenerationService(
            new ChargeRepository(_context),
            chargeTypeRepository,
            new UnitOfWork(_context),
            rateResolver,
            leaseRepository,
            new UnitRepository(_context),
            new TenantRepository(_context));
        return new LeaseService(
            leaseRepository,
            rateRepository,
            new ChargeLineItemRepository(_context),
            chargeTypeRepository,
            new UnitRepository(_context),
            new TenantRepository(_context),
            chargeGenerationOverride ?? chargeGeneration,
            new UnitOfWork(_context),
            new StatisticsService(
                chargeTypeRepository,
                rateResolver,
                new TestOperationalPolicyProvider()),
            new LeaseReviewHistoryRepository(_context),
            new DocumentRepository(_context),
            new DocumentTypeRepository(_context),
            new ApplicationUserRepository(_context),
            new TestCurrentUserContext(actorUserId, isSuperAdmin));
    }

    private IChargeGenerationService CreateChargeGenerationService()
    {
        var leaseRepository = new LeaseRepository(_context);
        var rateRepository = new LeaseRateOverrideRepository(_context);
        var rateResolver = new RateResolverService(
            rateRepository,
            new UnitRateRepository(_context),
            new PropertyRateOverrideRepository(_context),
            new RateScheduleRepository(_context),
            leaseRepository,
            new UnitRepository(_context),
            new TenantRepository(_context));
        return new ChargeGenerationService(
            new ChargeRepository(_context),
            new ChargeTypeRepository(_context),
            new UnitOfWork(_context),
            rateResolver,
            leaseRepository,
            new UnitRepository(_context),
            new TenantRepository(_context));
    }

    private async Task AddAllRequiredDocumentsAsync(int leaseId)
    {
        var requiredTypes = await _context.DocumentTypes
            .Where(type => type.TargetEntity == DocumentOwnerType.Lease
                && type.Required
                && type.IsActive)
            .ToListAsync();
        _context.Belgeler.AddRange(requiredTypes.Select(type => new Document
        {
            DocumentTypeId = type.Id,
            OwnerType = DocumentOwnerType.Lease,
            OwnerId = leaseId,
            FileName = $"required-{type.Id}.pdf",
            MimeType = "application/pdf",
            FileSize = 1
        }));
        await _context.SaveChangesAsync();
    }

    private async Task CleanupCommittedSeedAsync(ServiceSeed seed)
    {
        _context.ChangeTracker.Clear();
        await _context.SozlesmeIncelemeGecmisleri.IgnoreQueryFilters()
            .Where(history => history.Lease.UnitId == seed.UnitId).ExecuteDeleteAsync();
        await _context.SozlesmeIslemGecmisleri.IgnoreQueryFilters()
            .Where(activity => _context.Leases.IgnoreQueryFilters()
                .Where(lease => lease.UnitId == seed.UnitId)
                .Select(lease => lease.Id)
                .Contains(activity.LeaseId))
            .ExecuteDeleteAsync();
        await _context.Charges.IgnoreQueryFilters()
            .Where(charge => charge.UnitId == seed.UnitId).ExecuteDeleteAsync();
        await _context.SozlesmeTarifeler.IgnoreQueryFilters()
            .Where(rate => rate.Lease.UnitId == seed.UnitId).ExecuteDeleteAsync();
        await _context.Belgeler.IgnoreQueryFilters()
            .Where(document => document.OwnerType == DocumentOwnerType.Lease
                && _context.Leases.IgnoreQueryFilters()
                    .Where(lease => lease.UnitId == seed.UnitId)
                    .Select(lease => lease.Id)
                    .Contains(document.OwnerId))
            .ExecuteDeleteAsync();
        await _context.Leases.IgnoreQueryFilters()
            .Where(lease => lease.UnitId == seed.UnitId).ExecuteDeleteAsync();
        await _context.Units.IgnoreQueryFilters()
            .Where(unit => unit.Id == seed.UnitId).ExecuteDeleteAsync();
        await _context.Tenants.IgnoreQueryFilters()
            .Where(tenant => tenant.Id == seed.TenantId).ExecuteDeleteAsync();
        await _context.ChargeTypes.IgnoreQueryFilters()
            .Where(type => type.Id == seed.ChargeTypeId).ExecuteDeleteAsync();
        await _context.Properties.IgnoreQueryFilters()
            .Where(property => property.Id == seed.PropertyId).ExecuteDeleteAsync();
        await _context.UnitTypes.IgnoreQueryFilters()
            .Where(type => type.Id == seed.UnitTypeId).ExecuteDeleteAsync();
        await _context.Users.IgnoreQueryFilters()
            .Where(user => user.Id == seed.ApplicantId || user.Id == seed.ReviewerId)
            .ExecuteDeleteAsync();
    }

    private static ApplicationUser CreateUser(string userName, string displayName) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        AdSoyad = displayName,
        UserType = UserType.Internal
    };

    private static IReadOnlyCollection<LeaseRateOverrideInput> Rates(ServiceSeed seed)
        => [new LeaseRateOverrideInput(seed.ChargeTypeId, 1000, CalculationMethod.Fixed, 20)];

    private static LeaseAccessScopeInput Scope() => new();

    private static ApproveLeaseInput Approval(Lease lease, string actorUserId)
        => new(lease.Id, "Onaylandı", lease.RowVersion, actorUserId, Scope());

    private sealed record ServiceSeed(
        string Suffix,
        int PropertyId,
        int UnitTypeId,
        int UnitId,
        int TenantId,
        string ApplicantId,
        string ReviewerId,
        int ChargeTypeId);

    private sealed class FailingChargeGenerationService(IChargeGenerationService inner)
        : IChargeGenerationService
    {
        public Task GenerateForLeaseAsync(GenerateLeaseChargesInput input)
            => throw new InvalidOperationException("Kontrollü tahakkuk hatası");

        public Task RegenerateAsync(RegenerateLeaseChargesInput input)
            => inner.RegenerateAsync(input);

        public Task CancelFutureChargesAsync(CancelFutureLeaseChargesInput input)
            => inner.CancelFutureChargesAsync(input);

        public Task RecalculatePendingDueDatesAsync(RecalculateLeaseDueDatesInput input)
            => inner.RecalculatePendingDueDatesAsync(input);

        public Task<IList<ChargeLineItemPreview>> ComposeLineItemsAsync(
            ComposeLeaseLineItemsInput input)
            => inner.ComposeLineItemsAsync(input);
    }

    private sealed class TestCurrentUserContext(string userId, bool isSuperAdmin = false) : ICurrentUserContext
    {
        public string? UserId => userId;
        public UserType? UserType => KiraTakip.Models.Enums.UserType.Internal;
        public int? TenantId => null;
        public bool IsKiraciUser => false;
        public bool IsSuperAdmin => isSuperAdmin;
    }
}
