using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.OnlinePayment;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Payments;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Tests;

/// <summary>
/// Production koduna eklenmeyen, yalnız test projesine özel sağlayıcı sahte implementasyonu
/// (ChargeReminderServiceTests'teki FakeMailService deseniyle aynı).
/// </summary>
internal sealed class FakeOnlinePaymentProvider(bool succeeds = true, PaymentInquiryResult? queryResult = null)
    : IOnlinePaymentProvider
{
    public string ProviderCode => PaymentProviderCodes.Paratika;

    public Task<CreatePaymentSessionResult> CreateSessionAsync(
        CreatePaymentSessionRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken)
        => Task.FromResult(succeeds
            ? new CreatePaymentSessionResult(
                IsSuccessful: true,
                ProviderTransactionId: "PGTRAN-1",
                SessionToken: "FAKE-SESSION-TOKEN",
                SessionExpiresAt: DateTime.UtcNow.AddMinutes(15),
                ResponseCode: "00",
                TransactionStatus: "IP",
                ErrorCode: null,
                SafeMessage: null)
            : new CreatePaymentSessionResult(
                IsSuccessful: false,
                ProviderTransactionId: null,
                SessionToken: null,
                SessionExpiresAt: null,
                ResponseCode: "98",
                TransactionStatus: null,
                ErrorCode: "GENERIC_ERROR",
                SafeMessage: "Sağlayıcı işlemi reddetti."));

    public Task<PaymentInquiryResult> QueryAsync(
        PaymentInquiryRequest request, PaymentProviderAccount account, CancellationToken cancellationToken)
        => queryResult is not null
            ? Task.FromResult(queryResult)
            : throw new NotSupportedException("Bu test senaryosunda QueryAsync sonucu tanımlanmadı.");

    public Task<PaymentCallbackResult> ValidateCallbackAsync(
        PaymentCallbackRequest request, PaymentProviderAccount account, CancellationToken cancellationToken)
        => throw new NotSupportedException("İç Faz 7 kapsamında controller seviyesinde kullanılmıyor.");

    public string BuildHostedPaymentPageUrl(string sessionToken)
        => $"https://fake-hosted-page.local/payment/{sessionToken}";
}

