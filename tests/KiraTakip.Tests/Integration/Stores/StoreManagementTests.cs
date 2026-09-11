using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos.Store;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Payments;
using KiraTakip.Services.Payments;
using KiraTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public sealed class StoreManagementTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly StoreAccountCredentialProtector _protector;
    private readonly MutableTimeProvider _timeProvider;

    public StoreManagementTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
        _protector = new StoreAccountCredentialProtector(new EphemeralDataProtectionProvider());
        _timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 8, 27, 8, 0, 0, TimeSpan.Zero));
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task Create_ShouldNormalizeNameAndGenerateUniqueCode()
    {
        var service = CreateService();
        var name = $"Mağaza {Guid.NewGuid():N}";

        var id = await service.CreateAsync(new CreateStoreInput($"  {name}  ", "  Açıklama  ", true));
        var store = await _context.Stores.SingleAsync(item => item.Id == id);

        Assert.Equal(name, store.Name);
        Assert.Equal(KiraTakip.Helpers.CodeSlugger.ToCode(name), store.Code);
        Assert.Equal("Açıklama", store.Description);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.CreateAsync(new CreateStoreInput(name, null, true)));
        Assert.Equal("STORE_CODE_EXISTS", exception.Code);
    }

    [Fact]
    public async Task Database_ShouldRejectTwoActiveAccountsForSameStore()
    {
        var store = await AddStoreAsync("STORE-UNIQUE");
        _context.StoreAccounts.Add(CreateAccount(store.Id, "merchant-1", isActive: true));
        await _context.SaveChangesAsync();
        _context.StoreAccounts.Add(CreateAccount(store.Id, "merchant-2", isActive: true));

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task ReplaceAccount_ShouldProtectSecretAndVersionExistingAccount()
    {
        var service = CreateService();
        var storeId = await service.CreateAsync(new CreateStoreInput("Sürümlü Mağaza", null, true));

        await service.ReplaceAccountAsync(AccountInput(storeId, "merchant-1", "secret-one"));
        _timeProvider.Advance(TimeSpan.FromHours(2));
        await service.ReplaceAccountAsync(AccountInput(storeId, "merchant-2", "secret-two"));

        var accounts = await _context.StoreAccounts
            .Where(account => account.StoreId == storeId)
            .OrderBy(account => account.ValidFrom)
            .ToListAsync();

        Assert.Equal(2, accounts.Count);
        Assert.False(accounts[0].IsActive);
        Assert.Equal(accounts[1].ValidFrom, accounts[0].ValidUntil);
        Assert.True(accounts[1].IsActive);
        Assert.Null(accounts[1].ValidUntil);
        Assert.NotEqual("secret-two", accounts[1].ProtectedMerchantPassword);
        Assert.DoesNotContain("secret-two", accounts[1].ProtectedMerchantPassword, StringComparison.Ordinal);
        Assert.Equal("secret-two", _protector.Unprotect(accounts[1].ProtectedMerchantPassword));
    }

    [Fact]
    public async Task DeactivateAccount_ShouldKeepHistoricalRecord()
    {
        var service = CreateService();
        var storeId = await service.CreateAsync(new CreateStoreInput("Kapanan Hesap", null, true));
        await service.ReplaceAccountAsync(AccountInput(storeId, "merchant-close", "secret"));
        var account = await _context.StoreAccounts.SingleAsync(item => item.StoreId == storeId);

        _timeProvider.Advance(TimeSpan.FromMinutes(30));
        await service.DeactivateAccountAsync(storeId, account.Id);

        var stored = await _context.StoreAccounts.SingleAsync(item => item.Id == account.Id);
        Assert.False(stored.IsActive);
        Assert.False(stored.IsDeleted);
        Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime, stored.ValidUntil);
    }

    [Fact]
    public async Task ToggleStore_ShouldPreserveAccountHistory()
    {
        var service = CreateService();
        var storeId = await service.CreateAsync(new CreateStoreInput("Pasif Mağaza", null, true));
        await service.ReplaceAccountAsync(AccountInput(storeId, "merchant-passive", "secret"));

        var isActive = await service.ToggleStatusAsync(storeId);
        var detail = await service.GetDetailAsync(storeId);

        Assert.False(isActive);
        Assert.NotNull(detail);
        Assert.False(detail!.IsActive);
        Assert.Single(detail.Accounts);
        Assert.True(detail.Accounts[0].IsActive);
    }

    [Fact]
    public async Task Database_ShouldRejectInvalidValidityRange()
    {
        var store = await AddStoreAsync("STORE-DATE-CHECK");
        var account = CreateAccount(store.Id, "merchant-date", isActive: false);
        account.ValidFrom = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);
        account.ValidUntil = new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc);
        _context.StoreAccounts.Add(account);

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    private StoreService CreateService()
    {
        var storeRepository = new StoreRepository(_context);
        var accountRepository = new StoreAccountRepository(_context);
        var rules = new StoreBusinessRules(storeRepository, accountRepository);
        return new StoreService(
            storeRepository,
            accountRepository,
            rules,
            _protector,
            new UnitOfWork(_context),
            _timeProvider,
            new SqlServerUniqueConstraintViolationDetector());
    }

    private async Task<Store> AddStoreAsync(string code)
    {
        var store = new Store { Code = code, Name = code, IsActive = true };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        return store;
    }

    private StoreAccount CreateAccount(int storeId, string merchantId, bool isActive)
        => new()
        {
            StoreId = storeId,
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = merchantId,
            MerchantUser = "merchant-user",
            ProtectedMerchantPassword = _protector.Protect("secret"),
            ValidFrom = _timeProvider.GetUtcNow().UtcDateTime,
            IsActive = isActive
        };

    private static CreateStoreAccountVersionInput AccountInput(int storeId, string merchantId, string password)
        => new(
            storeId,
            PaymentProviderCodes.Paratika,
            CurrencyCodes.Try,
            merchantId,
            "merchant-user",
            password);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
