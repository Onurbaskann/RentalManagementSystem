using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class BankTransactionSchemaTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public BankTransactionSchemaTests(DatabaseFixture fixture)
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
    public async Task Migration_ShouldCreateNotNullStoreAccountColumn()
    {
        var columns = await _context.Database.SqlQueryRaw<string>(@"
            SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'BankaHareketleri' AND COLUMN_NAME = 'MagazaHesapBilgisiId'")
            .ToListAsync();
        Assert.Equal("NO", Assert.Single(columns));
    }

    [Fact]
    public async Task Migration_ShouldCreateStoreAccountIndex()
    {
        var counts = await _context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_BankaHareketleri_MagazaHesapBilgisiId'")
            .ToListAsync();
        Assert.Equal(1, Assert.Single(counts));
    }

    [Fact]
    public async Task Migration_ShouldCreateRestrictForeignKey()
    {
        var counts = await _context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) FROM sys.foreign_keys WHERE name = 'FK_BankaHareketleri_MagazaHesapBilgileri_MagazaHesapBilgisiId' AND delete_referential_action = 0")
            .ToListAsync();
        Assert.Equal(1, Assert.Single(counts));
    }

    [Fact]
    public async Task DeletingStoreAccountUsedByBankTransaction_ShouldBeRestricted()
    {
        var storeAccountId = await SeedStoreAccountAsync();
        _context.BankTransactions.Add(new BankTransaction
        {
            BankCode = "AKBANK",
            TransactionAmount = 100m,
            TransactionDate = DateTime.Today,
            Description = "Şema testi hareketi",
            StoreAccountId = storeAccountId,
            MatchStatus = BankMatchStatus.Unmatched
        });
        await _context.SaveChangesAsync();

        // EF'in ilişki-fixup davranışı devreye girmesin diye DB seviyesindeki gerçek FK
        // Restrict kısıtı ham SQL ile test edilir (bkz. ChargeLineItemPaymentSchemaTests).
        await Assert.ThrowsAsync<SqlException>(() => _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM MagazaHesapBilgileri WHERE Id = {storeAccountId}"));
    }

    private async Task<int> SeedStoreAccountAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var store = new Store
        {
            Name = $"Banka Şema Mağazası {suffix}",
            Code = $"BANKSCHEMA_{suffix}",
            IsActive = true
        };
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
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        return store.Accounts.Single().Id;
    }
}
