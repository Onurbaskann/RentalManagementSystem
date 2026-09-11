using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

/// <summary>
/// `docs/migration-scripts/phase-20-inner-phase-3-preflight.sql` içindeki üç salt-okunur
/// raporun (R1/R2/R3) doğru satırları yakaladığını doğrular. Bu script migration'dan ÖNCE
/// çalıştırılmak üzere yazıldı; burada migration sonrası şema üzerinde SQL mantığının
/// kendisini regresyona karşı korumak için çalıştırılıyor.
/// </summary>
[Collection("Database collection")]
public class ChargeLineItemPaymentMigrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ChargeLineItemPaymentMigrationTests(DatabaseFixture fixture)
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

    private async Task<(Charge Charge, ChargeType ChargeType, string UserId, int StoreAccountId)> SeedBaseAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Preflight {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Preflight Birim {suffix}",
            Code = $"PRE_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { TenantNo = $"PRE-{suffix}", Name = $"Preflight Kiracı {suffix}" };
        var chargeType = new ChargeType
        {
            Name = $"Preflight Borç Tipi {suffix}",
            Code = $"PRECT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.UserManual
        };
        var store = new Store { Name = $"Preflight Mağaza {suffix}", Code = $"PRESTORE_{suffix}", IsActive = true };
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
            Name = $"Preflight Ofis {suffix}",
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

        var userId = await _context.Users.Select(user => user.Id).FirstAsync();
        return (charge, chargeType, userId, store.Accounts.Single().Id);
    }

    [Fact]
    public async Task PreflightQuery_ShouldReportPaymentsOnMultiLineCharges()
    {
        var seed = await SeedBaseAsync();
        var firstLineItem = new ChargeLineItem
        {
            ChargeId = seed.Charge.Id,
            ChargeTypeId = seed.ChargeType.Id,
            Description = "Kalem 1",
            Amount = 600m,
            TotalAmount = 600m
        };
        var secondLineItem = new ChargeLineItem
        {
            ChargeId = seed.Charge.Id,
            ChargeTypeId = seed.ChargeType.Id,
            Description = "Kalem 2",
            Amount = 400m,
            TotalAmount = 400m
        };
        _context.ChargeLineItems.AddRange(firstLineItem, secondLineItem);
        await _context.SaveChangesAsync();

        var payment = new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = firstLineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            CreatedByUserId = seed.UserId,
            PaymentDate = DateTime.Today,
            Amount = 300m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        };
        _context.PaymentAllocations.Add(payment);
        await _context.SaveChangesAsync();

        var reportedIds = await _context.Database.SqlQueryRaw<int>(@"
            SELECT o.Id
            FROM TahakkukOdemeleri o
            WHERE o.IsDeleted = 0
              AND (SELECT COUNT(*) FROM TahakkukKalemleri k2
                   WHERE k2.TahakkukId = o.TahakkukId AND k2.IsDeleted = 0) > 1")
            .ToListAsync();

        Assert.Contains(payment.Id, reportedIds);
    }

    [Fact]
    public async Task PreflightQuery_ShouldReportOrphanPayments()
    {
        var seed = await SeedBaseAsync();
        var lineItem = new ChargeLineItem
        {
            ChargeId = seed.Charge.Id,
            ChargeTypeId = seed.ChargeType.Id,
            Description = "Silinecek kalem",
            Amount = 1000m,
            TotalAmount = 1000m
        };
        _context.ChargeLineItems.Add(lineItem);
        await _context.SaveChangesAsync();

        var payment = new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = lineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            CreatedByUserId = seed.UserId,
            PaymentDate = DateTime.Today,
            Amount = 400m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        };
        _context.PaymentAllocations.Add(payment);
        await _context.SaveChangesAsync();

        // Kalem sonradan silinmiş (soft-delete) ama ödeme kaydı kalmış senaryosu.
        lineItem.IsDeleted = true;
        await _context.SaveChangesAsync();

        var reportedIds = await _context.Database.SqlQueryRaw<int>(@"
            SELECT o.Id
            FROM TahakkukOdemeleri o
            WHERE o.IsDeleted = 0
              AND NOT EXISTS (
                  SELECT 1 FROM TahakkukKalemleri k2
                  WHERE k2.TahakkukId = o.TahakkukId AND k2.IsDeleted = 0)")
            .ToListAsync();

        Assert.Contains(payment.Id, reportedIds);
    }

    [Fact]
    public async Task PreflightQuery_ShouldReportOverpaidLineItemsAfterBackfill()
    {
        var seed = await SeedBaseAsync();
        var lineItem = new ChargeLineItem
        {
            ChargeId = seed.Charge.Id,
            ChargeTypeId = seed.ChargeType.Id,
            Description = "Fazla ödenmiş kalem",
            Amount = 500m,
            TotalAmount = 500m
        };
        _context.ChargeLineItems.Add(lineItem);
        await _context.SaveChangesAsync();

        // Servis katmanından geçmeden doğrudan iki onaylı ödeme eklenir; toplam (700) kalemin
        // ToplamTutar'ını (500) aşıyor — R3'ün yakalaması gereken tutarsızlık budur.
        _context.PaymentAllocations.AddRange(
            new PaymentAllocation
            {
                ChargeId = seed.Charge.Id,
                ChargeLineItemId = lineItem.Id,
                StoreAccountId = seed.StoreAccountId,
                CreatedByUserId = seed.UserId,
                PaymentDate = DateTime.Today,
                Amount = 400m,
                PaymentChannel = PaymentChannel.Eft,
                Status = PaymentStatus.Approved
            },
            new PaymentAllocation
            {
                ChargeId = seed.Charge.Id,
                ChargeLineItemId = lineItem.Id,
                StoreAccountId = seed.StoreAccountId,
                CreatedByUserId = seed.UserId,
                PaymentDate = DateTime.Today,
                Amount = 300m,
                PaymentChannel = PaymentChannel.Eft,
                Status = PaymentStatus.Approved
            });
        await _context.SaveChangesAsync();

        var reportedIds = await _context.Database.SqlQueryRaw<int>(@"
            SELECT k.Id
            FROM TahakkukKalemleri k
            CROSS APPLY (
                SELECT SUM(o.Tutar) AS OnayliToplam
                FROM TahakkukOdemeleri o
                WHERE o.IsDeleted = 0
                  AND o.Durum = 2
                  AND o.TahakkukId = k.TahakkukId
                  AND (SELECT COUNT(*) FROM TahakkukKalemleri k2
                       WHERE k2.TahakkukId = k.TahakkukId AND k2.IsDeleted = 0) = 1
            ) onayli
            WHERE k.IsDeleted = 0
              AND onayli.OnayliToplam > k.ToplamTutar")
            .ToListAsync();

        Assert.Contains(lineItem.Id, reportedIds);
    }
}