[Collection("Database collection")]
public class OnlinePaymentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly StoreAccountCredentialProtector _protector = new(new EphemeralDataProtectionProvider());

    public OnlinePaymentServiceTests(DatabaseFixture fixture)
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
    public async Task InitiateAsync_ShouldCreatePendingTransactionWithSessionEvents_WhenProviderSucceeds()
    {
        var scenario = await SeedScenarioAsync();

        var result = await CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
            new InitiateOnlinePaymentInput(
                scenario.TenantId,
                scenario.ChargeId,
                scenario.LineItemId,
                600m,
                scenario.ActorId,
                new PaymentAccessScopeInput()));

        Assert.Equal(OnlinePaymentTransactionStatus.Pending, result.Status);
        Assert.Equal(PaymentProviderCodes.Paratika, result.ProviderCode);
        Assert.Equal("FAKE-SESSION-TOKEN", result.SessionToken);
        Assert.Equal("https://fake-hosted-page.local/payment/FAKE-SESSION-TOKEN", result.RedirectUrl);

        var transaction = await _context.OnlinePaymentTransactions.SingleAsync(t => t.Id == result.OnlinePaymentTransactionId);
        Assert.Equal(scenario.LineItemId, transaction.ChargeLineItemId);
        Assert.Equal(600m, transaction.Amount);
        Assert.Equal(result.MerchantPaymentId, transaction.MerchantPaymentId);

        var events = await _context.OnlinePaymentEvents
            .Where(e => e.OnlinePaymentTransactionId == result.OnlinePaymentTransactionId)
            .ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.EventType == OnlinePaymentEventType.SessionRequested);
        Assert.Contains(events, e => e.EventType == OnlinePaymentEventType.SessionResult);
    }

    [Fact]
    public async Task InitiateAsync_ShouldRejectAmountAboveAvailable_AndWriteNoRows()
    {
        var scenario = await SeedScenarioAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
                new InitiateOnlinePaymentInput(
                    scenario.TenantId,
                    scenario.ChargeId,
                    scenario.LineItemId,
                    1500m,
                    scenario.ActorId,
                    new PaymentAccessScopeInput())));

        Assert.Equal("ONLINE_PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE", exception.Code);
        Assert.False(await _context.OnlinePaymentTransactions.AnyAsync(t => t.ChargeLineItemId == scenario.LineItemId));
    }

    [Fact]
    public async Task InitiateAsync_ShouldRejectSecondAttempt_WhenActivePendingAttemptExists()
    {
        var scenario = await SeedScenarioAsync();
        _context.OnlinePaymentTransactions.Add(new OnlinePaymentTransaction
        {
            ChargeLineItemId = scenario.LineItemId,
            StoreAccountId = scenario.StoreAccountId,
            InitiatedByUserId = scenario.ActorId,
            ProviderCode = PaymentProviderCodes.Paratika,
            MerchantPaymentId = Guid.NewGuid().ToString("N"),
            Amount = 200m,
            Currency = CurrencyCodes.Try,
            Status = OnlinePaymentTransactionStatus.Pending
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
                new InitiateOnlinePaymentInput(
                    scenario.TenantId,
                    scenario.ChargeId,
                    scenario.LineItemId,
                    300m,
                    scenario.ActorId,
                    new PaymentAccessScopeInput())));

        Assert.Equal("ONLINE_PAYMENT_ACTIVE_ATTEMPT_EXISTS", exception.Code);
    }

    [Fact]
    public async Task InitiateAsync_ShouldThrowNotFound_WhenNoProviderMatchesStoreAccountProviderCode()
    {
        var scenario = await SeedScenarioAsync(providerCode: "UnknownProvider");

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
                new InitiateOnlinePaymentInput(
                    scenario.TenantId,
                    scenario.ChargeId,
                    scenario.LineItemId,
                    300m,
                    scenario.ActorId,
                    new PaymentAccessScopeInput())));

        Assert.Equal("ONLINE_PAYMENT_PROVIDER_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task MerchantPaymentId_ShouldBeUniqueAtDatabaseLevel()
    {
        var scenario = await SeedScenarioAsync();
        var duplicateMerchantPaymentId = Guid.NewGuid().ToString("N");
        _context.OnlinePaymentTransactions.Add(new OnlinePaymentTransaction
        {
            ChargeLineItemId = scenario.LineItemId,
            StoreAccountId = scenario.StoreAccountId,
            InitiatedByUserId = scenario.ActorId,
            ProviderCode = PaymentProviderCodes.Paratika,
            MerchantPaymentId = duplicateMerchantPaymentId,
            Amount = 100m,
            Currency = CurrencyCodes.Try,
            Status = OnlinePaymentTransactionStatus.Failed
        });
        await _context.SaveChangesAsync();

        _context.OnlinePaymentTransactions.Add(new OnlinePaymentTransaction
        {
            ChargeLineItemId = scenario.LineItemId,
            StoreAccountId = scenario.StoreAccountId,
            InitiatedByUserId = scenario.ActorId,
            ProviderCode = PaymentProviderCodes.Paratika,
            MerchantPaymentId = duplicateMerchantPaymentId,
            Amount = 100m,
            Currency = CurrencyCodes.Try,
            Status = OnlinePaymentTransactionStatus.Failed
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task CompleteAsync_ShouldCreateApprovedPaymentAllocation_WhenQueryReturnsApproved()
    {
        var scenario = await SeedScenarioAsync();
        var initiateResult = await CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
            new InitiateOnlinePaymentInput(
                scenario.TenantId,
                scenario.ChargeId,
                scenario.LineItemId,
                600m,
                scenario.ActorId,
                new PaymentAccessScopeInput()));

        var queryResult = new PaymentInquiryResult(
            IsSuccessful: true,
            ProviderTransactionId: "PGTRAN-APPROVED",
            ResponseCode: "00",
            TransactionStatus: "AP",
            ErrorCode: null,
            SafeMessage: "Approved");

        var result = await CreateService([new FakeOnlinePaymentProvider(queryResult: queryResult)]).CompleteAsync(
            new CompleteOnlinePaymentInput(initiateResult.MerchantPaymentId, scenario.TenantId));

        Assert.Equal(OnlinePaymentTransactionStatus.Approved, result.Status);
        Assert.NotNull(result.PaymentAllocationId);
        Assert.Equal(scenario.ChargeId, result.ChargeId);

        var payment = await _context.PaymentAllocations.SingleAsync(p => p.Id == result.PaymentAllocationId);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.Equal(PaymentSourceType.VirtualPos, payment.PaymentSourceType);
        Assert.Equal(PaymentChannel.Card, payment.PaymentChannel);
        Assert.Equal("PGTRAN-APPROVED", payment.PosReferenceNo);
        Assert.Equal(600m, payment.Amount);

        var chargeLineItem = await _context.ChargeLineItems.SingleAsync(li => li.Id == scenario.LineItemId);
        Assert.Equal(600m, chargeLineItem.PaidAmount);

        var events = await _context.OnlinePaymentEvents
            .Where(e => e.OnlinePaymentTransactionId == initiateResult.OnlinePaymentTransactionId)
            .ToListAsync();
        Assert.Contains(events, e => e.EventType == OnlinePaymentEventType.InquiryPerformed);
        Assert.Contains(events, e => e.EventType == OnlinePaymentEventType.Succeeded);
    }

    [Fact]
    public async Task CompleteAsync_ShouldNotCreatePayment_WhenQueryReturnsFailed()
    {
        var scenario = await SeedScenarioAsync();
        var initiateResult = await CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
            new InitiateOnlinePaymentInput(
                scenario.TenantId,
                scenario.ChargeId,
                scenario.LineItemId,
                600m,
                scenario.ActorId,
                new PaymentAccessScopeInput()));

        var queryResult = new PaymentInquiryResult(
            IsSuccessful: true,
            ProviderTransactionId: "PGTRAN-FAILED",
            ResponseCode: "00",
            TransactionStatus: "FA",
            ErrorCode: null,
            SafeMessage: "Declined");

        var result = await CreateService([new FakeOnlinePaymentProvider(queryResult: queryResult)]).CompleteAsync(
            new CompleteOnlinePaymentInput(initiateResult.MerchantPaymentId, scenario.TenantId));

        Assert.Equal(OnlinePaymentTransactionStatus.Failed, result.Status);
        Assert.Null(result.PaymentAllocationId);
        Assert.False(await _context.PaymentAllocations.AnyAsync(p => p.ChargeLineItemId == scenario.LineItemId));
    }

    [Fact]
    public async Task CompleteAsync_ShouldBeIdempotent_WhenCalledAgainAfterApproval()
    {
        var scenario = await SeedScenarioAsync();
        var initiateResult = await CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
            new InitiateOnlinePaymentInput(
                scenario.TenantId,
                scenario.ChargeId,
                scenario.LineItemId,
                600m,
                scenario.ActorId,
                new PaymentAccessScopeInput()));

        var approvedResult = new PaymentInquiryResult(
            IsSuccessful: true,
            ProviderTransactionId: "PGTRAN-APPROVED",
            ResponseCode: "00",
            TransactionStatus: "AP",
            ErrorCode: null,
            SafeMessage: "Approved");
        await CreateService([new FakeOnlinePaymentProvider(queryResult: approvedResult)]).CompleteAsync(
            new CompleteOnlinePaymentInput(initiateResult.MerchantPaymentId, scenario.TenantId));

        var secondResult = await CreateService([new FakeOnlinePaymentProvider(queryResult: approvedResult)]).CompleteAsync(
            new CompleteOnlinePaymentInput(initiateResult.MerchantPaymentId, scenario.TenantId));

        Assert.Equal(OnlinePaymentTransactionStatus.Approved, secondResult.Status);
        var paymentCount = await _context.PaymentAllocations.CountAsync(p => p.ChargeLineItemId == scenario.LineItemId);
        Assert.Equal(1, paymentCount);
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrowForbidden_WhenTenantDoesNotOwnTransaction()
    {
        var scenario = await SeedScenarioAsync();
        var initiateResult = await CreateService([new FakeOnlinePaymentProvider()]).InitiateAsync(
            new InitiateOnlinePaymentInput(
                scenario.TenantId,
                scenario.ChargeId,
                scenario.LineItemId,
                600m,
                scenario.ActorId,
                new PaymentAccessScopeInput()));

        var otherTenantId = scenario.TenantId + 1;

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService([new FakeOnlinePaymentProvider()]).CompleteAsync(
                new CompleteOnlinePaymentInput(initiateResult.MerchantPaymentId, otherTenantId)));

        Assert.Equal("ONLINE_PAYMENT_TRANSACTION_FORBIDDEN", exception.Code);
    }

    private OnlinePaymentService CreateService(IEnumerable<IOnlinePaymentProvider> providers)
    {
        var unitOfWork = new UnitOfWork(_context);
        var chargeLineItemRepository = new ChargeLineItemRepository(_context);
        var chargeService = new ChargeService(
            new ChargeRepository(_context),
            new PaymentAllocationRepository(_context),
            new UnitRepository(_context),
            chargeLineItemRepository,
            unitOfWork);
        var storeResolver = new PaymentStoreResolver(new PaymentStoreRoutingRepository(_context));

        return new OnlinePaymentService(
            chargeService,
            chargeLineItemRepository,
            new OnlinePaymentBusinessRules(),
            new OnlinePaymentTransactionRepository(_context),
            new OnlinePaymentEventRepository(_context),
            new PaymentAllocationRepository(_context),
            storeResolver,
            new StoreAccountRepository(_context),
            _protector,
            new TenantRepository(_context),
            providers,
            unitOfWork);
    }

    private sealed record Scenario(
        int TenantId, int ChargeId, int LineItemId, int StoreAccountId, string ActorId);

    private async Task<Scenario> SeedScenarioAsync(string providerCode = PaymentProviderCodes.Paratika)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Sanal POS Tasınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Sanal POS Birim Tipi {suffix}",
            Code = $"OPT-{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { TenantNo = $"OPT-{suffix}", Name = $"Sanal POS Kiracı {suffix}" };
        var chargeType = new ChargeType
        {
            Name = $"Sanal POS Borç Tipi {suffix}",
            Code = $"OPTCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.UserManual
        };
        var store = new Store { Name = $"Sanal POS Mağaza {suffix}", Code = $"OPTSTORE_{suffix}", IsActive = true };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = providerCode,
            Currency = CurrencyCodes.Try,
            MerchantId = $"MERCHANT-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = _protector.Protect("test-password"),
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });
        _context.AddRange(property, unitType, tenant, chargeType, store);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Sanal POS Ofis {suffix}",
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
            Description = "Sanal POS kalemi",
            Amount = 1000m,
            TotalAmount = 1000m
        };
        _context.ChargeLineItems.Add(lineItem);
        _context.PaymentStoreRoutings.Add(new PaymentStoreRouting
        {
            ChargeTypeId = chargeType.Id,
            PropertyId = null,
            UnitId = null,
            StoreId = store.Id,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var actorId = await _context.Users.Select(user => user.Id).FirstAsync();

        return new Scenario(tenant.Id, charge.Id, lineItem.Id, store.Accounts.Single().Id, actorId);
    }
}

