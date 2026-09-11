using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class ChargeLineItemPaymentSchemaTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ChargeLineItemPaymentSchemaTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
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
    public async Task Migration_ShouldCreateNotNullPaymentRoutingColumns()
    {
        var allocationColumns = await _context.Database.SqlQueryRaw<string>(@"
            SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'TahakkukOdemeleri' AND COLUMN_NAME IN ('TahakkukKalemiId', 'MagazaHesapBilgisiId')")
            .ToListAsync();
        Assert.Equal(2, allocationColumns.Count);
        Assert.All(allocationColumns, value => Assert.Equal("NO", value));

        var lineItemColumns = await _context.Database.SqlQueryRaw<string>(@"
            SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'TahakkukKalemleri' AND COLUMN_NAME = 'OdenenTutar'")
            .ToListAsync();
        Assert.Equal("NO", Assert.Single(lineItemColumns));
    }

    [Fact]
    public async Task Migration_ShouldCreatePaidAmountLimitCheckConstraint()
    {
        var counts = await _context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) FROM sys.check_constraints WHERE name = 'CK_TahakkukKalemleri_OdenenLimit'")
            .ToListAsync();
        Assert.Equal(1, Assert.Single(counts));
    }

    [Fact]
    public async Task PaidAmountAboveTotalAmount_ShouldViolateCheckConstraint()
    {
        var lineItem = await SeedLineItemAsync();
        lineItem.PaidAmount = lineItem.TotalAmount + 1m;

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task NegativePaidAmount_ShouldViolateCheckConstraint()
    {
        var lineItem = await SeedLineItemAsync();
        lineItem.PaidAmount = -1m;

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingStoreAccountUsedByPayment_ShouldBeRestricted()
    {
        var (lineItem, storeAccountId) = await SeedLineItemWithStoreAsync();
        var payment = new PaymentAllocation
        {
            ChargeId = lineItem.ChargeId,
            ChargeLineItemId = lineItem.Id,
            StoreAccountId = storeAccountId,
            CreatedByUserId = await _context.Users.Select(u => u.Id).FirstAsync(),
            PaymentDate = DateTime.Today,
            Amount = 100m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        };
        _context.PaymentAllocations.Add(payment);
        await _context.SaveChangesAsync();

        // EF'in ilişki-fixup davranışı (Remove() ile takip edilen bir StoreAccount silinirken
        // zorunlu FK'yi null'lamaya çalışıp istemci tarafında hata vermesi) devreye girmesin
        // diye DB seviyesindeki gerçek FK Restrict kısıtı ham SQL ile test edilir.
        await Assert.ThrowsAsync<SqlException>(() => _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM MagazaHesapBilgileri WHERE Id = {storeAccountId}"));
    }

    [Fact]
    public async Task TenantUser_ShouldNotSeeOtherTenantsChargeLineItems()
    {
        // Sorgu filtresi test edilebilsin diye bu test kendi verisini AYRI, COMMIT edilmiş bir
        // bağlamda oluşturur (sınıfın paylaşılan _context/transaction'ı rollback edildiği için
        // farklı bir bağlantıdan görünmez) ve sonunda kendi temizliğini yapar.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        int firstTenantId, secondTenantId, firstLineItemId, secondLineItemId;
        int firstChargeId, secondChargeId, propertyId, unitTypeId, unitId, chargeTypeId;

        await using (var setup = _fixture.CreateContext())
        {
            var property = new Property { Name = $"Kiracı İzolasyon {suffix}" };
            var unitType = new UnitType
            {
                Name = $"Kiracı İzolasyon Birimi {suffix}",
                Code = $"ISO_{suffix}",
                Usage = UnitTypeUsage.Rentable
            };
            var firstTenant = new Tenant { TenantNo = $"ISO-A-{suffix}", Name = $"İzolasyon Kiracı A {suffix}" };
            var secondTenant = new Tenant { TenantNo = $"ISO-B-{suffix}", Name = $"İzolasyon Kiracı B {suffix}" };
            var chargeType = new ChargeType
            {
                Name = $"İzolasyon Borç Tipi {suffix}",
                Code = $"ISOCT_{suffix}",
                IsActive = true,
                Behavior = ChargeTypeBehavior.UserManual
            };
            setup.AddRange(property, unitType, firstTenant, secondTenant, chargeType);
            await setup.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitTypeId = unitType.Id,
                Name = $"İzolasyon Ofis {suffix}",
                Area = 40m
            };
            setup.Units.Add(unit);
            await setup.SaveChangesAsync();

            var firstCharge = new Charge
            {
                TenantId = firstTenant.Id,
                UnitId = unit.Id,
                PeriodStart = new DateTime(2026, 1, 1),
                PeriodEnd = new DateTime(2026, 1, 31),
                DueDate = new DateTime(2026, 2, 5),
                ExpectedAmount = 100m,
                TotalAmount = 100m,
                Status = ChargeStatus.Pending
            };
            var secondCharge = new Charge
            {
                TenantId = secondTenant.Id,
                UnitId = unit.Id,
                PeriodStart = new DateTime(2026, 1, 1),
                PeriodEnd = new DateTime(2026, 1, 31),
                DueDate = new DateTime(2026, 2, 5),
                ExpectedAmount = 100m,
                TotalAmount = 100m,
                Status = ChargeStatus.Pending
            };
            setup.Charges.AddRange(firstCharge, secondCharge);
            await setup.SaveChangesAsync();

            var firstLineItem = new ChargeLineItem
            {
                ChargeId = firstCharge.Id,
                ChargeTypeId = chargeType.Id,
                Description = "A kiracısı kalemi",
                Amount = 100m,
                TotalAmount = 100m
            };
            var secondLineItem = new ChargeLineItem
            {
                ChargeId = secondCharge.Id,
                ChargeTypeId = chargeType.Id,
                Description = "B kiracısı kalemi",
                Amount = 100m,
                TotalAmount = 100m
            };
            setup.ChargeLineItems.AddRange(firstLineItem, secondLineItem);
            await setup.SaveChangesAsync();

            firstTenantId = firstTenant.Id;
            secondTenantId = secondTenant.Id;
            firstLineItemId = firstLineItem.Id;
            secondLineItemId = secondLineItem.Id;
            firstChargeId = firstCharge.Id;
            secondChargeId = secondCharge.Id;
            propertyId = property.Id;
            unitTypeId = unitType.Id;
            unitId = unit.Id;
            chargeTypeId = chargeType.Id;
        }

        try
        {
            await using var tenantContext = _fixture.CreateContext(
                new TenantCurrentUserContext { TenantId = firstTenantId });
            var visibleIds = await tenantContext.ChargeLineItems
                .Select(item => item.Id)
                .ToListAsync();

            Assert.Contains(firstLineItemId, visibleIds);
            Assert.DoesNotContain(secondLineItemId, visibleIds);
        }
        finally
        {
            await using var cleanup = _fixture.CreateContext();
            cleanup.ChargeLineItems.RemoveRange(
                cleanup.ChargeLineItems.Where(item => item.Id == firstLineItemId || item.Id == secondLineItemId));
            await cleanup.SaveChangesAsync();
            cleanup.Charges.RemoveRange(
                cleanup.Charges.Where(c => c.Id == firstChargeId || c.Id == secondChargeId));
            await cleanup.SaveChangesAsync();
            cleanup.ChargeTypes.Remove(await cleanup.ChargeTypes.SingleAsync(ct => ct.Id == chargeTypeId));
            cleanup.Units.Remove(await cleanup.Units.SingleAsync(u => u.Id == unitId));
            await cleanup.SaveChangesAsync();
            cleanup.Tenants.RemoveRange(
                cleanup.Tenants.Where(t => t.Id == firstTenantId || t.Id == secondTenantId));
            cleanup.UnitTypes.Remove(await cleanup.UnitTypes.SingleAsync(ut => ut.Id == unitTypeId));
            cleanup.Properties.Remove(await cleanup.Properties.SingleAsync(p => p.Id == propertyId));
            await cleanup.SaveChangesAsync();
        }
    }

    private async Task<ChargeLineItem> SeedLineItemAsync()
    {
        var (lineItem, _) = await SeedLineItemWithStoreAsync();
        return lineItem;
    }

    private async Task<(ChargeLineItem LineItem, int StoreAccountId)> SeedLineItemWithStoreAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Şema Test Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Şema Test Birim Türü {suffix}",
            Code = $"SCHEMA_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { TenantNo = $"SCHEMA-{suffix}", Name = $"Şema Test Kiracı {suffix}" };
        var chargeType = new ChargeType
        {
            Name = $"Şema Test Borç Tipi {suffix}",
            Code = $"SCHEMACT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.UserManual
        };
        var store = new Store { Name = $"Şema Test Mağaza {suffix}", Code = $"SCHEMASTORE_{suffix}", IsActive = true };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"MERCHANT-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });
        _context.AddRange(property, unitType, tenant, chargeType, store);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Şema Test Ofis {suffix}",
            Area = 40m
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var charge = new Charge
        {
            TenantId = tenant.Id,
            UnitId = unit.Id,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = new DateTime(2026, 2, 5),
            ExpectedAmount = 1000m,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            Status = ChargeStatus.Pending
        };
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        var lineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Şema testi kalemi",
            Amount = 1000m,
            TotalAmount = 1000m
        };
        _context.ChargeLineItems.Add(lineItem);
        await _context.SaveChangesAsync();

        return (lineItem, store.Accounts.Single().Id);
    }

    private sealed class TenantCurrentUserContext : ICurrentUserContext
    {
        public int? TenantId { get; set; }
        public string? UserId => "schema-test-tenant-user";
        public UserType? UserType => KiraTakip.Models.Enums.UserType.Tenant;
        public bool IsKiraciUser => true;
    }
}
