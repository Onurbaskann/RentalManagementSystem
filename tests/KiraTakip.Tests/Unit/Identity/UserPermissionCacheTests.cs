using System.Collections.Frozen;
using System.Linq.Expressions;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KiraTakip.Tests;

public class UserPermissionCacheTests
{
    [Fact]
    public async Task GetAsync_FirstCallLoadsFromRepository_SubsequentCallUsesCache()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.PermissionsByUser["user-1"] = ["Lease.View", "Payment.View"];

        var firstResult = await cacheService.GetAsync("user-1");
        var secondResult = await cacheService.GetAsync("user-1");

        Assert.Equal(1, fakeRepo.CallCountByUser["user-1"]);
        Assert.Equal(2, firstResult.Count);
        Assert.Contains("Lease.View", firstResult);
        Assert.Contains("Payment.View", firstResult);
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public async Task GetAsync_IsolatesCachePerUser()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.PermissionsByUser["user-1"] = ["Property.View"];
        fakeRepo.PermissionsByUser["user-2"] = ["Tenant.View"];

        var user1Perms = await cacheService.GetAsync("user-1");
        var user2Perms = await cacheService.GetAsync("user-2");

        Assert.Equal(1, fakeRepo.CallCountByUser["user-1"]);
        Assert.Equal(1, fakeRepo.CallCountByUser["user-2"]);
        Assert.Contains("Property.View", user1Perms);
        Assert.DoesNotContain("Tenant.View", user1Perms);
        Assert.Contains("Tenant.View", user2Perms);
        Assert.DoesNotContain("Property.View", user2Perms);
    }

    [Fact]
    public async Task Invalidate_ForcesSubsequentCallToReloadFromRepository()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.PermissionsByUser["user-1"] = ["Lease.View"];

        await cacheService.GetAsync("user-1");
        Assert.Equal(1, fakeRepo.CallCountByUser["user-1"]);

        cacheService.Invalidate("user-1");

        fakeRepo.PermissionsByUser["user-1"] = ["Lease.View", "Lease.Create"];
        var reloaded = await cacheService.GetAsync("user-1");

        Assert.Equal(2, fakeRepo.CallCountByUser["user-1"]);
        Assert.Contains("Lease.Create", reloaded);
    }

    [Fact]
    public async Task InvalidateMany_EvictsAllSpecifiedUsersAndIgnoresDuplicatesAndEmpty()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.PermissionsByUser["user-1"] = ["P1"];
        fakeRepo.PermissionsByUser["user-2"] = ["P2"];

        await cacheService.GetAsync("user-1");
        await cacheService.GetAsync("user-2");
        Assert.Equal(1, fakeRepo.CallCountByUser["user-1"]);
        Assert.Equal(1, fakeRepo.CallCountByUser["user-2"]);

        cacheService.InvalidateMany(["user-1", "user-1", "user-2", " ", null!]);

        await cacheService.GetAsync("user-1");
        await cacheService.GetAsync("user-2");

        Assert.Equal(2, fakeRepo.CallCountByUser["user-1"]);
        Assert.Equal(2, fakeRepo.CallCountByUser["user-2"]);
    }

    [Fact]
    public async Task Concurrent_InvalidationDuringLoad_PreventsStaleCachePollution()
    {
        var (cacheService, fakeRepo, _) = CreateSut();

        // In-flight invalidation scenario:
        // While GetPermissionsAsync is executing, an Invalidate call occurs (e.g. from another thread).
        fakeRepo.OnGetPermissionsAsync = (userId, ct) =>
        {
            cacheService.Invalidate(userId);
            return Task.FromResult(new List<string> { "Stale.Permission" });
        };

        var callerResult = await cacheService.GetAsync("user-race");
        Assert.Contains("Stale.Permission", callerResult);

        // Subsequent call must NOT find "Stale.Permission" in cache because Invalidate occurred during load.
        fakeRepo.OnGetPermissionsAsync = null;
        fakeRepo.PermissionsByUser["user-race"] = ["Fresh.Permission"];

        var nextResult = await cacheService.GetAsync("user-race");
        Assert.Equal(2, fakeRepo.CallCountByUser["user-race"]);
        Assert.Contains("Fresh.Permission", nextResult);
        Assert.DoesNotContain("Stale.Permission", nextResult);
    }

    [Fact]
    public async Task Concurrent_ControlledRace_InvalidateDuringLoad_DoesNotOverwriteNewerCache()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        const string userId = "user-deterministic-race";

        var firstLoadEnteredTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstLoadProceedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        fakeRepo.OnGetPermissionsAsync = async (uid, ct) =>
        {
            int callIndex;
            lock (fakeRepo.SyncRoot)
            {
                callIndex = fakeRepo.CallCountByUser[uid];
            }

            if (callIndex == 1)
            {
                // İlk GetAsync cache miss alsın; repository eski izin sonucuyla tamamlanmadan beklesin.
                firstLoadEnteredTcs.TrySetResult();
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var reg = timeoutCts.Token.Register(() => firstLoadProceedTcs.TrySetCanceled());
                await firstLoadProceedTcs.Task;
                return ["Old.Permission"];
            }

            // İkinci GetAsync yeni izinleri yüklesin
            return ["New.Permission"];
        };

        // 1. İlk GetAsync çağrılır ve repo içinde asılı kalır
        var firstLoadTask = cacheService.GetAsync(userId);
        await firstLoadEnteredTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // 2. Invalidate çağrılsın
        cacheService.Invalidate(userId);

        // 3. İkinci GetAsync yeni izinleri yüklesin ve cache'e yazsın
        var secondLoadResult = await cacheService.GetAsync(userId).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("New.Permission", secondLoadResult);
        Assert.DoesNotContain("Old.Permission", secondLoadResult);

        // 4. Ardından ilk yükleme eski izinlerle tamamlansın
        firstLoadProceedTcs.TrySetResult();
        var firstLoadResult = await firstLoadTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("Old.Permission", firstLoadResult);

        // 5. Üçüncü GetAsync yeni izinleri cache’den almalı; repository yeniden çağrılmamalı
        var thirdLoadResult = await cacheService.GetAsync(userId).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("New.Permission", thirdLoadResult);
        Assert.DoesNotContain("Old.Permission", thirdLoadResult);
        Assert.Same(secondLoadResult, thirdLoadResult);

        // 6. Eski yüklemenin yeni cache kaydını ezmediğini ve repository çağrı sayısını doğrula
        Assert.Equal(2, fakeRepo.CallCountByUser[userId]);
    }

    [Fact]

    public async Task EmptyPermissions_IsCachedAndDoesNotReload()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.PermissionsByUser["empty-user"] = [];

        var first = await cacheService.GetAsync("empty-user");
        var second = await cacheService.GetAsync("empty-user");

        Assert.Empty(first);
        Assert.Same(first, second);
        Assert.Equal(1, fakeRepo.CallCountByUser["empty-user"]);
    }

    [Fact]
    public async Task Duplicates_AreDeduplicated_And_ResultIsImmutable_WithOrdinalComparer()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.PermissionsByUser["user-dups"] = ["Module.Action", "Module.Action", "Other.Action"];

        var result = await cacheService.GetAsync("user-dups");

        Assert.Equal(2, result.Count);
        Assert.True(result.Contains("Module.Action"));
        Assert.False(result.Contains("module.action")); // Ordinal comparison
        Assert.True(result.Contains("Other.Action"));

        // Result cannot be mutated
        Assert.IsAssignableFrom<IReadOnlySet<string>>(result);
        Assert.True(result is FrozenSet<string>);
    }

    [Fact]
    public async Task DatabaseError_IsNotCached()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.OnGetPermissionsAsync = (_, _) => throw new InvalidOperationException("DB connection failure");

        await Assert.ThrowsAsync<InvalidOperationException>(() => cacheService.GetAsync("user-err"));

        // Now database recovers
        fakeRepo.OnGetPermissionsAsync = null;
        fakeRepo.PermissionsByUser["user-err"] = ["Recovered.Permission"];

        var recoveredResult = await cacheService.GetAsync("user-err");
        Assert.Contains("Recovered.Permission", recoveredResult);
        Assert.Equal(2, fakeRepo.CallCountByUser["user-err"]);
    }

    [Fact]
    public async Task AbsoluteExpiration_30Minutes_ExpiresCorrectlyWithoutRealWait()
    {
        var clockType = typeof(MemoryCacheOptions).GetProperty("Clock")!.PropertyType;
        var interceptor = new MutableClockInterceptor(new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero));
        var generator = new Castle.DynamicProxy.ProxyGenerator();
        var clockProxy = generator.CreateInterfaceProxyWithoutTarget(clockType, interceptor);

        var memoryCacheOptions = new MemoryCacheOptions();
        typeof(MemoryCacheOptions).GetProperty("Clock")!.SetValue(memoryCacheOptions, clockProxy);
        var memoryCache = new MemoryCache(memoryCacheOptions);

        var fakeRepo = new FakeUserRoleRepository();
        fakeRepo.PermissionsByUser["user-ttl"] = ["Permission.A"];

        var services = new ServiceCollection();
        services.AddScoped<IUserRoleRepository>(_ => fakeRepo);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var cacheService = new UserPermissionCacheService(memoryCache, scopeFactory);

        // Initial load at t = 0 min
        var result1 = await cacheService.GetAsync("user-ttl");
        Assert.Equal(1, fakeRepo.CallCountByUser["user-ttl"]);
        Assert.Contains("Permission.A", result1);

        // Advance 29 minutes (t = 29 min, TTL is 30 min) -> must still be cached
        interceptor.UtcNow = interceptor.UtcNow.AddMinutes(29);
        var result2 = await cacheService.GetAsync("user-ttl");
        Assert.Equal(1, fakeRepo.CallCountByUser["user-ttl"]);
        Assert.Same(result1, result2);

        // Advance 2 more minutes (t = 31 min, past 30 min TTL) -> must have expired and reload
        interceptor.UtcNow = interceptor.UtcNow.AddMinutes(2);
        fakeRepo.PermissionsByUser["user-ttl"] = ["Permission.A", "Permission.B"];
        var result3 = await cacheService.GetAsync("user-ttl");
        Assert.Equal(2, fakeRepo.CallCountByUser["user-ttl"]);
        Assert.Contains("Permission.B", result3);
    }








    [Fact]
    public async Task ArgumentValidation_RejectsInvalidUserIds()
    {
        var (cacheService, _, _) = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => cacheService.GetAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => cacheService.GetAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => cacheService.GetAsync("   "));

        Assert.Throws<ArgumentException>(() => cacheService.Invalidate(null!));
        Assert.Throws<ArgumentException>(() => cacheService.Invalidate(""));
        Assert.Throws<ArgumentException>(() => cacheService.Invalidate("   "));

        Assert.Throws<ArgumentNullException>(() => cacheService.InvalidateMany(null!));
    }

    [Fact]
    public async Task CancellationToken_IsPassedToRepository()
    {
        var (cacheService, fakeRepo, _) = CreateSut();
        fakeRepo.PermissionsByUser["user-ct"] = ["P1"];

        using var cts = new CancellationTokenSource();
        await cacheService.GetAsync("user-ct", cts.Token);

        Assert.Equal(cts.Token, fakeRepo.LastCancellationToken);
    }

    [Fact]
    public void DependencyInjection_Registration_ScopeAndLifetime()
    {
        var services = new ServiceCollection();
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;Trusted_Connection=True;",
            ["Smtp:Host"] = "localhost",
            ["DataProtection:KeyRingPath"] = ""
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

        services.AddInfrastructureModule(configuration);

        // Verify service registration
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IUserPermissionCache));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(UserPermissionCacheService), descriptor.ImplementationType);

        // Verify it can be resolved as singleton across scopes
        // Stub IUserRoleRepository for scope creation
        services.AddScoped<IUserRoleRepository, FakeUserRoleRepository>();
        var rootProvider = services.BuildServiceProvider();

        var singleton1 = rootProvider.GetRequiredService<IUserPermissionCache>();
        using (var scope = rootProvider.CreateScope())
        {
            var singleton2 = scope.ServiceProvider.GetRequiredService<IUserPermissionCache>();
            Assert.Same(singleton1, singleton2);
        }
    }

    [Fact]
    public async Task ScopedDependency_Lifecycle_ProperlyCreatesAndDisposesScope()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var services = new ServiceCollection();
        services.AddSingleton<IMemoryCache>(memoryCache);
        services.AddSingleton<IUserPermissionCache, UserPermissionCacheService>();
        services.AddScoped<IUserRoleRepository, TrackingScopedUserRoleRepository>();

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        var cache = serviceProvider.GetRequiredService<IUserPermissionCache>();
        TrackingScopedUserRoleRepository.Reset();

        // 1. İlk cache miss için scope/repository oluşturulduğunu ve GetAsync tamamlandığında dispose edildiğini doğrula.
        var missResult = await cache.GetAsync("user-scope-test");
        Assert.Contains("Perm.Scoped", missResult);
        Assert.Single(TrackingScopedUserRoleRepository.CreatedInstances);
        var firstRepo = TrackingScopedUserRoleRepository.CreatedInstances[0];
        Assert.True(firstRepo.IsDisposed);
        Assert.Equal(1, firstRepo.DisposeCount);

        // 2. Cache hit sırasında yeni repository oluşturulmadığını doğrula.
        var hitResult = await cache.GetAsync("user-scope-test");
        Assert.Same(missResult, hitResult);
        Assert.Single(TrackingScopedUserRoleRepository.CreatedInstances);
        Assert.Equal(1, firstRepo.DisposeCount);

        // 3. Invalidate sonrası yeni okumada farklı bir repository örneği oluşturulup dispose edildiğini doğrula.
        cache.Invalidate("user-scope-test");
        var reloadResult = await cache.GetAsync("user-scope-test");
        Assert.Contains("Perm.Scoped", reloadResult);
        Assert.Equal(2, TrackingScopedUserRoleRepository.CreatedInstances.Count);
        var secondRepo = TrackingScopedUserRoleRepository.CreatedInstances[1];
        Assert.NotSame(firstRepo, secondRepo);
        Assert.True(secondRepo.IsDisposed);
        Assert.Equal(1, secondRepo.DisposeCount);

        // 4. Repository hata verdiğinde de scope’un dispose edildiğini doğrula.
        TrackingScopedUserRoleRepository.ShouldThrow = true;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync("user-scope-error"));
        Assert.Equal("Simulated repository error", ex.Message);
        Assert.Equal(3, TrackingScopedUserRoleRepository.CreatedInstances.Count);
        var thirdRepo = TrackingScopedUserRoleRepository.CreatedInstances[2];
        Assert.NotSame(secondRepo, thirdRepo);
        Assert.True(thirdRepo.IsDisposed);
        Assert.Equal(1, thirdRepo.DisposeCount);
    }


    private static (UserPermissionCacheService Sut, FakeUserRoleRepository Repo, IMemoryCache Cache) CreateSut(TimeProvider? timeProvider = null)
    {
        var memoryCacheOptions = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(memoryCacheOptions);


        var fakeRepo = new FakeUserRoleRepository();
        var services = new ServiceCollection();
        services.AddScoped<IUserRoleRepository>(_ => fakeRepo);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var sut = new UserPermissionCacheService(memoryCache, scopeFactory);
        return (sut, fakeRepo, memoryCache);
    }

    private sealed class MutableClockInterceptor(DateTimeOffset initial) : Castle.DynamicProxy.IInterceptor
    {
        public DateTimeOffset UtcNow { get; set; } = initial;

        public void Intercept(Castle.DynamicProxy.IInvocation invocation)
        {
            if (invocation.Method.Name == "get_UtcNow")
            {
                invocation.ReturnValue = UtcNow;
            }
            else
            {
                invocation.Proceed();
            }
        }
    }


    private sealed class FakeUserRoleRepository : IUserRoleRepository
    {
        public readonly object SyncRoot = new();
        public Dictionary<string, List<string>> PermissionsByUser { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> CallCountByUser { get; } = new(StringComparer.Ordinal);
        public Func<string, CancellationToken, Task<List<string>>>? OnGetPermissionsAsync { get; set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public async Task<List<string>> GetPermissionsAsync(string userId, CancellationToken ct = default)
        {
            LastCancellationToken = ct;
            Func<string, CancellationToken, Task<List<string>>>? handler;
            lock (SyncRoot)
            {
                CallCountByUser[userId] = CallCountByUser.GetValueOrDefault(userId) + 1;
                handler = OnGetPermissionsAsync;
            }

            if (handler != null)
                return await handler(userId, ct);

            lock (SyncRoot)
            {
                if (PermissionsByUser.TryGetValue(userId, out var list))
                    return [.. list];
            }

            return [];
        }


        public Task<int> CountUsersInRoleAsync(int roleId) => throw new NotImplementedException();
        public Task<int> CountUsersInRoleForTenantAsync(int roleId, int tenantId) => throw new NotImplementedException();
        public Task<bool> HasAnyUsersInRoleAsync(int roleId) => throw new NotImplementedException();
        public Task<int?> GetFirstRoleIdAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(int RoleId, string RoleName)?> GetUserRoleInfoAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<string>> GetRoleNamesAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserRole?> GetByUserAndRoleNameAsync(string userId, string roleName, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<UserRole>> GetAllByUserIgnoringFiltersAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<string>> GetUserIdsByRoleNameAsync(string roleName, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserRole?> GetByUserAndRoleIdIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<string>> GetUserIdsByRoleIdAsync(int roleId, CancellationToken ct = default) => throw new NotImplementedException();
        public void RemoveRange(IEnumerable<UserRole> userRoles) => throw new NotImplementedException();
        public Task<UserRole?> GetByIdAsync(int id, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => throw new NotImplementedException();
        public Task<UserRole?> GetAsync(Expression<Func<UserRole, bool>> predicate, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => throw new NotImplementedException();
        public Task<List<UserRole>> GetAllAsync(Expression<Func<UserRole, bool>>? filter = null, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => throw new NotImplementedException();
        public Task<TResult?> GetByIdAsync<TResult>(int id, Expression<Func<UserRole, TResult>> selector) => throw new NotImplementedException();
        public Task<TResult?> GetAsync<TResult>(Expression<Func<UserRole, bool>> predicate, Expression<Func<UserRole, TResult>> selector) => throw new NotImplementedException();
        public Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<UserRole, bool>>? filter, Expression<Func<UserRole, TResult>> selector) => throw new NotImplementedException();
        public Task<bool> AnyAsync(Expression<Func<UserRole, bool>> predicate) => throw new NotImplementedException();
        public Task<int> CountAsync(Expression<Func<UserRole, bool>>? filter = null) => throw new NotImplementedException();
        public Task AddAsync(UserRole entity) => throw new NotImplementedException();
        public Task UpdateAsync(UserRole entity) => throw new NotImplementedException();
        public Task DeleteAsync(int id, bool hardDelete = false) => throw new NotImplementedException();
    }

    private sealed class TrackingScopedUserRoleRepository : IUserRoleRepository, IDisposable
    {
        private static readonly object _sync = new();
        public static List<TrackingScopedUserRoleRepository> CreatedInstances { get; } = [];
        public static bool ShouldThrow { get; set; }

        public bool IsDisposed { get; private set; }
        public int DisposeCount { get; private set; }

        public TrackingScopedUserRoleRepository()
        {
            lock (_sync)
            {
                CreatedInstances.Add(this);
            }
        }

        public static void Reset()
        {
            lock (_sync)
            {
                CreatedInstances.Clear();
                ShouldThrow = false;
            }
        }

        public Task<List<string>> GetPermissionsAsync(string userId, CancellationToken ct = default)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(TrackingScopedUserRoleRepository));

            if (ShouldThrow)
                throw new InvalidOperationException("Simulated repository error");

            return Task.FromResult<List<string>>(["Perm.Scoped"]);
        }

        public void Dispose()
        {
            lock (_sync)
            {
                IsDisposed = true;
                DisposeCount++;
            }
        }

        public Task<int> CountUsersInRoleAsync(int roleId) => throw new NotImplementedException();
        public Task<int> CountUsersInRoleForTenantAsync(int roleId, int tenantId) => throw new NotImplementedException();
        public Task<bool> HasAnyUsersInRoleAsync(int roleId) => throw new NotImplementedException();
        public Task<int?> GetFirstRoleIdAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(int RoleId, string RoleName)?> GetUserRoleInfoAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<string>> GetRoleNamesAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserRole?> GetByUserAndRoleNameAsync(string userId, string roleName, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<UserRole>> GetAllByUserIgnoringFiltersAsync(string userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<string>> GetUserIdsByRoleNameAsync(string roleName, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserRole?> GetByUserAndRoleIdIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<string>> GetUserIdsByRoleIdAsync(int roleId, CancellationToken ct = default) => throw new NotImplementedException();
        public void RemoveRange(IEnumerable<UserRole> userRoles) => throw new NotImplementedException();
        public Task<UserRole?> GetByIdAsync(int id, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => throw new NotImplementedException();
        public Task<UserRole?> GetAsync(Expression<Func<UserRole, bool>> predicate, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => throw new NotImplementedException();
        public Task<List<UserRole>> GetAllAsync(Expression<Func<UserRole, bool>>? filter = null, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => throw new NotImplementedException();
        public Task<TResult?> GetByIdAsync<TResult>(int id, Expression<Func<UserRole, TResult>> selector) => throw new NotImplementedException();
        public Task<TResult?> GetAsync<TResult>(Expression<Func<UserRole, bool>> predicate, Expression<Func<UserRole, TResult>> selector) => throw new NotImplementedException();
        public Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<UserRole, bool>>? filter, Expression<Func<UserRole, TResult>> selector) => throw new NotImplementedException();
        public Task<bool> AnyAsync(Expression<Func<UserRole, bool>> predicate) => throw new NotImplementedException();
        public Task<int> CountAsync(Expression<Func<UserRole, bool>>? filter = null) => throw new NotImplementedException();
        public Task AddAsync(UserRole entity) => throw new NotImplementedException();
        public Task UpdateAsync(UserRole entity) => throw new NotImplementedException();
        public Task DeleteAsync(int id, bool hardDelete = false) => throw new NotImplementedException();
    }
}

