using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Settings;
using KiraTakip.Repositories.Settings;
using KiraTakip.Services;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Payments;
using KiraTakip.Services.Reservations;
using KiraTakip.Services.Settings;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class SystemSettingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly DatabaseFixture _fixture;

    public SystemSettingTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
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
    public async Task Migration_ShouldSeedAllDefinedSystemSettings()
    {
        var repository = new SystemSettingRepository(_context);

        var settings = await repository.GetActiveListAsync();

        Assert.Equal(SystemSettingDefinitions.All.Count, settings.Count);
        Assert.All(SystemSettingDefinitions.All, definition =>
            Assert.Contains(settings, setting =>
                setting.Key == definition.Key && setting.Value == definition.DefaultValue));
    }

    [Fact]
    public async Task Update_ShouldNormalizeValueAndRefreshPolicyCache()
    {
        var repository = new SystemSettingRepository(_context);
        var provider = new RecordingPolicyProvider();
        var operationalProvider = new TestOperationalPolicyProvider();
        var service = new SystemSettingService(
            repository,
            provider,
            operationalProvider,
            new UnitOfWork(_context));
        var setting = (await repository.GetActiveListAsync()).Single(item =>
            item.Key == SystemSettingDefinitions.Reservation.MinimumDurationMinutes);

        await service.UpdateAsync(new UpdateSystemSettingInput(setting.Id, " 30 "));

        Assert.Equal("30", (await repository.GetByIdAsync(setting.Id))!.Value);
        Assert.Equal(1, provider.RefreshCount);
        Assert.Equal(1, operationalProvider.RefreshCount);
    }

    [Fact]
    public async Task GetPaged_ShouldReturnRequestedPageWithTotalCount()
    {
        var service = new SystemSettingService(
            new SystemSettingRepository(_context),
            new RecordingPolicyProvider(),
            new TestOperationalPolicyProvider(),
            new UnitOfWork(_context));

        var result = await service.GetPagedAsync(new TableQuery { Page = 2, Size = 5 });

        Assert.Equal(SystemSettingDefinitions.All.Count, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.Size);
        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task Update_ShouldRejectPolicyThatMakesMaximumSmallerThanMinimum()
    {
        var repository = new SystemSettingRepository(_context);
        var service = new SystemSettingService(
            repository,
            new RecordingPolicyProvider(),
            new TestOperationalPolicyProvider(),
            new UnitOfWork(_context));
        var maximum = (await repository.GetActiveListAsync()).Single(item =>
            item.Key == SystemSettingDefinitions.Reservation.MaximumDurationMinutes);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.UpdateAsync(new UpdateSystemSettingInput(maximum.Id, "10")));

        Assert.Equal("SYSTEM_SETTING_INVALID_VALUE", exception.Code);
    }

    [Fact]
    public void Definitions_ShouldCreateStronglyTypedReservationPolicy()
    {
        var values = SystemSettingDefinitions.All.ToDictionary(
            definition => definition.Key,
            definition => definition.DefaultValue,
            StringComparer.OrdinalIgnoreCase);

        var policy = SystemSettingDefinitions.CreateReservationPolicy(values);

        Assert.Equal(15, policy.MinimumDurationMinutes);
        Assert.Equal(1440, policy.MaximumDurationMinutes);
        Assert.Equal("Turkey Standard Time", ReservationPolicySettings.TimeZoneId);
    }

    [Fact]
    public void Definitions_ShouldCreateStronglyTypedOperationalPolicy()
    {
        var values = SystemSettingDefinitions.All.ToDictionary(
            definition => definition.Key,
            definition => definition.DefaultValue,
            StringComparer.OrdinalIgnoreCase);

        var policy = SystemSettingDefinitions.CreateOperationalPolicy(values);

        Assert.Equal(7, policy.InvitationValidityDays);
        Assert.Equal(2, policy.BankMatchingAmountTolerancePercent);
        Assert.Equal(15, policy.BankMatchingDateToleranceDays);
    }

    [Fact]
    public async Task Provider_ShouldLoadStronglyTypedPolicyFromDatabase()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _fixture.CreateContext());
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        using var rootProvider = services.BuildServiceProvider();
        var provider = new ReservationPolicyProvider(
            rootProvider.GetRequiredService<IServiceScopeFactory>());

        await provider.RefreshAsync();

        Assert.Equal(15, provider.Current.MinimumDurationMinutes);
        Assert.Equal(100, provider.Current.MaximumAttendeeCount);
    }

    [Fact]
    public async Task OperationalProvider_ShouldLoadStronglyTypedPolicyFromDatabase()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _fixture.CreateContext());
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        using var rootProvider = services.BuildServiceProvider();
        var provider = new OperationalPolicyProvider(
            rootProvider.GetRequiredService<IServiceScopeFactory>());

        await provider.RefreshAsync();

        Assert.Equal(30, provider.Current.LeaseExpiringSoonStatusDays);
    }

    private sealed class RecordingPolicyProvider : IReservationPolicyProvider
    {
        public int RefreshCount { get; private set; }
        public ReservationPolicySettings Current { get; } = new();

        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            RefreshCount++;
            return Task.CompletedTask;
        }
    }
}
