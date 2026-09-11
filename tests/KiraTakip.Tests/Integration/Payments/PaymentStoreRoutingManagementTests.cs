using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Common;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Properties;
using KiraTakip.Services.Payments;
using KiraTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public sealed class PaymentStoreRoutingManagementTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public PaymentStoreRoutingManagementTests(DatabaseFixture fixture)
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
    public async Task UpsertAndDeactivate_ShouldPreserveScopeAndHistory()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        await service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.General, null, null, seed.StoreAId));
        await service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.Property, seed.PropertyId, null, seed.StoreBId));
        await service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.Unit, null, seed.UnitId, seed.StoreAId));

        await service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.General, null, null, seed.StoreBId));
        var general = await _context.PaymentStoreRoutings.SingleAsync(routing =>
            routing.ChargeTypeId == seed.ChargeTypeId && routing.PropertyId == null && routing.UnitId == null);
        Assert.Equal(seed.StoreBId, general.StoreId);

        var managementData = await service.GetManagementDataAsync(new());
        Assert.Contains(managementData.Routings.Items, item => item.Id == general.Id);
        Assert.DoesNotContain(managementData.MissingDefaults, item => item.ChargeTypeId == seed.ChargeTypeId);
        Assert.Contains(managementData.Stores, item => item.Id == seed.StoreAId);

        var unitOverride = await _context.PaymentStoreRoutings.SingleAsync(routing => routing.UnitId == seed.UnitId);
        await service.DeactivateOverrideAsync(unitOverride.Id);
        await service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.Unit, null, seed.UnitId, seed.StoreBId));

        var unitHistory = await _context.PaymentStoreRoutings.IgnoreQueryFilters()
            .Where(routing => routing.UnitId == seed.UnitId)
            .OrderBy(routing => routing.Id)
            .ToListAsync();
        Assert.Equal(2, unitHistory.Count);
        Assert.False(unitHistory[0].IsActive);
        Assert.True(unitHistory[1].IsActive);
        Assert.False(unitHistory[0].IsDeleted);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.DeactivateOverrideAsync(general.Id));
        Assert.Equal("PAYMENT_ROUTING_DEFAULT_CANNOT_DEACTIVATE", exception.Code);
    }

    [Fact]
    public async Task GetManagementDataAsync_ShouldHideHistoryByDefaultAndExposeItViaStatusFilter()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        await service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.Unit, null, seed.UnitId, seed.StoreAId));
        var unitOverride = await _context.PaymentStoreRoutings.SingleAsync(routing => routing.UnitId == seed.UnitId);
        await service.DeactivateOverrideAsync(unitOverride.Id);

        var defaultView = await service.GetManagementDataAsync(new());
        Assert.DoesNotContain(defaultView.Routings.Items, item => item.Id == unitOverride.Id);
        Assert.True(defaultView.HistoryCount > 0);

        var historyView = await service.GetManagementDataAsync(new TableQuery { Status = "gecmis" });
        Assert.Contains(historyView.Routings.Items, item => item.Id == unitOverride.Id && !item.IsActive);
    }

    [Fact]
    public async Task Database_ShouldEnforceScopeConstraintAndFilteredUniqueIndexes()
    {
        var seed = await SeedAsync();
        _context.PaymentStoreRoutings.Add(new PaymentStoreRouting
        {
            ChargeTypeId = seed.ChargeTypeId,
            PropertyId = seed.PropertyId,
            UnitId = seed.UnitId,
            StoreId = seed.StoreAId
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        _context.ChangeTracker.Clear();

        _context.PaymentStoreRoutings.AddRange(
            new PaymentStoreRouting { ChargeTypeId = seed.ChargeTypeId, StoreId = seed.StoreAId },
            new PaymentStoreRouting { ChargeTypeId = seed.ChargeTypeId, StoreId = seed.StoreBId });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Upsert_ShouldRejectInactiveOrAccountlessStore()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var inactiveStore = new Store
        {
            Name = Unique("Pasif"),
            Code = Unique("PASIF"),
            IsActive = false
        };
        var accountlessStore = new Store
        {
            Name = Unique("Hesapsız"),
            Code = Unique("HESAPSIZ"),
            IsActive = true
        };
        _context.Stores.AddRange(inactiveStore, accountlessStore);
        await _context.SaveChangesAsync();

        var inactive = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.General, null, null, inactiveStore.Id)));
        Assert.Equal("PAYMENT_ROUTING_STORE_INACTIVE", inactive.Code);

        var accountless = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.General, null, null, accountlessStore.Id)));
        Assert.Equal("PAYMENT_ROUTING_ACTIVE_ACCOUNT_NOT_FOUND", accountless.Code);
    }

    [Fact]
    public async Task GetManagementDataAsync_ShouldReturnChargeTypeOptionsAndSurfaceInactiveMissingDefaults()
    {
        var seed = await SeedAsync();
        var inactiveChargeType = new ChargeType
        {
            Name = Unique("Pasif Borç"),
            Code = Unique("PASIFBORC"),
            Behavior = ChargeTypeBehavior.UserManual,
            IsActive = false
        };
        _context.ChargeTypes.Add(inactiveChargeType);
        await _context.SaveChangesAsync();
        var service = CreateService();

        var managementData = await service.GetManagementDataAsync(new());

        var activeOption = Assert.Single(managementData.ChargeTypes, item => item.Id == seed.ChargeTypeId);
        Assert.True(activeOption.IsActive);
        var inactiveOption = Assert.Single(managementData.ChargeTypes, item => item.Id == inactiveChargeType.Id);
        Assert.False(inactiveOption.IsActive);

        var missing = Assert.Single(managementData.MissingDefaults, item => item.ChargeTypeId == inactiveChargeType.Id);
        Assert.False(missing.IsChargeTypeActive);
    }

    [Fact]
    public async Task RoutingHistory_ShouldBlockUnitAndPropertyStructureRemoval()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        await service.UpsertAsync(new(seed.ChargeTypeId, PaymentRoutingScope.Unit, null, seed.UnitId, seed.StoreAId));
        var routing = await _context.PaymentStoreRoutings.SingleAsync(r => r.UnitId == seed.UnitId);
        await service.DeactivateOverrideAsync(routing.Id);

        Assert.True(await new UnitRepository(_context).HasHistoricalDependencyAsync(seed.UnitId));
        Assert.False(await new PropertyRepository(_context).CanChangeUnitStructureAsync(seed.PropertyId));
    }

    private PaymentStoreRoutingService CreateService()
    {
        var routingRepository = new PaymentStoreRoutingRepository(_context);
        var chargeTypeRepository = new ChargeTypeRepository(_context);
        var propertyRepository = new PropertyRepository(_context);
        var unitRepository = new UnitRepository(_context);
        var storeRepository = new StoreRepository(_context);
        var rules = new PaymentStoreRoutingBusinessRules(
            chargeTypeRepository,
            propertyRepository,
            unitRepository,
            storeRepository,
            routingRepository);
        return new PaymentStoreRoutingService(
            routingRepository,
            chargeTypeRepository,
            propertyRepository,
            unitRepository,
            storeRepository,
            rules,
            new UnitOfWork(_context),
            new SqlServerUniqueConstraintViolationDetector());
    }

    private async Task<Seed> SeedAsync()
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
        var property = new Property { Name = Unique("Taşınmaz"), IsActive = true };
        var storeA = CreateStore("A");
        var storeB = CreateStore("B");
        _context.AddRange(chargeType, unitType, property, storeA, storeB);
        await _context.SaveChangesAsync();
        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = Unique("Ofis 101"),
            IsActive = true
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return new Seed(chargeType.Id, property.Id, unit.Id, storeA.Id, storeB.Id);
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
    private sealed record Seed(int ChargeTypeId, int PropertyId, int UnitId, int StoreAId, int StoreBId);
}
