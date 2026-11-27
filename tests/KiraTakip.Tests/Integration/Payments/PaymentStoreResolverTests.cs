using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Payments;
using KiraTakip.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public sealed class PaymentStoreResolverTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public PaymentStoreResolverTests(DatabaseFixture fixture)
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
    public async Task Resolve_ShouldApplyUnitPropertyGeneralPrecedenceForUserExamples()
    {
        var seed = await SeedScenarioAsync();
        var resolver = new PaymentStoreResolver(new PaymentStoreRoutingRepository(_context));

        var a101 = await resolver.ResolveAsync(seed.ChargeTypeId, seed.UnitA101Id);
        var a102 = await resolver.ResolveAsync(seed.ChargeTypeId, seed.UnitA102Id);
        var b101 = await resolver.ResolveAsync(seed.ChargeTypeId, seed.UnitB101Id);
        var b102 = await resolver.ResolveAsync(seed.ChargeTypeId, seed.UnitB102Id);
        var other = await resolver.ResolveAsync(seed.ChargeTypeId, seed.UnitOtherId);

        Assert.Equal((seed.StoreAId, seed.UnitA101Id), (a101.StoreId, a101.MatchedSpaceId));
        Assert.Equal((seed.StoreBId, seed.UnitA102Id), (a102.StoreId, a102.MatchedSpaceId));
        Assert.Equal((seed.StoreBId, seed.UnitB101Id), (b101.StoreId, b101.MatchedSpaceId));
        Assert.Equal((seed.StoreCId, seed.PropertyBId), (b102.StoreId, b102.MatchedSpaceId));
        Assert.Equal((seed.StoreAId, (int?)null), (other.StoreId, other.MatchedSpaceId));
    }

    [Fact]
    public async Task Resolve_ShouldNotFallbackWhenMostSpecificStoreBecomesInvalid()
    {
        var seed = await SeedScenarioAsync();
        var storeB = await _context.Stores.SingleAsync(store => store.Id == seed.StoreBId);
        storeB.IsActive = false;
        await _context.SaveChangesAsync();
        var resolver = new PaymentStoreResolver(new PaymentStoreRoutingRepository(_context));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            resolver.ResolveAsync(seed.ChargeTypeId, seed.UnitB101Id));

        Assert.Equal("PAYMENT_ROUTING_STORE_INACTIVE", exception.Code);
    }

    [Fact]
    public async Task Resolve_ShouldUseRoutingForPassiveChargeTypeAndReturnStableErrors()
    {
        var seed = await SeedScenarioAsync();
        var chargeType = await _context.ChargeTypes.SingleAsync(item => item.Id == seed.ChargeTypeId);
        chargeType.IsActive = false;
        await _context.SaveChangesAsync();
        var resolver = new PaymentStoreResolver(new PaymentStoreRoutingRepository(_context));

        var resolved = await resolver.ResolveAsync(seed.ChargeTypeId, seed.UnitOtherId);
        Assert.Equal(seed.StoreAId, resolved.StoreId);

        var missingRouting = await Assert.ThrowsAsync<BusinessException>(() =>
            resolver.ResolveAsync(int.MaxValue, seed.UnitOtherId));
        Assert.Equal("PAYMENT_ROUTING_NOT_FOUND", missingRouting.Code);

        var missingUnit = await Assert.ThrowsAsync<BusinessException>(() =>
            resolver.ResolveAsync(seed.ChargeTypeId, int.MaxValue));
        Assert.Equal("PAYMENT_ROUTING_SPACE_NOT_FOUND", missingUnit.Code);
    }

    private async Task<Scenario> SeedScenarioAsync()
    {
        var chargeType = new ChargeType
        {
            Name = Unique("Portal"),
            Code = Unique("PORTAL"),
            Behavior = ChargeTypeBehavior.UserManual,
            IsActive = true
        };
        var storeA = CreateStore("A");
        var storeB = CreateStore("B");
        var storeC = CreateStore("C");
        _context.AddRange(chargeType, storeA, storeB, storeC);
        await _context.SaveChangesAsync();

        var propertyA = await SpaceTestSeed.AddRootAsync(_context, Unique("Taşınmaz A"));
        var propertyB = await SpaceTestSeed.AddRootAsync(_context, Unique("Taşınmaz B"));
        var propertyOther = await SpaceTestSeed.AddRootAsync(_context, Unique("Başka Taşınmaz"));
        var unitA101 = await SpaceTestSeed.AddChildAsync(_context, propertyA, Unique("Ofis 101 A"));
        var unitA102 = await SpaceTestSeed.AddChildAsync(_context, propertyA, Unique("Ofis 102 A"));
        var unitB101 = await SpaceTestSeed.AddChildAsync(_context, propertyB, Unique("Ofis 101 B"));
        var unitB102 = await SpaceTestSeed.AddChildAsync(_context, propertyB, Unique("Ofis 102 B"));
        var unitOther = await SpaceTestSeed.AddChildAsync(_context, propertyOther, Unique("Ofis 101 Diğer"));

        _context.PaymentStoreRoutings.AddRange(
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, StoreId = storeA.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, SpaceId = propertyB.Id, StoreId = storeC.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, SpaceId = unitA101.Id, StoreId = storeA.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, SpaceId = unitA102.Id, StoreId = storeB.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, SpaceId = unitB101.Id, StoreId = storeB.Id });
        await _context.SaveChangesAsync();

        return new Scenario(
            chargeType.Id, propertyB.Id, unitA101.Id, unitA102.Id, unitB101.Id, unitB102.Id, unitOther.Id,
            storeA.Id, storeB.Id, storeC.Id);
    }

    private Store CreateStore(string suffix)
    {
        var store = new Store { Name = Unique($"Mağaza {suffix}"), Code = Unique($"STORE-{suffix}"), IsActive = true };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = Unique("merchant"),
            MerchantUser = "user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });
        return store;
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
    private sealed record Scenario(
        int ChargeTypeId,
        int PropertyBId,
        int UnitA101Id,
        int UnitA102Id,
        int UnitB101Id,
        int UnitB102Id,
        int UnitOtherId,
        int StoreAId,
        int StoreBId,
        int StoreCId);
}
