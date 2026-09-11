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

        Assert.Equal((seed.StoreAId, PaymentRoutingScope.Unit), (a101.StoreId, a101.MatchedScope));
        Assert.Equal((seed.StoreBId, PaymentRoutingScope.Unit), (a102.StoreId, a102.MatchedScope));
        Assert.Equal((seed.StoreBId, PaymentRoutingScope.Unit), (b101.StoreId, b101.MatchedScope));
        Assert.Equal((seed.StoreCId, PaymentRoutingScope.Property), (b102.StoreId, b102.MatchedScope));
        Assert.Equal((seed.StoreAId, PaymentRoutingScope.General), (other.StoreId, other.MatchedScope));
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
        Assert.Equal("PAYMENT_ROUTING_UNIT_NOT_FOUND", missingUnit.Code);
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
        var unitType = new UnitType
        {
            Name = Unique("Ofis"),
            Code = Unique("OFIS"),
            Usage = UnitTypeUsage.Rentable,
            IsActive = true
        };
        var propertyA = new Property { Name = Unique("Taşınmaz A"), IsActive = true };
        var propertyB = new Property { Name = Unique("Taşınmaz B"), IsActive = true };
        var propertyOther = new Property { Name = Unique("Başka Taşınmaz"), IsActive = true };
        var storeA = CreateStore("A");
        var storeB = CreateStore("B");
        var storeC = CreateStore("C");
        _context.AddRange(chargeType, unitType, propertyA, propertyB, propertyOther, storeA, storeB, storeC);
        await _context.SaveChangesAsync();

        var unitA101 = CreateUnit(propertyA.Id, unitType.Id, "Ofis 101 A");
        var unitA102 = CreateUnit(propertyA.Id, unitType.Id, "Ofis 102 A");
        var unitB101 = CreateUnit(propertyB.Id, unitType.Id, "Ofis 101 B");
        var unitB102 = CreateUnit(propertyB.Id, unitType.Id, "Ofis 102 B");
        var unitOther = CreateUnit(propertyOther.Id, unitType.Id, "Ofis 101 Diğer");
        _context.Units.AddRange(unitA101, unitA102, unitB101, unitB102, unitOther);
        await _context.SaveChangesAsync();

        _context.PaymentStoreRoutings.AddRange(
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, StoreId = storeA.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, PropertyId = propertyB.Id, StoreId = storeC.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, UnitId = unitA101.Id, StoreId = storeA.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, UnitId = unitA102.Id, StoreId = storeB.Id },
            new PaymentStoreRouting { ChargeTypeId = chargeType.Id, UnitId = unitB101.Id, StoreId = storeB.Id });
        await _context.SaveChangesAsync();

        return new Scenario(
            chargeType.Id, unitA101.Id, unitA102.Id, unitB101.Id, unitB102.Id, unitOther.Id,
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

    private static Unit CreateUnit(int propertyId, int unitTypeId, string name)
        => new() { PropertyId = propertyId, UnitTypeId = unitTypeId, Name = Unique(name), IsActive = true };

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
    private sealed record Scenario(
        int ChargeTypeId,
        int UnitA101Id,
        int UnitA102Id,
        int UnitB101Id,
        int UnitB102Id,
        int UnitOtherId,
        int StoreAId,
        int StoreBId,
        int StoreCId);
}
