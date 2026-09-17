using Castle.DynamicProxy;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.AuditLog;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Repositories.Identity;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using KiraTakip.Models.Dtos.Role;

namespace KiraTakip.Tests;

public class TenantRoleValidationTests
{
    [Fact]
    public void Validator_RejectsPermissionOutsideTenantCatalog()
    {
        var model = new TenantRoleFormViewModel
        {
            Name = "Test Rolü",
            SelectedPermissions = [PermissionCatalog.Role.Delete]
        };

        var result = new TenantRoleFormViewModelValidator().Validate(model);

        Assert.Contains(result.Errors, error =>
            error.Field == nameof(model.SelectedPermissions)
            && error.Message == "Geçersiz izin seçimi.");
    }
}

[Collection("Database collection")]
public class TenantRoleArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public TenantRoleArchitectureTests(DatabaseFixture fixture)
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
    public async Task Repository_ReturnsOnlyGlobalAndOwnedRolesWithTenantUserCounts()
    {
        var seed = await SeedAsync();
        var repository = new RoleRepository(_context);

        var roles = await repository.GetTenantRolesWithDetailsAsync(seed.FirstTenantId);

        Assert.Contains(roles, role => role.Id == seed.GlobalRoleId && role.UserCount == 1);
        Assert.Contains(roles, role => role.Id == seed.FirstTenantRoleId && role.UserCount == 1);
        Assert.DoesNotContain(roles, role => role.Id == seed.SecondTenantRoleId);
        Assert.Equal(
            1,
            Assert.Single(roles, role => role.Id == seed.FirstTenantRoleId).PermissionCount);
    }

    [Fact]
    public async Task Service_RejectsForeignRoleAndInvalidPermission()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var foreignException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetTenantRoleForEditAsync(
                new GetTenantRoleForEditInput(seed.SecondTenantRoleId, seed.FirstTenantId)));
        Assert.Equal("TENANT_ROLE_NOT_FOUND", foreignException.Code);

        var invalidPermissionException = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.UpdateTenantRoleAsync(new UpdateTenantRoleInput(
                seed.FirstTenantRoleId,
                seed.FirstTenantId,
                "Güncel Rol",
                null,
                [PermissionCatalog.Role.Delete],
                "tenant-role-test")));
        Assert.Equal("TENANT_ROLE_INVALID_PERMISSION", invalidPermissionException.Code);

        var storedRole = await _context.Roller.AsNoTracking()
            .SingleAsync(role => role.Id == seed.FirstTenantRoleId);
        Assert.Equal(seed.FirstTenantRoleName, storedRole.Name);
    }

    [Fact]
    public async Task Update_ReplacesPermissionsWithinOwnedTenantRole()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        await service.UpdateTenantRoleAsync(new UpdateTenantRoleInput(
            seed.FirstTenantRoleId,
            seed.FirstTenantId,
            "Güncel Rol",
            "Güncel açıklama",
            [
                PermissionCatalog.TenantPortal.Lease.Module,
                PermissionCatalog.TenantPortal.Charge.Module
            ],
            "tenant-role-test"));

        _context.ChangeTracker.Clear();
        var role = await _context.Roller.AsNoTracking()
            .SingleAsync(item => item.Id == seed.FirstTenantRoleId);
        var permissions = await _context.RolPermissions.AsNoTracking()
            .Where(permission => permission.RoleId == seed.FirstTenantRoleId)
            .Select(permission => permission.Permission)
            .OrderBy(permission => permission)
            .ToListAsync();

        Assert.Equal("Güncel Rol", role.Name);
        Assert.Equal(2, permissions.Count);
        Assert.Contains(PermissionCatalog.TenantPortal.Lease.Module, permissions);
        Assert.Contains(PermissionCatalog.TenantPortal.Charge.Module, permissions);
    }

    private RoleService CreateService(IAuditService? auditService = null)
        => new(
            new RoleRepository(_context),
            new RolePermissionRepository(_context),
            new UserRoleRepository(_context),
            auditService ?? new NoOpAuditService(),
            new NoOpUserSecurityService(),
            null!,
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

    private async Task<TenantRoleSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstTenant = new Tenant
        {
            TenantNo = $"TR1-{suffix}",
            Name = $"Birinci Rol Kiracısı {suffix}"
        };
        var secondTenant = new Tenant
        {
            TenantNo = $"TR2-{suffix}",
            Name = $"İkinci Rol Kiracısı {suffix}"
        };
        _context.Tenants.AddRange(firstTenant, secondTenant);
        await _context.SaveChangesAsync();

        var globalRole = CreateRole(null, $"Genel Rol {suffix}", isSystemRole: true);
        var firstTenantRole = CreateRole(firstTenant.Id, $"Birinci Rol {suffix}");
        var secondTenantRole = CreateRole(secondTenant.Id, $"İkinci Rol {suffix}");
        _context.Roller.AddRange(globalRole, firstTenantRole, secondTenantRole);
        await _context.SaveChangesAsync();

        _context.RolPermissions.Add(new RolePermission
        {
            RoleId = firstTenantRole.Id,
            Permission = PermissionCatalog.TenantPortal.Lease.Module
        });

        var firstUser = CreateUser(firstTenant.Id, $"first-{suffix}");
        var secondUser = CreateUser(secondTenant.Id, $"second-{suffix}");
        _context.Users.AddRange(firstUser, secondUser);
        _context.UserRoller.AddRange(
            new UserRole { UserId = firstUser.Id, RoleId = globalRole.Id },
            new UserRole { UserId = secondUser.Id, RoleId = globalRole.Id },
            new UserRole { UserId = firstUser.Id, RoleId = firstTenantRole.Id });
        await _context.SaveChangesAsync();

        return new TenantRoleSeed(
            firstTenant.Id,
            secondTenant.Id,
            globalRole.Id,
            firstTenantRole.Id,
            firstTenantRole.Name,
            secondTenantRole.Id);
    }

    private static Role CreateRole(int? tenantId, string name, bool isSystemRole = false)
        => new()
        {
            Name = name,
            Scope = RoleScope.Tenant,
            TenantId = tenantId,
            IsSystemRole = isSystemRole,
            IsActive = true
        };

    private static ApplicationUser CreateUser(int tenantId, string key)
        => new()
        {
            Id = key,
            UserName = $"{key}@example.com",
            NormalizedUserName = $"{key.ToUpperInvariant()}@EXAMPLE.COM",
            Email = $"{key}@example.com",
            NormalizedEmail = $"{key.ToUpperInvariant()}@EXAMPLE.COM",
            AdSoyad = "Rol Test Kullanıcısı",
            TenantId = tenantId,
            UserType = UserType.Tenant,
            SecurityStamp = Guid.NewGuid().ToString()
        };

    private sealed record TenantRoleSeed(
        int FirstTenantId,
        int SecondTenantId,
        int GlobalRoleId,
        int FirstTenantRoleId,
        string FirstTenantRoleName,
        int SecondTenantRoleId);
}

