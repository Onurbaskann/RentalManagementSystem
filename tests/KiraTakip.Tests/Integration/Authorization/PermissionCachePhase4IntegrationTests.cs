using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Security.Claims;
using Castle.DynamicProxy;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Persistence;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Infrastructure.Identity;
using KiraTakip.Models.Dtos.Role;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KiraTakip.Tests;

public sealed class RepositoryReadCounter
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);
    public void Increment(string userId) => _counts.AddOrUpdate(userId, 1, (_, c) => c + 1);
    public int GetCount(string userId) => _counts.GetValueOrDefault(userId, 0);
    public void Reset() => _counts.Clear();
}

public sealed class CountingUserRoleRepository(IUserRoleRepository inner, RepositoryReadCounter counter) : IUserRoleRepository
{
    public Task<List<string>> GetPermissionsAsync(string userId, CancellationToken ct = default)
    {
        counter.Increment(userId);
        return inner.GetPermissionsAsync(userId, ct);
    }

    public Task<int> CountUsersInRoleAsync(int roleId) => inner.CountUsersInRoleAsync(roleId);
    public Task<int> CountUsersInRoleForTenantAsync(int roleId, int tenantId) => inner.CountUsersInRoleForTenantAsync(roleId, tenantId);
    public Task<bool> HasAnyUsersInRoleAsync(int roleId) => inner.HasAnyUsersInRoleAsync(roleId);
    public Task<int?> GetFirstRoleIdAsync(string userId, CancellationToken ct = default) => inner.GetFirstRoleIdAsync(userId, ct);
    public Task<(int RoleId, string RoleName)?> GetUserRoleInfoAsync(string userId, CancellationToken ct = default) => inner.GetUserRoleInfoAsync(userId, ct);
    public Task<List<string>> GetRoleNamesAsync(string userId, CancellationToken ct = default) => inner.GetRoleNamesAsync(userId, ct);
    public Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken ct = default) => inner.IsInRoleAsync(userId, roleName, ct);
    public Task<UserRole?> GetByUserAndRoleNameAsync(string userId, string roleName, CancellationToken ct = default) => inner.GetByUserAndRoleNameAsync(userId, roleName, ct);
    public Task<List<UserRole>> GetAllByUserIgnoringFiltersAsync(string userId, CancellationToken ct = default) => inner.GetAllByUserIgnoringFiltersAsync(userId, ct);
    public Task<List<string>> GetUserIdsByRoleNameAsync(string roleName, CancellationToken ct = default) => inner.GetUserIdsByRoleNameAsync(roleName, ct);
    public Task<bool> ExistsIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default) => inner.ExistsIgnoringFiltersAsync(userId, roleId, ct);
    public Task<UserRole?> GetByUserAndRoleIdIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default) => inner.GetByUserAndRoleIdIgnoringFiltersAsync(userId, roleId, ct);
    public Task<List<string>> GetUserIdsByRoleIdAsync(int roleId, CancellationToken ct = default) => inner.GetUserIdsByRoleIdAsync(roleId, ct);
    public void RemoveRange(IEnumerable<UserRole> userRoles) => inner.RemoveRange(userRoles);
    public Task AddAsync(UserRole entity) => inner.AddAsync(entity);
    public Task UpdateAsync(UserRole entity) => inner.UpdateAsync(entity);
    public Task DeleteAsync(int id, bool hardDelete = false) => inner.DeleteAsync(id, hardDelete);
    public Task<UserRole?> GetByIdAsync(int id, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => inner.GetByIdAsync(id, include);
    public Task<UserRole?> GetAsync(Expression<Func<UserRole, bool>> predicate, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => inner.GetAsync(predicate, include);
    public Task<List<UserRole>> GetAllAsync(Expression<Func<UserRole, bool>>? filter = null, Func<IQueryable<UserRole>, IQueryable<UserRole>>? include = null) => inner.GetAllAsync(filter, include);
    public Task<TResult?> GetByIdAsync<TResult>(int id, Expression<Func<UserRole, TResult>> selector) => inner.GetByIdAsync(id, selector);
    public Task<TResult?> GetAsync<TResult>(Expression<Func<UserRole, bool>> predicate, Expression<Func<UserRole, TResult>> selector) => inner.GetAsync(predicate, selector);
    public Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<UserRole, bool>>? filter, Expression<Func<UserRole, TResult>> selector) => inner.GetAllAsync(filter, selector);
    public Task<bool> AnyAsync(Expression<Func<UserRole, bool>> predicate) => inner.AnyAsync(predicate);
    public Task<int> CountAsync(Expression<Func<UserRole, bool>>? filter = null) => inner.CountAsync(filter);
}

