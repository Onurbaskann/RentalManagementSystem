using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Documents;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Spaces;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Leases;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Reporting;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

internal sealed class LeaseServiceTestHarness(ApplicationDbContext context)
{
    public sealed record Seed(
        string Suffix,
        int SpaceId,
        int TenantId,
        string ApplicantId,
        string ReviewerId,
        int RentChargeTypeId);

    public static SpaceAccessScope Scope() => new(true, []);

    public IReadOnlyCollection<LeaseRateOverrideInput> Rates(Seed seed)
        => [new LeaseRateOverrideInput(seed.RentChargeTypeId, 1000, CalculationMethod.Fixed, 20)];

    public async Task<Seed> SeedAsync(decimal area = 50, bool isShared = false)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantCategory = TestTenantCategory.New(), TenantNo = $"SNP-{suffix}", Name = $"Anlık Kiracı {suffix}" };
        var applicant = CreateUser($"snp-app-{suffix}", $"Başvuran {suffix}");
        var reviewer = CreateUser($"snp-rev-{suffix}", $"Onaylayan {suffix}");
        context.AddRange(tenant, applicant, reviewer);
        await context.SaveChangesAsync();
        var rentChargeType = await context.ChargeTypes.SingleAsync(type =>
            type.Code == BorcTipiConsts.Kira);
        rentChargeType.IsActive = true;
        var space = await SpaceTestSeed.AddRootAsync(context, $"Anlık Alan {suffix}", area);
        if (isShared)
        {
            space.IsShared = true;
            await context.SaveChangesAsync();
        }
        return new Seed(
            suffix,
            space.Id,
            tenant.Id,
            applicant.Id,
            reviewer.Id,
            rentChargeType.Id);
    }

    public Task<Space> AddSpaceAsync(Seed seed, decimal area)
        => SpaceTestSeed.AddRootAsync(context, $"Anlık Alan 2 {seed.Suffix}", area);

    public async Task<int> AddTenantAsync(string suffix)
    {
        var tenant = new Tenant { TenantCategory = TestTenantCategory.New(), TenantNo = $"SNP2-{suffix}", Name = $"Anlık Kiracı 2 {suffix}" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant.Id;
    }

    public async Task<Lease> CreateDraftAsync(
        Seed seed,
        DateTime startDate,
        DateTime endDate,
        int? spaceId = null,
        int? tenantId = null)
    {
        var lease = await CreateService(seed.ApplicantId).CreateDraftAsync(new CreateLeaseDraftInput(
            spaceId ?? seed.SpaceId,
            tenantId ?? seed.TenantId,
            startDate,
            endDate,
            DueDateRuleType.FixedDayOfMonth,
            10,
            "Test başvurusu",
            Rates(seed),
            seed.ApplicantId,
            Scope()));
        lease.CreatedBy = seed.ApplicantId;
        await context.SaveChangesAsync();
        await context.Entry(lease).ReloadAsync();
        return lease;
    }

    public async Task AddAllRequiredDocumentsAsync(int leaseId)
    {
        var requiredTypes = await context.DocumentTypes
            .Where(type => type.TargetEntity == DocumentOwnerType.Lease
                && type.Required
                && type.IsActive)
            .ToListAsync();
        context.Belgeler.AddRange(requiredTypes.Select(type => new Document
        {
            DocumentTypeId = type.Id,
            OwnerType = DocumentOwnerType.Lease,
            OwnerId = leaseId,
            FileName = $"required-{type.Id}.pdf",
            MimeType = "application/pdf",
            FileSize = 1
        }));
        await context.SaveChangesAsync();
    }

    public LeaseService CreateService(string actorUserId)
    {
        var leaseRepository = new LeaseRepository(context);
        var rateRepository = new LeaseRateOverrideRepository(context);
        var chargeTypeRepository = new ChargeTypeRepository(context);
        var rateResolver = new RateResolverService(
            rateRepository,
            new SpaceRateResolver(
                new SpaceRepository(context),
                new SpaceRateRepository(context),
                new RateScheduleRepository(context)),
            leaseRepository,
            new TenantRepository(context));
        var batchRateResolver = new BatchRateResolver(
            rateRepository,
            new SpaceRepository(context),
            new SpaceRateRepository(context),
            new RateScheduleRepository(context),
            leaseRepository);
        var chargeGeneration = new ChargeGenerationService(
            new ChargeRepository(context),
            chargeTypeRepository,
            new UnitOfWork(context),
            rateResolver,
            leaseRepository,
            new SpaceRepository(context),
            new TenantRepository(context));
        return new LeaseService(
            leaseRepository,
            rateRepository,
            new ChargeLineItemRepository(context),
            chargeTypeRepository,
            new TenantRepository(context),
            chargeGeneration,
            new UnitOfWork(context),
            new StatisticsService(
                chargeTypeRepository,
                leaseRepository,
                rateResolver,
                batchRateResolver,
                new TestOperationalPolicyProvider()),
            new LeaseReviewHistoryRepository(context),
            new DocumentRepository(context),
            new DocumentTypeRepository(context),
            new ApplicationUserRepository(context),
            new TestUserContext(actorUserId),
            CreateRules(leaseRepository, rateRepository, chargeTypeRepository, chargeGeneration));
    }

    private LeaseApplicationRules CreateRules(
        LeaseRepository leaseRepository,
        LeaseRateOverrideRepository rateRepository,
        ChargeTypeRepository chargeTypeRepository,
        ChargeGenerationService chargeGeneration)
        => new(
            leaseRepository,
            rateRepository,
            chargeTypeRepository,
            new TenantRepository(context),
            chargeGeneration,
            new DocumentRepository(context),
            new DocumentTypeRepository(context),
            new LeaseReviewHistoryRepository(context));

    public LeaseGroupService CreateGroupService(
        string actorUserId,
        IDepositRefundStatusService? depositRefundStatusService = null)
    {
        var leaseRepository = new LeaseRepository(context);
        var rateRepository = new LeaseRateOverrideRepository(context);
        var chargeTypeRepository = new ChargeTypeRepository(context);
        var rateResolver = new RateResolverService(
            rateRepository,
            new SpaceRateResolver(
                new SpaceRepository(context),
                new SpaceRateRepository(context),
                new RateScheduleRepository(context)),
            leaseRepository,
            new TenantRepository(context));
        var chargeGeneration = new ChargeGenerationService(
            new ChargeRepository(context),
            chargeTypeRepository,
            new UnitOfWork(context),
            rateResolver,
            leaseRepository,
            new SpaceRepository(context),
            new TenantRepository(context));
        var batchRateResolver = new BatchRateResolver(
            rateRepository,
            new SpaceRepository(context),
            new SpaceRateRepository(context),
            new RateScheduleRepository(context),
            leaseRepository);
        return new LeaseGroupService(
            new LeaseGroupRepository(context),
            leaseRepository,
            rateRepository,
            new DocumentRepository(context),
            new ApplicationUserRepository(context),
            chargeGeneration,
            CreateRules(leaseRepository, rateRepository, chargeTypeRepository, chargeGeneration),
            new StatisticsService(
                chargeTypeRepository,
                leaseRepository,
                rateResolver,
                batchRateResolver,
                new TestOperationalPolicyProvider()),
            depositRefundStatusService ?? new DummyDepositRefundStatusService(),
            new TestUserContext(actorUserId),
            new UnitOfWork(context));
    }

    private static ApplicationUser CreateUser(string userName, string displayName) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        AdSoyad = displayName,
        UserType = UserType.Internal
    };

    private sealed class TestUserContext(string userId) : ICurrentUserContext
    {
        public string? UserId => userId;
        public UserType? UserType => KiraTakip.Models.Enums.UserType.Internal;
        public int? TenantId => null;
        public bool IsKiraciUser => false;
        public bool IsSuperAdmin => false;
    }
}
