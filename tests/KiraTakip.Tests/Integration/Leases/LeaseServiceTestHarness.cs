using KiraTakip.Data;
using KiraTakip.Models.Constants;
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
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Leases;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Reporting;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

internal sealed class LeaseServiceTestHarness(ApplicationDbContext context)
{
    public sealed record Seed(
        string Suffix,
        int PropertyId,
        int UnitTypeId,
        int UnitId,
        int TenantId,
        string ApplicantId,
        string ReviewerId,
        int RentChargeTypeId);

    public static LeaseAccessScopeInput Scope() => new();

    public IReadOnlyCollection<LeaseRateOverrideInput> Rates(Seed seed)
        => [new LeaseRateOverrideInput(seed.RentChargeTypeId, 1000, CalculationMethod.Fixed, 20)];

    public async Task<Seed> SeedAsync(decimal area = 50, string? unitTypeName = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Anlık Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = unitTypeName ?? $"Anlık Tip {suffix}",
            Code = $"SNP_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { TenantNo = $"SNP-{suffix}", Name = $"Anlık Kiracı {suffix}" };
        var applicant = CreateUser($"snp-app-{suffix}", $"Başvuran {suffix}");
        var reviewer = CreateUser($"snp-rev-{suffix}", $"Onaylayan {suffix}");
        context.AddRange(property, unitType, tenant, applicant, reviewer);
        await context.SaveChangesAsync();
        var rentChargeType = await context.ChargeTypes.SingleAsync(type =>
            type.Code == BorcTipiConsts.Kira);
        rentChargeType.IsActive = true;
        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Anlık Birim {suffix}",
            Area = area
        };
        context.Units.Add(unit);
        await context.SaveChangesAsync();
        return new Seed(
            suffix,
            property.Id,
            unitType.Id,
            unit.Id,
            tenant.Id,
            applicant.Id,
            reviewer.Id,
            rentChargeType.Id);
    }

    public async Task<(UnitType UnitType, Unit Unit)> AddUnitAsync(
        Seed seed,
        decimal area,
        string unitTypeName)
    {
        var unitType = new UnitType
        {
            Name = unitTypeName,
            Code = $"SNP2_{Guid.NewGuid():N}"[..20],
            Usage = UnitTypeUsage.Rentable
        };
        context.UnitTypes.Add(unitType);
        await context.SaveChangesAsync();
        var unit = new Unit
        {
            PropertyId = seed.PropertyId,
            UnitTypeId = unitType.Id,
            Name = $"Anlık Birim 2 {seed.Suffix}",
            Area = area
        };
        context.Units.Add(unit);
        await context.SaveChangesAsync();
        return (unitType, unit);
    }

    public async Task<int> AddTenantAsync(string suffix)
    {
        var tenant = new Tenant { TenantNo = $"SNP2-{suffix}", Name = $"Anlık Kiracı 2 {suffix}" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant.Id;
    }

    public async Task<Lease> CreateDraftAsync(
        Seed seed,
        DateTime startDate,
        DateTime endDate,
        int? unitId = null,
        int? tenantId = null)
    {
        var lease = await CreateService(seed.ApplicantId).CreateDraftAsync(new CreateLeaseDraftInput(
            unitId ?? seed.UnitId,
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
            new UnitRateRepository(context),
            new PropertyRateOverrideRepository(context),
            new RateScheduleRepository(context),
            leaseRepository,
            new UnitRepository(context),
            new TenantRepository(context));
        var batchRateResolver = new BatchRateResolver(
            rateRepository,
            new UnitRateRepository(context),
            new PropertyRateOverrideRepository(context),
            new RateScheduleRepository(context),
            leaseRepository);
        var chargeGeneration = new ChargeGenerationService(
            new ChargeRepository(context),
            chargeTypeRepository,
            new UnitOfWork(context),
            rateResolver,
            leaseRepository,
            new UnitRepository(context),
            new TenantRepository(context));
        return new LeaseService(
            leaseRepository,
            rateRepository,
            new ChargeLineItemRepository(context),
            chargeTypeRepository,
            new UnitRepository(context),
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
            new TestOperationalPolicyProvider());
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
