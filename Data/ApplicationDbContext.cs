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
    public DbSet<PaymentLinkRecord> OdemeLinkKayitlari { get; set; }

    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<Document> Belgeler { get; set; }
    public DbSet<DocumentContent> DocumentContents { get; set; }

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
            entity.Property(t => t.UnitStructure).HasComment(EC<UnitStructure>());
            entity.HasOne(t => t.PropertyType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Unit>(entity =>
        {
            entity.Property(b => b.Name).HasMaxLength(200);
            entity.Property(b => b.UnitNo).HasMaxLength(50);
            entity.Property(b => b.Area).HasPrecision(18, 2);
            entity.HasOne(b => b.Property)
                  .WithMany(t => t.Units)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.UnitType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
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
            // Not: "Bir birimde tek devam-eden aktif sözleşme" kuralı tarih koşulu içerdiğinden
            // filtered index ile ifade edilemez (SQL Server GETDATE() destegi yok);
            // kontrol uygulama katmanında yapılır (LeaseController.Create).
            entity.HasIndex(s => s.UnitId, "IX_Lease_UnitId")
                  .HasDatabaseName("IX_Sozlesmeler_BirimId");
            entity.HasIndex(s => s.TenantId)
                  .HasDatabaseName("IX_Sozlesmeler_KiraciId_Aktif")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(s => s.UnitId, "UX_Lease_UnitId_OpenApplication")
                  .IsUnique()
                  .HasDatabaseName("UX_Sozlesmeler_BirimId_AcikBasvuru")
                  .HasFilter("[IsDeleted] = 0 AND [Durum] IN (4, 5)");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Sozlesmeler_TarihSirasi", "[BitisTarihi] > [BaslangicTarihi]");
                t.HasCheckConstraint("CK_Sozlesmeler_VadeGunu", "[VadeGunu] BETWEEN 1 AND 31");
            });
        });

        builder.Entity<LeaseActivityLog>(entity =>
        {
            entity.Property(g => g.Description).HasMaxLength(1000);
            entity.Property(g => g.ActivityType).HasComment(EC<LeaseActivityType>());
            entity.Property(g => g.OldRentAmount).HasPrecision(18, 2);
            entity.Property(g => g.NewRentAmount).HasPrecision(18, 2);
            entity.Property(g => g.InflationRate).HasPrecision(5, 2);
            entity.Property(g => g.KdvRate).HasPrecision(5, 2);
            entity.Property(g => g.KdvAmount).HasPrecision(18, 2);
            entity.Property(g => g.KdvIncludedAmount).HasPrecision(18, 2);
            entity.HasOne(g => g.Lease)
                  .WithMany(s => s.ActivityLog)
                  .HasForeignKey(g => g.LeaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LeaseReviewHistory>(entity =>
        {
            entity.Property(g => g.ActionType).HasComment(EC<LeaseReviewActionType>());
            entity.Property(g => g.FromStatus).HasComment(EC<LeaseStatus>());
            entity.Property(g => g.ToStatus).HasComment(EC<LeaseStatus>());
            entity.Property(g => g.Explanation).HasMaxLength(1000);
            entity.Property(g => g.ActorUserId).IsRequired();
            entity.HasOne(g => g.Lease)
                  .WithMany(s => s.ReviewHistory)
                  .HasForeignKey(g => g.LeaseId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(g => g.ActorUser)
                  .WithMany()
                  .HasForeignKey(g => g.ActorUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(g => new { g.LeaseId, g.ActionDate })
                  .HasDatabaseName("IX_SozlesmeIncelemeGecmisleri_SozlesmeId_IslemTarihi");
            entity.HasIndex(g => g.ActorUserId)
                  .HasDatabaseName("IX_SozlesmeIncelemeGecmisleri_IslemYapanKullaniciId");
        });

        builder.Entity<UserPermission>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.Permission }).IsUnique();
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.Permission).IsRequired().HasMaxLength(100);
        });

        builder.Entity<UserPermissionScope>(entity =>
        {
            entity.Property(k => k.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(k => new { k.UserId, k.ScopeType, k.ScopeId }).IsUnique();
        });

        builder.Entity<PropertyType>(entity =>
        {
            entity.Property(k => k.Name).HasMaxLength(150);
            entity.Property(k => k.Code).HasMaxLength(50);
            entity.HasIndex(k => k.Code).IsUnique();
        });

        builder.Entity<Category>(entity =>
        {
            entity.Property(k => k.Name).HasMaxLength(150);
            entity.Property(k => k.Code).HasMaxLength(50);
            entity.HasIndex(k => new { k.Type, k.Code }).IsUnique();
        });

        builder.Entity<UnitType>(entity =>
        {
            entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Code).IsRequired().HasMaxLength(100);
            entity.HasIndex(b => b.Code).IsUnique();
            entity.Property(b => b.Usage)
                  .HasDefaultValue(UnitTypeUsage.Rentable)
                  .HasComment(EC<UnitTypeUsage>());

            entity.HasOne(b => b.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PropertyRateOverride>(entity =>
        {
            entity.Property(f => f.UnitValue).HasPrecision(18, 2);
            entity.Property(f => f.KdvRate).HasPrecision(5, 2);
            entity.HasIndex(f => new { f.PropertyId, f.TenantCategoryId, f.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_TasinmazTarifeler_TasinmazKategoriBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TasinmazTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
            });
            entity.HasOne(f => f.Property)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.TenantCategory)
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

        builder.Entity<RateSchedule>(entity =>
        {
            entity.Property(k => k.UnitValue).HasPrecision(18, 4);
            entity.Property(k => k.KdvRate).HasPrecision(5, 2);
            entity.Property(k => k.CalculationMethod).HasComment(EC<CalculationMethod>());
            entity.HasIndex(k => new { k.Year, k.TenantCategoryId, k.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_GenelTarifeler_YilKategoriBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_GenelTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
            });
            entity.HasOne(k => k.TenantCategory)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(k => k.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaseRateOverride>(entity =>
        {
            entity.Property(r => r.UnitValue).HasPrecision(18, 4);
            entity.Property(r => r.KdvRate).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.LeaseId, r.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_SozlesmeTarifeler_SozlesmeBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_SozlesmeTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
            });
            entity.HasOne(r => r.Lease)
                  .WithMany(s => s.LeaseRateOverrides)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.ChargeType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UnitRate>(entity =>
        {
            entity.Property(r => r.UnitValue).HasPrecision(18, 4);
            entity.Property(r => r.KdvRate).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.UnitId, r.TenantCategoryId, r.ChargeTypeId })
                  .IsUnique()
                  .HasDatabaseName("UX_BirimTarifeler_BirimKategoriBorc")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_BirimTarifeler_Degerler", "[BirimDeger] >= 0 AND [KdvOrani] BETWEEN 0 AND 100");
            });
            entity.HasOne(r => r.Unit)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.TenantCategory)
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
                  .HasFilter("[SozlesmeId] IS NOT NULL AND [KaynakTipi] = 1 AND [IsDeleted] = 0");
            entity.HasIndex(t => t.TenantId)
                  .HasDatabaseName("IX_Tahakkuklar_KiraciId_Aktif")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(t => t.UnitId)
                  .HasDatabaseName("IX_Tahakkuklar_BirimId_Aktif")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasIndex(t => t.ReservationId)
                  .IsUnique()
                  .HasDatabaseName("UX_Tahakkuklar_RezervasyonId_TekTahakkuk")
                  .HasFilter("[RezervasyonId] IS NOT NULL AND [IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Tahakkuklar_TarihSirasi", "[DonemBitisi] > [DonemBaslangici]");
                t.HasCheckConstraint("CK_Tahakkuklar_Tutarlar_Pozitif", "[BeklenenTutar] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0 AND [OdenenTutar] >= 0");
                t.HasCheckConstraint("CK_Tahakkuklar_OdenenLimit", "[OdenenTutar] <= [ToplamTutar]");
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
                t.HasCheckConstraint("CK_TahakkukKalemleri_Tutarlar_Pozitif", "[Tutar] >= 0 AND [KdvTutari] >= 0 AND [ToplamTutar] >= 0");
                t.HasCheckConstraint("CK_TahakkukKalemleri_KdvOrani", "[KdvOrani] BETWEEN 0 AND 100");
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
                t.HasCheckConstraint("CK_TahakkukOdemeler_Tutar_Pozitif", "[Tutar] > 0");
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
                  .HasFilter("[BankaReferansNo] IS NOT NULL AND [IsDeleted] = 0");
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

        builder.Entity<ReservationRateOverride>(entity =>
        {
            entity.Property(r => r.PeriodRate).HasPrecision(18, 2);
            entity.Property(r => r.KdvRate).HasPrecision(5, 2);
            entity.Property(r => r.Description).HasMaxLength(300);
            entity.HasOne(r => r.Unit)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(r => r.UnitType)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => new { r.UnitTypeId, r.Year })
                  .IsUnique()
                  .HasDatabaseName("UX_RezervasyonTarifeler_BirimTuruYil_GenelKural")
                  .HasFilter("[BirimId] IS NULL");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_RezervasyonTarife_BirimOrYilTuru",
                    "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)");
                t.HasCheckConstraint(
                    "CK_RezervasyonTarifeler_Degerler_Pozitif",
                    "[PeriyotUcreti] >= 0 AND [UcretsizSureDakika] >= 0 AND [UcretlendirmePeriyoduDakika] > 0 AND [KdvOrani] BETWEEN 0 AND 100");
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
                  .HasDatabaseName("IX_Rezervasyonlari_KiraciId_Aktif")
                  .HasFilter("[IsDeleted] = 0");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rezervasyonlari_TarihSirasi", "[BitisTarihi] > [BaslangicTarihi]");
                t.HasCheckConstraint("CK_Rezervasyonlari_Tutarlar_Pozitif", "[TarifeTutari] >= 0 AND [ToplamTutar] >= 0 AND ([KdvTutari] IS NULL OR [KdvTutari] >= 0)");
                t.HasCheckConstraint("CK_Rezervasyonlari_KdvOrani", "[KdvOrani] IS NULL OR [KdvOrani] BETWEEN 0 AND 100");
            });
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(u => u.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ApplicationUser_SuperAdmin_KiraciYok", "[IsSuperAdmin] = 0 OR [KiraciId] IS NULL");
            });
        });

        builder.Entity<Role>(entity =>
        {
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.Property(r => r.Scope).HasComment(EC<RoleScope>());
            entity.HasIndex(r => new { r.Scope, r.TenantId, r.Name }).IsUnique();
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(r => r.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.Property(rp => rp.Permission).IsRequired().HasMaxLength(150);
            entity.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();
            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
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

        builder.Entity<Invitation>(entity =>
        {
            entity.Property(d => d.Email).IsRequired().HasMaxLength(256);
            entity.Property(d => d.FullName).HasMaxLength(200);
            entity.Property(d => d.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(d => new { d.Email, d.Status });
            entity.HasIndex(d => d.TenantId)
                  .HasDatabaseName("IX_Davetiyeler_KiraciId_Aktif")
                  .HasFilter("[IsDeleted] = 0");
            entity.HasOne(d => d.Role)
                  .WithMany()
                  .HasForeignKey(d => d.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PasswordResetRequest>(entity =>
        {
            entity.Property(t => t.UserId).IsRequired();
            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(t => t.RequestIp).HasMaxLength(64);
            entity.HasIndex(t => new { t.UserId, t.Status });
        });

        builder.Entity<PaymentLinkRecord>(entity =>
        {
            entity.Property(o => o.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(o => o.CancelledByUserId).HasMaxLength(450);
            entity.HasIndex(o => new { o.TenantId, o.Status });
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
            entity.Property(b => b.TargetEntity).HasComment(EC<DocumentOwnerType>());
            entity.HasOne(b => b.TemplateDocument)
                  .WithMany()
                  .HasForeignKey(b => b.TemplateDocumentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Document>(entity =>
        {
            entity.Property(b => b.FileName).HasMaxLength(255).IsRequired();
            entity.Property(b => b.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Description).HasMaxLength(500);
            entity.Property(b => b.OwnerType).HasComment(EC<DocumentOwnerType>());
            entity.HasOne(b => b.DocumentType)
                  .WithMany()
                  .HasForeignKey(b => b.DocumentTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.ReplacedByDocument)
                  .WithMany()
                  .HasForeignKey(b => b.ReplacedByDocumentId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(b => new { b.OwnerType, b.OwnerId, b.IsInvalid, b.IsDeleted });
            entity.HasIndex(b => b.DocumentTypeId);
        });

        builder.Entity<DocumentContent>(entity =>
        {
            entity.HasKey(i => i.DocumentId);
            entity.HasOne(i => i.Document)
                  .WithOne(b => b.Content)
                  .HasForeignKey<DocumentContent>(i => i.DocumentId)
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
            k => !k.IsDeleted && (!_currentUser.IsKiraciUser || k.Id == _currentUser.TenantId));

        builder.Entity<Lease>().HasQueryFilter(
            s => !s.IsDeleted && (!_currentUser.IsKiraciUser || s.TenantId == _currentUser.TenantId));

        builder.Entity<Charge>().HasQueryFilter(
            t => !t.IsDeleted && (!_currentUser.IsKiraciUser || t.TenantId == _currentUser.TenantId));

        builder.Entity<PaymentAllocation>().HasQueryFilter(
            o => !o.IsDeleted && (!_currentUser.IsKiraciUser || o.Charge.TenantId == _currentUser.TenantId));

        builder.Entity<Reservation>().HasQueryFilter(
            r => !r.IsDeleted && (!_currentUser.IsKiraciUser || r.TenantId == _currentUser.TenantId));

        builder.Entity<LeaseActivityLog>().HasQueryFilter(
            g => !g.IsDeleted && (!_currentUser.IsKiraciUser ||
                 g.Lease!.TenantId == _currentUser.TenantId));

        builder.Entity<UserRole>().HasQueryFilter(ur => !ur.IsDeleted);
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
                TargetEntity = DocumentOwnerType.Payment,
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
