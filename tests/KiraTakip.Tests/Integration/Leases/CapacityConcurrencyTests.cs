using Castle.DynamicProxy;
using KiraTakip.Application.DependencyInjection;
using KiraTakip.Data;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Web.DependencyInjection;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Properties;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Services.Properties;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Leases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace KiraTakip.Tests;

/// <summary>
/// Açık ofis (CapacityBased) kapasite kilitlerinin gerçek eşzamanlılıkta (ayrı bağlantı + gerçek commit)
/// çalıştığını kanıtlar. Veri commit edilir; her test kendi verisini finally'de siler.
/// </summary>
[Collection("Database collection")]
public class CapacityConcurrencyTests(DatabaseFixture fixture)
{
    private static readonly DateTime Start = new(2030, 1, 1);
    private static readonly DateTime End = new(2030, 12, 31);
    private static readonly ProxyGenerator Generator = new();

    private sealed record Data(
        LeaseServiceTestHarness.Seed Seed,
        int UnitId,
        int UnitTypeId,
        List<int> TenantIds);

    [Fact]
    public async Task T15_Parallel_TwoDraftsForLastSlot_ShouldAllowOnlyOne()
    {
        var data = new Data(null!, 0, 0, []);
        try
        {
            data = await SeedAsync(capacity: 3, filled: 2);
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = Task.Run(() => TryCreateAsync(data, ready.Task));
            var second = Task.Run(() => TryCreateAsync(data, ready.Task));
            ready.SetResult();
            var results = await Task.WhenAll(first, second);

            Assert.Single(results, r => r == "created");
            Assert.Single(results, r => r == "Lease.CapacityExceeded");
            Assert.Equal(3, await OpenLeaseCountAsync(data.UnitId));
        }
        finally
        {
            await CleanupAsync(data);
        }
    }