[Collection("Database collection")]
public class TenantRoleTransactionTests
{
    private readonly DatabaseFixture _fixture;

    public TenantRoleTransactionTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_RollsBackRoleAndPermissionsWhenAuditFails()
    {
        await using var context = _fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant
        {
            TenantNo = $"RB-{suffix}",
            Name = $"Rol Rollback Kiracısı {suffix}"
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        try
        {
            var roleName = $"Rollback Rol {suffix}";
            var target = new RoleService(
                new RoleRepository(context),
                new RolePermissionRepository(context),
                new UserRoleRepository(context),
                new ThrowingAuditService(),
                new NoOpUserSecurityService(),
                null!,
                new NoOpUserPermissionCacheInvalidator(),
                new UnitOfWork(context));
            var interceptor = new TransactionInterceptor(
                context,
                NullLogger<TransactionInterceptor>.Instance);
            var service = new ProxyGenerator().CreateInterfaceProxyWithTarget<IRoleService>(
                target,
                interceptor.ToInterceptor());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateTenantRoleAsync(new CreateTenantRoleInput(
                    tenant.Id,
                    roleName,
                    null,
                    [PermissionCatalog.TenantPortal.Lease.Module],
                    "tenant-role-test")));

            context.ChangeTracker.Clear();
            Assert.False(await context.Roller.IgnoreQueryFilters()
                .AnyAsync(role => role.Name == roleName));
        }
        finally
        {
            context.ChangeTracker.Clear();
            var storedTenant = await context.Tenants.IgnoreQueryFilters()
                .SingleAsync(item => item.Id == tenant.Id);
            context.Tenants.Remove(storedTenant);
            await context.SaveChangesAsync();
        }
    }
}

internal sealed class NoOpAuditService : IAuditService
{
    public Task LogAsync(
        string eventType,
        string? entityType = null,
        string? entityId = null,
        string? details = null)
        => Task.CompletedTask;

    public Task<QueryResult> QueryAsync(QueryInput input, CancellationToken ct = default)
        => throw new NotSupportedException();
}

internal sealed class ThrowingAuditService : IAuditService
{
    public Task LogAsync(
        string eventType,
        string? entityType = null,
        string? entityId = null,
        string? details = null)
        => throw new InvalidOperationException("Audit failure");

    public Task<QueryResult> QueryAsync(QueryInput input, CancellationToken ct = default)
        => throw new NotSupportedException();
}

internal sealed class NoOpUserSecurityService : IUserSecurityService
{
    public Task UpdateStampAsync(string userId) => Task.CompletedTask;
    public Task UpdateStampForRoleUsersAsync(int rolId) => Task.CompletedTask;
}
