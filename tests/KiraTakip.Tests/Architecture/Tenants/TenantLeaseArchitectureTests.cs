using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Leases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Charge;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class TenantLeaseArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public TenantLeaseArchitectureTests(DatabaseFixture fixture)
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
    public async Task LeaseDetails_ShouldHideAnotherTenantsLease()
    {
        var seed = await SeedAsync();
        var repository = new LeaseRepository(_context);

        var visible = await repository.GetTenantDetailsAsync(
            seed.FirstLeaseId,
            seed.FirstTenantId);
        var hidden = await repository.GetTenantDetailsAsync(
            seed.FirstLeaseId,
            seed.SecondTenantId);

        Assert.NotNull(visible);
        Assert.Null(hidden);
    }

    [Fact]
    public async Task ChargeData_ShouldApplyTenantScopeAndCalculateStatusWithoutMutation()
    {
        var seed = await SeedAsync();
        var repository = new ChargeRepository(_context);
        var today = new DateTime(2026, 7, 1);

        var result = await repository.GetTenantLeaseDataAsync(
            new GetTenantLeaseChargeDataInput(
                seed.FirstTenantId,
                seed.FirstLeaseId,
                today,
                IncludeHistory: true));

        Assert.Equal(2, result.Charges.Count);
        Assert.All(result.Charges, charge => Assert.Equal(seed.FirstTenantId, charge.TenantId));
        Assert.Equal(
            ChargeStatus.Overdue,
            Assert.Single(result.Charges, charge => charge.Id == seed.FirstLeaseChargeId).Status);
        Assert.Equal(seed.FirstLeaseChargePeriod, result.CurrentCharge.Period);
        Assert.Single(result.CurrentCharge.LineItems);

        var storedStatus = await _context.Charges
            .Where(charge => charge.Id == seed.FirstLeaseChargeId)
            .Select(charge => charge.Status)
            .SingleAsync();
        Assert.Equal(ChargeStatus.Pending, storedStatus);
    }

    [Fact]
    public async Task DepositQuery_ShouldApplyTenantScope()
    {
        var seed = await SeedAsync();
        var repository = new ChargeLineItemRepository(_context);

        var deposits = await repository.GetDepositAmountsByLeaseIdsAsync(
            [seed.FirstLeaseId, seed.SecondLeaseId],
            seed.FirstTenantId);

        Assert.Single(deposits);
        Assert.Equal(250m, deposits[seed.FirstLeaseId]);
        Assert.DoesNotContain(seed.SecondLeaseId, deposits.Keys);
    }

    [Fact]
    public async Task LeaseAndChargeDetails_ShouldHonorUnitScope()
    {
        var seed = await SeedAsync();
        var leaseRepository = new LeaseRepository(_context);
        var chargeRepository = new ChargeRepository(_context);
        var unitId = await _context.Leases
            .Where(lease => lease.Id == seed.FirstLeaseId)
            .Select(lease => lease.UnitId)
            .SingleAsync();
        var emptyPropertyScope = new List<int>();
        var emptyUnitScope = new List<int>();

        Assert.NotNull(await leaseRepository.GetTenantDetailsAsync(
            seed.FirstLeaseId,
            seed.FirstTenantId,
            [],
            [unitId]));
        Assert.Null(await leaseRepository.GetTenantDetailsAsync(
            seed.FirstLeaseId,
            seed.FirstTenantId,
            emptyPropertyScope,
            emptyUnitScope));
        Assert.Null(await chargeRepository.GetTenantDetailsAsync(
            seed.FirstLeaseChargeId,
            seed.FirstTenantId,
            emptyPropertyScope,
            emptyUnitScope));
        var chargeData = await chargeRepository.GetTenantLeaseDataAsync(
            new GetTenantLeaseChargeDataInput(
                seed.FirstTenantId,
                seed.FirstLeaseId,
                new DateTime(2026, 7, 1),
                true,
                emptyPropertyScope,
                emptyUnitScope));
        Assert.Empty(chargeData.Charges);
        Assert.Empty(chargeData.CurrentCharge.LineItems);
    }
    private async Task<TenantLeaseSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Sözleşme Taşınmazı {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Sözleşme Birim Türü {suffix}",
            Code = $"TLS_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var firstTenant = new Tenant
        {
            TenantNo = $"TLS1-{suffix}",
            Name = $"Birinci Kiracı {suffix}"
        };
        var secondTenant = new Tenant
        {
            TenantNo = $"TLS2-{suffix}",
            Name = $"İkinci Kiracı {suffix}"
        };
        var monthlyType = new ChargeType
        {
            Name = $"Kira {suffix}",
            Code = $"KIRA_{suffix}",
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        var depositType = await _context.ChargeTypes
            .FirstOrDefaultAsync(chargeType => chargeType.Code == BorcTipiConsts.Depozito);
        if (depositType == null)
        {
            depositType = new ChargeType
            {
                Name = $"Depozito {suffix}",
                Code = BorcTipiConsts.Depozito,
                Behavior = ChargeTypeBehavior.FirstMonthOneTime
            };
            _context.ChargeTypes.Add(depositType);
        }

        _context.AddRange(
            property,
            unitType,
            firstTenant,
            secondTenant,
            monthlyType);
        await _context.SaveChangesAsync();

        var firstUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Birim 1 {suffix}",
            Area = 25m
        };
        var secondUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Birim 2 {suffix}",
            Area = 30m
        };
        _context.Units.AddRange(firstUnit, secondUnit);
        await _context.SaveChangesAsync();

        var firstLease = CreateLease(firstTenant.Id, firstUnit.Id);
        var secondLease = CreateLease(secondTenant.Id, secondUnit.Id);
        _context.Leases.AddRange(firstLease, secondLease);
        await _context.SaveChangesAsync();

        var leaseChargePeriod = new DateTime(2026, 5, 1);
        var firstLeaseCharge = CreateCharge(
            firstTenant.Id,
            firstUnit.Id,
            firstLease.Id,
            leaseChargePeriod,
            ChargeSourceType.Lease);
        var firstManualCharge = CreateCharge(
            firstTenant.Id,
            firstUnit.Id,
            firstLease.Id,
            new DateTime(2026, 6, 1),
            ChargeSourceType.Manual);
        var secondLeaseCharge = CreateCharge(
            secondTenant.Id,
            secondUnit.Id,
            secondLease.Id,
            leaseChargePeriod,
            ChargeSourceType.Lease);
        _context.Charges.AddRange(firstLeaseCharge, firstManualCharge, secondLeaseCharge);
        await _context.SaveChangesAsync();

        _context.ChargeLineItems.AddRange(
            CreateLineItem(firstLeaseCharge.Id, monthlyType.Id, 100m),
            CreateLineItem(firstLeaseCharge.Id, depositType.Id, 250m),
            CreateLineItem(secondLeaseCharge.Id, depositType.Id, 500m));
        await _context.SaveChangesAsync();

        return new TenantLeaseSeed(
            firstTenant.Id,
            secondTenant.Id,
            firstLease.Id,
            secondLease.Id,
            firstLeaseCharge.Id,
            leaseChargePeriod);
    }

    private static Lease CreateLease(int tenantId, int unitId)
        => new()
        {
            TenantId = tenantId,
            UnitId = unitId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = LeaseStatus.Active
        };

    private static Charge CreateCharge(
        int tenantId,
        int unitId,
        int leaseId,
        DateTime period,
        ChargeSourceType sourceType)
        => new()
        {
            TenantId = tenantId,
            UnitId = unitId,
            LeaseId = leaseId,
            PeriodStart = period,
            PeriodEnd = period.AddMonths(1).AddDays(-1),
            DueDate = period.AddDays(14),
            ExpectedAmount = 100m,
            TotalAmount = 100m,
            PaidAmount = 0m,
            Status = ChargeStatus.Pending,
            SourceType = sourceType
        };

    private static ChargeLineItem CreateLineItem(
        int chargeId,
        int chargeTypeId,
        decimal amount)
        => new()
        {
            ChargeId = chargeId,
            ChargeTypeId = chargeTypeId,
            Description = "Test kalemi",
            CalculationMethod = CalculationMethod.Fixed,
            UnitValue = amount,
            Multiplier = 1m,
            Amount = amount,
            TotalAmount = amount,
            SourceType = LineItemSourceType.LeaseRateOverride
        };

    private sealed record TenantLeaseSeed(
        int FirstTenantId,
        int SecondTenantId,
        int FirstLeaseId,
        int SecondLeaseId,
        int FirstLeaseChargeId,
        DateTime FirstLeaseChargePeriod);
}
