using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace KiraTakip.Data;

public class ApplicationDbContext : IdentityUserContext<ApplicationUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserContext _currentUser;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor,
        ICurrentUserContext currentUser)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
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
                entry.Entity.IsSuperAdmin && entry.Entity.KiraciId != null)
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
    public DbSet<SozlesmeIslemGecmisi> SozlesmeIslemGecmisleri { get; set; }
    public DbSet<KullaniciYetkiKapsami> KullaniciYetkiKapsamlari { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    public DbSet<UnitType> BirimTurleri { get; set; }
    public DbSet<TasinmazTipi> TasinmazTipleri { get; set; }
    public DbSet<Kategori> Kategoriler { get; set; }

    public DbSet<TasinmazTarife> TasinmazTarifeler { get; set; }

    public DbSet<RezervasyonTarife> RezervasyonTarifeler { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    public DbSet<ChargeType> ChargeTypes { get; set; }
    public DbSet<GenelTarife> GenelTarifeler { get; set; }
    public DbSet<BirimTarife> BirimTarifeler { get; set; }
    public DbSet<SozlesmeTarife> SozlesmeTarifeler { get; set; }

    public DbSet<Charge> Charges { get; set; }
    public DbSet<ChargeLineItem> ChargeLineItems { get; set; }
    public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
    public DbSet<BankTransaction> BankTransactions { get; set; }
    public DbSet<PaymentMatch> PaymentMatches { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }
    public DbSet<Rol> Roller { get; set; }
    public DbSet<RolPermission> RolPermissions { get; set; }
    public DbSet<UserRol> UserRoller { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Davetiye> Davetiyeler { get; set; }
    public DbSet<SifreSifirlamaTalebi> SifreSifirlamaTalepleri { get; set; }
    public DbSet<OdemeLinkKayit> OdemeLinkKayitlari { get; set; }

    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<Belge> Belgeler { get; set; }
    public DbSet<BelgeIcerik> BelgeIcerikleri { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LookupValue>(entity =>
        {
            entity.Property(e => e.EnumName).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.HasIndex(e => new { e.EnumName, e.Value }).IsUnique();
        });

        builder.Entity<Property>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(200);
            entity.Property(t => t.City).HasMaxLength(100);
            entity.Property(t => t.District).HasMaxLength(100);
            entity.Property(t => t.Neighborhood).HasMaxLength(200);
            entity.Property(t => t.Address).HasMaxLength(500);
            entity.Property(t => t.OpenArea).HasPrecision(18, 2);
            entity.Property(t => t.ClosedArea).HasPrecision(18, 2);
            entity.Property(t => t.RentalMode).HasComment(EC<RentalMode>());
            entity.HasOne(t => t.PropertyType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Unit>(entity =>
        {
            entity.Property(b => b.Name).HasMaxLength(200);
            entity.Property(b => b.UnitNo).HasMaxLength(50);
            entity.Property(b => b.Area).HasPrecision(18, 2);
            entity.Property(b => b.UnitKind).HasComment(EC<UnitKind>());
            entity.HasOne(b => b.Property)
                  .WithMany(t => t.Units)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.UnitType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Tenant>(entity =>
        {
            entity.Property(k => k.TenantNo).HasMaxLength(20);
            entity.HasIndex(k => k.TenantNo).IsUnique();
            entity.Property(k => k.Name).HasMaxLength(200);
            entity.Property(k => k.Phone).HasMaxLength(30);
            entity.Property(k => k.Email).HasMaxLength(200);
            entity.Property(k => k.TaxNo).HasMaxLength(20);
            entity.HasIndex(k => k.TaxNo)
                  .IsUnique()
                  .HasDatabaseName("UX_Kiraciler_VergiNo")
                  .HasFilter("[VergiNo] IS NOT NULL AND [VergiNo] <> ''");
            entity.HasOne(k => k.TenantCategory)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(k => k.Sector)
                  .WithMany()
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        builder.Entity<Lease>(entity =>
        {
            entity.Property(s => s.Status).HasComment(EC<LeaseStatus>());
            entity.HasOne(s => s.Unit)
                  .WithMany(b => b.Leases)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.Tenant)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(s => s.UnitId)
                  .IsUnique()
                  .HasDatabaseName("UX_Sozlesmeler_BirimId_TekAktifSozlesme")
                  .HasFilter("[Durum] = 1 AND [IsDeleted] = 0");
            entity.HasIndex(s => s.TenantId)
                  .HasDatabaseName("IX_Sozlesmeler_KiraciId_Active")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Sozlesmeler_TarihSirasi", "[EndDate] > [StartDate]");
                t.HasCheckConstraint("CK_Sozlesmeler_VadeGunu", "[VadeGunu] BETWEEN 1 AND 31");
            });
        });

        builder.Entity<SozlesmeIslemGecmisi>(entity =>
        {
            entity.Property(g => g.Aciklama).HasMaxLength(1000);
            entity.Property(g => g.IslemTipi).HasComment(EC<LeaseActivityType>());
            entity.Property(g => g.EskiKiraBedeli).HasPrecision(18, 2);
            entity.Property(g => g.YeniKiraBedeli).HasPrecision(18, 2);
            entity.Property(g => g.TufeOrani).HasPrecision(5, 2);
            entity.Property(g => g.KdvRate).HasPrecision(5, 2);
            entity.Property(g => g.KdvTutari).HasPrecision(18, 2);
            entity.Property(g => g.KdvDahilTutar).HasPrecision(18, 2);
            entity.HasOne(g => g.Lease)
                  .WithMany(s => s.ActivityLog)
                  .HasForeignKey(g => g.LeaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserPermission>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.Permission }).IsUnique();
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.Permission).IsRequired().HasMaxLength(100);
        });

        builder.Entity<KullaniciYetkiKapsami>(entity =>
        {
            entity.Property(k => k.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(k => new { k.UserId, k.ScopeType, k.KapsamId }).IsUnique();
        });

        builder.Entity<TasinmazTipi>(entity =>
        {
            entity.Property(k => k.Ad).HasMaxLength(150);
            entity.Property(k => k.Kod).HasMaxLength(50);
            entity.HasIndex(k => k.Kod).IsUnique();
        });

        builder.Entity<Kategori>(entity =>
        {
            entity.Property(k => k.Ad).HasMaxLength(150);
            entity.Property(k => k.Kod).HasMaxLength(50);
            entity.HasIndex(k => new { k.Tipi, k.Kod }).IsUnique();
        });

        builder.Entity<UnitType>(entity =>
        {
            entity.Property(b => b.Ad).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Kod).IsRequired().HasMaxLength(100);
            entity.HasIndex(b => b.Kod).IsUnique();

            entity.HasOne(b => b.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TasinmazTarife>(entity =>
        {
            entity.Property(f => f.UnitValue).HasPrecision(18, 2);
            entity.Property(f => f.KdvRate).HasPrecision(5, 2);
            entity.HasIndex(f => new { f.PropertyId, f.KiraciKategoriId, f.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_TasinmazTarifeler_TasinmazKategoriBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TasinmazTarifeler_Degerler", "[UnitValue] >= 0 AND [KdvRate] BETWEEN 0 AND 100");
            });
            entity.HasOne(f => f.Property)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.KiraciKategori)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(f => f.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ChargeType>(entity =>
        {
            entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Code).IsRequired().HasMaxLength(100);
            entity.HasIndex(b => b.Code).IsUnique();
            entity.Property(b => b.Behavior).HasComment(EC<ChargeTypeBehavior>());
        });

        builder.Entity<GenelTarife>(entity =>
        {
            entity.Property(k => k.UnitValue).HasPrecision(18, 4);
            entity.Property(k => k.KdvRate).HasPrecision(5, 2);
            entity.Property(k => k.CalculationMethod).HasComment(EC<CalculationMethod>());
            entity.HasIndex(k => new { k.Yil, k.KiraciKategoriId, k.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_GenelTarifeler_YilKategoriBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_GenelTarifeler_Degerler", "[UnitValue] >= 0 AND [KdvRate] BETWEEN 0 AND 100");
            });
            entity.HasOne(k => k.KiraciKategori)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(k => k.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SozlesmeTarife>(entity =>
        {
            entity.Property(r => r.UnitValue).HasPrecision(18, 4);
            entity.Property(r => r.KdvRate).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.LeaseId, r.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_SozlesmeTarifeler_SozlesmeBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_SozlesmeTarifeler_Degerler", "[UnitValue] >= 0 AND [KdvRate] BETWEEN 0 AND 100");
            });
            entity.HasOne(r => r.Lease)
                  .WithMany(s => s.LeaseRateOverrides)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BirimTarife>(entity =>
        {
            entity.Property(r => r.UnitValue).HasPrecision(18, 4);
            entity.Property(r => r.KdvRate).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.UnitId, r.KiraciKategoriId, r.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_BirimTarifeler_BirimKategoriBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_BirimTarifeler_Degerler", "[UnitValue] >= 0 AND [KdvRate] BETWEEN 0 AND 100");
            });
            entity.HasOne(r => r.Unit)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.KiraciKategori)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Charge>(entity =>
        {
            entity.Property(t => t.ExpectedAmount).HasPrecision(18, 2);
            entity.Property(t => t.Status).HasComment(EC<ChargeStatus>());
            entity.Property(t => t.SourceType).HasComment(EC<ChargeSourceType>());
            entity.Property(t => t.KdvAmount).HasPrecision(18, 2);
            entity.Property(t => t.TotalAmount).HasPrecision(18, 2);
            entity.Property(t => t.PaidAmount).HasPrecision(18, 2);
            entity.Property(t => t.CancellationNote).HasMaxLength(500);
            entity.HasOne(t => t.Tenant)
                  .WithMany()
                  .HasForeignKey(t => t.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Unit)
                  .WithMany()
                  .HasForeignKey(t => t.UnitId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Lease)
                  .WithMany()
                  .HasForeignKey(t => t.LeaseId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Reservation)
                  .WithMany()
                  .HasForeignKey(t => t.ReservationId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => new { t.LeaseId, t.PeriodStart })
                  .IsUnique()
                  .HasDatabaseName("UX_Tahakkuklar_SozlesmeDonem_TekTahakkuk")
                  .HasFilter("[LeaseId] IS NOT NULL AND [SourceType] = 1 AND [IsDeleted] = 0");
            entity.HasIndex(t => t.TenantId)
                  .HasDatabaseName("IX_Tahakkuklar_KiraciId_Active")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(t => t.UnitId)
                  .HasDatabaseName("IX_Tahakkuklar_BirimId_Active")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(t => t.ReservationId)
                  .IsUnique()
                  .HasDatabaseName("UX_Tahakkuklar_RezervasyonId_TekTahakkuk")
                  .HasFilter("[ReservationId] IS NOT NULL AND [IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Tahakkuklar_TarihSirasi", "[PeriodEnd] > [PeriodStart]");
                t.HasCheckConstraint("CK_Tahakkuklar_Tutarlar_Pozitif", "[ExpectedAmount] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0 AND [PaidAmount] >= 0");
                t.HasCheckConstraint("CK_Tahakkuklar_OdenenLimit", "[PaidAmount] <= [ToplamTutar]");
            });
        });

        builder.Entity<ChargeLineItem>(entity =>
        {
            entity.Property(k => k.Description).HasMaxLength(200);
            entity.Property(k => k.UnitValue).HasPrecision(18, 4);
            entity.Property(k => k.CalculationMethod).HasComment(EC<CalculationMethod>());
            entity.Property(k => k.SourceType).HasComment(EC<LineItemSourceType>());
            entity.Property(k => k.Multiplier).HasPrecision(18, 4);
            entity.Property(k => k.Amount).HasPrecision(18, 2);
            entity.Property(k => k.KdvRate).HasPrecision(5, 2);
            entity.Property(k => k.KdvAmount).HasPrecision(18, 2);
            entity.Property(k => k.TotalAmount).HasPrecision(18, 2);
            entity.HasOne(k => k.Charge)
                  .WithMany(t => t.LineItems)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(k => k.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TahakkukKalemleri_Tutarlar_Pozitif", "[Amount] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0");
                t.HasCheckConstraint("CK_TahakkukKalemleri_KdvOrani", "[KdvRate] BETWEEN 0 AND 100");
            });
        });

        builder.Entity<PaymentAllocation>(entity =>
        {
            entity.Property(o => o.Amount).HasPrecision(18, 2);
            entity.Property(o => o.RejectionReason).HasMaxLength(500);
            entity.Property(o => o.PosReferenceNo).HasMaxLength(100);
            entity.Property(o => o.PaymentChannel).HasComment(EC<PaymentChannel>());
            entity.Property(o => o.PaymentSourceType).HasComment(EC<PaymentSourceType>());
            entity.Property(o => o.Status).HasComment(EC<PaymentStatus>());
            entity.HasOne(o => o.Charge)
                  .WithMany(t => t.Allocations)
                  .HasForeignKey(o => o.ChargeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.Lease)
                  .WithMany()
                  .HasForeignKey(o => o.LeaseId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(o => o.GirenUser)
                  .WithMany()
                  .HasForeignKey(o => o.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.OnaylayanUser)
                  .WithMany()
                  .HasForeignKey(o => o.ApprovedByUserId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TahakkukOdemeler_Tutar_Pozitif", "[Amount] > 0");
            });
        });

        builder.Entity<BankTransaction>(entity =>
        {
            entity.Property(b => b.TransactionAmount).HasPrecision(18, 2);
            entity.Property(b => b.Description).HasMaxLength(500);
            entity.Property(b => b.MatchStatus).HasComment(EC<BankMatchStatus>());
            entity.Property(b => b.SenderIban).HasMaxLength(50);
            entity.Property(b => b.SenderInfo).HasMaxLength(200);
            entity.Property(b => b.BankReferenceNo).HasMaxLength(100);
            entity.Property(b => b.BankCode).HasMaxLength(20);
            entity.HasIndex(b => b.BankReferenceNo)
                  .IsUnique()
                  .HasDatabaseName("UX_BankaHareketleri_BankaReferansNo")
                  .HasFilter("[BankReferenceNo] IS NOT NULL AND [IsDeleted] = 0");
        });

        builder.Entity<PaymentMatch>(entity =>
        {
            entity.Property(e => e.MatchType).HasComment(EC<KiraTakip.Models.MatchType>());
            entity.HasOne(e => e.PaymentAllocation)
                  .WithMany(o => o.BankMatches)
                  .HasForeignKey(e => e.PaymentAllocationId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BankTransaction)
                  .WithMany(b => b.Matches)
                  .HasForeignKey(e => e.BankTransactionId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.BankTransactionId)
                  .IsUnique()
                  .HasDatabaseName("UX_OdemeBankaEslesmeleri_BankaHareketi_Birebir")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(e => e.PaymentAllocationId)
                  .IsUnique()
                  .HasDatabaseName("UX_OdemeBankaEslesmeleri_TahakkukOdeme_Birebir")
                  .HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<RezervasyonTarife>(entity =>
        {
            entity.Property(r => r.PeriyotUcreti).HasPrecision(18, 2);
            entity.Property(r => r.KdvRate).HasPrecision(5, 2);
            entity.Property(r => r.Aciklama).HasMaxLength(300);
            entity.HasOne(r => r.Unit)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(r => r.UnitType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => new { r.UnitTypeId, r.Yil })
                  .IsUnique()
                  .HasDatabaseName("UX_RezervasyonTarifeler_UnitTypeYil_GenelKural")
                  .HasFilter("[BirimId] IS NULL");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_RezervasyonTarife_BirimOrYilTuru",
                    "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)");
                t.HasCheckConstraint(
                    "CK_RezervasyonTarifeler_Degerler_Pozitif",
                    "[PeriyotUcreti] >= 0 AND [FreeDurationMinutes] >= 0 AND [UcretlendirmePeriyoduDakika] > 0 AND [KdvRate] BETWEEN 0 AND 100");
            });
        });

        builder.Entity<Reservation>(entity =>
        {
            entity.Property(r => r.UnitRate).HasPrecision(18, 2);
            entity.Property(r => r.RateAmount).HasPrecision(18, 2);
            entity.Property(r => r.KdvRate).HasPrecision(5, 2);
            entity.Property(r => r.KdvAmount).HasPrecision(18, 2);
            entity.Property(r => r.TotalAmount).HasPrecision(18, 2);
            entity.Property(r => r.Status).HasComment(EC<ReservationStatus>());
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.HasOne(r => r.Unit)
                  .WithMany()
                  .HasForeignKey(r => r.UnitId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Tenant)
                  .WithMany()
                  .HasForeignKey(r => r.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => new { r.UnitId, r.StartDate });
            entity.HasIndex(r => r.TenantId)
                  .HasDatabaseName("IX_Rezervasyonlari_KiraciId_Active")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rezervasyonlari_TarihSirasi", "[EndDate] > [StartDate]");
                t.HasCheckConstraint("CK_Rezervasyonlari_Tutarlar_Pozitif", "[RateAmount] >= 0 AND [ToplamTutar] >= 0 AND ([KdvTutari] IS NULL OR [KdvTutari] >= 0)");
                t.HasCheckConstraint("CK_Rezervasyonlari_KdvOrani", "[KdvRate] IS NULL OR [KdvRate] BETWEEN 0 AND 100");
            });
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(u => u.KiraciId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ApplicationUser_SuperAdmin_NoTenant", "[IsSuperAdmin] = 0 OR [KiraciId] IS NULL");
            });
        });

        builder.Entity<Rol>(entity =>
        {
            entity.Property(r => r.Ad).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Aciklama).HasMaxLength(500);
            entity.Property(r => r.Scope).HasComment(EC<RoleScope>());
            entity.HasIndex(r => new { r.Scope, r.KiraciId, r.Ad }).IsUnique();
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(r => r.KiraciId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RolPermission>(entity =>
        {
            entity.Property(rp => rp.Permission).IsRequired().HasMaxLength(150);
            entity.HasIndex(rp => new { rp.RolId, rp.Permission }).IsUnique();
            entity.HasOne(rp => rp.Rol)
                  .WithMany(r => r.RolPermissions)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserRol>(entity =>
        {
            entity.HasIndex(ur => new { ur.UserId, ur.RolId }).IsUnique();
            entity.HasOne(ur => ur.Rol)
                  .WithMany(r => r.UserRoller)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.EventType).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityType).HasMaxLength(100);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.IpAddress).HasMaxLength(64);
            entity.Property(a => a.UserAgent).HasMaxLength(500);
            entity.HasIndex(a => new { a.EventType, a.CreatedAt });
            entity.HasIndex(a => new { a.UserId, a.CreatedAt });
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
        });

        builder.Entity<Davetiye>(entity =>
        {
            entity.Property(d => d.Email).IsRequired().HasMaxLength(256);
            entity.Property(d => d.AdSoyad).HasMaxLength(200);
            entity.Property(d => d.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(d => new { d.Email, d.Durum });
            entity.HasIndex(d => d.KiraciId)
                  .HasDatabaseName("IX_Davetiyeler_KiraciId_Active")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasOne(d => d.Rol)
                  .WithMany()
                  .HasForeignKey(d => d.RolId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SifreSifirlamaTalebi>(entity =>
        {
            entity.Property(t => t.UserId).IsRequired();
            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(t => t.TalepEdenIp).HasMaxLength(64);
            entity.HasIndex(t => new { t.UserId, t.Durum });
        });

        builder.Entity<OdemeLinkKayit>(entity =>
        {
            entity.Property(o => o.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(o => o.IptalEdenUserId).HasMaxLength(450);
            entity.HasIndex(o => new { o.TenantId, o.Durum });
            entity.HasOne(o => o.Tenant)
                  .WithMany()
                  .HasForeignKey(o => o.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DocumentType>(entity =>
        {
            entity.Property(b => b.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(b => b.Code).IsUnique();
            entity.Property(b => b.Name).HasMaxLength(200).IsRequired();
            entity.Property(b => b.Description).HasMaxLength(500);
            entity.Property(b => b.AllowedExtensions).HasMaxLength(200);
            entity.Property(b => b.TargetEntity).HasComment(EC<BelgeOwnerTipi>());
            entity.HasOne(b => b.TemplateDocument)
                  .WithMany()
                  .HasForeignKey(b => b.TemplateDocumentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Belge>(entity =>
        {
            entity.Property(b => b.DosyaAdi).HasMaxLength(255).IsRequired();
            entity.Property(b => b.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Aciklama).HasMaxLength(500);
            entity.Property(b => b.OwnerType).HasComment(EC<BelgeOwnerTipi>());
            entity.HasOne(b => b.DocumentType)
                  .WithMany()
                  .HasForeignKey(b => b.DocumentTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.DegistirenBelge)
                  .WithMany()
                  .HasForeignKey(b => b.DegistirenBelgeId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(b => new { b.OwnerType, b.OwnerId, b.Gecersiz, b.IsDeleted });
            entity.HasIndex(b => b.DocumentTypeId);
        });

        builder.Entity<BelgeIcerik>(entity =>
        {
            entity.HasKey(i => i.BelgeId);
            entity.HasOne(i => i.Belge)
                  .WithOne(b => b.Icerik)
                  .HasForeignKey<BelgeIcerik>(i => i.BelgeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.FindProperty(nameof(BaseEntity.IsDeleted)) != null)
            {
                var param = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(param, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false));
                entityType.SetQueryFilter(Expression.Lambda(body, param));
            }
        }

        // Kiracı portal — kiracı kullanıcısı sadece kendi verilerini görür.
        // Bu filtreler soft-delete filter'ın üzerine yazar (IsDeleted + KiraciId koşullarını birleştirir).
        builder.Entity<Tenant>().HasQueryFilter(
            k => !k.IsDeleted && (!_currentUser.IsKiraciUser || k.Id == _currentUser.KiraciId));

        builder.Entity<Lease>().HasQueryFilter(
            s => !s.IsDeleted && (!_currentUser.IsKiraciUser || s.TenantId == _currentUser.KiraciId));

        builder.Entity<Charge>().HasQueryFilter(
            t => !t.IsDeleted && (!_currentUser.IsKiraciUser || t.TenantId == _currentUser.KiraciId));

        builder.Entity<PaymentAllocation>().HasQueryFilter(
            o => !o.IsDeleted && (!_currentUser.IsKiraciUser || o.Charge.TenantId == _currentUser.KiraciId));

        builder.Entity<Reservation>().HasQueryFilter(
            r => !r.IsDeleted && (!_currentUser.IsKiraciUser || r.TenantId == _currentUser.KiraciId));

        builder.Entity<SozlesmeIslemGecmisi>().HasQueryFilter(
            g => !g.IsDeleted && (!_currentUser.IsKiraciUser ||
                 g.Lease!.TenantId == _currentUser.KiraciId));

        builder.Entity<UserRol>().HasQueryFilter(ur => !ur.IsDeleted);
        builder.Entity<UserPermission>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);

        // Production Seeding (HasData)
        builder.Entity<ChargeType>().HasData(
            new ChargeType
            {
                Id = 1,
                Code = "KIRA",
                Name = "Kira Bedeli",
                IsActive = true,
                SortOrder = 1,
                Behavior = ChargeTypeBehavior.MonthlyFixed,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "System",

                IsDeleted = false
            },
            new ChargeType
            {
                Id = 2,
                Code = "DEPOZITO",
                Name = "Depozito",
                IsActive = true,
                SortOrder = 99,
                Behavior = ChargeTypeBehavior.FirstMonthOneTime,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "System",

                IsDeleted = false
            },
            new ChargeType
            {
                Id = 3,
                Code = "DIGER",
                Name = "Diğer",
                IsActive = true,
                SortOrder = 100,
                Behavior = ChargeTypeBehavior.UserManual,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "System",

                IsDeleted = false
            }
        );

        builder.Entity<DocumentType>().HasData(
            new DocumentType
            {
                Id = 1,
                Code = "ODEME_DEKONT",
                Name = "Ödeme Dekontu",
                TargetEntity = BelgeOwnerTipi.Payment,
                AllowedExtensions = "pdf,jpg,jpeg,png",
                MaxSizeMb = 5,
                SortOrder = 1,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "System",
                IsActive = true,
                IsDeleted = false
            }
        );

    }

    private static string EC<TEnum>() where TEnum : struct, Enum
        => string.Join(", ", Enum.GetValues<TEnum>().Select(v => $"{v}={(int)(object)v}"));
}
