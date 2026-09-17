using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos.ChargeType;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Properties;
using KiraTakip.Services.Charges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public sealed class ChargeTypeDefaultRoutingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ChargeTypeDefaultRoutingTests(DatabaseFixture fixture)
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
    public async Task NewChargeType_ShouldStartPassiveAndRequireUsableDefaultForActivation()
    {
        var routingRepository = new PaymentStoreRoutingRepository(_context);
        var service = new ChargeTypeService(
            new ChargeTypeRepository(_context),
            new UnitTypeRepository(_context),
            routingRepository,
            new UnitOfWork(_context));
        var name = $"Yeni Borç {Guid.NewGuid():N}";

        var id = await service.CreateAsync(new CreateInput(name, ChargeTypeBehavior.UserManual, 999));
        Assert.False((await _context.ChargeTypes.SingleAsync(item => item.Id == id)).IsActive);

        var missing = await Assert.ThrowsAsync<BusinessException>(() => service.ToggleStatusAsync(id));
        Assert.Equal("CHARGE_TYPE_DEFAULT_STORE_REQUIRED", missing.Code);

        var store = new Store
        {
            Name = $"Mağaza {Guid.NewGuid():N}",
            Code = $"STORE-{Guid.NewGuid():N}",
            IsActive = true,
            Accounts =
            [
                new StoreAccount
                {
                    ProviderCode = PaymentProviderCodes.Paratika,
                    Currency = CurrencyCodes.Try,
                    MerchantId = "merchant",
                    MerchantUser = "user",
                    ProtectedMerchantPassword = "protected",
                    ValidFrom = DateTime.UtcNow,
                    IsActive = true
                }
            ]
        };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        _context.PaymentStoreRoutings.Add(new PaymentStoreRouting
        {
            ChargeTypeId = id,
            StoreId = store.Id,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        Assert.True(await service.ToggleStatusAsync(id));
    }

    [Fact]
    public async Task Update_ShouldAlsoBlockPassiveToActiveWithoutDefault()
    {
        var routingRepository = new PaymentStoreRoutingRepository(_context);
        var service = new ChargeTypeService(
            new ChargeTypeRepository(_context),
            new UnitTypeRepository(_context),
            routingRepository,
            new UnitOfWork(_context));
        var name = $"Güncellenecek {Guid.NewGuid():N}";
        var id = await service.CreateAsync(new CreateInput(name, ChargeTypeBehavior.UserManual, 998));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UpdateAsync(
            id,
            new EditInput(name, ChargeTypeBehavior.UserManual, 998, true)));

        Assert.Equal("CHARGE_TYPE_DEFAULT_STORE_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Update_ShouldNotDeactivateAnySystemChargeType()
    {
        var systemChargeType = new ChargeType
        {
            Name = $"Sistem Borç Tipi {Guid.NewGuid():N}",
            Code = $"SYS-{Guid.NewGuid():N}",
            Behavior = ChargeTypeBehavior.MonthlyFixed,
            SortOrder = 997,
            IsSystem = true,
            IsActive = true
        };
        _context.ChargeTypes.Add(systemChargeType);
        await _context.SaveChangesAsync();

        var service = new ChargeTypeService(
            new ChargeTypeRepository(_context),
            new UnitTypeRepository(_context),
            new PaymentStoreRoutingRepository(_context),
            new UnitOfWork(_context));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UpdateAsync(
            systemChargeType.Id,
            new EditInput(
                systemChargeType.Name,
                systemChargeType.Behavior,
                systemChargeType.SortOrder,
                false)));

        Assert.Equal("CHARGE_TYPE_SYSTEM_DEACTIVATION_FORBIDDEN", exception.Code);
        _context.ChangeTracker.Clear();
        Assert.True((await _context.ChargeTypes.SingleAsync(type => type.Id == systemChargeType.Id)).IsActive);
    }
}
