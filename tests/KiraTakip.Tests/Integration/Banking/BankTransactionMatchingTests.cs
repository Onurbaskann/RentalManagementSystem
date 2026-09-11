using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Banking;
using KiraTakip.Repositories.Payments;
using KiraTakip.Services.Banking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text;
using KiraTakip.Models.Dtos.BankTransaction;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class BankTransactionMatchingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public BankTransactionMatchingTests(DatabaseFixture fixture)
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

    private BankTransactionService CreateService()
    {
        return new(new BankTransactionRepository(_context),
                   new PaymentAllocationRepository(_context),
                   new PaymentMatchRepository(_context),
                   [new AkbankCsvParser()],
                   new TestOperationalPolicyProvider(),
                   new StoreRepository(_context),
                   new StoreAccountRepository(_context),
                   new UnitOfWork(_context));
    }

    private async Task<(Store Store, int StoreAccountId)> CreateStoreAsync(string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var store = new Store
        {
            Name = $"{label} {suffix}",
            Code = $"BTM_{label}_{suffix}",
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
        return (store, store.Accounts.Single().Id);
    }

    private async Task<(Charge Charge, int StoreAccountId)> SeedChargeAsync(int storeAccountId)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"BTM Plaza {suffix}", City = "İstanbul", District = "Kadıköy" };
        var unitType = new UnitType { Name = $"BTM Ofis {suffix}", Code = $"BTM_{suffix}", Usage = UnitTypeUsage.Rentable };
        var tenant = new Tenant { Name = $"BTM Kiracı {suffix}", TenantNo = $"BTM-{suffix}" };
        var chargeType = new ChargeType
        {
            Name = $"BTM Borç Tipi {suffix}",
            Code = $"BTMCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.UserManual
        };
        _context.Properties.Add(property);
        _context.UnitTypes.Add(unitType);
        _context.Tenants.Add(tenant);
        _context.ChargeTypes.Add(chargeType);
        await _context.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Ofis {suffix}", Area = 50 };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var lease = new Lease
        {
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = LeaseStatus.Active
        };
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();

        var charge = new Charge
        {
            TenantId = tenant.Id,
            UnitId = unit.Id,
            LeaseId = lease.Id,
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

        var lineItem = await PaymentLineItemTestHelper.AddLineItemAsync(_context, charge);

        var payment = new PaymentAllocation
        {
            ChargeId = charge.Id,
            ChargeLineItemId = lineItem.Id,
            StoreAccountId = storeAccountId,
            LeaseId = lease.Id,
            CreatedByUserId = (await SeedUserAsync(suffix)).Id,
            PaymentDate = new DateTime(2026, 2, 1),
            Amount = 500m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        };
        _context.PaymentAllocations.Add(payment);
        await _context.SaveChangesAsync();

        return (charge, storeAccountId);
    }

    private async Task<ApplicationUser> SeedUserAsync(string suffix)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"btm-{suffix}@test.local",
            NormalizedUserName = $"BTM-{suffix}@TEST.LOCAL",
            Email = $"btm-{suffix}@test.local",
            NormalizedEmail = $"BTM-{suffix}@TEST.LOCAL",
            EmailConfirmed = true,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<BankTransaction> AddTransactionAsync(int storeAccountId, decimal amount, DateTime date)
    {
        var transaction = new BankTransaction
        {
            BankCode = "AKBANK",
            TransactionAmount = amount,
            TransactionDate = date,
            Description = "Test hareketi",
            StoreAccountId = storeAccountId,
            MatchStatus = BankMatchStatus.Unmatched
        };
        _context.BankTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    [Fact]
    public async Task Import_WithoutActiveStoreAccount_IsRejected()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var storeWithoutAccount = new Store { Name = $"Hesapsız {suffix}", Code = $"NOACC_{suffix}", IsActive = true };
        _context.Stores.Add(storeWithoutAccount);
        await _context.SaveChangesAsync();

        var csv = "Tarih;Açıklama;Borç;Alacak;Bakiye;Karşı Hesap No;Karşı Hesap Adı\r\n"
            + "01.02.2026;Kira Ödemesi;0;500,00;1000,00;TR00;Test Gönderen\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService().ImportAsync(new ImportBankTransactionsInput(stream, "AKBANK", storeWithoutAccount.Id)));

        Assert.Equal("BANK_IMPORT_STORE_ACCOUNT_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task Import_EmptyCsv_IsRejected()
    {
        var (store, _) = await CreateStoreAsync("Bos");
        var csv = "Tarih;Açıklama;Borç;Alacak;Bakiye;Karşı Hesap No;Karşı Hesap Adı\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateService().ImportAsync(new ImportBankTransactionsInput(stream, "AKBANK", store.Id)));

        Assert.Equal("BANK_IMPORT_NO_TRANSACTIONS", exception.Code);
    }

    [Fact]
    public async Task Import_AssignsSelectedStoreAccountToEveryTransaction()
    {
        var (store, storeAccountId) = await CreateStoreAsync("Dolu");
        var csv = "Tarih;Açıklama;Borç;Alacak;Bakiye;Karşı Hesap No;Karşı Hesap Adı\r\n"
            + "01.02.2026;Kira Ödemesi 1;0;500,00;1000,00;TR00;Gönderen 1\r\n"
            + "02.02.2026;Kira Ödemesi 2;0;300,00;1300,00;TR01;Gönderen 2\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        await CreateService().ImportAsync(new ImportBankTransactionsInput(stream, "AKBANK", store.Id));

        var imported = await _context.BankTransactions
            .Where(transaction => transaction.Description.StartsWith("Kira Ödemesi"))
            .ToListAsync();
        Assert.Equal(2, imported.Count);
        Assert.All(imported, transaction => Assert.Equal(storeAccountId, transaction.StoreAccountId));
    }

    [Fact]
    public async Task GetPaymentCandidates_IncludesPaymentsOfSameStoreAccount()
    {
        var (_, storeAccountId) = await CreateStoreAsync("Ayni");
        var (_, _) = await SeedChargeAsync(storeAccountId);
        var transaction = await AddTransactionAsync(storeAccountId, 500m, new DateTime(2026, 2, 1));

        var candidates = await CreateService().GetPaymentCandidatesAsync(
            new GetBankTransactionPaymentCandidatesInput(transaction.Id));

        Assert.Single(candidates);
    }

    [Fact]
    public async Task GetPaymentCandidates_ExcludesPaymentsOfOtherStoreAccount()
    {
        var (_, storeAccountId) = await CreateStoreAsync("Odeme");
        var (_, otherStoreAccountId) = await CreateStoreAsync("Hareket");
        await SeedChargeAsync(storeAccountId);
        var transaction = await AddTransactionAsync(otherStoreAccountId, 500m, new DateTime(2026, 2, 1));

        var candidates = await CreateService().GetPaymentCandidatesAsync(
            new GetBankTransactionPaymentCandidatesInput(transaction.Id));

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task GetTransactionCandidates_IncludesTransactionsOfSameStoreAccount()
    {
        var (_, storeAccountId) = await CreateStoreAsync("AyniTx");
        var (_, _) = await SeedChargeAsync(storeAccountId);
        await AddTransactionAsync(storeAccountId, 500m, new DateTime(2026, 2, 1));

        var payment = await _context.PaymentAllocations.SingleAsync(p => p.StoreAccountId == storeAccountId);
        var candidates = await CreateService().GetTransactionCandidatesAsync(
            new GetBankTransactionCandidatesInput(payment.Id));

        Assert.Single(candidates);
    }

    [Fact]
    public async Task GetTransactionCandidates_ExcludesTransactionsOfOtherStoreAccount()
    {
        var (_, storeAccountId) = await CreateStoreAsync("OdemeTx");
        var (_, otherStoreAccountId) = await CreateStoreAsync("HareketTx");
        await SeedChargeAsync(storeAccountId);
        await AddTransactionAsync(otherStoreAccountId, 500m, new DateTime(2026, 2, 1));

        var payment = await _context.PaymentAllocations.SingleAsync(p => p.StoreAccountId == storeAccountId);
        var candidates = await CreateService().GetTransactionCandidatesAsync(
            new GetBankTransactionCandidatesInput(payment.Id));

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task Match_DifferentStoreAccount_IsRejected()
    {
        var (_, storeAccountId) = await CreateStoreAsync("Uyusmaz1");
        var (_, otherStoreAccountId) = await CreateStoreAsync("Uyusmaz2");
        await SeedChargeAsync(storeAccountId);
        var transaction = await AddTransactionAsync(otherStoreAccountId, 500m, new DateTime(2026, 2, 1));
        var payment = await _context.PaymentAllocations.SingleAsync(p => p.StoreAccountId == storeAccountId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService().MatchAsync(new MatchBankTransactionInput(payment.Id, transaction.Id)));

        Assert.Equal("BANK_MATCH_STORE_ACCOUNT_MISMATCH", exception.Code);
    }

    [Fact]
    public async Task Match_SameStoreAccount_CreatesMatchAndMarksTransactionManuallyMatched()
    {
        var (_, storeAccountId) = await CreateStoreAsync("Eslesir");
        await SeedChargeAsync(storeAccountId);
        var transaction = await AddTransactionAsync(storeAccountId, 500m, new DateTime(2026, 2, 1));
        var payment = await _context.PaymentAllocations.SingleAsync(p => p.StoreAccountId == storeAccountId);

        await CreateService().MatchAsync(new MatchBankTransactionInput(payment.Id, transaction.Id));

        var updatedTransaction = await _context.BankTransactions.FindAsync(transaction.Id);
        Assert.Equal(BankMatchStatus.ManuallyMatched, updatedTransaction!.MatchStatus);
        Assert.True(await _context.PaymentMatches.AnyAsync(match =>
            match.PaymentAllocationId == payment.Id && match.BankTransactionId == transaction.Id));
    }

    [Fact]
    public async Task Match_AlreadyMatchedBankTransaction_IsRejected()
    {
        var (_, storeAccountId) = await CreateStoreAsync("Tekrar");
        await SeedChargeAsync(storeAccountId);
        var transaction = await AddTransactionAsync(storeAccountId, 500m, new DateTime(2026, 2, 1));
        var payment = await _context.PaymentAllocations.SingleAsync(p => p.StoreAccountId == storeAccountId);
        var service = CreateService();
        await service.MatchAsync(new MatchBankTransactionInput(payment.Id, transaction.Id));

        var (secondCharge, _) = await SeedChargeAsync(storeAccountId);
        var secondPayment = await _context.PaymentAllocations
            .Where(p => p.ChargeId == secondCharge.Id)
            .SingleAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.MatchAsync(new MatchBankTransactionInput(secondPayment.Id, transaction.Id)));

        Assert.Equal("Banka hareketi başka bir ödemeyle zaten eşleştirilmiş.", exception.Message);
    }

    [Fact]
    public async Task Unmatch_ResetsBankTransactionToUnmatched()
    {
        var (_, storeAccountId) = await CreateStoreAsync("Geri");
        await SeedChargeAsync(storeAccountId);
        var transaction = await AddTransactionAsync(storeAccountId, 500m, new DateTime(2026, 2, 1));
        var payment = await _context.PaymentAllocations.SingleAsync(p => p.StoreAccountId == storeAccountId);
        var service = CreateService();
        await service.MatchAsync(new MatchBankTransactionInput(payment.Id, transaction.Id));

        var match = await _context.PaymentMatches.SingleAsync(item => item.BankTransactionId == transaction.Id);
        await service.UnmatchAsync(new UnmatchBankTransactionInput(match.Id));

        var updatedTransaction = await _context.BankTransactions.FindAsync(transaction.Id);
        Assert.Equal(BankMatchStatus.Unmatched, updatedTransaction!.MatchStatus);
        Assert.False(await _context.PaymentMatches.AnyAsync(item => item.Id == match.Id));
    }
}
