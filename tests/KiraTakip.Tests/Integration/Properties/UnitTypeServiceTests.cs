using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.UnitType;
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
}