    [Fact]
    public async Task T15_LockHeldByOtherTransaction_ShouldBlockCreateUntilCommit()
    {
        var data = new Data(null!, 0, 0, []);
        try
        {
            data = await SeedAsync(capacity: 3, filled: 2);
            await using var holder = fixture.CreateContext();
            await using var holderTx = await holder.Database.BeginTransactionAsync();
            await new UnitRepository(holder).AcquireCapacityLocksAsync(data.UnitId);

            var blocked = Task.Run(() => TryCreateAsync(data, Task.CompletedTask));
            var finishedEarly = await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.NotSame(blocked, finishedEarly);

            await holderTx.CommitAsync();

            Assert.Equal("created", await blocked.WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.Equal(3, await OpenLeaseCountAsync(data.UnitId));
        }
        finally
        {
            await CleanupAsync(data);
        }
    }

    [Fact]
    public async Task T15_LeaseCreatedInHeldTransaction_ShouldMakeWaitingCreateFailAfterCommit()
    {
        var data = new Data(null!, 0, 0, []);
        try
        {
            data = await SeedAsync(capacity: 3, filled: 2);
            await using var holder = fixture.CreateContext();
            await using var holderTx = await holder.Database.BeginTransactionAsync();
            // Proxy mevcut transaction'a katılır; kilit bu transaction'ın sonuna kadar tutulur.
            await CreateProxiedLeaseService(holder, data.Seed.ApplicantId).CreateDraftAsync(Input(data));

            var blocked = Task.Run(() => TryCreateAsync(data, Task.CompletedTask));
            var finishedEarly = await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.NotSame(blocked, finishedEarly);

            await holderTx.CommitAsync();

            Assert.Equal("Lease.CapacityExceeded", await blocked.WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.Equal(3, await OpenLeaseCountAsync(data.UnitId));
        }
        finally
        {
            await CleanupAsync(data);
        }
    }

    [Fact]
    public async Task CapacityReduction_RacingWithDraftCreate_ShouldNeverLeaveMoreLeasesThanCapacity()
    {
        // Sıra iki yönde de olabilir; birkaç tur ile iki sıralamayı da yakalama şansı artırılır.
        for (var round = 0; round < 3; round++)
        {
            var data = new Data(null!, 0, 0, []);
            try
            {
                data = await SeedAsync(capacity: 3, filled: 2);
                var planInput = new CapacityPlanInput(data.UnitId, 2, new LeaseAccessScopeInput(), null);
                string hash;
                await using (var previewContext = fixture.CreateContext())
                {
                    hash = (await CreateProxiedPlanService(previewContext).PreviewAsync(planInput)).Hash;
                }

                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var apply = Task.Run(async () =>
                {
                    await ready.Task;
                    await using var context = fixture.CreateContext();
                    try
                    {
                        await CreateProxiedPlanService(context).ApplyAsync(planInput, hash);
                        return "applied";
                    }
                    catch (BusinessException exception)
                    {
                        return exception.Code ?? "business-error";
                    }
                });
                var create = Task.Run(() => TryCreateAsync(data, ready.Task));
                ready.SetResult();
                var applyResult = await apply;
                var createResult = await create;

                await using var verify = fixture.CreateContext();
                var capacity = (await verify.Units.AsNoTracking().SingleAsync(u => u.Id == data.UnitId)).Capacity;
                var count = await OpenLeaseCountAsync(data.UnitId);

                Assert.True(count <= capacity, $"Tur {round}: sözleşme {count} > kontenjan {capacity} (apply={applyResult}, create={createResult})");
                if (applyResult == "applied")
                {
                    Assert.Equal(2, capacity);
                    Assert.Equal("Lease.CapacityExceeded", createResult);
                }
                else
                {
                    Assert.Contains(applyResult, new[] { CapacityPlanCodes.CountExceeded, CapacityPlanCodes.PreviewOutdated });
                    Assert.Equal("created", createResult);
                    Assert.Equal(3, capacity);
                }
            }
            finally
            {
                await CleanupAsync(data);
            }
        }
    }

    [Fact]
    public async Task CapacityReduction_AppliedWhileCreateWaitsForLock_ShouldRejectCreateWithFreshCapacity()
    {
        // Deterministik sıra: create birimi (K=3) okuyup kilitte bekler; bu sırada K=2 uygulanıp commit edilir.
        var data = new Data(null!, 0, 0, []);
        try
        {
            data = await SeedAsync(capacity: 3, filled: 2);
            var planInput = new CapacityPlanInput(data.UnitId, 2, new LeaseAccessScopeInput(), null);
            await using var holder = fixture.CreateContext();
            var plan = CreateProxiedPlanService(holder);
            var hash = (await plan.PreviewAsync(planInput)).Hash;
            await using var holderTx = await holder.Database.BeginTransactionAsync();
            await new UnitRepository(holder).AcquireCapacityLocksAsync(data.UnitId);

            var blocked = Task.Run(() => TryCreateAsync(data, Task.CompletedTask));
            var finishedEarly = await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.NotSame(blocked, finishedEarly);

            await plan.ApplyAsync(planInput, hash);
            await holderTx.CommitAsync();
            var createResult = await blocked.WaitAsync(TimeSpan.FromSeconds(30));

            await using var verify = fixture.CreateContext();
            var capacity = (await verify.Units.AsNoTracking().SingleAsync(u => u.Id == data.UnitId)).Capacity;
            var count = await OpenLeaseCountAsync(data.UnitId);
            Assert.True(count <= capacity, $"sözleşme {count} > kontenjan {capacity} (create={createResult})");
        }
        finally
        {
            await CleanupAsync(data);
        }
    }

    [Fact]
    public void RealDiRegistration_ShouldReturnCastleProxiesForCapacityServices()
    {
        // Program.cs sırası: modüller, ardından AddTransactionalProxies.
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddInfrastructureModule(configuration);
        services.AddIdentityModule();
        services.AddWebModule(configuration);
        services.AddRepositoryModule();
        services.AddApplicationModule();
        services.AddTransactionalProxies();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.True(ProxyUtil.IsProxy(scope.ServiceProvider.GetRequiredService<ILeaseService>()));
        Assert.True(ProxyUtil.IsProxy(scope.ServiceProvider.GetRequiredService<ICapacityPlanService>()));
    }

    private async Task<string> TryCreateAsync(Data data, Task start)
    {
        await start;
        await using var context = fixture.CreateContext();
        try
        {
            await CreateProxiedLeaseService(context, data.Seed.ApplicantId).CreateDraftAsync(Input(data));
            return "created";
        }
        catch (BusinessException exception)
        {
            return exception.Code ?? "business-error";
        }
    }

    private static CreateLeaseDraftInput Input(Data data)
        => new(
            data.UnitId,
            data.Seed.TenantId,
            Start,
            End,
            DueDateRuleType.FixedDayOfMonth,
            10,
            null,
            new LeaseServiceTestHarness(null!).Rates(data.Seed),
            data.Seed.ApplicantId,
            LeaseServiceTestHarness.Scope(),
            false,
            5m,
            null,
            null);

    private static ILeaseService CreateProxiedLeaseService(ApplicationDbContext context, string actorId)
        => Generator.CreateInterfaceProxyWithTarget<ILeaseService>(
            new LeaseServiceTestHarness(context).CreateService(actorId),
            new TransactionInterceptor(context, NullLogger<TransactionInterceptor>.Instance).ToInterceptor());

    private static ICapacityPlanService CreateProxiedPlanService(ApplicationDbContext context)
        => Generator.CreateInterfaceProxyWithTarget<ICapacityPlanService>(
            new CapacityPlanService(
                new UnitRepository(context),
                new LeaseRepository(context),
                new UnitOfWork(context),
                new TestOperationalPolicyProvider(new OperationalPolicySettings { LeaseMinimumUsageArea = 5m }),
                new DefaultAllocationSuggestionStrategy(),
                new ProportionalReductionStrategy(),
                TimeProvider.System),
            new TransactionInterceptor(context, NullLogger<TransactionInterceptor>.Instance).ToInterceptor());

    private async Task<int> OpenLeaseCountAsync(int unitId)
    {
        await using var context = fixture.CreateContext();
        return await context.Leases.AsNoTracking().CountAsync(lease =>
            lease.UnitId == unitId
            && (lease.Status == LeaseStatus.Draft || lease.Status == LeaseStatus.Active
                || lease.Status == LeaseStatus.RevisionRequested));
    }

    private async Task<Data> SeedAsync(int capacity, int filled)
    {
        await using var context = fixture.CreateContext();
        var harness = new LeaseServiceTestHarness(context);
        var seed = await harness.SeedAsync(area: 70);
        var unitType = new UnitType
        {
            Name = $"Eşzamanlı Tip {seed.Suffix}",
            Code = $"CC{seed.Suffix}",
            Usage = UnitTypeUsage.Rentable,
            RentalMode = UnitRentalMode.CapacityBased
        };
        context.UnitTypes.Add(unitType);
        await context.SaveChangesAsync();
        var unit = new Unit
        {
            PropertyId = seed.PropertyId,
            UnitTypeId = unitType.Id,
            Name = $"Eşzamanlı Birim {seed.Suffix}",
            Area = 70m,
            Capacity = capacity
        };
        context.Units.Add(unit);
        await context.SaveChangesAsync();
        for (var i = 0; i < filled; i++)
        {
            context.Leases.Add(new Lease
            {
                LeaseNo = $"CC-{Guid.NewGuid().ToString("N")[..8]}",
                UnitId = unit.Id,
                TenantId = seed.TenantId,
                Status = LeaseStatus.Active,
                StartDate = Start,
                EndDate = End,
                RentalModeSnapshot = UnitRentalMode.CapacityBased,
                CreatedBy = seed.ApplicantId,
                LeaseArea = new LeaseArea { RentedArea = 5m, AllocatedArea = 5m }
            });
        }
        await context.SaveChangesAsync();
        return new Data(seed, unit.Id, unitType.Id, [seed.TenantId]);
    }

    private async Task CleanupAsync(Data data)
    {
        if (data.Seed is null) return;
        await using var context = fixture.CreateContext();
        var leases = context.Leases.IgnoreQueryFilters().Where(lease => lease.UnitId == data.UnitId);
        var leaseIds = leases.Select(lease => lease.Id);
        await context.LeaseAreaHistories.Where(h => h.LeaseArea.Lease.UnitId == data.UnitId).ExecuteDeleteAsync();
        await context.LeaseAreas.Where(a => a.Lease.UnitId == data.UnitId).ExecuteDeleteAsync();
        await context.SozlesmeTarifeler.Where(r => leaseIds.Contains(r.LeaseId)).ExecuteDeleteAsync();
        await context.SozlesmeIslemGecmisleri.Where(l => leaseIds.Contains(l.LeaseId)).ExecuteDeleteAsync();
        await context.SozlesmeIncelemeGecmisleri.Where(h => leaseIds.Contains(h.LeaseId)).ExecuteDeleteAsync();
        await leases.ExecuteDeleteAsync();
        await context.UnitCapacityHistories.Where(h => h.UnitId == data.UnitId).ExecuteDeleteAsync();
        await context.Units.IgnoreQueryFilters().Where(u => u.Id == data.UnitId).ExecuteDeleteAsync();
        await context.UnitTypes.Where(t => t.Id == data.UnitTypeId).ExecuteDeleteAsync();
        var categoryIds = await TestTenantCategory.IdsOfAsync(context, data.TenantIds);
        await context.Tenants.IgnoreQueryFilters().Where(t => data.TenantIds.Contains(t.Id)).ExecuteDeleteAsync();
        await TestTenantCategory.DeleteAsync(context, categoryIds);
        await context.Properties.IgnoreQueryFilters().Where(p => p.Id == data.Seed.PropertyId).ExecuteDeleteAsync();
        await context.UnitTypes.Where(t => t.Id == data.Seed.UnitTypeId).ExecuteDeleteAsync();
        await context.Users.Where(u => u.Id == data.Seed.ApplicantId || u.Id == data.Seed.ReviewerId).ExecuteDeleteAsync();
    }
}
