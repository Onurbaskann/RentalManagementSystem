using KiraTakip.Services.Interfaces.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace KiraTakip.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IHttpContextAccessor httpContextAccessor,
    ICurrentUserContext currentUser) : IdentityUserContext<ApplicationUser>(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if ((entry.State == EntityState.Added || entry.State == EntityState.Modified) &&
                entry.Entity.IsSuperAdmin && entry.Entity.TenantId != null)
            {
                throw new InvalidOperationException("Bir Süper Admin aynı zamanda bir kiracıya ait olamaz!");
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<Property> Properties { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Lease> Leases { get; set; }
    public DbSet<LeaseActivityLog> SozlesmeIslemGecmisleri { get; set; }
    public DbSet<LeaseReviewHistory> SozlesmeIncelemeGecmisleri { get; set; }
    public DbSet<UserPermissionScope> KullaniciYetkiKapsamlari { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    public DbSet<UnitType> UnitTypes { get; set; }
    public DbSet<PropertyType> TasinmazTipleri { get; set; }
    public DbSet<Category> Kategoriler { get; set; }

    public DbSet<PropertyRateOverride> TasinmazTarifeler { get; set; }

    public DbSet<ReservationRateOverride> RezervasyonTarifeler { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationAttendee> ReservationAttendees { get; set; }

    public DbSet<ChargeType> ChargeTypes { get; set; }
    public DbSet<RateSchedule> GenelTarifeler { get; set; }
    public DbSet<UnitRate> UnitRates { get; set; }
    public DbSet<LeaseRateOverride> SozlesmeTarifeler { get; set; }

    public DbSet<Charge> Charges { get; set; }
    public DbSet<ChargeLineItem> ChargeLineItems { get; set; }
    public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
    public DbSet<BankTransaction> BankTransactions { get; set; }
    public DbSet<PaymentMatch> PaymentMatches { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }
    public DbSet<Role> Roller { get; set; }
    public DbSet<RolePermission> RolPermissions { get; set; }
    public DbSet<UserRole> UserRoller { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Invitation> Davetiyeler { get; set; }
    public DbSet<PasswordResetRequest> SifreSifirlamaTalepleri { get; set; }

    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<Document> Belgeler { get; set; }
    public DbSet<DocumentContent> DocumentContents { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<StoreAccount> StoreAccounts { get; set; }
    public DbSet<PaymentStoreRouting> PaymentStoreRoutings { get; set; }
    public DbSet<OnlinePaymentTransaction> OnlinePaymentTransactions { get; set; }
    public DbSet<OnlinePaymentEvent> OnlinePaymentEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)
                && entityType.FindProperty(nameof(ISoftDeletable.IsDeleted)) != null)
            {
                var param = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(param, nameof(ISoftDeletable.IsDeleted)),
                    Expression.Constant(false));
                entityType.SetQueryFilter(Expression.Lambda(body, param));
            }
        }

        // Kiracı portal — kiracı kullanıcısı sadece kendi verilerini görür.
        // Bu filtreler soft-delete filter'ın üzerine yazar (IsDeleted + KiraciId koşullarını birleştirir).
        builder.Entity<Tenant>().HasQueryFilter(
            k => !k.IsDeleted && (!currentUser.IsKiraciUser || k.Id == currentUser.TenantId));

        builder.Entity<Lease>().HasQueryFilter(
            s => !s.IsDeleted && (!currentUser.IsKiraciUser || s.TenantId == currentUser.TenantId));

        builder.Entity<Charge>().HasQueryFilter(
            t => !t.IsDeleted && (!currentUser.IsKiraciUser || t.TenantId == currentUser.TenantId));

        builder.Entity<PaymentAllocation>().HasQueryFilter(
            o => !o.IsDeleted && (!currentUser.IsKiraciUser || o.Charge.TenantId == currentUser.TenantId));

        builder.Entity<ChargeLineItem>().HasQueryFilter(
            k => !k.IsDeleted && (!currentUser.IsKiraciUser || k.Charge.TenantId == currentUser.TenantId));

        builder.Entity<Reservation>().HasQueryFilter(
            r => !r.IsDeleted && (!currentUser.IsKiraciUser || r.TenantId == currentUser.TenantId));

        builder.Entity<ReservationAttendee>().HasQueryFilter(
            attendee => !attendee.IsDeleted &&
                (!currentUser.IsKiraciUser || attendee.Reservation.TenantId == currentUser.TenantId));

        builder.Entity<LeaseActivityLog>().HasQueryFilter(
            g => !g.IsDeleted && (!currentUser.IsKiraciUser ||
                 g.Lease!.TenantId == currentUser.TenantId));

        builder.Entity<UserRole>().HasQueryFilter(ur => !ur.IsDeleted);
        builder.Entity<UserPermission>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);
    }
}
