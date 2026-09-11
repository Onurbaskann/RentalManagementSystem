using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Properties;
using KiraTakip.Services.Charges;
using KiraTakip.Web.Validators;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Report;

namespace KiraTakip.Tests;

public class ReportQueryValidationTests
{
    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Validator_ShouldRejectYearOutsideSupportedRange(int year)
    {
        var result = new ReportQueryViewModelValidator()
            .Validate(new ReportQueryViewModel { Year = year });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(ReportQueryViewModel.Year));
    }
}

[Collection("Database collection")]
public class ReportArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ReportArchitectureTests(DatabaseFixture fixture)
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
    public async Task Report_DirectUnitScope_ShouldExcludeCancelledAndCalculateOverdueDynamically()
    {
        var data = await SeedReportDataAsync();
        var service = CreateService();

        var report = await service.GetMonthlyCollectionReportAsync(
            new GetMonthlyCollectionReportInput(
                2026,
                new DateTime(2026, 2, 1),
                [],
                [data.FirstUnitId]));

        Assert.Equal(12, report.Rows.Count);
        var january = Assert.Single(report.Rows, row => row.Month == 1);
        Assert.Equal(1, january.ChargeCount);
        Assert.Equal(100m, january.ExpectedAmount);
        Assert.Equal(20m, january.CollectedAmount);
        Assert.Equal(1, january.OverdueChargeCount);
        Assert.Equal(80m, january.OverdueAmount);
        Assert.Contains(2026, report.AvailableYears);
    }

    [Fact]
    public async Task Report_PropertyAndUnitScopes_ShouldBeCombinedAsUnion()
    {
        var data = await SeedReportDataAsync();
        var repository = new ChargeRepository(_context);

        var report = await repository.GetMonthlyCollectionReportAsync(
            new GetMonthlyCollectionReportInput(
                2026,
                new DateTime(2026, 2, 1),
                [data.FirstPropertyId],
                [data.SecondUnitId]));

        var january = Assert.Single(report.Rows);
        Assert.Equal(2, january.ChargeCount);
        Assert.Equal(300m, january.ExpectedAmount);
        Assert.Equal(20m, january.CollectedAmount);
        Assert.Equal(2, january.OverdueChargeCount);
        Assert.Equal(280m, january.OverdueAmount);
    }

    private ChargeService CreateService()
    {
        return new(new ChargeRepository(_context),
                   new PaymentAllocationRepository(_context),
                   new UnitRepository(_context),
                   new ChargeLineItemRepository(_context),
                   new UnitOfWork(_context));
    }

    private async Task<ReportSeedData> SeedReportDataAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstProperty = new Property { Name = $"Rapor Taşınmaz 1 {suffix}" };
        var secondProperty = new Property { Name = $"Rapor Taşınmaz 2 {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Rapor Birim Türü {suffix}",
            Code = $"RPR_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant
        {
            TenantNo = $"RPR-{suffix}",
            Name = $"Rapor Kiracı {suffix}"
        };

        _context.AddRange(firstProperty, secondProperty, unitType, tenant);
        await _context.SaveChangesAsync();

        var firstUnit = new Unit
        {
            PropertyId = firstProperty.Id,
            UnitTypeId = unitType.Id,
            Name = $"Rapor Birim 1 {suffix}",
            Area = 10m
        };
        var secondUnit = new Unit
        {
            PropertyId = secondProperty.Id,
            UnitTypeId = unitType.Id,
            Name = $"Rapor Birim 2 {suffix}",
            Area = 20m
        };

        _context.Units.AddRange(firstUnit, secondUnit);
        await _context.SaveChangesAsync();

        _context.Charges.AddRange(
            CreateCharge(tenant.Id, firstUnit.Id, 100m, 20m, ChargeStatus.Pending),
            CreateCharge(tenant.Id, firstUnit.Id, 500m, 0m, ChargeStatus.Cancelled),
            CreateCharge(tenant.Id, secondUnit.Id, 200m, 0m, ChargeStatus.Pending));
        await _context.SaveChangesAsync();

        return new ReportSeedData(
            firstProperty.Id,
            firstUnit.Id,
            secondUnit.Id);
    }

    private static Charge CreateCharge(
        int tenantId,
        int unitId,
        decimal totalAmount,
        decimal paidAmount,
        ChargeStatus status)
        => new()
        {
            TenantId = tenantId,
            UnitId = unitId,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = new DateTime(2026, 1, 15),
            ExpectedAmount = totalAmount,
            TotalAmount = totalAmount,
            PaidAmount = paidAmount,
            Status = status,
            SourceType = ChargeSourceType.Lease
        };

    private sealed record ReportSeedData(
        int FirstPropertyId,
        int FirstUnitId,
        int SecondUnitId);
}
