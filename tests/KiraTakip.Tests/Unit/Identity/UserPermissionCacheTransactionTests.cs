using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Persistence;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos.Role;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Tenants;
using KiraTakip.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiraTakip.Tests;

public class TestLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Logs { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Logs.Add((logLevel, formatter(state, exception), exception));
    }
}

public class UserPermissionCacheTransactionUnitTests
{
    private class FakeUserPermissionCache : IUserPermissionCache
    {
        public List<string> InvalidatedUsers { get; } = [];
        public HashSet<string> FailingUsers { get; } = [];
        public bool ShouldThrowOnInvalidate { get; set; }

        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        }

        public void Invalidate(string userId)
        {
            if (ShouldThrowOnInvalidate || FailingUsers.Contains(userId))
                throw new InvalidOperationException($"Cache invalidation failed for {userId}");
            InvalidatedUsers.Add(userId);
        }

        public void InvalidateMany(IEnumerable<string> userIds)
        {
            foreach (var userId in userIds)
            {
                Invalidate(userId);
            }
        }
    }

    private static void SetTransactionId(object eventData, Guid txId)
    {
        var type = eventData.GetType();
        while (type != null && type != typeof(object))
        {
            var field = type.GetField("<TransactionId>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(eventData, txId);
                return;
            }
            type = type.BaseType;
        }
    }

    private static TransactionEndEventData CreateEndEventData(Guid txId)
    {
        var data = (TransactionEndEventData)RuntimeHelpers.GetUninitializedObject(typeof(TransactionEndEventData));
        SetTransactionId(data, txId);
        return data;
    }

    private static TransactionErrorEventData CreateErrorEventData(Guid txId)
    {
        var data = (TransactionErrorEventData)RuntimeHelpers.GetUninitializedObject(typeof(TransactionErrorEventData));
        SetTransactionId(data, txId);
        return data;
    }

    [Fact]
    public void TransactionState_AddAndRemove_TracksAndDeduplicates()
    {
        using var state = new PermissionCacheTransactionState();
        var txId = Guid.NewGuid();

        state.AddPending(txId, "u1");
        state.AddPending(txId, "u2");
        state.AddPending(txId, "u1"); // duplicate
        state.AddPendingRange(txId, ["u2", "u3", "u1", ""]);

        var removed = state.RemovePending(txId);
        Assert.Equal(3, removed.Count);
        Assert.Contains("u1", removed);
        Assert.Contains("u2", removed);
        Assert.Contains("u3", removed);

        // Subsequent remove should return empty
        Assert.Empty(state.RemovePending(txId));
    }

    [Fact]
    public void TransactionState_ClearPending_RemovesEntries()
    {
        using var state = new PermissionCacheTransactionState();
        var txId = Guid.NewGuid();

        state.AddPending(txId, "u1");
        state.ClearPending(txId);

        Assert.Empty(state.RemovePending(txId));
    }

    [Fact]
    public void TransactionState_ClearAllPending_RemovesAllEntriesAcrossTransactions()
    {
        using var state = new PermissionCacheTransactionState();
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();

        state.AddPending(tx1, "u1");
        state.AddPending(tx2, "u2");

        state.ClearAllPending();

        Assert.Empty(state.RemovePending(tx1));
        Assert.Empty(state.RemovePending(tx2));
    }

    [Fact]
    public void TransactionState_IsolatesDifferentTransactions()
    {
        using var state = new PermissionCacheTransactionState();
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();

        state.AddPending(tx1, "u1");
        state.AddPending(tx2, "u2");

        var u1 = state.RemovePending(tx1);
        Assert.Single(u1, "u1");

        var u2 = state.RemovePending(tx2);
        Assert.Single(u2, "u2");
    }

    [Fact]
    public void Interceptor_OnTransactionCommitted_CallsInvalidate_WithTrackedUsers()
    {
        var cache = new FakeUserPermissionCache();
        var state = new PermissionCacheTransactionState();
        var interceptor = new PermissionCacheTransactionInterceptor(cache, state);
        var txId = Guid.NewGuid();

        state.AddPending(txId, "user-abc");
        state.AddPending(txId, "user-xyz");

        var eventData = CreateEndEventData(txId);

        interceptor.TransactionCommitted(null!, eventData);

        Assert.Equal(2, cache.InvalidatedUsers.Count);
        Assert.Contains("user-abc", cache.InvalidatedUsers);
        Assert.Contains("user-xyz", cache.InvalidatedUsers);
    }

    [Fact]
    public void Interceptor_OnTransactionRolledBack_ClearsPending_WithoutInvalidation()
    {
        var cache = new FakeUserPermissionCache();
        var state = new PermissionCacheTransactionState();
        var interceptor = new PermissionCacheTransactionInterceptor(cache, state);
        var txId = Guid.NewGuid();

        state.AddPending(txId, "user-abc");

        var eventData = CreateEndEventData(txId);

        interceptor.TransactionRolledBack(null!, eventData);

        Assert.Empty(cache.InvalidatedUsers);
        Assert.Empty(state.RemovePending(txId));
    }

    [Fact]
    public void Interceptor_OnTransactionFailed_ClearsPending_WithoutInvalidation()
    {
        var cache = new FakeUserPermissionCache();
        var state = new PermissionCacheTransactionState();
        var interceptor = new PermissionCacheTransactionInterceptor(cache, state);
        var txId = Guid.NewGuid();

        state.AddPending(txId, "user-abc");

        var errorData = CreateErrorEventData(txId);

        interceptor.TransactionFailed(null!, errorData);

        Assert.Empty(cache.InvalidatedUsers);
        Assert.Empty(state.RemovePending(txId));
    }

    [Fact]
    public void Interceptor_TransactionStarting_ClearsAllPending()
    {
        var cache = new FakeUserPermissionCache();
        var state = new PermissionCacheTransactionState();
        var interceptor = new PermissionCacheTransactionInterceptor(cache, state);

        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        state.AddPending(tx1, "u1");
        state.AddPending(tx2, "u2");

        interceptor.TransactionStarting(null!, null!, default);

        Assert.Empty(state.RemovePending(tx1));
        Assert.Empty(state.RemovePending(tx2));
    }

    [Fact]
    public async Task Interceptor_TransactionStartingAsync_ClearsAllPending()
    {
        var cache = new FakeUserPermissionCache();
        var state = new PermissionCacheTransactionState();
        var interceptor = new PermissionCacheTransactionInterceptor(cache, state);

        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        state.AddPending(tx1, "u1");
        state.AddPending(tx2, "u2");

        await interceptor.TransactionStartingAsync(null!, null!, default);

        Assert.Empty(state.RemovePending(tx1));
        Assert.Empty(state.RemovePending(tx2));
    }

    [Fact]
    public void Interceptor_PostCommitFailure_OneUserThrows_DoesNotBlockOtherUsers_AndLogsErrorAndWarning()
    {
        var cache = new FakeUserPermissionCache();
        cache.FailingUsers.Add("user-fail");

        var state = new PermissionCacheTransactionState();
        var logger = new TestLogger<PermissionCacheTransactionInterceptor>();
        var interceptor = new PermissionCacheTransactionInterceptor(cache, state, logger);
        var txId = Guid.NewGuid();

        state.AddPending(txId, "user-fail");
        state.AddPending(txId, "user-success");

        var eventData = CreateEndEventData(txId);

        var ex = Record.Exception(() => interceptor.TransactionCommitted(null!, eventData));
        Assert.Null(ex);

        // user-success must still be invalidated despite user-fail throwing
        Assert.Single(cache.InvalidatedUsers, "user-success");
        Assert.DoesNotContain("user-fail", cache.InvalidatedUsers);

        // Logger should contain Error for failed user and Warning summary
        var errorLog = Assert.Single(logger.Logs, l => l.Level == LogLevel.Error);
        Assert.Contains("user-fail", errorLog.Message);
        Assert.Contains(txId.ToString(), errorLog.Message);

        var warningLog = Assert.Single(logger.Logs, l => l.Level == LogLevel.Warning);
        Assert.Contains("1/2", warningLog.Message);
        Assert.Contains("user-fail", warningLog.Message);
        Assert.Contains("user-success", warningLog.Message);
    }
}

