using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.TenantPanel;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class TenantPanelArchitectureTests : IDisposable
{
    private static readonly DateTime Today = new(2026, 7, 15);
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public TenantPanelArchitectureTests(DatabaseFixture fixture)
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
    public async Task ActiveLeaseCount_ShouldHonorTenantStatusAndDateRange()
    {
        var seed = await SeedAsync();
        var repository = new LeaseRepository(_context);

        var count = await repository.CountActiveByTenantAsync(seed.TenantId, Today);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DashboardChargeData_ShouldUseDynamicBalanceAndExcludeCancelledCharges()
    {
        var seed = await SeedAsync();
        var chargeRepository = new ChargeRepository(_context);
        var lineItemRepository = new ChargeLineItemRepository(_context);

        var chargeData = await chargeRepository.GetTenantPanelDataAsync(
            new GetTenantPanelChargeDataInput(
                seed.TenantId,
                Today,
                IncludeDebtData: true,
                IncludeMonthlyExpected: true));
        var distribution = await lineItemRepository.GetTenantDebtDistributionAsync(seed.TenantId);

        Assert.Equal(250m, chargeData.TotalOutstandingDebt);
        Assert.Equal(1, chargeData.UpcomingPaymentCount);
        Assert.Equal(150m, chargeData.UpcomingPaymentAmount);
        Assert.Equal(1, chargeData.OverdueCount);
        Assert.Equal(100m, chargeData.OverdueAmount);
        Assert.Equal(2, chargeData.UpcomingCharges.Count);
        Assert.DoesNotContain(chargeData.UpcomingCharges, charge =>
            charge.ChargeId == seed.FullyPaidChargeId
            || charge.ChargeId == seed.CancelledChargeId);

        var julyExpected = Assert.Single(chargeData.MonthlyExpected, total =>
            total.Year == Today.Year && total.Month == Today.Month);
        Assert.Equal(600m, julyExpected.Total);
        Assert.Equal(300m, Assert.Single(distribution).Amount);
    }

    [Fact]
    public async Task DashboardService_ShouldReturnOnlyAuthorizedModuleData()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var noModuleDashboard = await service.GetDashboardAsync(
            new GetTenantPanelDashboardInput(
                seed.TenantId,
                seed.UserId,
                Today,
                CanViewLeases: false,
                CanViewCharges: false,
                CanViewPayments: false));

        Assert.Equal(0, noModuleDashboard.ActiveLeaseCount);
        Assert.Equal(0m, noModuleDashboard.TotalOutstandingDebt);
        Assert.Empty(noModuleDashboard.MonthlyCashFlow);
        Assert.Empty(noModuleDashboard.DebtTypeDistribution);
        Assert.Empty(noModuleDashboard.UpcomingCharges);
        Assert.Empty(noModuleDashboard.RecentPayments);

        var paymentOnlyDashboard = await service.GetDashboardAsync(
            new GetTenantPanelDashboardInput(
                seed.TenantId,
                seed.UserId,
                Today,
                CanViewLeases: false,
                CanViewCharges: false,
                CanViewPayments: true));

        Assert.Equal(0m, paymentOnlyDashboard.TotalOutstandingDebt);
        Assert.Empty(paymentOnlyDashboard.DebtTypeDistribution);
        Assert.Empty(paymentOnlyDashboard.UpcomingCharges);
        Assert.Equal(6, paymentOnlyDashboard.MonthlyCashFlow.Count);
        Assert.Contains(paymentOnlyDashboard.MonthlyCashFlow, month =>
            month.Expected == 600m && month.Paid == 75m);
        Assert.Single(paymentOnlyDashboard.RecentPayments);
    }

    [Fact]
    public async Task DashboardService_ShouldGuardMissingTenant()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetDashboardAsync(
                new GetTenantPanelDashboardInput(
                    int.MaxValue,
                    "missing-user",
                    Today,
                    CanViewLeases: false,
                    CanViewCharges: false,
                    CanViewPayments: false)));

        Assert.Equal("TENANT_PANEL_TENANT_NOT_FOUND", exception.Code);
        Assert.Equal(ErrorType.NotFound, exception.ErrorType);
    }

    private TenantPanelService CreateService()
    {
        return new(new TenantRepository(_context),
                   new ApplicationUserRepository(_context),
                   new LeaseRepository(_context),
                   new ChargeRepository(_context),
                   new ChargeLineItemRepository(_context),
                   new PaymentAllocationRepository(_context));
    }

    [Fact]
    public async Task DashboardRepositories_ShouldHonorEmptyUnitScope()
    {
        var seed = await SeedAsync();
        var unitId = await _context.Leases
            .Where(lease => lease.TenantId == seed.TenantId)
            .Select(lease => lease.UnitId)
            .FirstAsync();
        var emptyPropertyScope = new List<int>();
        var emptyUnitScope = new List<int>();

        Assert.Equal(1, await new LeaseRepository(_context).CountActiveByTenantAsync(
            seed.TenantId,
            Today,
            [],
            [unitId]));
        Assert.Equal(0, await new LeaseRepository(_context).CountActiveByTenantAsync(
            seed.TenantId,
            Today,
            emptyPropertyScope,
            emptyUnitScope));
        var chargeData = await new ChargeRepository(_context).GetTenantPanelDataAsync(
            new GetTenantPanelChargeDataInput(
                seed.TenantId,
                Today,
                true,
                true,
                emptyPropertyScope,
                emptyUnitScope));
        Assert.Equal(0m, chargeData.TotalOutstandingDebt);
        Assert.Empty(chargeData.UpcomingCharges);
        Assert.Empty(await new ChargeLineItemRepository(_context)
            .GetTenantDebtDistributionAsync(
                seed.TenantId,
                emptyPropertyScope,
                emptyUnitScope));
        Assert.Empty((await new PaymentAllocationRepository(_context)
            .GetTenantPanelDataAsync(new GetTenantPanelPaymentDataInput(
                seed.TenantId,
                Today,
                emptyPropertyScope,
                emptyUnitScope))).RecentPayments);
    }
    private async Task<TenantPanelSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant
        {
            TenantNo = $"TP-{suffix}",
            Name = $"Panel Kiracısı {suffix}"
        };
        var otherTenant = new Tenant
        {
            TenantNo = $"TPX-{suffix}",
            Name = $"Diğer Kiracı {suffix}"
        };
        var property = new Property { Name = $"Panel Taşınmazı {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Panel Birim Türü {suffix}",
            Code = $"TP_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var chargeType = new ChargeType
        {
            Name = $"Panel Borç Türü {suffix}",
            Code = $"TPB_{suffix}",
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };

        _context.AddRange(tenant, otherTenant, property, unitType, chargeType);
        await _context.SaveChangesAsync();

        var user = new ApplicationUser
        {
            Id = $"panel-user-{suffix}",
            UserName = $"panel-{suffix}@example.com",
            NormalizedUserName = $"PANEL-{suffix}@EXAMPLE.COM",
            Email = $"panel-{suffix}@example.com",
            NormalizedEmail = $"PANEL-{suffix}@EXAMPLE.COM",
            AdSoyad = "Panel Kullanıcısı",
            TenantId = tenant.Id,
            UserType = UserType.Tenant,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Panel Birimi {suffix}",
            Area = 50m
        };
        _context.Users.Add(user);
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var currentLease = CreateLease(tenant.Id, unit.Id, Today.AddMonths(-1), Today.AddMonths(1));
        var futureLease = CreateLease(tenant.Id, unit.Id, Today.AddDays(1), Today.AddMonths(2));
        var expiredLease = CreateLease(tenant.Id, unit.Id, Today.AddMonths(-2), Today.AddDays(-1));
        var otherTenantLease = CreateLease(
            otherTenant.Id,
            unit.Id,
            Today.AddMonths(-1),
            Today.AddMonths(1));
        _context.Leases.AddRange(currentLease, futureLease, expiredLease, otherTenantLease);
        await _context.SaveChangesAsync();

        var overdueCharge = CreateCharge(
            tenant.Id,
            unit.Id,
            currentLease.Id,
            Today.AddDays(-1),
            100m,
            0m,
            ChargeStatus.Pending);
        var upcomingStaleStatusCharge = CreateCharge(
            tenant.Id,
            unit.Id,
            futureLease.Id,
            Today.AddDays(5),
            200m,
            50m,
            ChargeStatus.Overdue);
        var fullyPaidCharge = CreateCharge(
            tenant.Id,
            unit.Id,
            expiredLease.Id,
            Today.AddDays(3),
            300m,
            300m,
            ChargeStatus.Pending);
        var cancelledCharge = CreateCharge(
            tenant.Id,
            unit.Id,
            null,
            Today.AddDays(2),
            400m,
            0m,
            ChargeStatus.Cancelled);
        var otherTenantCharge = CreateCharge(
            otherTenant.Id,
            unit.Id,
            otherTenantLease.Id,
            Today.AddDays(1),
            500m,
            0m,
            ChargeStatus.Pending);
        _context.Charges.AddRange(
            overdueCharge,
            upcomingStaleStatusCharge,
            fullyPaidCharge,
            cancelledCharge,
            otherTenantCharge);
        await _context.SaveChangesAsync();

        var upcomingLineItem = CreateLineItem(upcomingStaleStatusCharge.Id, chargeType.Id, 200m);
        _context.ChargeLineItems.AddRange(
            CreateLineItem(overdueCharge.Id, chargeType.Id, 100m),
            upcomingLineItem,
            CreateLineItem(fullyPaidCharge.Id, chargeType.Id, 300m),
            CreateLineItem(cancelledCharge.Id, chargeType.Id, 400m));
        await _context.SaveChangesAsync();

        var storeAccountId = await PaymentLineItemTestHelper.CreateStoreAccountAsync(_context);
        _context.PaymentAllocations.Add(new PaymentAllocation
        {
            ChargeId = upcomingStaleStatusCharge.Id,
            ChargeLineItemId = upcomingLineItem.Id,
            StoreAccountId = storeAccountId,
            LeaseId = currentLease.Id,
            CreatedByUserId = user.Id,
            PaymentDate = Today,
            Amount = 75m,
            PaymentChannel = PaymentChannel.BankTransfer,
            Status = PaymentStatus.Approved
        });
        await _context.SaveChangesAsync();

        return new TenantPanelSeed(
            tenant.Id,
            user.Id,
            fullyPaidCharge.Id,
            cancelledCharge.Id);
    }

    private static Lease CreateLease(
        int tenantId,
        int unitId,
        DateTime startDate,
        DateTime endDate)
        => new()
        {
            TenantId = tenantId,
            UnitId = unitId,
            StartDate = startDate,
            EndDate = endDate,
            Status = LeaseStatus.Active
        };

    private static Charge CreateCharge(
        int tenantId,
        int unitId,
        int? leaseId,
        DateTime dueDate,
        decimal totalAmount,
        decimal paidAmount,
        ChargeStatus status)
        => new()
        {
            TenantId = tenantId,
            UnitId = unitId,
            LeaseId = leaseId,
            PeriodStart = new DateTime(dueDate.Year, dueDate.Month, 1),
            PeriodEnd = new DateTime(dueDate.Year, dueDate.Month, 1).AddMonths(1).AddDays(-1),
            DueDate = dueDate,
            ExpectedAmount = totalAmount,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            Status = status,
            SourceType = ChargeSourceType.Lease
        };

    private static ChargeLineItem CreateLineItem(
        int chargeId,
        int chargeTypeId,
        decimal amount)
        => new()
        {
            ChargeId = chargeId,
            ChargeTypeId = chargeTypeId,
            Description = "Panel test kalemi",
            CalculationMethod = CalculationMethod.Fixed,
            UnitValue = amount,
            Multiplier = 1m,
            Amount = amount,
            TotalAmount = amount,
            SourceType = LineItemSourceType.LeaseRateOverride
        };

    private sealed record TenantPanelSeed(
        int TenantId,
        string UserId,
        int FullyPaidChargeId,
        int CancelledChargeId);
}
