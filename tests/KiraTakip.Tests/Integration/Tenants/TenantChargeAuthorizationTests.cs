using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Properties;
using KiraTakip.Services.Charges;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Charge;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class TenantChargeAuthorizationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public TenantChargeAuthorizationTests(DatabaseFixture fixture)
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

    private ChargeService CreateService()
    {
        return new(new ChargeRepository(_context),
                   new PaymentAllocationRepository(_context),
                   new UnitRepository(_context),
                   new ChargeLineItemRepository(_context),
                   new UnitOfWork(_context));
    }

    private async Task<(Property Property, Unit Unit, Tenant Tenant, Tenant OtherTenant, Charge Charge)> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Yetki Testi Plaza {suffix}", City = "İstanbul", District = "Kadıköy" };
        var unitType = new UnitType
        {
            Name = $"Yetki Testi Ofis {suffix}",
            Code = $"AUTH_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { Name = $"Yetki Testi Kiracı {suffix}", TenantNo = $"AUTH-{suffix}" };
        var otherTenant = new Tenant { Name = $"Diğer Kiracı {suffix}", TenantNo = $"AUTHB-{suffix}" };

        _context.Properties.Add(property);
        _context.UnitTypes.Add(unitType);
        _context.Tenants.AddRange(tenant, otherTenant);
        await _context.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Ofis {suffix}", Area = 50 };
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

        return (property, unit, tenant, otherTenant, charge);
    }

    [Fact]
    public async Task GetTenantDetailsAsync_DifferentTenant_ThrowsNotFound()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetTenantDetailsAsync(new GetTenantChargeDetailsInput(
                seed.Charge.Id, seed.OtherTenant.Id)));

        Assert.Equal("TENANT_CHARGE_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task GetTenantDetailsAsync_OutsideUnitScope_ThrowsNotFound()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetTenantDetailsAsync(new GetTenantChargeDetailsInput(
                seed.Charge.Id, seed.Tenant.Id, PropertyIds: [], UnitIds: [])));

        Assert.Equal("TENANT_CHARGE_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task GetTenantDetailsAsync_SameTenantWithinScope_Succeeds()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var details = await service.GetTenantDetailsAsync(new GetTenantChargeDetailsInput(
            seed.Charge.Id,
            seed.Tenant.Id,
            PropertyIds: [seed.Property.Id],
            UnitIds: [seed.Unit.Id]));

        Assert.Equal(seed.Charge.Id, details.Id);
    }

    [Fact]
    public async Task GetTenantDetailsAsync_SameTenantGlobalAccess_Succeeds()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var details = await service.GetTenantDetailsAsync(new GetTenantChargeDetailsInput(
            seed.Charge.Id, seed.Tenant.Id));

        Assert.Equal(seed.Charge.Id, details.Id);
    }
}