[Collection("Database collection")]
public class PermissionCachePhase4IntegrationTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];
    private readonly List<string> _createdUserIds = [];
    private readonly List<int> _createdRoleIds = [];

    public PermissionCachePhase4IntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public void Dispose()
    {
        // Cleanup test data from KiraTakipDb_Test
        try
        {
            using var context = _fixture.CreateContext();
            if (_createdUserIds.Count > 0)
            {
                var userRoles = context.UserRoller.Where(ur => _createdUserIds.Contains(ur.UserId)).ToList();
                if (userRoles.Count > 0) context.UserRoller.RemoveRange(userRoles);

                var users = context.Users.Where(u => _createdUserIds.Contains(u.Id)).ToList();
                if (users.Count > 0) context.Users.RemoveRange(users);
            }

            if (_createdRoleIds.Count > 0)
            {
                var rolePerms = context.RolPermissions.Where(rp => _createdRoleIds.Contains(rp.RoleId)).ToList();
                if (rolePerms.Count > 0) context.RolPermissions.RemoveRange(rolePerms);

                var roles = context.Roller.Where(r => _createdRoleIds.Contains(r.Id)).ToList();
                if (roles.Count > 0) context.Roller.RemoveRange(roles);
            }

            context.SaveChanges();
        }
        catch
        {
            // Suppress cleanup exceptions in Dispose
        }
    }

    private ServiceProvider CreateHostProvider(IMemoryCache? sharedMemoryCache = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // 1. Shared MemoryCache (singleton representing the application instance)
        if (sharedMemoryCache != null)
        {
            services.AddSingleton(sharedMemoryCache);
        }
        else
        {
            services.AddMemoryCache();
        }

        // 2. Transaction interceptor & state
        services.AddScoped<IPermissionCacheTransactionState, PermissionCacheTransactionState>();
        services.AddScoped<PermissionCacheTransactionInterceptor>();

        // 3. Database Context using DatabaseFixture connection string
        services.AddScoped<ICurrentUserContext, DummyCurrentUserContext>();
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(_fixture.ConnectionString);
            options.AddInterceptors(sp.GetRequiredService<PermissionCacheTransactionInterceptor>());
        });

        // 4. Cache & Invalidator
        services.AddSingleton<IUserPermissionCache, UserPermissionCacheService>();
        services.AddScoped<IUserPermissionCacheInvalidator, UserPermissionCacheInvalidator>();

        // 5. Repositories
        services.AddSingleton<RepositoryReadCounter>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<UserRoleRepository>();
        services.AddScoped<IUserRoleRepository>(sp =>
            new CountingUserRoleRepository(sp.GetRequiredService<UserRoleRepository>(), sp.GetRequiredService<RepositoryReadCounter>()));
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditService, NoOpAuditService>();
        services.AddScoped<IUserSecurityService, NoOpUserSecurityService>();

        // 6. DynamicProxy TransactionInterceptor for transactional services
        services.AddScoped<TransactionInterceptor>();
        services.AddScoped<IRoleService>(sp =>
        {
            var target = new RoleService(
                sp.GetRequiredService<IRoleRepository>(),
                sp.GetRequiredService<IRolePermissionRepository>(),
                sp.GetRequiredService<IUserRoleRepository>(),
                sp.GetRequiredService<IAuditService>(),
                sp.GetRequiredService<IUserSecurityService>(),
                null!,
                sp.GetRequiredService<IUserPermissionCacheInvalidator>(),
                sp.GetRequiredService<IUnitOfWork>());
            var txInterceptor = sp.GetRequiredService<TransactionInterceptor>();
            return new ProxyGenerator().CreateInterfaceProxyWithTarget<IRoleService>(target, txInterceptor.ToInterceptor());
        });

        services.AddScoped<IUserRoleService>(sp =>
        {
            var target = new UserRoleService(
                sp.GetRequiredService<IUserRoleRepository>(),
                sp.GetRequiredService<IRoleRepository>(),
                sp.GetRequiredService<IApplicationUserRepository>(),
                sp.GetRequiredService<IUnitOfWork>(),
                sp.GetRequiredService<IUserPermissionCacheInvalidator>());
            var txInterceptor = sp.GetRequiredService<TransactionInterceptor>();
            return new ProxyGenerator().CreateInterfaceProxyWithTarget<IUserRoleService>(target, txInterceptor.ToInterceptor());
        });

        // 7. Request-scoped Authorization Pipeline
        services.AddScoped<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUserPermissionContext, CurrentUserPermissionContext>();
        services.AddScoped<ICurrentUserPermissionService, CurrentUserPermissionService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AdminBypassHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var m in PermissionCatalog.AllModules)
            {
                var modulePath = m.Path;
                options.AddPolicy(modulePath, policy => policy.AddRequirements(new PermissionRequirement(modulePath)));
                foreach (var action in m.Actions)
                {
                    var actionPath = action;
                    options.AddPolicy(actionPath, policy => policy.AddRequirements(new PermissionRequirement(actionPath)));
                }
            }
            options.AddPolicy("TenantUser", policy => policy.RequireClaim(AppClaimTypes.UserType, ((int)UserType.Tenant).ToString()));
        });

        return services.BuildServiceProvider();
    }

    private async Task<(ApplicationUser User1, ApplicationUser UnrelatedUser, Role Role1, Role UnrelatedRole)> SeedTestDataAsync()
    {
        using var context = _fixture.CreateContext();

        var u1 = new ApplicationUser
        {
            Id = $"u1-{_suffix}",
            UserName = $"user1-{_suffix}@example.com",
            Email = $"user1-{_suffix}@example.com",
            UserType = UserType.Internal,
            IsSuperAdmin = false,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var u2 = new ApplicationUser
        {
            Id = $"u2-{_suffix}",
            UserName = $"user2-{_suffix}@example.com",
            Email = $"user2-{_suffix}@example.com",
            UserType = UserType.Internal,
            IsSuperAdmin = false,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var role1 = new Role
        {
            Name = $"Role1-{_suffix}",
            Description = "Phase 4 Test Role 1",
            Scope = RoleScope.Internal,
            IsSystemRole = false,
            IsActive = true
        };
        var role2 = new Role
        {
            Name = $"Role2-{_suffix}",
            Description = "Phase 4 Test Role 2",
            Scope = RoleScope.Internal,
            IsSystemRole = false,
            IsActive = true
        };

        context.Users.AddRange(u1, u2);
        context.Roller.AddRange(role1, role2);
        await context.SaveChangesAsync();

        _createdUserIds.Add(u1.Id);
        _createdUserIds.Add(u2.Id);
        _createdRoleIds.Add(role1.Id);
        _createdRoleIds.Add(role2.Id);

        // Assign initial permissions:
        // Role 1 has Property.Module + Property.Edit
        context.RolPermissions.AddRange(
            new RolePermission { RoleId = role1.Id, Permission = PermissionCatalog.Property.Module },
            new RolePermission { RoleId = role1.Id, Permission = PermissionCatalog.Property.Edit },
            // Role 2 has Tenant.Module
            new RolePermission { RoleId = role2.Id, Permission = PermissionCatalog.Tenant.Module });

        // Assign roles to users
        context.UserRoller.AddRange(
            new UserRole { UserId = u1.Id, RoleId = role1.Id },
            new UserRole { UserId = u2.Id, RoleId = role2.Id });

        await context.SaveChangesAsync();

        return (u1, u2, role1, role2);
    }

    private static ClaimsPrincipal CreatePrincipal(ApplicationUser user)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? "test"),
            new Claim(AppClaimTypes.UserType, ((int)user.UserType).ToString()),
            new Claim("IsSuperAdmin", user.IsSuperAdmin ? "true" : "false")
        ], "TestCookieAuth"));
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // 1. YETKÄ° DEÄÄ°ÅÄ°KLÄ°ÄÄ°NÄ°N SONRAKÄ° Ä°STEÄE YANSIMASI
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [Fact]
    public async Task EndToEnd_PermissionChange_ReflectsInNextScope_AndPreservesCurrentSnapshot()
    {
        var (u1, u2, role1, role2) = await SeedTestDataAsync();

        // Share single IMemoryCache across all request scopes (representing the single app instance)
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var hostProvider = CreateHostProvider(memoryCache);

        var p1 = CreatePrincipal(u1);
        var p2 = CreatePrincipal(u2);

        // â”€â”€ SCOPE 1: Initial request by User 1 â”€â”€
        using (var scope1 = hostProvider.CreateScope())
        {
            scope1.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope1.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var canEdit = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(canEdit.Succeeded, "Scope 1: User 1 must initially have Property.Edit permission");

            // â”€â”€ SCOPE 2: Admin removes Property.Edit from Role 1 and commits â”€â”€
            using (var scope2 = hostProvider.CreateScope())
            {
                var roleService = scope2.ServiceProvider.GetRequiredService<IRoleService>();
                // Keep only Property.Module, remove Property.Edit
                await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(
                    role1.Id,
                    [PermissionCatalog.Property.Module],
                    "admin-changer"));
            }

            // â”€â”€ VERIFY SNAPSHOT IN SCOPE 1: Same request snapshot must be preserved! â”€â”€
            var stillCanEditInScope1 = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(stillCanEditInScope1.Succeeded, "Scope 1: In-flight request snapshot must NOT be mutated mid-request");
        }

        // â”€â”€ SCOPE 3: New request by User 1 -> Access must now be DENIED! â”€â”€
        using (var scope3 = hostProvider.CreateScope())
        {
            scope3.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope3.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var canEdit = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.False(canEdit.Succeeded, "Scope 3: User 1 must now be DENIED Property.Edit");

            var canViewModule = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Module);
            Assert.True(canViewModule.Succeeded, "Scope 3: User 1 still retains Property.Module");
        }

        // â”€â”€ SCOPE 4: Admin re-adds Property.Edit and commits â”€â”€
        using (var scope4 = hostProvider.CreateScope())
        {
            var roleService = scope4.ServiceProvider.GetRequiredService<IRoleService>();
            await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(
                role1.Id,
                [PermissionCatalog.Property.Module, PermissionCatalog.Property.Edit],
                "admin-restorer"));
        }

        // â”€â”€ SCOPE 5: New request by User 1 -> Access must be GRANTED again! â”€â”€
        using (var scope5 = hostProvider.CreateScope())
        {
            scope5.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope5.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var canEdit = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(canEdit.Succeeded, "Scope 5: User 1 must have Property.Edit restored");
        }

        // ── SCOPE 6: Role Removal via RemoveAllRolesAsync ──
        using (var scope6 = hostProvider.CreateScope())
        {
            var userRoleService = scope6.ServiceProvider.GetRequiredService<IUserRoleService>();
            await userRoleService.RemoveAllRolesAsync(u1.Id);
        }

        // ── SCOPE 7: User 1 has no roles -> all access denied ──
        using (var scope7 = hostProvider.CreateScope())
        {
            scope7.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope7.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var canView = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Module);
            Assert.False(canView.Succeeded, "Scope 7: User 1 with role removed must have no access");
        }

        // ── SCOPE 8: Re-assign Role 1 by ID ──
        using (var scope8 = hostProvider.CreateScope())
        {
            var userRoleService = scope8.ServiceProvider.GetRequiredService<IUserRoleService>();
            await userRoleService.AddRoleByRolIdAsync(u1.Id, role1.Id, atayanUserId: u1.Id);
        }

        // ── SCOPE 9: User 1 has role restored -> access granted ──
        using (var scope9 = hostProvider.CreateScope())
        {
            scope9.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope9.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var canEdit = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(canEdit.Succeeded, "Scope 9: User 1 with role re-assigned must have access restored");
        }

        // ── SCOPE 10: Verify Unrelated User 2 was NEVER affected ──
        using (var scope10 = hostProvider.CreateScope())
        {
            scope10.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p2 };
            var auth = scope10.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var u2HasTenant = await auth.AuthorizeAsync(p2, PermissionCatalog.Tenant.Module);
            Assert.True(u2HasTenant.Succeeded, "Scope 10: Unrelated User 2 must retain Tenant.Module throughout");

            var u2HasProperty = await auth.AuthorizeAsync(p2, PermissionCatalog.Property.Module);
            Assert.False(u2HasProperty.Succeeded, "Scope 10: User 2 never had Property access");
        }
    }

    [Fact]
    public async Task Rollback_DoesNotInvalidateOrCorruptCache_AndCommitInvalidatesCache()
    {
        var (u1, _, role1, _) = await SeedTestDataAsync();

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var hostProvider = CreateHostProvider(memoryCache);
        var counter = hostProvider.GetRequiredService<RepositoryReadCounter>();

        var p1 = CreatePrincipal(u1);

        // Scope 1: Populate cache
        using (var scope1 = hostProvider.CreateScope())
        {
            scope1.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope1.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var res = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(res.Succeeded);
        }

        // Cache populated: repository read count for user 1 is 1
        Assert.Equal(1, counter.GetCount(u1.Id));

        // Scope 2: Mutate permissions via real service inside explicit transaction, then ROLLBACK
        using (var scope2 = hostProvider.CreateScope())
        {
            var db = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleService = scope2.ServiceProvider.GetRequiredService<IRoleService>();

            using var tx = await db.Database.BeginTransactionAsync();
            await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(
                role1.Id,
                [PermissionCatalog.Tenant.Module],
                u1.Id
            ));

            // Explicit rollback
            await tx.RollbackAsync();
        }

        // Scope 3: Next scope verification after rollback
        using (var scope3 = hostProvider.CreateScope())
        {
            // 1. Verify DB mutation was completely rolled back
            var db = scope3.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var perms = await db.RolPermissions.Where(rp => rp.RoleId == role1.Id).Select(rp => rp.Permission).ToListAsync();
            Assert.Contains(PermissionCatalog.Property.Edit, perms);
            Assert.DoesNotContain(PermissionCatalog.Tenant.Module, perms);

            // 2. Verify authorization: old permission retained, new permission not granted
            scope3.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope3.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var resOld = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(resOld.Succeeded, "Old permission must be retained after rollback");
            var resNew = await auth.AuthorizeAsync(p1, PermissionCatalog.Tenant.Module);
            Assert.False(resNew.Succeeded, "Rolled back permission must NOT be granted");

            // 3. Verify repository read counter: cache was NOT invalidated, so repository was NOT queried again
            Assert.Equal(1, counter.GetCount(u1.Id));
        }

        // Scope 4: Same service mutation inside explicit transaction, followed by COMMIT
        using (var scope4 = hostProvider.CreateScope())
        {
            var db = scope4.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleService = scope4.ServiceProvider.GetRequiredService<IRoleService>();

            using var tx = await db.Database.BeginTransactionAsync();
            await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(
                role1.Id,
                [PermissionCatalog.Tenant.Module],
                u1.Id
            ));

            await tx.CommitAsync();
        }

        // Scope 5: Verification after commit
        using (var scope5 = hostProvider.CreateScope())
        {
            // 1. Verify DB mutation committed
            var db = scope5.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var perms = await db.RolPermissions.Where(rp => rp.RoleId == role1.Id).Select(rp => rp.Permission).ToListAsync();
            Assert.DoesNotContain(PermissionCatalog.Property.Edit, perms);
            Assert.Contains(PermissionCatalog.Tenant.Module, perms);

            // 2. Verify authorization: old permission revoked, new permission granted
            scope5.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope5.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var resOld = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.False(resOld.Succeeded, "Old permission must be revoked after commit");
            var resNew = await auth.AuthorizeAsync(p1, PermissionCatalog.Tenant.Module);
            Assert.True(resNew.Succeeded, "Committed permission must be granted");

            // 3. Verify repository read counter: cache WAS invalidated on commit and reloaded in Scope 5
            Assert.Equal(2, counter.GetCount(u1.Id));
        }
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // 2. CACHE BOÅALMASI / YENÄ°DEN YÃœKLEME
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    [Fact]
    public async Task ColdStartOrInstanceRestartSimulation_ReloadsFromDatabaseSuccessfully()
    {
        var (u1, _, _, _) = await SeedTestDataAsync();
        var p1 = CreatePrincipal(u1);

        // Instance A with MemoryCache A
        using var memoryCacheA = new MemoryCache(new MemoryCacheOptions());
        using (var hostA = CreateHostProvider(memoryCacheA))
        {
            using var scopeA = hostA.CreateScope();
            scopeA.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var authA = scopeA.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var resA = await authA.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(resA.Succeeded);
        }

        // Simulate new application instance (Instance B with completely empty MemoryCache B and new DI root)
        using var memoryCacheB = new MemoryCache(new MemoryCacheOptions());
        using (var hostB = CreateHostProvider(memoryCacheB))
        {
            using var scopeB = hostB.CreateScope();
            scopeB.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var authB = scopeB.ServiceProvider.GetRequiredService<IAuthorizationService>();

            // Must reload seamlessly from database
            var resB = await authB.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(resB.Succeeded, "Instance restart simulation must reload permissions from database");
        }
    }

    [Fact]
    public async Task CacheEmpty_DoesNotInvalidateUserSessionOrAuthentication()
    {
        var (u1, _, _, _) = await SeedTestDataAsync();
        var p1 = CreatePrincipal(u1);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var hostProvider = CreateHostProvider(memoryCache);

        using var scope = hostProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
            new DefaultHttpContext { User = p1 };

        // Before any cache calls, verify session/identity state is valid
        Assert.True(p1.Identity!.IsAuthenticated);
        Assert.Equal(u1.Id, p1.FindFirstValue(ClaimTypes.NameIdentifier));

        // Authorize with cold cache
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var result = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Module);

        Assert.True(result.Succeeded);
        Assert.True(p1.Identity.IsAuthenticated, "Cold cache must not invalidate principal authentication");
    }

    [Fact]
    public async Task UserRoleService_SoftDeletedRole_Reassignment_ByRoleId_ReactivatesRecord_AndRestoresPermission()
    {
        var (u1, _, role1, _) = await SeedTestDataAsync();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var hostProvider = CreateHostProvider(memoryCache);

        var p1 = CreatePrincipal(u1);

        // Scope 0: Verify initial authorization succeeds
        using (var scope0 = hostProvider.CreateScope())
        {
            scope0.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope0.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var initialCheck = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(initialCheck.Succeeded);
        }

        // Step 1: Remove role via RemoveRoleByNameAsync (soft delete)
        using (var scope1 = hostProvider.CreateScope())
        {
            var userRoleService = scope1.ServiceProvider.GetRequiredService<IUserRoleService>();
            await userRoleService.RemoveRoleByNameAsync(u1.Id, role1.Name);
        }

        int originalRecordId;
        // Verify soft-deleted in DB and capture original record ID
        using (var verifyScope1 = hostProvider.CreateScope())
        {
            var db = verifyScope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ur = await db.UserRoller.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UserId == u1.Id && x.RoleId == role1.Id);
            Assert.NotNull(ur);
            Assert.True(ur.IsDeleted, "UserRole must be marked IsDeleted = true by RemoveRoleByNameAsync");
            originalRecordId = ur.Id;
        }

        // Scope 2: Verify permission is revoked in subsequent scope
        using (var scope2 = hostProvider.CreateScope())
        {
            scope2.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope2.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var res = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.False(res.Succeeded, "Permission must be revoked after role removal");
        }

        // Step 2: Re-assign the exact same role via AddRoleByRolIdAsync
        using (var scope3 = hostProvider.CreateScope())
        {
            var userRoleService = scope3.ServiceProvider.GetRequiredService<IUserRoleService>();
            await userRoleService.AddRoleByRolIdAsync(u1.Id, role1.Id, atayanUserId: u1.Id);
        }

        // Step 3: Check DB state -> Must reactivate existing row, not create duplicate
        using (var verifyScope2 = hostProvider.CreateScope())
        {
            var db = verifyScope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var allMatching = await db.UserRoller.IgnoreQueryFilters()
                .Where(x => x.UserId == u1.Id && x.RoleId == role1.Id)
                .ToListAsync();

            Assert.Single(allMatching);
            var ur = allMatching[0];
            Assert.Equal(originalRecordId, ur.Id);
            Assert.False(ur.IsDeleted, "Soft-deleted record must be reactivated (IsDeleted = false)");
        }

        // Step 4: Verify subsequent request scope has permission restored
        using (var scope4 = hostProvider.CreateScope())
        {
            scope4.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope4.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var res = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(res.Succeeded, "Permissions must be restored in subsequent request scope after AddRoleByRolIdAsync");
        }
    }

    [Fact]
    public async Task UserRoleService_SoftDeletedRole_Reassignment_ByName_ReactivatesRecord_AndRestoresPermission()
    {
        var (u1, _, role1, _) = await SeedTestDataAsync();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var hostProvider = CreateHostProvider(memoryCache);

        var p1 = CreatePrincipal(u1);

        // Scope 0: Verify initial authorization succeeds
        using (var scope0 = hostProvider.CreateScope())
        {
            scope0.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope0.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var initialCheck = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(initialCheck.Succeeded);
        }

        // Step 1: Remove role via RemoveRoleByNameAsync (soft delete)
        using (var scope1 = hostProvider.CreateScope())
        {
            var userRoleService = scope1.ServiceProvider.GetRequiredService<IUserRoleService>();
            await userRoleService.RemoveRoleByNameAsync(u1.Id, role1.Name);
        }

        int originalRecordId;
        // Verify soft-deleted in DB and capture original record ID
        using (var verifyScope1 = hostProvider.CreateScope())
        {
            var db = verifyScope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ur = await db.UserRoller.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UserId == u1.Id && x.RoleId == role1.Id);
            Assert.NotNull(ur);
            Assert.True(ur.IsDeleted, "UserRole must be marked IsDeleted = true by RemoveRoleByNameAsync");
            originalRecordId = ur.Id;
        }

        // Scope 2: Verify permission is revoked in subsequent scope
        using (var scope2 = hostProvider.CreateScope())
        {
            scope2.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope2.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var res = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.False(res.Succeeded, "Permission must be revoked after role removal");
        }

        // Step 2: Re-assign the exact same role via AddRoleByNameAsync
        using (var scope3 = hostProvider.CreateScope())
        {
            var userRoleService = scope3.ServiceProvider.GetRequiredService<IUserRoleService>();
            await userRoleService.AddRoleByNameAsync(u1.Id, role1.Name, atayanUserId: u1.Id);
        }

        // Step 3: Check DB state -> Must reactivate existing row, not create duplicate
        using (var verifyScope2 = hostProvider.CreateScope())
        {
            var db = verifyScope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var allMatching = await db.UserRoller.IgnoreQueryFilters()
                .Where(x => x.UserId == u1.Id && x.RoleId == role1.Id)
                .ToListAsync();

            Assert.Single(allMatching);
            var ur = allMatching[0];
            Assert.Equal(originalRecordId, ur.Id);
            Assert.False(ur.IsDeleted, "Soft-deleted record must be reactivated (IsDeleted = false)");
        }

        // Step 4: Verify subsequent request scope has permission restored
        using (var scope4 = hostProvider.CreateScope())
        {
            scope4.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = p1 };
            var auth = scope4.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var res = await auth.AuthorizeAsync(p1, PermissionCatalog.Property.Edit);
            Assert.True(res.Succeeded, "Permissions must be restored in subsequent request scope after AddRoleByNameAsync");
        }
    }
}