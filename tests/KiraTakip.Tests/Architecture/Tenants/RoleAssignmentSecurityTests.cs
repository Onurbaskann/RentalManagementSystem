using Castle.DynamicProxy;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.AuditLog;
using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Models.Dtos.Role;
using KiraTakip.Models.Dtos.TenantUser;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Repositories.Leases;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Notifications;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Tenants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class RoleAssignmentSecurityTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public RoleAssignmentSecurityTests(DatabaseFixture fixture)
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

    private UserRoleService CreateUserRoleService()
    {
        return new UserRoleService(
            new UserRoleRepository(_context),
            new RoleRepository(_context),
            new ApplicationUserRepository(_context),
            new UnitOfWork(_context),
            new NoOpUserPermissionCacheInvalidator());
    }

    private RoleService CreateRoleService()
    {
        return new RoleService(
            new RoleRepository(_context),
            new RolePermissionRepository(_context),
            new UserRoleRepository(_context),
            new NoOpAuditService(),
            new NoOpUserSecurityService(),
            null!,
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));
    }

    [Fact]
    public async Task InternalUser_CannotBeAssignedTenantRole_ThrowsValidationException()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var internalUser = new ApplicationUser
        {
            Id = $"int-user-{suffix}",
            UserName = $"int-{suffix}@example.com",
            Email = $"int-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var tenantRole = new Role
        {
            Name = $"TenantRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsActive = true
        };
        _context.Users.Add(internalUser);
        _context.Roller.Add(tenantRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.AddRoleByRolIdAsync(internalUser.Id, tenantRole.Id));

        Assert.Equal("İç kullanıcılara yalnızca iç roller atanabilir.", ex.Message);
    }

    [Fact]
    public async Task TenantUser_CannotBeAssignedInternalRole_ThrowsValidationException()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantUser = new ApplicationUser
        {
            Id = $"tenant-user-{suffix}",
            UserName = $"tenant-{suffix}@example.com",
            Email = $"tenant-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var internalRole = new Role
        {
            Name = $"InternalRole-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        _context.Users.Add(tenantUser);
        _context.Roller.Add(internalRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.AddRoleByRolIdAsync(tenantUser.Id, internalRole.Id));

        Assert.Equal("Kiracı kullanıcılarına yalnızca kiracı rolleri atanabilir.", ex.Message);
    }

    [Fact]
    public async Task TenantUser_CannotBeAssignedOtherTenantRole_ThrowsValidationException()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantA = new Tenant { TenantNo = $"TA-{suffix}", Name = $"TenantA-{suffix}" };
        var tenantB = new Tenant { TenantNo = $"TB-{suffix}", Name = $"TenantB-{suffix}" };
        _context.Tenants.AddRange(tenantA, tenantB);
        await _context.SaveChangesAsync();

        var tenantAUser = new ApplicationUser
        {
            Id = $"userA-{suffix}",
            UserName = $"userA-{suffix}@example.com",
            Email = $"userA-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenantA.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var tenantBRole = new Role
        {
            Name = $"RoleB-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenantB.Id,
            IsActive = true
        };
        _context.Users.Add(tenantAUser);
        _context.Roller.Add(tenantBRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.AddRoleByRolIdAsync(tenantAUser.Id, tenantBRole.Id));

        Assert.Equal("Farklı bir kiracıya ait özel rol bu kullanıcıya atanamaz.", ex.Message);
    }

    [Fact]
    public async Task TenantUser_CannotAssignKiraciYoneticisiRole_ThrowsForbiddenException()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantActor = new ApplicationUser
        {
            Id = $"actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var targetTenantUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.AddRange(tenantActor, targetTenantUser);
        _context.Roller.Add(kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AddRoleByRolIdAsync(targetTenantUser.Id, kiraciYoneticisiRole.Id, tenantActor.Id));

        Assert.Contains("Kiracı kullanıcıları Kiracı Yöneticisi rolünü atayamaz.", ex.Message);
    }

    [Fact]
    public async Task InternalUser_CanAssignKiraciYoneticisiRole_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var internalActor = new ApplicationUser
        {
            Id = $"int-actor-{suffix}",
            UserName = $"int-actor-{suffix}@example.com",
            Email = $"int-actor-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var adminRole = new Role
        {
            Name = $"InternalAdmin-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        _context.Users.Add(internalActor);
        _context.Roller.Add(adminRole);
        await _context.SaveChangesAsync();

        _context.RolPermissions.AddRange(
            new RolePermission { RoleId = adminRole.Id, Permission = PermissionCatalog.User.Module },
            new RolePermission { RoleId = adminRole.Id, Permission = PermissionCatalog.User.Edit });
        _context.UserRoller.Add(new UserRole { UserId = internalActor.Id, RoleId = adminRole.Id });

        var targetTenantUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.Add(targetTenantUser);
        _context.Roller.Add(kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        await service.AddRoleByRolIdAsync(targetTenantUser.Id, kiraciYoneticisiRole.Id, internalActor.Id);

        var assigned = await _context.UserRoller.AnyAsync(ur =>
            ur.UserId == targetTenantUser.Id && ur.RoleId == kiraciYoneticisiRole.Id);
        Assert.True(assigned);
    }

    [Fact]
    public async Task InternalUser_WithInvitationPermission_CanAssignKiraciYoneticisiForInvitation()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        var internalActor = new ApplicationUser
        {
            Id = $"invitation-actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var invitationManagerRole = new Role
        {
            Name = $"InvitationManager-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        var targetTenantUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.AddRange(internalActor, targetTenantUser);
        _context.Roller.AddRange(invitationManagerRole, kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        _context.RolPermissions.AddRange(
            new RolePermission { RoleId = invitationManagerRole.Id, Permission = PermissionCatalog.Invitation.Module },
            new RolePermission { RoleId = invitationManagerRole.Id, Permission = PermissionCatalog.Invitation.Create });
        _context.UserRoller.Add(new UserRole { UserId = internalActor.Id, RoleId = invitationManagerRole.Id });
        await _context.SaveChangesAsync();

        await CreateUserRoleService().AddRoleByRolIdAsync(
            targetTenantUser.Id,
            kiraciYoneticisiRole.Id,
            internalActor.Id,
            RoleAssignmentOperation.Invitation);

        Assert.True(await _context.UserRoller.AnyAsync(userRole =>
            userRole.UserId == targetTenantUser.Id && userRole.RoleId == kiraciYoneticisiRole.Id));
    }

    [Fact]
    public async Task RoleService_InternalRole_WithTenantPermissions_ThrowsWithoutModifyingDatabase()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role = new Role
        {
            Name = $"InternalRole-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        _context.Roller.Add(role);
        await _context.SaveChangesAsync();

        var initialPermission = PermissionCatalog.Property.Module;
        _context.RolPermissions.Add(new RolePermission { RoleId = role.Id, Permission = initialPermission });
        await _context.SaveChangesAsync();

        var service = CreateRoleService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.SetRolePermissionsAsync(new SetRolePermissionsInput(
                role.Id,
                [PermissionCatalog.TenantPortal.Lease.Module, PermissionCatalog.Property.Edit],
                "test-user")));

        Assert.Equal("Geçersiz izin seçimi.", ex.Message);

        _context.ChangeTracker.Clear();
        var perms = await _context.RolPermissions.Where(rp => rp.RoleId == role.Id).ToListAsync();
        Assert.Single(perms);
        Assert.Equal(initialPermission, perms[0].Permission);
    }

    [Fact]
    public async Task RoleService_TenantRole_WithInternalPermissions_ThrowsWithoutModifyingDatabase()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var role = new Role
        {
            Name = $"TenantRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsActive = true
        };
        _context.Roller.Add(role);
        await _context.SaveChangesAsync();

        var initialPermission = PermissionCatalog.TenantPortal.Lease.Module;
        _context.RolPermissions.Add(new RolePermission { RoleId = role.Id, Permission = initialPermission });
        await _context.SaveChangesAsync();

        var service = CreateRoleService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.SetRolePermissionsAsync(new SetRolePermissionsInput(
                role.Id,
                [PermissionCatalog.Role.Delete, PermissionCatalog.TenantPortal.Charge.Module],
                "test-user")));

        Assert.Equal("Geçersiz izin seçimi.", ex.Message);

        _context.ChangeTracker.Clear();
        var perms = await _context.RolPermissions.Where(rp => rp.RoleId == role.Id).ToListAsync();
        Assert.Single(perms);
        Assert.Equal(initialPermission, perms[0].Permission);
    }

    [Fact]
    public async Task RoleService_CreateInternalRole_WithKiraciYoneticisiName_ThrowsValidationException()
    {
        var service = CreateRoleService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.CreateRoleAsync(new CreateRoleInput(RoleNames.KiraciYoneticisi, null, "admin")));

        Assert.Contains(RoleNames.KiraciYoneticisi, ex.Message);
    }

    [Fact]
    public async Task TenantUserService_GetInviteDataAsync_ForTenantSuperAdmin_HidesKiraciYoneticisi()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantActor = new ApplicationUser
        {
            Id = $"tenant-actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Tenant,
            IsSuperAdmin = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        var customTenantRole = new Role
        {
            Name = $"CustomRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsSystemRole = false,
            IsActive = true
        };
        _context.Users.Add(tenantActor);
        _context.Roller.AddRange(kiraciYoneticisiRole, customTenantRole);
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            null!,
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        // Act 1: Tenant Actor
        var inviteDataForTenant = await service.GetInviteDataAsync(new GetInviteDataInput(tenant.Id, tenantActor.Id));
        Assert.DoesNotContain(inviteDataForTenant.Roles, r => r.Name == RoleNames.KiraciYoneticisi);
        Assert.Contains(inviteDataForTenant.Roles, r => r.Name == customTenantRole.Name);

        // Act 2: Internal Actor
        var internalActor = new ApplicationUser
        {
            Id = $"int-actor-{suffix}",
            UserName = $"int-actor-{suffix}@example.com",
            Email = $"int-actor-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var invitationManagerRole = new Role
        {
            Name = $"InvitationManager-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        _context.Users.Add(internalActor);
        _context.Roller.Add(invitationManagerRole);
        await _context.SaveChangesAsync();

        _context.RolPermissions.AddRange(
            new RolePermission { RoleId = invitationManagerRole.Id, Permission = PermissionCatalog.Invitation.Module },
            new RolePermission { RoleId = invitationManagerRole.Id, Permission = PermissionCatalog.Invitation.Create });
        _context.UserRoller.Add(new UserRole { UserId = internalActor.Id, RoleId = invitationManagerRole.Id });
        await _context.SaveChangesAsync();

        var inviteDataForInternal = await service.GetInviteDataAsync(new GetInviteDataInput(tenant.Id, internalActor.Id));
        Assert.Contains(inviteDataForInternal.Roles, r => r.Name == RoleNames.KiraciYoneticisi);
        Assert.Contains(inviteDataForInternal.Roles, r => r.Name == customTenantRole.Name);
    }

    [Fact]
    public async Task TenantUserService_SendInvitationAsync_TenantActor_InvitingKiraciYoneticisi_ThrowsForbidden()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantActor = new ApplicationUser
        {
            Id = $"tenant-actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.Add(tenantActor);
        _context.Roller.Add(kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            null!,
            null!,
            new NoOpApplicationUserManager(),
            null!,
            null!,
            null!,
            null!,
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SendInvitationAsync(new SendTenantInvitationInput(
                tenant.Id,
                $"invitee-{suffix}@example.com",
                "Invitee",
                kiraciYoneticisiRole.Id,
                tenantActor.Id,
                null)));

        Assert.Equal("TENANT_INVITATION_FORBIDDEN_ROLE", ex.Code);
    }

    [Fact]
    public async Task TenantUserService_EditTenantUserAsync_WithoutRoleChange_PreservesExistingRole()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantActor = new ApplicationUser
        {
            Id = $"actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var targetUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            AdSoyad = "Eski Isim",
            TumTasinmazlaraErisim = true,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.AddRange(tenantActor, targetUser);
        _context.Roller.Add(kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        var initialUserRole = new UserRole { UserId = targetUser.Id, RoleId = kiraciYoneticisiRole.Id };
        _context.UserRoller.Add(initialUserRole);
        await _context.SaveChangesAsync();
        var initialUserRoleId = initialUserRole.Id;

        var recordingAudit = new RecordingAuditService();
        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            null!,
            new UserPermissionScopeRepository(_context),
            new NoOpApplicationUserManager(),
            null!,
            recordingAudit,
            new NoOpUserSecurityService(),
            new NoOpPermissionScopeCache(),
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        // Act: Update user fullName, keeping the same RoleId (KiraciYoneticisi)
        await service.EditTenantUserAsync(new EditTenantUserInput(
            tenant.Id,
            targetUser.Id,
            "Yeni Isim",
            kiraciYoneticisiRole.Id,
            true,
            [],
            tenantActor.Id,
            new Models.Dtos.Reservation.ReservationAccessScopeInput()));

        _context.ChangeTracker.Clear();
        var updatedUserRole = await _context.UserRoller.SingleOrDefaultAsync(ur => ur.UserId == targetUser.Id);
        Assert.NotNull(updatedUserRole);
        Assert.Equal(kiraciYoneticisiRole.Id, updatedUserRole.RoleId);
        Assert.Equal(initialUserRoleId, updatedUserRole.Id); // Identity preserved!

        var updatedUser = await _context.Users.SingleAsync(u => u.Id == targetUser.Id);
        Assert.Equal("Yeni Isim", updatedUser.AdSoyad);
        Assert.DoesNotContain("User.RoleChanged", recordingAudit.Actions); // No RoleChanged audit!
    }

    [Fact]
    public async Task InternalUser_WithoutEditPermission_CannotAssignKiraciYoneticisiRole_ThrowsForbidden()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var internalActor = new ApplicationUser
        {
            Id = $"int-actor-{suffix}",
            UserName = $"int-actor-{suffix}@example.com",
            Email = $"int-actor-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var targetTenantUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.AddRange(internalActor, targetTenantUser);
        _context.Roller.Add(kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AddRoleByRolIdAsync(targetTenantUser.Id, kiraciYoneticisiRole.Id, internalActor.Id));

        Assert.Contains("Kiracı Yöneticisi rolünü atamak için gerekli işlem yetkiniz bulunmamaktadır.", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ActorValidation_NullOrWhitespaceActor_ThrowsValidationException(string? actorId)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var targetTenantUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var tenantRole = new Role
        {
            Name = $"Role-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsActive = true
        };
        _context.Users.Add(targetTenantUser);
        _context.Roller.Add(tenantRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.AddRoleByRolIdAsync(targetTenantUser.Id, tenantRole.Id, actorId));

        Assert.Equal("Rol atayan aktör belirtilmelidir.", ex.Message);
    }

    [Theory]
    [InlineData("non-existent-user-id")]
    [InlineData("system")]
    public async Task ActorValidation_NotFoundActor_ThrowsNotFoundException(string actorId)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var targetTenantUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var tenantRole = new Role
        {
            Name = $"Role-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsActive = true
        };
        _context.Users.Add(targetTenantUser);
        _context.Roller.Add(tenantRole);
        await _context.SaveChangesAsync();

        var service = CreateUserRoleService();

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AddRoleByRolIdAsync(targetTenantUser.Id, tenantRole.Id, actorId));

        Assert.Equal("Rol atayan kullanıcı bulunamadı.", ex.Message);
    }

    [Fact]
    public async Task TenantUserService_GetEditRoleOptionsAsync_AuthorizedInternalGetsKiraciYoneticisi()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        var customRole = new Role
        {
            Name = $"CustomRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsSystemRole = false,
            IsActive = true
        };
        _context.Roller.AddRange(kiraciYoneticisiRole, customRole);

        var authorizedActor = new ApplicationUser
        {
            Id = $"auth-{suffix}",
            UserName = $"auth-{suffix}@example.com",
            Email = $"auth-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var adminRole = new Role
        {
            Name = $"AdminRole-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        _context.Users.Add(authorizedActor);
        _context.Roller.Add(adminRole);
        await _context.SaveChangesAsync();

        _context.RolPermissions.AddRange(
            new RolePermission { RoleId = adminRole.Id, Permission = PermissionCatalog.User.Module },
            new RolePermission { RoleId = adminRole.Id, Permission = PermissionCatalog.User.Edit });
        _context.UserRoller.Add(new UserRole { UserId = authorizedActor.Id, RoleId = adminRole.Id });

        var unauthorizedActor = new ApplicationUser
        {
            Id = $"unauth-{suffix}",
            UserName = $"unauth-{suffix}@example.com",
            Email = $"unauth-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        _context.Users.Add(unauthorizedActor);
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            null!,
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        // Authorized Internal Actor should see KiraciYoneticisi even if target user currently has customRole
        var authorizedOptions = await service.GetEditRoleOptionsAsync(tenant.Id, customRole.Id, authorizedActor.Id);
        Assert.Contains(authorizedOptions, r => r.Name == RoleNames.KiraciYoneticisi);

        // Unauthorized Internal Actor should NOT see KiraciYoneticisi
        var unauthorizedOptions = await service.GetEditRoleOptionsAsync(tenant.Id, customRole.Id, unauthorizedActor.Id);
        Assert.DoesNotContain(unauthorizedOptions, r => r.Name == RoleNames.KiraciYoneticisi);
    }

    [Fact]
    public async Task TenantUserService_TenantActor_CannotSeeOrChangeExistingKiraciYoneticisiRole()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var tenantActor = new ApplicationUser
        {
            Id = $"actor-{suffix}",
            UserName = $"actor-{suffix}@example.com",
            Email = $"actor-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
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
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        var customRole = new Role
        {
            Name = $"CustomRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsSystemRole = false,
            IsActive = true
        };
        _context.Users.AddRange(tenantActor, targetUser);
        _context.Roller.AddRange(kiraciYoneticisiRole, customRole);
        await _context.SaveChangesAsync();
        _context.UserRoller.Add(new UserRole
        {
            UserId = targetUser.Id,
            RoleId = kiraciYoneticisiRole.Id
        });
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        var options = await service.GetEditRoleOptionsAsync(
            tenant.Id,
            kiraciYoneticisiRole.Id,
            tenantActor.Id);

        Assert.DoesNotContain(options, role => role.Id == kiraciYoneticisiRole.Id);
        Assert.Contains(options, role => role.Id == customRole.Id);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.EditTenantUserAsync(new EditTenantUserInput(
                tenant.Id,
                targetUser.Id,
                targetUser.AdSoyad,
                customRole.Id,
                true,
                [],
                tenantActor.Id,
                new Models.Dtos.Reservation.ReservationAccessScopeInput())));

        Assert.Equal("TENANT_USER_ROLE_ASSIGNMENT_FORBIDDEN", exception.Code);
        _context.ChangeTracker.Clear();
        var storedRole = await _context.UserRoller.SingleAsync(role => role.UserId == targetUser.Id);
        Assert.Equal(kiraciYoneticisiRole.Id, storedRole.RoleId);
    }

    [Fact]
    public async Task TenantUserService_EditTenantUserAsync_AuthorizedInternalCanPromoteToKiraciYoneticisi()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var authorizedActor = new ApplicationUser
        {
            Id = $"auth-{suffix}",
            UserName = $"auth-{suffix}@example.com",
            Email = $"auth-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var adminRole = new Role
        {
            Name = $"AdminRole-{suffix}",
            Scope = RoleScope.Internal,
            IsActive = true
        };
        _context.Users.Add(authorizedActor);
        _context.Roller.Add(adminRole);
        await _context.SaveChangesAsync();

        _context.RolPermissions.AddRange(
            new RolePermission { RoleId = adminRole.Id, Permission = PermissionCatalog.User.Module },
            new RolePermission { RoleId = adminRole.Id, Permission = PermissionCatalog.User.Edit });
        _context.UserRoller.Add(new UserRole { UserId = authorizedActor.Id, RoleId = adminRole.Id });

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
        var customRole = new Role
        {
            Name = $"CustomRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsSystemRole = false,
            IsActive = true
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.Add(targetUser);
        _context.Roller.AddRange(customRole, kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        _context.UserRoller.Add(new UserRole { UserId = targetUser.Id, RoleId = customRole.Id });
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            null!,
            new UserPermissionScopeRepository(_context),
            new NoOpApplicationUserManager(),
            null!,
            new NoOpAuditService(),
            new NoOpUserSecurityService(),
            new NoOpPermissionScopeCache(),
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        await service.EditTenantUserAsync(new EditTenantUserInput(
            tenant.Id,
            targetUser.Id,
            "Target User",
            kiraciYoneticisiRole.Id,
            true,
            [],
            authorizedActor.Id,
            new Models.Dtos.Reservation.ReservationAccessScopeInput()));

        _context.ChangeTracker.Clear();
        var userRole = await _context.UserRoller.SingleOrDefaultAsync(ur => ur.UserId == targetUser.Id);
        Assert.NotNull(userRole);
        Assert.Equal(kiraciYoneticisiRole.Id, userRole.RoleId);
    }

    [Fact]
    public async Task TenantUserService_EditTenantUserAsync_UnauthorizedInternalCannotPromoteToKiraciYoneticisi()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var unauthorizedActor = new ApplicationUser
        {
            Id = $"unauth-{suffix}",
            UserName = $"unauth-{suffix}@example.com",
            Email = $"unauth-{suffix}@example.com",
            UserType = UserType.Internal,
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
        var customRole = new Role
        {
            Name = $"CustomRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsSystemRole = false,
            IsActive = true
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.AddRange(unauthorizedActor, targetUser);
        _context.Roller.AddRange(customRole, kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        _context.UserRoller.Add(new UserRole { UserId = targetUser.Id, RoleId = customRole.Id });
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            null!,
            new UserPermissionScopeRepository(_context),
            new NoOpApplicationUserManager(),
            null!,
            new NoOpAuditService(),
            new NoOpUserSecurityService(),
            new NoOpPermissionScopeCache(),
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.EditTenantUserAsync(new EditTenantUserInput(
                tenant.Id,
                targetUser.Id,
                "Target User",
                kiraciYoneticisiRole.Id,
                true,
                [],
                unauthorizedActor.Id,
                new Models.Dtos.Reservation.ReservationAccessScopeInput())));

        Assert.Equal("TENANT_USER_ROLE_ASSIGNMENT_FORBIDDEN", ex.Code);
    }

    [Fact]
    public async Task TenantUserService_EditTenantUserAsync_InvalidRole_ThrowsBusinessValidationException_AndDoesNotMutate()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var actor = new ApplicationUser
        {
            Id = $"act-{suffix}",
            UserName = $"act-{suffix}@example.com",
            Email = $"act-{suffix}@example.com",
            UserType = UserType.Internal,
            IsSuperAdmin = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var targetUser = new ApplicationUser
        {
            Id = $"target-{suffix}",
            UserName = $"target-{suffix}@example.com",
            Email = $"target-{suffix}@example.com",
            AdSoyad = "Original Name",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            TumTasinmazlaraErisim = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var initialRole = new Role
        {
            Name = $"InitialRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenant.Id,
            IsActive = true
        };
        _context.Users.AddRange(actor, targetUser);
        _context.Roller.Add(initialRole);
        await _context.SaveChangesAsync();

        _context.UserRoller.Add(new UserRole { UserId = targetUser.Id, RoleId = initialRole.Id });
        await _context.SaveChangesAsync();

        var auditService = new RecordingAuditService();
        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            null!,
            new UserPermissionScopeRepository(_context),
            new NoOpApplicationUserManager(),
            null!,
            auditService,
            new NoOpUserSecurityService(),
            new NoOpPermissionScopeCache(),
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        try
        {
            const int nonExistentRoleId = 999999;
            var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
                service.EditTenantUserAsync(new EditTenantUserInput(
                    tenant.Id,
                    targetUser.Id,
                    "Mutated Name",
                    nonExistentRoleId,
                    true,
                    [],
                    actor.Id,
                    new Models.Dtos.Reservation.ReservationAccessScopeInput())));

            Assert.Equal("RoleId", ex.Field);
            Assert.Equal("TENANT_USER_INVALID_ROLE", ex.Code);
            Assert.Equal("Geçersiz rol seçildi.", ex.Message);
            Assert.Equal(ErrorType.Failure, ex.ErrorType);

            // Mutation check: DB was not updated
            _context.ChangeTracker.Clear();
            var reloadedUser = await _context.Users.FindAsync(targetUser.Id);
            Assert.NotNull(reloadedUser);
            Assert.Equal("Original Name", reloadedUser.AdSoyad);

            var userRoles = await _context.UserRoller.Where(ur => ur.UserId == targetUser.Id).ToListAsync();
            var singleRole = Assert.Single(userRoles);
            Assert.Equal(initialRole.Id, singleRole.RoleId);

            Assert.Empty(auditService.Actions);
        }
        finally
        {
            _context.ChangeTracker.Clear();
            var urList = await _context.UserRoller.Where(ur => ur.UserId == targetUser.Id).ToListAsync();
            _context.UserRoller.RemoveRange(urList);
            var uList = await _context.Users.Where(u => u.Id == actor.Id || u.Id == targetUser.Id).ToListAsync();
            _context.Users.RemoveRange(uList);
            var rList = await _context.Roller.Where(r => r.Id == initialRole.Id).ToListAsync();
            _context.Roller.RemoveRange(rList);
            var tList = await _context.Tenants.Where(t => t.Id == tenant.Id).ToListAsync();
            _context.Tenants.RemoveRange(tList);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task TenantUserService_SendInvitationAsync_UnauthorizedInternalCannotInviteKiraciYoneticisi()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var unauthorizedActor = new ApplicationUser
        {
            Id = $"unauth-{suffix}",
            UserName = $"unauth-{suffix}@example.com",
            Email = $"unauth-{suffix}@example.com",
            UserType = UserType.Internal,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            TenantId = null,
            IsSystemRole = true,
            IsActive = true
        };
        _context.Users.Add(unauthorizedActor);
        _context.Roller.Add(kiraciYoneticisiRole);
        await _context.SaveChangesAsync();

        var service = new TenantUserService(
            new ApplicationUserRepository(_context),
            new InvitationRepository(_context),
            new TenantRepository(_context),
            new RoleRepository(_context),
            new UserRoleRepository(_context),
            new LeaseRepository(_context),
            null!,
            null!,
            new NoOpApplicationUserManager(),
            null!,
            null!,
            null!,
            null!,
            new NoOpUserPermissionCacheInvalidator(),
            new UnitOfWork(_context));

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.SendInvitationAsync(new SendTenantInvitationInput(
                tenant.Id,
                $"invitee-{suffix}@example.com",
                "Invitee",
                kiraciYoneticisiRole.Id,
                unauthorizedActor.Id,
                null)));

        Assert.Equal("TENANT_INVITATION_FORBIDDEN_ROLE", ex.Code);
    }

    [Fact]
    public async Task InvitationService_AcceptAsync_WithInactiveOrDeletedOrMismatchedRole_DoesNotCreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantA = new Tenant { TenantNo = $"TA-{suffix}", Name = $"TenantA-{suffix}" };
        var tenantB = new Tenant { TenantNo = $"TB-{suffix}", Name = $"TenantB-{suffix}" };
        _context.Tenants.AddRange(tenantA, tenantB);
        await _context.SaveChangesAsync();

        var inviter = new ApplicationUser
        {
            Id = $"inviter-{suffix}",
            UserName = $"inviter-{suffix}@example.com",
            Email = $"inviter-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenantA.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var inactiveRole = new Role
        {
            Name = $"InactiveRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenantA.Id,
            IsActive = false
        };
        var deletedRole = new Role
        {
            Name = $"DeletedRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenantA.Id,
            IsActive = true,
            IsDeleted = true
        };
        var mismatchedTenantRole = new Role
        {
            Name = $"TenantBRole-{suffix}",
            Scope = RoleScope.Tenant,
            TenantId = tenantB.Id,
            IsActive = true
        };
        _context.Users.Add(inviter);
        _context.Roller.AddRange(inactiveRole, deletedRole, mismatchedTenantRole);
        await _context.SaveChangesAsync();

        var userManager = new TestApplicationUserManager(_context);
        var service = new InvitationService(
            new InvitationRepository(_context),
            new RoleRepository(_context),
            new UserPermissionScopeRepository(_context),
            new UnitOfWork(_context),
            null!,
            null!,
            null!,
            null!,
            new NoOpAuditService(),
            userManager,
            CreateUserRoleService(),
            new NoOpPermissionScopeCache(),
            null!,
            NullLogger<InvitationService>.Instance);

        // Scenario 1: Inactive role
        var inviteInactive = new Invitation
        {
            Email = $"test-inactive-{suffix}@example.com",
            FullName = "Inactive Test",
            RoleId = inactiveRole.Id,
            InvitedByUserId = inviter.Id,
            TenantId = tenantA.Id,
            UserType = UserType.Tenant,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await Assert.ThrowsAnyAsync<BusinessException>(() =>
            service.AcceptAsync(inviteInactive, new AcceptInput("Inactive Test", "Password123!")));
        Assert.False(await _context.Users.AnyAsync(u => u.Email == inviteInactive.Email));

        // Scenario 2: Deleted role
        var inviteDeleted = new Invitation
        {
            Email = $"test-deleted-{suffix}@example.com",
            FullName = "Deleted Test",
            RoleId = deletedRole.Id,
            InvitedByUserId = inviter.Id,
            TenantId = tenantA.Id,
            UserType = UserType.Tenant,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await Assert.ThrowsAnyAsync<BusinessException>(() =>
            service.AcceptAsync(inviteDeleted, new AcceptInput("Deleted Test", "Password123!")));
        Assert.False(await _context.Users.AnyAsync(u => u.Email == inviteDeleted.Email));

        // Scenario 3: Mismatched tenant role
        var inviteMismatched = new Invitation
        {
            Email = $"test-mismatched-{suffix}@example.com",
            FullName = "Mismatched Test",
            RoleId = mismatchedTenantRole.Id,
            InvitedByUserId = inviter.Id,
            TenantId = tenantA.Id,
            UserType = UserType.Tenant,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await Assert.ThrowsAnyAsync<BusinessException>(() =>
            service.AcceptAsync(inviteMismatched, new AcceptInput("Mismatched Test", "Password123!")));
        Assert.False(await _context.Users.AnyAsync(u => u.Email == inviteMismatched.Email));
    }

}

[Collection("Database collection")]
public class InvitationTransactionTests
{
    private readonly DatabaseFixture _fixture;

    public InvitationTransactionTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AcceptAsync_WhenRoleAssignmentFails_RollsBackCreatedUser()
    {
        await using var context = _fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = new Tenant { TenantNo = $"T-{suffix}", Name = $"Tenant-{suffix}" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        var tenantInviter = new ApplicationUser
        {
            Id = $"tenant-inviter-{suffix}",
            UserName = $"inviter-{suffix}@example.com",
            Email = $"inviter-{suffix}@example.com",
            UserType = UserType.Tenant,
            TenantId = tenant.Id,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        var kiraciYoneticisiRole = new Role
        {
            Name = RoleNames.KiraciYoneticisi,
            Scope = RoleScope.Tenant,
            IsSystemRole = true,
            IsActive = true
        };
        context.Users.Add(tenantInviter);
        context.Roller.Add(kiraciYoneticisiRole);
        await context.SaveChangesAsync();

        var targetEmail = $"rollback-target-{suffix}@example.com";

        try
        {
            var userRoleService = new UserRoleService(
                new UserRoleRepository(context),
                new RoleRepository(context),
                new ApplicationUserRepository(context),
                new UnitOfWork(context),
                new NoOpUserPermissionCacheInvalidator());
            var target = new InvitationService(
                new InvitationRepository(context),
                new RoleRepository(context),
                new UserPermissionScopeRepository(context),
                new UnitOfWork(context),
                null!,
                null!,
                null!,
                null!,
                new NoOpAuditService(),
                new TestApplicationUserManager(context),
                userRoleService,
                new NoOpPermissionScopeCache(),
                null!,
                NullLogger<InvitationService>.Instance);
            var interceptor = new TransactionInterceptor(
                context,
                NullLogger<TransactionInterceptor>.Instance);
            var service = new ProxyGenerator().CreateInterfaceProxyWithTarget<IInvitationService>(
                target,
                interceptor.ToInterceptor());
            var invitation = new Invitation
            {
                Email = targetEmail,
                FullName = "Rollback Target",
                RoleId = kiraciYoneticisiRole.Id,
                InvitedByUserId = tenantInviter.Id,
                TenantId = tenant.Id,
                UserType = UserType.Tenant,
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            await Assert.ThrowsAnyAsync<BusinessException>(() =>
                service.AcceptAsync(invitation, new AcceptInput("Rollback Target", "Password123!")));

            context.ChangeTracker.Clear();
            Assert.False(await context.Users.IgnoreQueryFilters().AnyAsync(user => user.Email == targetEmail));
        }
        finally
        {
            context.ChangeTracker.Clear();
            var unexpectedUsers = await context.Users.IgnoreQueryFilters()
                .Where(user => user.Email == targetEmail)
                .ToListAsync();
            context.Users.RemoveRange(unexpectedUsers);

            var storedRole = await context.Roller.IgnoreQueryFilters()
                .SingleAsync(role => role.Id == kiraciYoneticisiRole.Id);
            var storedInviter = await context.Users.IgnoreQueryFilters()
                .SingleAsync(user => user.Id == tenantInviter.Id);
            var storedTenant = await context.Tenants.IgnoreQueryFilters()
                .SingleAsync(item => item.Id == tenant.Id);
            context.Roller.Remove(storedRole);
            context.Users.Remove(storedInviter);
            context.Tenants.Remove(storedTenant);
            await context.SaveChangesAsync();
        }
    }
}

internal sealed class NoOpApplicationUserManager : IApplicationUserManager
{
    public Task<ApplicationUser?> FindByIdAsync(string userId) => Task.FromResult<ApplicationUser?>(null);
    public Task<ApplicationUser?> FindByEmailAsync(string email) => Task.FromResult<ApplicationUser?>(null);
    public string NormalizeEmail(string email) => email.ToUpperInvariant();
    public Task<AppIdentityResult> CreateAsync(ApplicationUser user, string? password = null) => Task.FromResult(AppIdentityResult.Success());
    public Task<AppIdentityResult> UpdateAsync(ApplicationUser user) => Task.FromResult(AppIdentityResult.Success());
    public Task<AppIdentityResult> UpdateSecurityStampAsync(ApplicationUser user) => Task.FromResult(AppIdentityResult.Success());
    public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user) => Task.FromResult("token");
    public Task<AppIdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword) => Task.FromResult(AppIdentityResult.Success());
}

internal sealed class NoOpPermissionScopeCache : IPermissionScopeCache
{
    public Task<UserScopeDto> GetAsync(string userId) => Task.FromResult(new UserScopeDto());
    public void Invalidate(string userId) { }
    public void InvalidateMany(IEnumerable<string> userIds) { }
}

internal sealed class NoOpUserPermissionCacheInvalidator : IUserPermissionCacheInvalidator
{
    public void InvalidateAfterCommit(string userId) { }
    public void InvalidateManyAfterCommit(IEnumerable<string> userIds) { }
}

internal sealed class TestApplicationUserManager(ApplicationDbContext context) : IApplicationUserManager
{
    public Task<ApplicationUser?> FindByIdAsync(string userId) => context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    public Task<ApplicationUser?> FindByEmailAsync(string email) => context.Users.FirstOrDefaultAsync(u => u.Email == email);
    public string NormalizeEmail(string email) => email.ToUpperInvariant();
    public async Task<AppIdentityResult> CreateAsync(ApplicationUser user, string? password = null)
    {
        if (string.IsNullOrEmpty(user.Id)) user.Id = Guid.NewGuid().ToString();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return AppIdentityResult.Success();
    }
    public async Task<AppIdentityResult> UpdateAsync(ApplicationUser user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return AppIdentityResult.Success();
    }
    public Task<AppIdentityResult> UpdateSecurityStampAsync(ApplicationUser user) => Task.FromResult(AppIdentityResult.Success());
    public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user) => Task.FromResult("token");
    public Task<AppIdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword) => Task.FromResult(AppIdentityResult.Success());
}

internal sealed class RecordingAuditService : IAuditService
{
    public List<string> Actions { get; } = [];

    public Task LogAsync(
        string eventType,
        string? entityType = null,
        string? entityId = null,
        string? details = null)
    {
        Actions.Add(eventType);
        return Task.CompletedTask;
    }

    public Task<QueryResult> QueryAsync(QueryInput input, CancellationToken ct = default)
        => throw new NotSupportedException();
}
