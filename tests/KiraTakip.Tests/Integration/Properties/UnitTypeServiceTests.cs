using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.UnitType;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class UnitTypeServiceTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly IDbContextTransaction _tx;

    public UnitTypeServiceTests(DatabaseFixture fixture)
    {
        _ctx = fixture.CreateContext();
        _tx = _ctx.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _tx.Rollback();
        _tx.Dispose();
        _ctx.Dispose();
    }

    private UnitTypeService CreateService()
        => new(new UnitTypeRepository(_ctx),
               new ChargeTypeRepository(_ctx),
               new ChargeRepository(_ctx),
               new ReservationRepository(_ctx),
               new UnitOfWork(_ctx));

    private static CreateUnitTypeInput NewInput(string name)
        => new(name, 1, UnitTypeUsage.Rentable, null, true);

    private async Task<int> CreateAndGetIdAsync(UnitTypeService service, string name)
    {
        await service.CreateAsync(NewInput(name));
        return await _ctx.UnitTypes.Where(x => x.Name == name).Select(x => x.Id).SingleAsync();
    }

    [Fact]
    public async Task Update_ShouldNotChangeCode_WhenNameChanges()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var service = CreateService();
        var id = await CreateAndGetIdAsync(service, $"Ofis {uid}");
        var originalCode = await _ctx.UnitTypes.Where(x => x.Id == id).Select(x => x.Code).SingleAsync();

        await service.UpdateAsync(new EditUnitTypeInput(id, $"Salon {uid}", 1, UnitTypeUsage.Rentable, null, true));

        var updated = await _ctx.UnitTypes.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal($"Salon {uid}", updated.Name);
        Assert.Equal(originalCode, updated.Code);
    }

    [Fact]
    public async Task Create_ShouldRejectDuplicateName_CaseInsensitive()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var service = CreateService();
        await service.CreateAsync(NewInput($"Depo {uid}"));

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.CreateAsync(NewInput($"  DEPO {uid} ")));
    }

    [Fact]
    public async Task Update_ShouldRejectDuplicateName_OfAnotherRecord()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var service = CreateService();
        await service.CreateAsync(NewInput($"Birinci {uid}"));
        var secondId = await CreateAndGetIdAsync(service, $"Ikinci {uid}");

        await Assert.ThrowsAsync<BusinessValidationException>(() => service.UpdateAsync(
            new EditUnitTypeInput(secondId, $"birinci {uid}", 1, UnitTypeUsage.Rentable, null, true)));

        // Kendi adıyla güncelleme serbest
        await service.UpdateAsync(
            new EditUnitTypeInput(secondId, $"Ikinci {uid}", 2, UnitTypeUsage.Rentable, null, true));
    }

    [Fact]
    public async Task Create_ShouldAppendSuffix_WhenCodeCollidesWithSoftDeletedType()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var service = CreateService();
        var id = await CreateAndGetIdAsync(service, $"Kafe {uid}");
        var oldCode = await _ctx.UnitTypes.Where(x => x.Id == id).Select(x => x.Code).SingleAsync();

        var entity = await _ctx.UnitTypes.SingleAsync(x => x.Id == id);
        entity.IsDeleted = true;
        await _ctx.SaveChangesAsync();

        await service.CreateAsync(NewInput($"Kafe {uid}"));

        var newCode = await _ctx.UnitTypes.Where(x => x.Name == $"Kafe {uid}").Select(x => x.Code).SingleAsync();
        Assert.Equal($"{oldCode}-2", newCode);
    }

    [Fact]
    public async Task Create_ShouldAppendSuffix_WhenCodeCollidesWithRenamedType()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var service = CreateService();
        var id = await CreateAndGetIdAsync(service, $"Kafe {uid}");
        var oldCode = await _ctx.UnitTypes.Where(x => x.Id == id).Select(x => x.Code).SingleAsync();

        await service.UpdateAsync(new EditUnitTypeInput(id, $"Restoran {uid}", 1, UnitTypeUsage.Rentable, null, true));
        await service.CreateAsync(NewInput($"Kafe {uid}"));

        var newCode = await _ctx.UnitTypes.Where(x => x.Name == $"Kafe {uid}").Select(x => x.Code).SingleAsync();
        Assert.Equal($"{oldCode}-2", newCode);
    }

    private async Task<(int TypeId, int UnitId)> SeedTypeWithUnitAsync(
        string uid, UnitRentalMode mode, int? capacity, LeaseStatus? leaseStatus, bool leaseDeleted = false)
    {
        var service = CreateService();
        await service.CreateAsync(new CreateUnitTypeInput($"Tur {uid}", 1, UnitTypeUsage.Rentable, null, true, mode));
        var typeId = await _ctx.UnitTypes.Where(x => x.Name == $"Tur {uid}").Select(x => x.Id).SingleAsync();

        var property = new Property { Name = $"Plaza {uid}", City = "İstanbul", District = "Kadıköy" };
        var tenant = new Tenant { TenantCategory = TestTenantCategory.New(), Name = $"Kiracı {uid}", TenantNo = $"UT-{uid}" };
        _ctx.Properties.Add(property);
        _ctx.Tenants.Add(tenant);
        await _ctx.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, UnitTypeId = typeId, Name = $"Birim {uid}", Area = 50, Capacity = capacity };
        _ctx.Units.Add(unit);
        await _ctx.SaveChangesAsync();

        if (leaseStatus.HasValue)
        {
            _ctx.Leases.Add(new Lease
            {
                LeaseNo = $"UT-{uid}",
                UnitId = unit.Id,
                TenantId = tenant.Id,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                Status = leaseStatus.Value,
                IsDeleted = leaseDeleted
            });
            await _ctx.SaveChangesAsync();
        }

        return (typeId, unit.Id);
    }

    [Theory]
    [InlineData(LeaseStatus.Ended)]
    [InlineData(LeaseStatus.Terminated)]
    public async Task Update_ShouldRejectUsageChange_WhenTypeHasEndedLease(LeaseStatus status)
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var (typeId, _) = await SeedTypeWithUnitAsync(uid, UnitRentalMode.UnitBased, null, status);
        var chargeTypeId = await _ctx.ChargeTypes.Select(c => (int?)c.Id).FirstOrDefaultAsync();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => CreateService().UpdateAsync(
            new EditUnitTypeInput(typeId, $"Tur {uid}", 1, UnitTypeUsage.NonRentable, chargeTypeId, true)));

        Assert.Equal("UnitType.FeaturesLocked", ex.Code);
    }

    [Fact]
    public async Task Update_ShouldRejectRentalModeChange_ButAllowRename_WhenTypeHasLease()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var (typeId, _) = await SeedTypeWithUnitAsync(uid, UnitRentalMode.UnitBased, null, LeaseStatus.Active);
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => service.UpdateAsync(
            new EditUnitTypeInput(typeId, $"Tur {uid}", 1, UnitTypeUsage.Rentable, null, true, UnitRentalMode.CapacityBased)));
        Assert.Equal("UnitType.FeaturesLocked", ex.Code);

        await service.UpdateAsync(
            new EditUnitTypeInput(typeId, $"Yeni {uid}", 5, UnitTypeUsage.Rentable, null, true, UnitRentalMode.UnitBased));

        var updated = await _ctx.UnitTypes.AsNoTracking().SingleAsync(x => x.Id == typeId);
        Assert.Equal($"Yeni {uid}", updated.Name);
        Assert.Equal(UnitRentalMode.UnitBased, updated.RentalMode);
    }

    [Fact]
    public async Task Update_ShouldNotLock_WhenOnlyLeaseIsDeleted()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var (typeId, _) = await SeedTypeWithUnitAsync(uid, UnitRentalMode.UnitBased, null, LeaseStatus.Active, leaseDeleted: true);

        await CreateService().UpdateAsync(
            new EditUnitTypeInput(typeId, $"Tur {uid}", 1, UnitTypeUsage.Rentable, null, true, UnitRentalMode.CapacityBased));

        var updated = await _ctx.UnitTypes.AsNoTracking().SingleAsync(x => x.Id == typeId);
        Assert.Equal(UnitRentalMode.CapacityBased, updated.RentalMode);
    }

    [Fact]
    public async Task Update_ShouldSwitchToCapacityBased_AndKeepUnitCapacityNull_WhenNoLease()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var (typeId, unitId) = await SeedTypeWithUnitAsync(uid, UnitRentalMode.UnitBased, null, null);

        await CreateService().UpdateAsync(
            new EditUnitTypeInput(typeId, $"Tur {uid}", 1, UnitTypeUsage.Rentable, null, true, UnitRentalMode.CapacityBased));

        var type = await _ctx.UnitTypes.AsNoTracking().SingleAsync(x => x.Id == typeId);
        var unit = await _ctx.Units.AsNoTracking().SingleAsync(x => x.Id == unitId);
        Assert.Equal(UnitRentalMode.CapacityBased, type.RentalMode);
        Assert.Null(unit.Capacity);
    }

    [Fact]
    public async Task Update_ShouldClearCapacityWithoutHistory_WhenUsageLeavesRentable()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var (typeId, unitId) = await SeedTypeWithUnitAsync(uid, UnitRentalMode.CapacityBased, 25, null);
        var historyBefore = await _ctx.Set<UnitCapacityHistory>().CountAsync(h => h.UnitId == unitId);

        await CreateService().UpdateAsync(
            new EditUnitTypeInput(typeId, $"Tur {uid}", 1, UnitTypeUsage.NonRentable, null, true, UnitRentalMode.CapacityBased));

        _ctx.ChangeTracker.Clear();
        var type = await _ctx.UnitTypes.AsNoTracking().SingleAsync(x => x.Id == typeId);
        var unit = await _ctx.Units.AsNoTracking().SingleAsync(x => x.Id == unitId);
        Assert.Equal(UnitRentalMode.UnitBased, type.RentalMode);
        Assert.Null(unit.Capacity);
        Assert.Equal(historyBefore, await _ctx.Set<UnitCapacityHistory>().CountAsync(h => h.UnitId == unitId));
    }

    [Fact]
    public async Task Update_ShouldClearCapacity_WhenSwitchingToUnitBased()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var (typeId, unitId) = await SeedTypeWithUnitAsync(uid, UnitRentalMode.CapacityBased, 10, null);

        await CreateService().UpdateAsync(
            new EditUnitTypeInput(typeId, $"Tur {uid}", 1, UnitTypeUsage.Rentable, null, true, UnitRentalMode.UnitBased));

        _ctx.ChangeTracker.Clear();
        var unit = await _ctx.Units.AsNoTracking().SingleAsync(x => x.Id == unitId);
        Assert.Null(unit.Capacity);
    }

    [Fact]
    public async Task Create_ShouldSaveUnitBased_WhenUsageIsNotRentable()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];

        await CreateService().CreateAsync(
            new CreateUnitTypeInput($"Bilgi {uid}", 1, UnitTypeUsage.NonRentable, null, true, UnitRentalMode.CapacityBased));

        var type = await _ctx.UnitTypes.AsNoTracking().SingleAsync(x => x.Name == $"Bilgi {uid}");
        Assert.Equal(UnitRentalMode.UnitBased, type.RentalMode);
    }
}
