using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Settings;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Settings;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class MinimumUsageAreaChangeTests(DatabaseFixture fixture) : PropertyUnitTestBase(fixture)
{
    private SystemSettingService CreateSettingService()
        => new(
            new SystemSettingRepository(Ctx),
            new NoopReservationPolicyProvider(),
            new TestOperationalPolicyProvider(),
            new UnitRepository(Ctx),
            TimeProvider.System,
            new UnitOfWork(Ctx));

    private async Task<SystemSetting> MinimumSettingAsync(string value = "5")
    {
        var setting = await Ctx.SystemSettings.SingleAsync(item =>
            item.Key == SystemSettingDefinitions.Lease.MinimumUsageArea);
        setting.Value = value;
        await Ctx.SaveChangesAsync();
        return setting;
    }

    private async Task<string> StoredMinimumAsync(int id)
    {
        Ctx.ChangeTracker.Clear();
        return (await Ctx.SystemSettings.AsNoTracking().SingleAsync(item => item.Id == id)).Value;
    }

    private async Task<Unit> SeedCapacityUnitAsync(decimal area, int capacity)
    {
        var propertyType = await SeedPropertyTypeAsync();
        var capacityType = await SeedUnitTypeAsync("Kont", UnitRentalMode.CapacityBased);
        var property = await SeedPropertyAsync(propertyType, UnitStructure.MultipleUnits);
        return await SeedUnitAsync(property, capacityType, "1", area, capacity);
    }

    private async Task<Lease> SeedAreaLeaseAsync(
        Unit unit,
        decimal rented,
        decimal allocated,
        LeaseStatus status = LeaseStatus.Active,
        DateTime? end = null)
    {
        var lease = await SeedLeaseAsync(unit, status, rented, end: end);
        lease.LeaseArea!.AllocatedArea = allocated;
        await Ctx.SaveChangesAsync();
        return lease;
    }

    private static Task<SystemSettingUpdateResult> Update(
        SystemSettingService service, SystemSetting setting, string value)
        => service.UpdateAsync(new UpdateSystemSettingInput(setting.Id, value));

    private async Task<BusinessValidationException> RejectAsync(SystemSetting setting, string value)
    {
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => Update(CreateSettingService(), setting, value));
        Assert.Equal(SystemSettingCodes.MinimumUsageAreaConflict, exception.Code);
        Assert.Equal("5", await StoredMinimumAsync(setting.Id));
        return exception;
    }

    [Fact]
    public async Task T12a_ActiveLeaseBelowNewMinimum_ShouldRejectAndKeepSetting()
    {
        var setting = await MinimumSettingAsync();
        var unit = await SeedCapacityUnitAsync(70m, 10);
        await SeedAreaLeaseAsync(unit, 5m, 5m);

        var exception = await RejectAsync(setting, "6");

        Assert.Contains("kiralanan 5 m²", exception.Message);
        Assert.Contains("fiziksel 5 m²", exception.Message);
        Assert.Contains(unit.Name, exception.Message);
    }

    [Fact]
    public async Task T12b_OnlyDraftLeaseBelowNewMinimum_ShouldSaveWithWarning()
    {
        var setting = await MinimumSettingAsync();
        var unit = await SeedCapacityUnitAsync(70m, 10);
        await SeedAreaLeaseAsync(unit, 5m, 5m, LeaseStatus.Draft);

        var result = await Update(CreateSettingService(), setting, "6");

        Assert.Equal("6", await StoredMinimumAsync(setting.Id));
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task KmaxExceeded_ShouldRejectAndKeepSetting()
    {
        var setting = await MinimumSettingAsync();
        var unit = await SeedCapacityUnitAsync(70m, 14);

        var exception = await RejectAsync(setting, "6");

        Assert.Contains("kontenjan 14, en fazla 11", exception.Message);
    }

    [Fact]
    public async Task ActiveLeaseAllocatedBelowNewMinimum_ShouldReject()
    {
        var setting = await MinimumSettingAsync();
        var unit = await SeedCapacityUnitAsync(70m, 10);
        await SeedAreaLeaseAsync(unit, 7m, 5m);

        var exception = await RejectAsync(setting, "6");

        Assert.Contains("kiralanan 7 m²", exception.Message);
        Assert.Contains("fiziksel 5 m²", exception.Message);
    }

    [Fact]
    public async Task Decrease_ShouldSaveWithoutChecks()
    {
        var setting = await MinimumSettingAsync();
        var unit = await SeedCapacityUnitAsync(70m, 14);
        await SeedAreaLeaseAsync(unit, 5m, 5m);
        await SeedAreaLeaseAsync(unit, 5m, 5m, LeaseStatus.Draft);

        var result = await Update(CreateSettingService(), setting, "4");

        Assert.Equal("4", await StoredMinimumAsync(setting.Id));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ExpiredOrTerminatedLease_ShouldNotBlock()
    {
        var setting = await MinimumSettingAsync();
        var unit = await SeedCapacityUnitAsync(70m, 10);
        await SeedAreaLeaseAsync(unit, 5m, 5m, end: Today.AddDays(-1));
        await SeedAreaLeaseAsync(unit, 5m, 5m, LeaseStatus.Terminated);

        var result = await Update(CreateSettingService(), setting, "6");

        Assert.Equal("6", await StoredMinimumAsync(setting.Id));
        Assert.Empty(result.Warnings);
    }
}

internal sealed class NoopReservationPolicyProvider : IReservationPolicyProvider
{
    public ReservationPolicySettings Current { get; } = new();

    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