/// <summary>
/// AcquirePaymentLockAsync eşzamanlılık davranışını gerçek transaction ile doğrular —
/// PaymentConcurrentLineItemTests'teki ayrı bağlantı/commit deseniyle aynı (tek bir ambient
/// transaction paylaşımı sp_getapplock'un gerçek çakışmasını simüle edemez).
/// </summary>
[Collection("Database collection")]
public class OnlinePaymentServiceConcurrencyTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task ConcurrentInitiates_ShouldNotCreateTwoActiveAttemptsForSameLineItem()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var protector = new StoreAccountCredentialProtector(new EphemeralDataProtectionProvider());
        int chargeId, lineItemId, propertyId, unitTypeId, unitId, tenantId, chargeTypeId, storeId;
        string actorId;

        await using (var setup = fixture.CreateContext())
        {
            var property = new Property { Name = $"Eşzamanlı Sanal POS {suffix}" };
            var unitType = new UnitType
            {
                Name = $"Eşzamanlı Sanal POS Birimi {suffix}",
                Code = $"OPTC-{suffix}",
                Usage = UnitTypeUsage.Rentable
            };
            var tenant = new Tenant { TenantNo = $"OPTC-{suffix}", Name = $"Eşzamanlı Sanal POS Kiracı {suffix}" };
            var chargeType = new ChargeType
            {
                Name = $"Eşzamanlı Sanal POS Borç Tipi {suffix}",
                Code = $"OPTCCT_{suffix}",
                IsActive = true,
                Behavior = ChargeTypeBehavior.UserManual
            };
            var store = new Store { Name = $"Eşzamanlı Sanal POS Mağaza {suffix}", Code = $"OPTCSTORE_{suffix}", IsActive = true };
            store.Accounts.Add(new StoreAccount
            {
                ProviderCode = PaymentProviderCodes.Paratika,
                Currency = CurrencyCodes.Try,
                MerchantId = $"MERCHANT-C-{suffix}",
                MerchantUser = "test-user",
                ProtectedMerchantPassword = protector.Protect("test-password"),
                ValidFrom = DateTime.UtcNow,
                IsActive = true
            });
            setup.AddRange(property, unitType, tenant, chargeType, store);
            await setup.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitTypeId = unitType.Id,
                Name = $"Eşzamanlı Sanal POS Ofis {suffix}",
                Area = 40m
            };
            setup.Units.Add(unit);
            await setup.SaveChangesAsync();

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
            setup.Charges.Add(charge);
            await setup.SaveChangesAsync();

            var lineItem = new ChargeLineItem
            {
                ChargeId = charge.Id,
                ChargeTypeId = chargeType.Id,
                Description = "Eşzamanlı sanal POS kalemi",
                Amount = 1000m,
                TotalAmount = 1000m
            };
            setup.ChargeLineItems.Add(lineItem);
            setup.PaymentStoreRoutings.Add(new PaymentStoreRouting
            {
                ChargeTypeId = chargeType.Id,
                PropertyId = null,
                UnitId = null,
                StoreId = store.Id,
                IsActive = true
            });
            await setup.SaveChangesAsync();

            actorId = await setup.Users.Select(user => user.Id).FirstAsync();
            chargeId = charge.Id;
            lineItemId = lineItem.Id;
            propertyId = property.Id;
            unitTypeId = unitType.Id;
            unitId = unit.Id;
            tenantId = tenant.Id;
            chargeTypeId = chargeType.Id;
            storeId = store.Id;
        }

        try
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstTask = InitiateAsync(ready.Task);
            var secondTask = InitiateAsync(ready.Task);
            ready.SetResult();
            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Single(results, result => result == "initiated");
            Assert.Single(results, result => result == "ONLINE_PAYMENT_ACTIVE_ATTEMPT_EXISTS");

            await using var verify = fixture.CreateContext();
            var pendingCount = await verify.OnlinePaymentTransactions
                .CountAsync(t => t.ChargeLineItemId == lineItemId && t.Status == OnlinePaymentTransactionStatus.Pending);
            Assert.Equal(1, pendingCount);
        }
        finally
        {
            await using var cleanup = fixture.CreateContext();
            cleanup.OnlinePaymentEvents.RemoveRange(
                cleanup.OnlinePaymentEvents.Where(e => e.OnlinePaymentTransaction.ChargeLineItemId == lineItemId));
            await cleanup.SaveChangesAsync();
            cleanup.OnlinePaymentTransactions.RemoveRange(
                cleanup.OnlinePaymentTransactions.Where(t => t.ChargeLineItemId == lineItemId));
            await cleanup.SaveChangesAsync();
            cleanup.ChargeLineItems.RemoveRange(cleanup.ChargeLineItems.Where(item => item.ChargeId == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.Charges.Remove(await cleanup.Charges.SingleAsync(c => c.Id == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.PaymentStoreRoutings.RemoveRange(cleanup.PaymentStoreRoutings.Where(r => r.ChargeTypeId == chargeTypeId));
            await cleanup.SaveChangesAsync();
            cleanup.StoreAccounts.RemoveRange(cleanup.StoreAccounts.Where(a => a.StoreId == storeId));
            await cleanup.SaveChangesAsync();
            cleanup.Stores.Remove(await cleanup.Stores.SingleAsync(s => s.Id == storeId));
            cleanup.ChargeTypes.Remove(await cleanup.ChargeTypes.SingleAsync(ct => ct.Id == chargeTypeId));
            cleanup.Units.Remove(await cleanup.Units.SingleAsync(u => u.Id == unitId));
            await cleanup.SaveChangesAsync();
            cleanup.Tenants.Remove(await cleanup.Tenants.SingleAsync(t => t.Id == tenantId));
            cleanup.UnitTypes.Remove(await cleanup.UnitTypes.SingleAsync(ut => ut.Id == unitTypeId));
            cleanup.Properties.Remove(await cleanup.Properties.SingleAsync(p => p.Id == propertyId));
            await cleanup.SaveChangesAsync();
        }

        async Task<string> InitiateAsync(Task start)
        {
            await start;
            await using var context = fixture.CreateContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var unitOfWork = new UnitOfWork(context);
            var chargeLineItemRepository = new ChargeLineItemRepository(context);
            var chargeService = new ChargeService(
                new ChargeRepository(context),
                new PaymentAllocationRepository(context),
                new UnitRepository(context),
                chargeLineItemRepository,
                unitOfWork);
            var service = new OnlinePaymentService(
                chargeService,
                chargeLineItemRepository,
                new OnlinePaymentBusinessRules(),
                new OnlinePaymentTransactionRepository(context),
                new OnlinePaymentEventRepository(context),
                new PaymentAllocationRepository(context),
                new PaymentStoreResolver(new PaymentStoreRoutingRepository(context)),
                new StoreAccountRepository(context),
                protector,
                new TenantRepository(context),
                [new FakeOnlinePaymentProvider()],
                unitOfWork);
            try
            {
                await service.InitiateAsync(new InitiateOnlinePaymentInput(
                    tenantId,
                    chargeId,
                    lineItemId,
                    300m,
                    actorId,
                    new PaymentAccessScopeInput()));
                await transaction.CommitAsync();
                return "initiated";
            }
            catch (BusinessException exception)
            {
                await transaction.RollbackAsync();
                return exception.Code ?? "business-error";
            }
        }
    }
}

public class OnlinePaymentBusinessRulesTests
{
    [Theory]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Approved, true)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Failed, true)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Cancelled, true)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Unknown, true)]
    [InlineData(OnlinePaymentTransactionStatus.Unknown, OnlinePaymentTransactionStatus.Approved, true)]
    [InlineData(OnlinePaymentTransactionStatus.Unknown, OnlinePaymentTransactionStatus.Failed, true)]
    [InlineData(OnlinePaymentTransactionStatus.Approved, OnlinePaymentTransactionStatus.Failed, false)]
    [InlineData(OnlinePaymentTransactionStatus.Failed, OnlinePaymentTransactionStatus.Approved, false)]
    [InlineData(OnlinePaymentTransactionStatus.Cancelled, OnlinePaymentTransactionStatus.Pending, false)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Pending, false)]
    public void IsValidStatusTransition_ShouldOnlyAllowPendingOrUnknownToTerminal(
        OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to, bool expected)
    {
        var rules = new OnlinePaymentBusinessRules();

        Assert.Equal(expected, rules.IsValidStatusTransition(from, to));
    }

    [Fact]
    public void EnsureValidStatusTransition_ShouldThrow_WhenTransitionFromTerminalStatus()
    {
        var rules = new OnlinePaymentBusinessRules();

        var exception = Assert.Throws<BusinessException>(() =>
            rules.EnsureValidStatusTransition(OnlinePaymentTransactionStatus.Approved, OnlinePaymentTransactionStatus.Failed));

        Assert.Equal("ONLINE_PAYMENT_INVALID_STATUS_TRANSITION", exception.Code);
    }

    [Fact]
    public void EnsureValidStatusTransition_ShouldNotThrow_ForAllowedTransition()
    {
        var rules = new OnlinePaymentBusinessRules();

        var exception = Record.Exception(() =>
            rules.EnsureValidStatusTransition(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Approved));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureAmountWithinAvailable_ShouldRejectNonPositiveAmount()
    {
        var rules = new OnlinePaymentBusinessRules();
        var balance = new ChargeLineItemPaymentBalanceDto(
            ChargeLineItemId: 1, ChargeId: 10, ChargeTypeId: 5, UnitId: 7, TenantId: 3,
            ChargeTypeName: "Kira Bedeli", Description: "Test kalemi",
            TotalAmount: 1000m, ApprovedAmount: 0m, PendingAmount: 0m);

        var exception = Assert.Throws<BusinessValidationException>(() =>
            rules.EnsureAmountWithinAvailable(balance, 0m));

        Assert.Equal("ONLINE_PAYMENT_AMOUNT_NOT_POSITIVE", exception.Code);
    }

    [Theory]
    [InlineData("AP", OnlinePaymentTransactionStatus.Approved)]
    [InlineData("FA", OnlinePaymentTransactionStatus.Failed)]
    [InlineData("CA", OnlinePaymentTransactionStatus.Failed)]
    [InlineData("VD", OnlinePaymentTransactionStatus.Cancelled)]
    [InlineData("IP", OnlinePaymentTransactionStatus.Pending)]
    [InlineData("MR", OnlinePaymentTransactionStatus.Unknown)]
    [InlineData("SOMETHING_UNKNOWN", OnlinePaymentTransactionStatus.Unknown)]
    [InlineData(null, OnlinePaymentTransactionStatus.Unknown)]
    public void NormalizeProviderStatus_ShouldMapTransactionStatusPerMainPlanSection54(
        string? transactionStatus, OnlinePaymentTransactionStatus expected)
    {
        var rules = new OnlinePaymentBusinessRules();

        var result = rules.NormalizeProviderStatus("00", transactionStatus);

        Assert.Equal(expected, result);
    }
}