public interface INestedTestService
{
    Task OuterAsync(string innerUser, string outerUser);
    Task InnerAsync(string innerUser);
}

public class NestedTestService : INestedTestService
{
    private readonly IUserPermissionCacheInvalidator _invalidator;
    private readonly UserPermissionCacheTransactionIntegrationTests.FakeUserPermissionCache _cache;
    public INestedTestService? SelfProxy { get; set; }

    public NestedTestService(
        IUserPermissionCacheInvalidator invalidator,
        UserPermissionCacheTransactionIntegrationTests.FakeUserPermissionCache cache)
    {
        _invalidator = invalidator;
        _cache = cache;
    }

    public async Task OuterAsync(string innerUser, string outerUser)
    {
        if (SelfProxy != null)
        {
            await SelfProxy.InnerAsync(innerUser);
        }

        // Inner has completed, but outer transaction is still open; cache must NOT be invalidated yet
        Assert.Empty(_cache.InvalidatedUsers);

        _invalidator.InvalidateAfterCommit(outerUser);
    }

    public Task InnerAsync(string innerUser)
    {
        _invalidator.InvalidateAfterCommit(innerUser);
        return Task.CompletedTask;
    }
}

[Collection("Database collection")]
public class UserPermissionCacheTransactionIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FakeUserPermissionCache _cache;
    private readonly ObservingTransactionState _state;
    private readonly PermissionCacheTransactionInterceptor _interceptor;
    private readonly UserPermissionCacheInvalidator _invalidator;

    public class FakeUserPermissionCache : IUserPermissionCache
    {
        public List<string> InvalidatedUsers { get; } = [];
        public HashSet<string> FailingUsers { get; } = [];
        public bool ShouldThrow { get; set; }

        public Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

        public void Invalidate(string userId)
        {
            if (ShouldThrow || FailingUsers.Contains(userId))
                throw new InvalidOperationException($"Simulated cache error for {userId}");
            InvalidatedUsers.Add(userId);
        }

        public void InvalidateMany(IEnumerable<string> userIds)
        {
            foreach (var userId in userIds)
            {
                Invalidate(userId);
            }
        }
    }

    /// <summary>
    /// Test-internal state decorator to observe pending transactions without mutating production state.
    /// </summary>
    public class ObservingTransactionState : IPermissionCacheTransactionState, IDisposable
    {
        private readonly PermissionCacheTransactionState _inner = new();
        private readonly Dictionary<Guid, HashSet<string>> _tracked = [];
        private readonly object _lock = new();

        public bool HasPending(Guid transactionId)
        {
            lock (_lock)
            {
                return _tracked.TryGetValue(transactionId, out var users) && users.Count > 0;
            }
        }

        public IReadOnlyCollection<string> GetPending(Guid transactionId)
        {
            lock (_lock)
            {
                return _tracked.TryGetValue(transactionId, out var users) ? users.ToList() : [];
            }
        }

        public void AddPending(Guid transactionId, string userId)
        {
            lock (_lock)
            {
                if (!_tracked.TryGetValue(transactionId, out var set))
                {
                    set = [];
                    _tracked[transactionId] = set;
                }
                set.Add(userId);
            }
            _inner.AddPending(transactionId, userId);
        }

        public void AddPendingRange(Guid transactionId, IEnumerable<string> userIds)
        {
            lock (_lock)
            {
                if (!_tracked.TryGetValue(transactionId, out var set))
                {
                    set = [];
                    _tracked[transactionId] = set;
                }
                foreach (var u in userIds) set.Add(u);
            }
            _inner.AddPendingRange(transactionId, userIds);
        }

        public IReadOnlyList<string> RemovePending(Guid transactionId)
        {
            lock (_lock)
            {
                _tracked.Remove(transactionId);
            }
            return _inner.RemovePending(transactionId);
        }

        public void ClearPending(Guid transactionId)
        {
            lock (_lock)
            {
                _tracked.Remove(transactionId);
            }
            _inner.ClearPending(transactionId);
        }

        public void ClearAllPending()
        {
            lock (_lock)
            {
                _tracked.Clear();
            }
            _inner.ClearAllPending();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _tracked.Clear();
            }
            _inner.Dispose();
        }
    }

    private class TestRetryableException : Exception
    {
        public TestRetryableException(string message) : base(message) { }
    }

    private class TestExecutionStrategy : ExecutionStrategy
    {
        public TestExecutionStrategy(DbContext context)
            : base(context, maxRetryCount: 2, maxRetryDelay: TimeSpan.FromMilliseconds(50))
        {
        }

        protected override bool ShouldRetryOn(Exception exception)
        {
            return exception is TestRetryableException;
        }
    }

    public UserPermissionCacheTransactionIntegrationTests(DatabaseFixture fixture)
    {
        _cache = new FakeUserPermissionCache();
        _state = new ObservingTransactionState();
        _interceptor = new PermissionCacheTransactionInterceptor(_cache, _state);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .AddInterceptors(_interceptor)
            .Options;

        _context = new ApplicationDbContext(
            options,
            new DummyHttpContextAccessor(),
            new DummyCurrentUserContext());

        _invalidator = new UserPermissionCacheInvalidator(_cache, _context, _state);
    }

    public void Dispose()
    {
        _context.Dispose();
        _state.Dispose();
    }

    [Fact]
    public async Task Transaction_WhenCommitted_InvalidatesCache()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userId = $"u-{suffix}";

        using var tx = await _context.Database.BeginTransactionAsync();

        _invalidator.InvalidateAfterCommit(userId);

        // Before commit: Cache must NOT be invalidated yet
        Assert.Empty(_cache.InvalidatedUsers);

        await tx.CommitAsync();

        // After commit: Cache MUST be invalidated
        Assert.Contains(userId, _cache.InvalidatedUsers);
    }

    [Fact]
    public async Task Transaction_WhenRolledBack_DoesNotInvalidateCache()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userId = $"u-{suffix}";

        using var tx = await _context.Database.BeginTransactionAsync();

        _invalidator.InvalidateAfterCommit(userId);

        Assert.Empty(_cache.InvalidatedUsers);

        await tx.RollbackAsync();

        // After rollback: Cache must NEVER be invalidated
        Assert.Empty(_cache.InvalidatedUsers);
    }

    [Fact]
    public void Transaction_WhenDisposedWithoutCommit_RetainsPendingUntilNextTransactionStarts_ThenClearsWithoutInvalidation()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var oldUser = $"old-{suffix}";
        var newUser = $"new-{suffix}";

        Guid txId;
        using (var tx = _context.Database.BeginTransaction())
        {
            txId = tx.TransactionId;
            _invalidator.InvalidateAfterCommit(oldUser);
            // Synchronous dispose without commit
        }

        // Known Limitation: EF Core does NOT invoke interceptor on uncommitted dispose; pending entries remain
        Assert.True(_state.HasPending(txId));
        Assert.Contains(oldUser, _state.GetPending(txId));
        Assert.Empty(_cache.InvalidatedUsers);

        // Next transaction starting hook (TransactionStarting) purges stale pending state
        using (var nextTx = _context.Database.BeginTransaction())
        {
            Assert.False(_state.HasPending(txId));
            _invalidator.InvalidateAfterCommit(newUser);
            nextTx.Commit();
        }

        // Only newUser is invalidated; oldUser was purged and is NEVER invalidated
        Assert.Contains(newUser, _cache.InvalidatedUsers);
        Assert.DoesNotContain(oldUser, _cache.InvalidatedUsers);
    }

    [Fact]
    public async Task Transaction_WhenDisposedAsyncWithoutCommit_RetainsPendingUntilNextTransactionStarts_ThenClearsWithoutInvalidation()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var oldUser = $"old-async-{suffix}";
        var newUser = $"new-async-{suffix}";

        Guid txId;
        await using (var tx = await _context.Database.BeginTransactionAsync())
        {
            txId = tx.TransactionId;
            _invalidator.InvalidateAfterCommit(oldUser);
            // Asynchronous dispose without commit
        }

        // Known Limitation: EF Core does NOT invoke interceptor on uncommitted disposeAsync; pending entries remain
        Assert.True(_state.HasPending(txId));
        Assert.Contains(oldUser, _state.GetPending(txId));
        Assert.Empty(_cache.InvalidatedUsers);

        // Next transaction starting hook (TransactionStartingAsync) purges stale pending state
        await using (var nextTx = await _context.Database.BeginTransactionAsync())
        {
            Assert.False(_state.HasPending(txId));
            _invalidator.InvalidateAfterCommit(newUser);
            await nextTx.CommitAsync();
        }

        // Only newUser is invalidated; oldUser was purged and is NEVER invalidated
        Assert.Contains(newUser, _cache.InvalidatedUsers);
        Assert.DoesNotContain(oldUser, _cache.InvalidatedUsers);
    }

    [Fact]
    public async Task ExecutionStrategy_AutomaticRetry_PurgesPendingFromFailedAttempt_AndOnlyInvalidatesCommittedAttempt()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var attempt1User = $"att1-{suffix}";
        var attempt2User = $"att2-{suffix}";

        var strategy = new TestExecutionStrategy(_context);
        var attemptCount = 0;
        Guid attempt1TxId = Guid.Empty;
        Guid attempt2TxId = Guid.Empty;

        await strategy.ExecuteAsync(async () =>
        {
            attemptCount++;

            if (attemptCount == 1)
            {
                // Attempt 1: Begin transaction, register pending invalidation, then fail before commit
                await using var tx1 = await _context.Database.BeginTransactionAsync();
                attempt1TxId = tx1.TransactionId;
                _invalidator.InvalidateAfterCommit(attempt1User);

                // Controlled retryable exception before commit
                throw new TestRetryableException("Controlled failure in attempt 1 to trigger automatic retry");
            }

            // Attempt 2: Automatically executed by ExecutionStrategy
            await using var tx2 = await _context.Database.BeginTransactionAsync();
            attempt2TxId = tx2.TransactionId;
            _invalidator.InvalidateAfterCommit(attempt2User);
            await tx2.CommitAsync();
        });

        // 1. Verify that the strategy executed exactly 2 attempts automatically
        Assert.Equal(2, attemptCount);
        Assert.NotEqual(Guid.Empty, attempt1TxId);
        Assert.NotEqual(Guid.Empty, attempt2TxId);
        Assert.NotEqual(attempt1TxId, attempt2TxId);

        // 2. Verify that attempt 1 user was NOT invalidated, and attempt 2 user WAS invalidated
        Assert.DoesNotContain(attempt1User, _cache.InvalidatedUsers);
        Assert.Contains(attempt2User, _cache.InvalidatedUsers);

        // 3. Verify that old pending records for attempt 1 were purged
        Assert.False(_state.HasPending(attempt1TxId));
        Assert.False(_state.HasPending(attempt2TxId));
    }

    [Fact]
    public async Task TransactionalProxy_NestedCalls_OnlyOuterCommitInvalidatesCache()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var innerUser = $"inner-{suffix}";
        var outerUser = $"outer-{suffix}";

        var generator = new ProxyGenerator();
        var txInterceptor = new TransactionInterceptor(_context, NullLogger<TransactionInterceptor>.Instance);
        var target = new NestedTestService(_invalidator, _cache);
        var proxy = generator.CreateInterfaceProxyWithTarget<INestedTestService>(target, txInterceptor);
        target.SelfProxy = proxy;

        // Calling outer through transactional proxy
        await proxy.OuterAsync(innerUser, outerUser);

        // After outer transaction completes and commits: both users should now be invalidated
        Assert.Equal(2, _cache.InvalidatedUsers.Count);
        Assert.Contains(innerUser, _cache.InvalidatedUsers);
        Assert.Contains(outerUser, _cache.InvalidatedUsers);
    }

    [Fact]
    public async Task Commit_WithRealCacheService_InvalidatesCache_AndNextQueryReloadsFreshPermissions()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IUserRoleRepository>(_ => new UserRoleRepository(_context));
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var realCache = new UserPermissionCacheService(sp.GetRequiredService<IMemoryCache>(), scopeFactory);

        using var state = new PermissionCacheTransactionState();
        var interceptor = new PermissionCacheTransactionInterceptor(realCache, state);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_context.Database.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        using var ctx = new ApplicationDbContext(options, new DummyHttpContextAccessor(), new DummyCurrentUserContext());
        var invalidator = new UserPermissionCacheInvalidator(realCache, ctx, state);
        var uow = new UnitOfWork(ctx);
        var userRoleService = new UserRoleService(
            new UserRoleRepository(ctx),
            new RoleRepository(ctx),
            new ApplicationUserRepository(ctx),
            uow,
            invalidator);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var admin = new ApplicationUser
        {
            Id = $"adm-{suffix}",
            UserName = $"adm-{suffix}@test.com",
            Email = $"adm-{suffix}@test.com",
            UserType = UserType.Internal,
            IsSuperAdmin = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var user = new ApplicationUser
        {
            Id = $"usr-{suffix}",
            UserName = $"usr-{suffix}@test.com",
            Email = $"usr-{suffix}@test.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var role1 = new Role { Name = $"R1-{suffix}", Scope = RoleScope.Internal, IsActive = true };
        var role2 = new Role { Name = $"R2-{suffix}", Scope = RoleScope.Internal, IsActive = true };

        ctx.Users.AddRange(admin, user);
        ctx.Roller.AddRange(role1, role2);
        await ctx.SaveChangesAsync();

        ctx.RolPermissions.Add(new RolePermission { RoleId = role1.Id, Permission = PermissionCatalog.Property.Module });
        ctx.RolPermissions.Add(new RolePermission { RoleId = role2.Id, Permission = PermissionCatalog.Property.Create });
        ctx.UserRoller.Add(new UserRole { UserId = user.Id, RoleId = role1.Id });
        await ctx.SaveChangesAsync();

        try
        {
            // 1. Initial read populates cache with Property.Module
            var perms1 = await realCache.GetAsync(user.Id);
            Assert.Contains(PermissionCatalog.Property.Module, perms1);
            Assert.DoesNotContain(PermissionCatalog.Property.Create, perms1);

            // 2. Begin transaction, add role2
            using (var tx = await ctx.Database.BeginTransactionAsync())
            {
                await userRoleService.AddRoleByNameAsync(user.Id, role2.Name, admin.Id);

                // Mid-transaction before commit: cache still returns cached old permissions
                var permsMidTx = await realCache.GetAsync(user.Id);
                Assert.Contains(PermissionCatalog.Property.Module, permsMidTx);
                Assert.DoesNotContain(PermissionCatalog.Property.Create, permsMidTx);

                await tx.CommitAsync();
            }

            // 3. After commit, cache was invalidated, next GetAsync reloads updated permissions from DB
            var permsAfterCommit = await realCache.GetAsync(user.Id);
            Assert.Contains(PermissionCatalog.Property.Module, permsAfterCommit);
            Assert.Contains(PermissionCatalog.Property.Create, permsAfterCommit);
        }
        finally
        {
            ctx.ChangeTracker.Clear();
            var userRoles = await ctx.UserRoller.Where(ur => ur.UserId == user.Id).ToListAsync();
            ctx.UserRoller.RemoveRange(userRoles);
            var rolePerms = await ctx.RolPermissions.Where(rp => rp.RoleId == role1.Id || rp.RoleId == role2.Id).ToListAsync();
            ctx.RolPermissions.RemoveRange(rolePerms);
            var roles = await ctx.Roller.Where(r => r.Id == role1.Id || r.Id == role2.Id).ToListAsync();
            ctx.Roller.RemoveRange(roles);
            var users = await ctx.Users.Where(u => u.Id == admin.Id || u.Id == user.Id).ToListAsync();
            ctx.Users.RemoveRange(users);
            await ctx.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Rollback_WithRealCacheService_PreservesExistingCache()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IUserRoleRepository>(_ => new UserRoleRepository(_context));
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var realCache = new UserPermissionCacheService(sp.GetRequiredService<IMemoryCache>(), scopeFactory);

        using var state = new PermissionCacheTransactionState();
        var interceptor = new PermissionCacheTransactionInterceptor(realCache, state);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_context.Database.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        using var ctx = new ApplicationDbContext(options, new DummyHttpContextAccessor(), new DummyCurrentUserContext());
        var invalidator = new UserPermissionCacheInvalidator(realCache, ctx, state);
        var uow = new UnitOfWork(ctx);
        var userRoleService = new UserRoleService(
            new UserRoleRepository(ctx),
            new RoleRepository(ctx),
            new ApplicationUserRepository(ctx),
            uow,
            invalidator);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var admin = new ApplicationUser
        {
            Id = $"adm-{suffix}",
            UserName = $"adm-{suffix}@test.com",
            Email = $"adm-{suffix}@test.com",
            UserType = UserType.Internal,
            IsSuperAdmin = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var user = new ApplicationUser
        {
            Id = $"usr-{suffix}",
            UserName = $"usr-{suffix}@test.com",
            Email = $"usr-{suffix}@test.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var role1 = new Role { Name = $"R1-{suffix}", Scope = RoleScope.Internal, IsActive = true };
        var role2 = new Role { Name = $"R2-{suffix}", Scope = RoleScope.Internal, IsActive = true };

        ctx.Users.AddRange(admin, user);
        ctx.Roller.AddRange(role1, role2);
        await ctx.SaveChangesAsync();

        ctx.RolPermissions.Add(new RolePermission { RoleId = role1.Id, Permission = PermissionCatalog.Property.Module });
        ctx.RolPermissions.Add(new RolePermission { RoleId = role2.Id, Permission = PermissionCatalog.Property.Create });
        ctx.UserRoller.Add(new UserRole { UserId = user.Id, RoleId = role1.Id });
        await ctx.SaveChangesAsync();

        try
        {
            // Populate cache
            var perms = await realCache.GetAsync(user.Id);
            Assert.Single(perms, PermissionCatalog.Property.Module);

            using (var tx = await ctx.Database.BeginTransactionAsync())
            {
                await userRoleService.AddRoleByNameAsync(user.Id, role2.Name, admin.Id);
                await tx.RollbackAsync();
            }

            // Cache must still hold the existing cached permissions without change
            var permsAfterRollback = await realCache.GetAsync(user.Id);
            Assert.Single(permsAfterRollback, PermissionCatalog.Property.Module);
        }
        finally
        {
            ctx.ChangeTracker.Clear();
            var userRoles = await ctx.UserRoller.Where(ur => ur.UserId == user.Id).ToListAsync();
            ctx.UserRoller.RemoveRange(userRoles);
            var rolePerms = await ctx.RolPermissions.Where(rp => rp.RoleId == role1.Id || rp.RoleId == role2.Id).ToListAsync();
            ctx.RolPermissions.RemoveRange(rolePerms);
            var roles = await ctx.Roller.Where(r => r.Id == role1.Id || r.Id == role2.Id).ToListAsync();
            ctx.Roller.RemoveRange(roles);
            var users = await ctx.Users.Where(u => u.Id == admin.Id || u.Id == user.Id).ToListAsync();
            ctx.Users.RemoveRange(users);
            await ctx.SaveChangesAsync();
        }
    }

    [Fact]
    public void NonTransactional_InvalidatesCacheImmediately()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userId = $"u-{suffix}";

        Assert.Null(_context.Database.CurrentTransaction);

        _invalidator.InvalidateAfterCommit(userId);

        // Without transaction: Invalidates immediately
        Assert.Single(_cache.InvalidatedUsers, userId);
    }

    [Fact]
    public void NonTransactional_InvalidateMany_InvalidatesCacheImmediately()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var u1 = $"u1-{suffix}";
        var u2 = $"u2-{suffix}";

        Assert.Null(_context.Database.CurrentTransaction);

        _invalidator.InvalidateManyAfterCommit([u1, u2]);

        Assert.Equal(2, _cache.InvalidatedUsers.Count);
        Assert.Contains(u1, _cache.InvalidatedUsers);
        Assert.Contains(u2, _cache.InvalidatedUsers);
    }

    [Fact]
    public async Task RoleService_SetRolePermissionsAsync_InvalidatesAllUsersInRole_AndPreservesUnrelatedUsers_AfterCommit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role = new Role
        {
            Name = $"TestRole-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        var otherRole = new Role
        {
            Name = $"OtherRole-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        _context.Roller.AddRange(role, otherRole);

        var u1 = new ApplicationUser
        {
            Id = $"u1-{suffix}",
            UserName = $"u1-{suffix}@example.com",
            Email = $"u1-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var u2 = new ApplicationUser
        {
            Id = $"u2-{suffix}",
            UserName = $"u2-{suffix}@example.com",
            Email = $"u2-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var unrelatedUser = new ApplicationUser
        {
            Id = $"u3-{suffix}",
            UserName = $"u3-{suffix}@example.com",
            Email = $"u3-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        _context.Users.AddRange(u1, u2, unrelatedUser);
        await _context.SaveChangesAsync();

        _context.UserRoller.AddRange(
            new UserRole { UserId = u1.Id, RoleId = role.Id },
            new UserRole { UserId = u2.Id, RoleId = role.Id },
            new UserRole { UserId = unrelatedUser.Id, RoleId = otherRole.Id });
        await _context.SaveChangesAsync();

        var roleRepo = new RoleRepository(_context);
        var rolePermissionRepo = new RolePermissionRepository(_context);
        var userRoleRepo = new UserRoleRepository(_context);
        var uow = new UnitOfWork(_context);

        var roleService = new RoleService(
            roleRepo,
            rolePermissionRepo,
            userRoleRepo,
            new NoOpAuditService(),
            new NoOpUserSecurityService(),
            null!,
            _invalidator,
            uow);

        try
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(
                role.Id,
                [PermissionCatalog.Property.Module, PermissionCatalog.Property.Create],
                u1.Id));

            // Before commit: Cache not invalidated
            Assert.Empty(_cache.InvalidatedUsers);

            await tx.CommitAsync();

            // After commit: Both role users must be invalidated, unrelated user must NOT be invalidated
            Assert.Equal(2, _cache.InvalidatedUsers.Count);
            Assert.Contains(u1.Id, _cache.InvalidatedUsers);
            Assert.Contains(u2.Id, _cache.InvalidatedUsers);
            Assert.DoesNotContain(unrelatedUser.Id, _cache.InvalidatedUsers);
        }
        finally
        {
            _context.UserRoller.RemoveRange(_context.UserRoller.Where(ur => ur.RoleId == role.Id || ur.RoleId == otherRole.Id));
            _context.RolPermissions.RemoveRange(_context.RolPermissions.Where(rp => rp.RoleId == role.Id || rp.RoleId == otherRole.Id));
            _context.Users.RemoveRange(u1, u2, unrelatedUser);
            _context.Roller.RemoveRange(role, otherRole);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task UserRoleService_AddRole_RemoveRole_RemoveAllRoles_InvalidatesTargetUser_AfterCommit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role1 = new Role
        {
            Name = $"Role1-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        var role2 = new Role
        {
            Name = $"Role2-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        var admin = new ApplicationUser
        {
            Id = $"admin-{suffix}",
            UserName = $"admin-{suffix}@example.com",
            Email = $"admin-{suffix}@example.com",
            UserType = UserType.Internal,
            IsSuperAdmin = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var targetUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        _context.Roller.AddRange(role1, role2);
        _context.Users.AddRange(admin, targetUser);
        await _context.SaveChangesAsync();

        var service = new UserRoleService(
            new UserRoleRepository(_context),
            new RoleRepository(_context),
            new ApplicationUserRepository(_context),
            new UnitOfWork(_context),
            _invalidator);

        try
        {
            // 1. AddRoleByNameAsync
            _cache.InvalidatedUsers.Clear();
            using (var tx1 = await _context.Database.BeginTransactionAsync())
            {
                await service.AddRoleByNameAsync(targetUser.Id, role1.Name, admin.Id);
                Assert.Empty(_cache.InvalidatedUsers);
                await tx1.CommitAsync();
            }
            Assert.Contains(targetUser.Id, _cache.InvalidatedUsers);

            // Add second role to prepare for remove tests
            await service.AddRoleByNameAsync(targetUser.Id, role2.Name, admin.Id);

            // 2. RemoveRoleByNameAsync
            _cache.InvalidatedUsers.Clear();
            using (var tx2 = await _context.Database.BeginTransactionAsync())
            {
                await service.RemoveRoleByNameAsync(targetUser.Id, role1.Name);
                Assert.Empty(_cache.InvalidatedUsers);
                await tx2.CommitAsync();
            }
            Assert.Contains(targetUser.Id, _cache.InvalidatedUsers);

            // 3. RemoveAllRolesAsync
            _cache.InvalidatedUsers.Clear();
            using (var tx3 = await _context.Database.BeginTransactionAsync())
            {
                await service.RemoveAllRolesAsync(targetUser.Id);
                Assert.Empty(_cache.InvalidatedUsers);
                await tx3.CommitAsync();
            }
            Assert.Contains(targetUser.Id, _cache.InvalidatedUsers);
        }
        finally
        {
            _context.UserRoller.RemoveRange(_context.UserRoller.Where(ur => ur.UserId == targetUser.Id));
            _context.Users.RemoveRange(admin, targetUser);
            _context.Roller.RemoveRange(role1, role2);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task PostCommitFailure_DoesNotFailTransaction_AndOtherUsersStillInvalidated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userFail = $"fail-{suffix}";
        var userOk = $"ok-{suffix}";

        _cache.FailingUsers.Add(userFail);

        using var tx = await _context.Database.BeginTransactionAsync();
        _invalidator.InvalidateAfterCommit(userFail);
        _invalidator.InvalidateAfterCommit(userOk);

        // Commit should succeed even though userFail throws
        var ex = await Record.ExceptionAsync(() => tx.CommitAsync());
        Assert.Null(ex);

        // userOk must still be invalidated despite userFail failing
        Assert.Contains(userOk, _cache.InvalidatedUsers);
        Assert.DoesNotContain(userFail, _cache.InvalidatedUsers);
    }

    [Fact]
    public async Task TenantUserService_EditTenantUserAsync_WhenRoleChanges_InvalidatesCache_AfterCommit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var role1 = new Role
        {
            Name = $"Role1-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsActive = true
        };
        var role2 = new Role
        {
            Name = $"Role2-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsActive = true
        };
        _context.Roller.AddRange(role1, role2);

        var actor = new ApplicationUser
        {
            Id = $"actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Internal,
            IsSuperAdmin = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var targetUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            AdSoyad = "Target User",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            TumTasinmazlaraErisim = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        _context.Users.AddRange(actor, targetUser);
        await _context.SaveChangesAsync();

        _context.UserRoller.Add(new UserRole { UserId = targetUser.Id, RoleId = role1.Id });
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            new UnitRepository(_context),
            new UserPermissionScopeRepository(_context),
            new NoOpApplicationUserManager(),
            null!,
            new NoOpAuditService(),
            new NoOpUserSecurityService(),
            new NoOpPermissionScopeCache(),
            _invalidator,
            new UnitOfWork(_context));

        try
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            // Change from role1 to role2
            await service.EditTenantUserAsync(new Models.Dtos.TenantUser.EditTenantUserInput(
                tenant.Id,
                targetUser.Id,
                "Target User Updated",
                role2.Id,
                true,
                [],
                actor.Id,
                new Models.Dtos.Reservation.ReservationAccessScopeInput()));

            // Before commit
            Assert.Empty(_cache.InvalidatedUsers);

            await tx.CommitAsync();

            // After commit: cache invalidated
            Assert.Contains(targetUser.Id, _cache.InvalidatedUsers);
        }
        finally
        {
            _context.UserRoller.RemoveRange(_context.UserRoller.Where(ur => ur.UserId == targetUser.Id));
            _context.Users.RemoveRange(actor, targetUser);
            _context.Roller.RemoveRange(role1, role2);
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task TenantUserService_EditTenantUserAsync_WhenRoleUnchanged_DoesNotInvalidateCache()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var role1 = new Role
        {
            Name = $"Role1-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsActive = true
        };
        _context.Roller.Add(role1);

        var actor = new ApplicationUser
        {
            Id = $"actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Internal,
            IsSuperAdmin = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var targetUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            AdSoyad = "Target User",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            TumTasinmazlaraErisim = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        _context.Users.AddRange(actor, targetUser);
        await _context.SaveChangesAsync();

        _context.UserRoller.Add(new UserRole { UserId = targetUser.Id, RoleId = role1.Id });
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            new UnitRepository(_context),
            new UserPermissionScopeRepository(_context),
            new NoOpApplicationUserManager(),
            null!,
            new NoOpAuditService(),
            new NoOpUserSecurityService(),
            new NoOpPermissionScopeCache(),
            _invalidator,
            new UnitOfWork(_context));

        try
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            // Same role1
            await service.EditTenantUserAsync(new Models.Dtos.TenantUser.EditTenantUserInput(
                tenant.Id,
                targetUser.Id,
                "Target User Same Role",
                role1.Id,
                true,
                [],
                actor.Id,
                new Models.Dtos.Reservation.ReservationAccessScopeInput()));

            await tx.CommitAsync();

            // Role unchanged -> cache must NOT be invalidated
            Assert.Empty(_cache.InvalidatedUsers);
        }
        finally
        {
            _context.UserRoller.RemoveRange(_context.UserRoller.Where(ur => ur.UserId == targetUser.Id));
            _context.Users.RemoveRange(actor, targetUser);
            _context.Roller.Remove(role1);
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
        }
    }
}
