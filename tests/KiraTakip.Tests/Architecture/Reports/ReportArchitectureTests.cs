using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Properties;
using KiraTakip.Services.Reporting;
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
                PropertyIds: [],
                UnitIds: [data.FirstUnitId]));

        Assert.Equal(12, report.Rows.Count);
        var january = Assert.Single(report.Rows, row => row.Month == 1);
        Assert.Equal(1, january.ChargeCount);
        Assert.Equal(100m, january.ExpectedAmount);
        Assert.Equal(1, january.OverdueChargeCount);
        Assert.Equal(80m, january.OverdueAmount);
        // Tahsilat artık ödeme tarihine göre (PaymentAllocation, Approved) — yalnız firstUnit
        // kapsamındaki onaylı ödeme (20) sayılır; store2'ye giden ve Pending olan ödemeler dışarıda kalır.
        Assert.Equal(20m, january.CollectedAmount);
        Assert.Equal(1, january.CollectedPaymentCount);
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
                PropertyIds: [data.FirstPropertyId],
                UnitIds: [data.SecondUnitId]));

        var january = Assert.Single(report.Rows);
        Assert.Equal(2, january.ChargeCount);
        Assert.Equal(300m, january.ExpectedAmount);
        Assert.Equal(2, january.OverdueChargeCount);
        Assert.Equal(280m, january.OverdueAmount);
        // Beklenen/gecikme tarafı (ChargeRepository) mağaza/tahsilat kavramı taşımaz —
        // tahsilat ayrı bir repository'den (PaymentAllocationRepository) gelir.
        Assert.Equal(0m, january.CollectedAmount);
        Assert.Equal(0, january.CollectedPaymentCount);
    }

    [Fact]
    public async Task Report_MultiLineItemCharge_ShouldCountOverdueByDistinctChargeNotLineItem()
    {
        var data = await SeedMultiLineItemChargeAsync();
        var repository = new ChargeRepository(_context);

        var unfiltered = await repository.GetMonthlyCollectionReportAsync(
            new GetMonthlyCollectionReportInput(
                2026,
                new DateTime(2026, 2, 1),
                UnitIds: [data.UnitId]));

        var january = Assert.Single(unfiltered.Rows, row => row.Month == 1);
        // Tek tahakkuk, iki gecikmiş kalemi var (Kira + Ortak Gider) — kalem sayısı değil,
        // distinct tahakkuk sayısı dönmeli.
        Assert.Equal(1, january.ChargeCount);
        Assert.Equal(1, january.OverdueChargeCount);
        Assert.Equal(160m, january.OverdueAmount);

        var filteredToKira = await repository.GetMonthlyCollectionReportAsync(
            new GetMonthlyCollectionReportInput(
                2026,
                new DateTime(2026, 2, 1),
                UnitIds: [data.UnitId],
                ChargeTypeId: data.KiraChargeTypeId));

        var januaryKira = Assert.Single(filteredToKira.Rows, row => row.Month == 1);
        // Kalem Tipi filtresi seçiliyken de aynı tahakkuk bir kez sayılmalı.
        Assert.Equal(1, januaryKira.OverdueChargeCount);
        Assert.Equal(100m, januaryKira.OverdueAmount);
    }

    [Fact]
    public async Task Report_CollectedByMonth_ShouldUseChargePeriodNotPaymentDate()
    {
        var data = await SeedCrossYearPaymentDataAsync();
        var repository = new PaymentAllocationRepository(_context);

        var rows = await repository.GetCollectedByMonthAsync(
            new GetMonthlyCollectionReportInput(
                2026,
                new DateTime(2026, 4, 1),
                UnitIds: [data.UnitId]));

        // Tahakkuk dönemi Ocak (2026-01), ödeme Mart'ta (2026-03-10) yapılmış — tutar dönem
        // ayına (Ocak) yazılmalı, ödeme ayına (Mart) değil.
        var january = Assert.Single(rows, row => row.Month == 1);
        Assert.Equal(100m, january.CollectedAmount);
        Assert.DoesNotContain(rows, row => row.Month == 3);
    }

    [Fact]
    public async Task Report_CashBasisByChargeYear_ShouldGroupByChargePeriodYear()
    {
        var data = await SeedCrossYearPaymentDataAsync();
        var repository = new PaymentAllocationRepository(_context);

        var breakdown = await repository.GetCashBasisByChargeYearAsync(
            new GetMonthlyCollectionReportInput(
                2026,
                new DateTime(2026, 4, 1),
                UnitIds: [data.UnitId]));

        // İkisi de 2026'da ödenmiş (nakit bazlı, ödeme tarihi eksenli) — biri 2026 dönemli
        // tahakkuka (100), biri 2025 dönemli (geçmiş yıl) tahakkuka (200) gitmiş.
        Assert.Equal(2, breakdown.Count);
        var year2025 = Assert.Single(breakdown, item => item.Year == 2025);
        Assert.Equal(1, year2025.Count);
        Assert.Equal(200m, year2025.Amount);
        var year2026 = Assert.Single(breakdown, item => item.Year == 2026);
        Assert.Equal(1, year2026.Count);
        Assert.Equal(100m, year2026.Amount);
    }

    [Fact]
    public async Task Report_PaymentYearBreakdown_ShouldGroupByPaymentDateYear()
    {
        var data = await SeedMultiYearPaymentBreakdownAsync();
        var repository = new PaymentAllocationRepository(_context);
        var input = new GetMonthlyCollectionReportInput(2026, new DateTime(2027, 2, 1), UnitIds: [data.UnitId]);

        var breakdown = await repository.GetPaymentYearBreakdownForChargeYearAsync(input);

        // 2026 dönemli tek tahakkuk, iki kısmi ödemeyle kapanmış: 100'ü 2026'da, 50'si 2027'de.
        Assert.Equal(2, breakdown.Count);
        Assert.Equal(1, Assert.Single(breakdown, item => item.Year == 2026).Count);
        Assert.Equal(1, Assert.Single(breakdown, item => item.Year == 2027).Count);

        // "Ödenen" tanımı ödeme tarihinden bağımsız kalmalı — her iki ödeme de (100+50=150)
        // tahakkukun kendi dönem ayına (Ocak) yazılmalı.
        var rows = await repository.GetCollectedByMonthAsync(input);
        var january = Assert.Single(rows, row => row.Month == 1);
        Assert.Equal(150m, january.CollectedAmount);
        Assert.Equal(2, january.CollectedPaymentCount);
    }

    [Fact]
    public async Task Report_PriorYearsOverdue_ShouldBeExcludedFromCurrentYearReport()
    {
        var data = await SeedPriorYearOverdueChargeAsync();
        var chargeRepository = new ChargeRepository(_context);
        var input = new GetMonthlyCollectionReportInput(2026, new DateTime(2026, 4, 1), UnitIds: [data.UnitId]);

        var priorYearsOverdue = await chargeRepository.GetPriorYearsOverdueAsync(input);
        Assert.Equal(1, priorYearsOverdue.OverdueChargeCount);
        Assert.Equal(150m, priorYearsOverdue.OverdueAmount);

        // 2025 dönemli bu tahakkuk, 2026 yılı raporunun (dönem-yıl filtreli) satırlarına hiç
        // girmemeli — geçmiş yıl gecikmesi ayrı bir sorgudan (yukarıdaki) gelir.
        var currentYearReport = await chargeRepository.GetMonthlyCollectionReportAsync(input);
        Assert.Equal(0, currentYearReport.Rows.Sum(row => row.ChargeCount));
        Assert.Equal(0m, currentYearReport.Rows.Sum(row => row.ExpectedAmount));
    }

    [Fact]
    public async Task Report_StoreFilter_ShouldOnlyIncludeAllocationsForThatStore()
    {
        var data = await SeedReportDataAsync();
        var repository = new PaymentAllocationRepository(_context);
        var scopeUnitIds = new List<int> { data.FirstUnitId, data.SecondUnitId };

        var store1Rows = await repository.GetCollectedByMonthAsync(
            new GetMonthlyCollectionReportInput(2026, new DateTime(2026, 2, 1),
                StoreId: data.FirstStoreId, UnitIds: scopeUnitIds));
        var store2Rows = await repository.GetCollectedByMonthAsync(
            new GetMonthlyCollectionReportInput(2026, new DateTime(2026, 2, 1),
                StoreId: data.SecondStoreId, UnitIds: scopeUnitIds));
        var allStoresRows = await repository.GetCollectedByMonthAsync(
            new GetMonthlyCollectionReportInput(2026, new DateTime(2026, 2, 1),
                UnitIds: scopeUnitIds));

        Assert.Equal(20m, Assert.Single(store1Rows, row => row.Month == 1).CollectedAmount);
        Assert.Equal(45m, Assert.Single(store2Rows, row => row.Month == 1).CollectedAmount);
        Assert.Equal(65m, Assert.Single(allStoresRows, row => row.Month == 1).CollectedAmount);
    }

    [Fact]
    public async Task Report_CollectedAmount_ShouldExcludeNonApprovedAllocations()
    {
        var data = await SeedReportDataAsync();
        var repository = new PaymentAllocationRepository(_context);

        var rows = await repository.GetCollectedByMonthAsync(
            new GetMonthlyCollectionReportInput(2026, new DateTime(2026, 3, 1),
                UnitIds: [data.FirstUnitId, data.SecondUnitId]));

        // Şubat'ta yalnız reddedilmiş bir ödeme var (999 tutar) — onaylı olmadığı için hiç sayılmamalı.
        Assert.DoesNotContain(rows, row => row.Month == 2);
    }

    private ReportService CreateService()
    {
        return new(
            new ChargeRepository(_context),
            new PaymentAllocationRepository(_context),
            new StoreRepository(_context),
            new ChargeTypeRepository(_context));
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
        var chargeType = new ChargeType
        {
            Name = $"Rapor Borç Tipi {suffix}",
            Code = $"RPRCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.UserManual
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"rapor-{suffix}@test.local",
            NormalizedUserName = $"RAPOR-{suffix}@TEST.LOCAL",
            Email = $"rapor-{suffix}@test.local",
            NormalizedEmail = $"RAPOR-{suffix}@TEST.LOCAL",
            EmailConfirmed = true,
            IsActive = true
        };
        var firstStore = new Store { Name = $"Rapor Mağaza 1 {suffix}", Code = $"RPRSTORE1_{suffix}", IsActive = true };
        firstStore.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"M1-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });
        var secondStore = new Store { Name = $"Rapor Mağaza 2 {suffix}", Code = $"RPRSTORE2_{suffix}", IsActive = true };
        secondStore.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"M2-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });

        _context.AddRange(firstProperty, secondProperty, unitType, tenant, chargeType, user);
        _context.Stores.AddRange(firstStore, secondStore);
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

        var chargeA = CreateCharge(tenant.Id, firstUnit.Id, 100m, ChargeStatus.Pending);
        var chargeCancelled = CreateCharge(tenant.Id, firstUnit.Id, 500m, ChargeStatus.Cancelled);
        var chargeB = CreateCharge(tenant.Id, secondUnit.Id, 200m, ChargeStatus.Pending);
        _context.Charges.AddRange(chargeA, chargeCancelled, chargeB);
        await _context.SaveChangesAsync();

        var lineItemA = new ChargeLineItem
        {
            ChargeId = chargeA.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Rapor Kalem A",
            Amount = 100m,
            TotalAmount = 100m,
            PaidAmount = 20m
        };
        var lineItemCancelled = new ChargeLineItem
        {
            ChargeId = chargeCancelled.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Rapor İptal Kalem",
            Amount = 500m,
            TotalAmount = 500m,
            PaidAmount = 0m
        };
        var lineItemB = new ChargeLineItem
        {
            ChargeId = chargeB.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Rapor Kalem B",
            Amount = 200m,
            TotalAmount = 200m,
            PaidAmount = 0m
        };
        _context.ChargeLineItems.AddRange(lineItemA, lineItemCancelled, lineItemB);
        await _context.SaveChangesAsync();

        var firstStoreAccountId = firstStore.Accounts.Single().Id;
        var secondStoreAccountId = secondStore.Accounts.Single().Id;

        _context.PaymentAllocations.AddRange(
            // Store 1 — onaylı, Ocak: rapora dahil olmalı (20).
            CreateAllocation(chargeA.Id, lineItemA.Id, firstStoreAccountId, user.Id, 20m,
                new DateTime(2026, 1, 20), PaymentStatus.Approved),
            // Store 1 — onay bekliyor, Ocak: rapora dahil olmamalı.
            CreateAllocation(chargeA.Id, lineItemA.Id, firstStoreAccountId, user.Id, 15m,
                new DateTime(2026, 1, 22), PaymentStatus.PendingApproval),
            // Store 2 — onaylı, Ocak: rapora dahil olmalı (45).
            CreateAllocation(chargeB.Id, lineItemB.Id, secondStoreAccountId, user.Id, 45m,
                new DateTime(2026, 1, 25), PaymentStatus.Approved),
            // Store 1 — reddedildi, Şubat: rapora hiç girmemeli.
            CreateAllocation(chargeA.Id, lineItemA.Id, firstStoreAccountId, user.Id, 999m,
                new DateTime(2026, 2, 10), PaymentStatus.Rejected));
        await _context.SaveChangesAsync();

        return new ReportSeedData(
            firstProperty.Id,
            firstUnit.Id,
            secondUnit.Id,
            firstStore.Id,
            secondStore.Id);
    }

    private async Task<MultiLineItemSeedData> SeedMultiLineItemChargeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Rapor Çoklu Kalem Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Rapor Çoklu Kalem Birim Türü {suffix}",
            Code = $"RPRMLI_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant
        {
            TenantNo = $"RPRMLI-{suffix}",
            Name = $"Rapor Çoklu Kalem Kiracı {suffix}"
        };
        var kiraChargeType = new ChargeType
        {
            Name = $"Rapor Kira {suffix}",
            Code = $"RPRKIRA_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        var ortakGiderChargeType = new ChargeType
        {
            Name = $"Rapor Ortak Gider {suffix}",
            Code = $"RPROG_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };

        _context.AddRange(property, unitType, tenant, kiraChargeType, ortakGiderChargeType);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Rapor Çoklu Kalem Birim {suffix}",
            Area = 10m
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var charge = CreateCharge(tenant.Id, unit.Id, 200m, ChargeStatus.PartiallyPaid);
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        var kiraLineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = kiraChargeType.Id,
            Description = "Rapor Kira Kalemi",
            Amount = 100m,
            TotalAmount = 100m,
            PaidAmount = 0m
        };
        var ortakGiderLineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = ortakGiderChargeType.Id,
            Description = "Rapor Ortak Gider Kalemi",
            Amount = 100m,
            TotalAmount = 100m,
            PaidAmount = 40m
        };
        _context.ChargeLineItems.AddRange(kiraLineItem, ortakGiderLineItem);
        await _context.SaveChangesAsync();

        return new MultiLineItemSeedData(unit.Id, kiraChargeType.Id);
    }

    private async Task<CrossYearSeedData> SeedCrossYearPaymentDataAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Rapor Çapraz Yıl Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Rapor Çapraz Yıl Birim Türü {suffix}",
            Code = $"RPRCY_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant
        {
            TenantNo = $"RPRCY-{suffix}",
            Name = $"Rapor Çapraz Yıl Kiracı {suffix}"
        };
        var chargeType = new ChargeType
        {
            Name = $"Rapor Çapraz Yıl Kalemi {suffix}",
            Code = $"RPRCYCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"raporcy-{suffix}@test.local",
            NormalizedUserName = $"RAPORCY-{suffix}@TEST.LOCAL",
            Email = $"raporcy-{suffix}@test.local",
            NormalizedEmail = $"RAPORCY-{suffix}@TEST.LOCAL",
            EmailConfirmed = true,
            IsActive = true
        };
        var store = new Store { Name = $"Rapor Çapraz Yıl Mağaza {suffix}", Code = $"RPRCYSTORE_{suffix}", IsActive = true };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"CYM-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });

        _context.AddRange(property, unitType, tenant, chargeType, user);
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Rapor Çapraz Yıl Birim {suffix}",
            Area = 10m
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var chargeCurrentYear = CreateChargeWithPeriod(tenant.Id, unit.Id, 100m,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 15), ChargeStatus.Paid);
        var chargePriorYear = CreateChargeWithPeriod(tenant.Id, unit.Id, 200m,
            new DateTime(2025, 6, 1), new DateTime(2025, 6, 15), ChargeStatus.Paid);
        _context.Charges.AddRange(chargeCurrentYear, chargePriorYear);
        await _context.SaveChangesAsync();

        var lineItemCurrentYear = new ChargeLineItem
        {
            ChargeId = chargeCurrentYear.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Rapor Çapraz Yıl Kalemi (2026)",
            Amount = 100m,
            TotalAmount = 100m,
            PaidAmount = 100m
        };
        var lineItemPriorYear = new ChargeLineItem
        {
            ChargeId = chargePriorYear.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Rapor Çapraz Yıl Kalemi (2025)",
            Amount = 200m,
            TotalAmount = 200m,
            PaidAmount = 200m
        };
        _context.ChargeLineItems.AddRange(lineItemCurrentYear, lineItemPriorYear);
        await _context.SaveChangesAsync();

        var storeAccountId = store.Accounts.Single().Id;

        _context.PaymentAllocations.AddRange(
            // 2026 dönemli tahakkuk, Mart 2026'da ödenmiş (dönem ayı != ödeme ayı senaryosu).
            CreateAllocation(chargeCurrentYear.Id, lineItemCurrentYear.Id, storeAccountId, user.Id, 100m,
                new DateTime(2026, 3, 10), PaymentStatus.Approved),
            // 2025 dönemli (geçmiş yıl) tahakkuk, 2026'da ödenmiş.
            CreateAllocation(chargePriorYear.Id, lineItemPriorYear.Id, storeAccountId, user.Id, 200m,
                new DateTime(2026, 2, 5), PaymentStatus.Approved));
        await _context.SaveChangesAsync();

        return new CrossYearSeedData(unit.Id);
    }

    private async Task<CrossYearSeedData> SeedMultiYearPaymentBreakdownAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Rapor Çok Yıllı Ödeme Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Rapor Çok Yıllı Ödeme Birim Türü {suffix}",
            Code = $"RPRMYP_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant
        {
            TenantNo = $"RPRMYP-{suffix}",
            Name = $"Rapor Çok Yıllı Ödeme Kiracı {suffix}"
        };
        var chargeType = new ChargeType
        {
            Name = $"Rapor Çok Yıllı Ödeme Kalemi {suffix}",
            Code = $"RPRMYPCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"rapormyp-{suffix}@test.local",
            NormalizedUserName = $"RAPORMYP-{suffix}@TEST.LOCAL",
            Email = $"rapormyp-{suffix}@test.local",
            NormalizedEmail = $"RAPORMYP-{suffix}@TEST.LOCAL",
            EmailConfirmed = true,
            IsActive = true
        };
        var store = new Store { Name = $"Rapor Çok Yıllı Ödeme Mağaza {suffix}", Code = $"RPRMYPSTORE_{suffix}", IsActive = true };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"MYPM-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });

        _context.AddRange(property, unitType, tenant, chargeType, user);
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Rapor Çok Yıllı Ödeme Birim {suffix}",
            Area = 10m
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var charge = CreateChargeWithPeriod(tenant.Id, unit.Id, 150m,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 15), ChargeStatus.Paid);
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        var lineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Rapor Çok Yıllı Ödeme Kalemi",
            Amount = 150m,
            TotalAmount = 150m,
            PaidAmount = 150m
        };
        _context.ChargeLineItems.Add(lineItem);
        await _context.SaveChangesAsync();

        var storeAccountId = store.Accounts.Single().Id;

        _context.PaymentAllocations.AddRange(
            // İlk kısmi ödeme, aynı yılda (2026).
            CreateAllocation(charge.Id, lineItem.Id, storeAccountId, user.Id, 100m,
                new DateTime(2026, 6, 1), PaymentStatus.Approved),
            // İkinci kısmi ödeme, ertesi yıla sarkmış (2027) — geç kalan tamamlayıcı ödeme.
            CreateAllocation(charge.Id, lineItem.Id, storeAccountId, user.Id, 50m,
                new DateTime(2027, 1, 10), PaymentStatus.Approved));
        await _context.SaveChangesAsync();

        return new CrossYearSeedData(unit.Id);
    }

    private async Task<PriorYearOverdueSeedData> SeedPriorYearOverdueChargeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Rapor Geçmiş Yıl Gecikme Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Rapor Geçmiş Yıl Gecikme Birim Türü {suffix}",
            Code = $"RPRPYO_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant
        {
            TenantNo = $"RPRPYO-{suffix}",
            Name = $"Rapor Geçmiş Yıl Gecikme Kiracı {suffix}"
        };
        var chargeType = new ChargeType
        {
            Name = $"Rapor Geçmiş Yıl Gecikme Kalemi {suffix}",
            Code = $"RPRPYOCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.MonthlyFixed
        };

        _context.AddRange(property, unitType, tenant, chargeType);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Rapor Geçmiş Yıl Gecikme Birim {suffix}",
            Area = 10m
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var charge = CreateChargeWithPeriod(tenant.Id, unit.Id, 150m,
            new DateTime(2025, 3, 1), new DateTime(2025, 3, 15), ChargeStatus.Overdue);
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        var lineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Rapor Geçmiş Yıl Gecikme Kalemi",
            Amount = 150m,
            TotalAmount = 150m,
            PaidAmount = 0m
        };
        _context.ChargeLineItems.Add(lineItem);
        await _context.SaveChangesAsync();

        return new PriorYearOverdueSeedData(unit.Id);
    }

    // Not: Charge.TotalAmount/PaidAmount artık rapor tarafından okunmuyor (kaynak ChargeLineItem) —
    // burada yalnız Charge-seviyesi check constraint'leri (>=0, PaidAmount<=TotalAmount) tatmin edilir.
    private static Charge CreateCharge(int tenantId, int unitId, decimal totalAmount, ChargeStatus status)
        => new()
        {
            ChargeNo = $"TEST-{Guid.NewGuid():N}"[..20],
            TenantId = tenantId,
            UnitId = unitId,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = new DateTime(2026, 1, 15),
            ExpectedAmount = totalAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0m,
            Status = status,
            SourceType = ChargeSourceType.Lease
        };

    private static Charge CreateChargeWithPeriod(
        int tenantId, int unitId, decimal totalAmount, DateTime periodStart, DateTime dueDate, ChargeStatus status)
        => new()
        {
            ChargeNo = $"TEST-{Guid.NewGuid():N}"[..20],
            TenantId = tenantId,
            UnitId = unitId,
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMonths(1).AddDays(-1),
            DueDate = dueDate,
            ExpectedAmount = totalAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0m,
            Status = status,
            SourceType = ChargeSourceType.Lease
        };

    private static PaymentAllocation CreateAllocation(
        int chargeId,
        int chargeLineItemId,
        int storeAccountId,
        string userId,
        decimal amount,
        DateTime paymentDate,
        PaymentStatus status)
        => new()
        {
            PaymentNo = $"TEST-{Guid.NewGuid():N}"[..20],
            ChargeId = chargeId,
            ChargeLineItemId = chargeLineItemId,
            StoreAccountId = storeAccountId,
            CreatedByUserId = userId,
            PaymentDate = paymentDate,
            Amount = amount,
            PaymentChannel = PaymentChannel.Eft,
            PaymentSourceType = PaymentSourceType.Manual,
            Status = status,
            EntryDate = paymentDate
        };

    private sealed record ReportSeedData(
        int FirstPropertyId,
        int FirstUnitId,
        int SecondUnitId,
        int FirstStoreId,
        int SecondStoreId);

    private sealed record MultiLineItemSeedData(int UnitId, int KiraChargeTypeId);
    private sealed record CrossYearSeedData(int UnitId);
    private sealed record PriorYearOverdueSeedData(int UnitId);
}
