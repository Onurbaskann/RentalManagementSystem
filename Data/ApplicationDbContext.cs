using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace KiraTakip.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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

        return await base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<Tasinmaz> Tasinmazlar { get; set; }
    public DbSet<Birim> Birimler { get; set; }
    public DbSet<Kiraci> Kiraciler { get; set; }
    public DbSet<KiraSozlesmesi> Sozlesmeler { get; set; }
    public DbSet<SozlesmeIslemGecmisi> SozlesmeIslemGecmisleri { get; set; }
    public DbSet<KullaniciYetkiKapsami> KullaniciYetkiKapsamlari { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    public DbSet<BirimTuru> BirimTurleri { get; set; }
    public DbSet<Kategori> Kategoriler { get; set; }

    public DbSet<TasinmazTarife> TasinmazTarifeler { get; set; }

    public DbSet<RezervasyonTarife> RezervasyonTarifeler { get; set; }
    public DbSet<Rezervasyon> Rezervasyonlari { get; set; }

    public DbSet<BorcTipi> BorcTipleri { get; set; }
    public DbSet<GenelTarife> GenelTarifeler { get; set; }
    public DbSet<BirimTarife> BirimTarifeler { get; set; }
    public DbSet<SozlesmeTarife> SozlesmeTarifeler { get; set; }

    public DbSet<Tahakkuk> Tahakkuklar { get; set; }
    public DbSet<TahakkukKalemi> TahakkukKalemleri { get; set; }
    public DbSet<KiraOdeme> KiraOdemeler { get; set; }
    public DbSet<Dekont> Dekontlar { get; set; }
    public DbSet<BankaHareketi> BankaHareketleri { get; set; }
    public DbSet<OdemeBankaEslesme> OdemeBankaEslesmeleri { get; set; }
    public DbSet<EnumDegeri> EnumDegerleri { get; set; }
    public DbSet<Rol> Roller { get; set; }
    public DbSet<RolPermission> RolPermissions { get; set; }
    public DbSet<UserRol> UserRoller { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Davetiye> Davetiyeler { get; set; }
    public DbSet<SifreSifirlamaTalebi> SifreSifirlamaTalepleri { get; set; }
    public DbSet<OdemeLinkKayit> OdemeLinkKayitlari { get; set; }

    public DbSet<BelgeTuru> BelgeTurleri { get; set; }
    public DbSet<Belge> Belgeler { get; set; }
    public DbSet<BelgeIcerik> BelgeIcerikleri { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EnumDegeri>(entity =>
        {
            entity.Property(e => e.EnumAdi).HasMaxLength(100);
            entity.Property(e => e.Ad).HasMaxLength(100);
            entity.Property(e => e.Aciklama).HasMaxLength(300);
            entity.HasIndex(e => new { e.EnumAdi, e.Deger }).IsUnique();
        });

        builder.Entity<Tasinmaz>(entity =>
        {
            entity.Property(t => t.Ad).HasMaxLength(200);
            entity.Property(t => t.Il).HasMaxLength(100);
            entity.Property(t => t.Ilce).HasMaxLength(100);
            entity.Property(t => t.Mahalle).HasMaxLength(200);
            entity.Property(t => t.AcikAdres).HasMaxLength(500);
            entity.Property(t => t.AcikYuzolcumu).HasPrecision(18, 2);
            entity.Property(t => t.KapaliYuzolcumu).HasPrecision(18, 2);
            entity.Property(t => t.KiralamaSekli).HasComment(EC<KiralamaSekli>());
            entity.HasOne(t => t.TasinmazTipi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Birim>(entity =>
        {
            entity.Property(b => b.Ad).HasMaxLength(200);
            entity.Property(b => b.BirimNo).HasMaxLength(50);
            entity.Property(b => b.Yuzolcumu).HasPrecision(18, 2);
            entity.Property(b => b.BirimTipi).HasComment(EC<BirimTipi>());
            entity.HasOne(b => b.Tasinmaz)
                  .WithMany(t => t.Birimler)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.BirimTuru)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Kiraci>(entity =>
        {
            entity.Property(k => k.KiraciNo).HasMaxLength(20);
            entity.HasIndex(k => k.KiraciNo).IsUnique();
            entity.Property(k => k.Ad).HasMaxLength(200);
            entity.Property(k => k.Telefon).HasMaxLength(30);
            entity.Property(k => k.Email).HasMaxLength(200);
            entity.Property(k => k.VergiNo).HasMaxLength(20);
            entity.HasIndex(k => k.VergiNo)
                  .IsUnique()
                  .HasFilter("[VergiNo] IS NOT NULL AND [VergiNo] <> ''");
            entity.HasOne(k => k.KiraciKategori)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(k => k.Sektor)
                  .WithMany()
                  .OnDelete(DeleteBehavior.ClientSetNull);
        });

        builder.Entity<KiraSozlesmesi>(entity =>
        {
            entity.Property(s => s.Durum).HasComment(EC<SozlesmeDurumu>());
            entity.HasOne(s => s.Birim)
                  .WithMany(b => b.Sozlesmeler)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.Kiraci)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SozlesmeIslemGecmisi>(entity =>
        {
            entity.Property(g => g.Aciklama).HasMaxLength(1000);
            entity.Property(g => g.IslemTipi).HasComment(EC<SozlesmeIslemTipi>());
            entity.Property(g => g.EskiKiraBedeli).HasPrecision(18, 2);
            entity.Property(g => g.YeniKiraBedeli).HasPrecision(18, 2);
            entity.Property(g => g.TufeOrani).HasPrecision(5, 2);
            entity.Property(g => g.KdvOrani).HasPrecision(5, 2);
            entity.Property(g => g.KdvTutari).HasPrecision(18, 2);
            entity.Property(g => g.KdvDahilTutar).HasPrecision(18, 2);
            entity.HasOne(g => g.KiraSozlesmesi)
                  .WithMany(s => s.IslemGecmisi)
                  .HasForeignKey(g => g.KiraSozlesmesiId)
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
            entity.Property(k => k.AtayanUserId).HasMaxLength(450);
            entity.HasIndex(k => new { k.UserId, k.KapsamTipi, k.KapsamId }).IsUnique();
        });

        builder.Entity<Kategori>(entity =>
        {
            entity.Property(k => k.Ad).HasMaxLength(150);
            entity.Property(k => k.Kod).HasMaxLength(50);
            entity.Property(k => k.Tipi).HasComment("Tasinmaz=1, Kiraci=2, Sektor=3");
            entity.HasIndex(k => new { k.Tipi, k.Kod }).IsUnique();
        });

        builder.Entity<BirimTuru>(entity =>
        {
            entity.Property(b => b.Ad).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Kod).IsRequired().HasMaxLength(20);
            entity.HasIndex(b => b.Kod).IsUnique();

            entity.HasOne(b => b.BorcTipi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TasinmazTarife>(entity =>
        {
            entity.Property(f => f.BirimDeger).HasPrecision(18, 2);
            entity.Property(f => f.KdvOrani).HasPrecision(5, 2);
            entity.HasIndex(f => new { f.TasinmazId, f.KiraciKategoriId, f.BorcTipiId }).IsUnique();
            entity.HasOne(f => f.Tasinmaz)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.KiraciKategori)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(f => f.BorcTipi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BorcTipi>(entity =>
        {
            entity.Property(b => b.Ad).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Kod).IsRequired().HasMaxLength(20);
            entity.HasIndex(b => b.Kod).IsUnique();
            entity.Property(b => b.Davranis).HasComment(EC<BorcTipiDavranisi>());
        });

        builder.Entity<GenelTarife>(entity =>
        {
            entity.Property(k => k.BirimDeger).HasPrecision(18, 4);
            entity.Property(k => k.KdvOrani).HasPrecision(5, 2);
            entity.Property(k => k.HesaplamaYontemi).HasComment(EC<HesaplamaYontemi>());
            entity.HasIndex(k => new { k.Yil, k.KiraciKategoriId, k.BorcTipiId }).IsUnique();
            entity.HasOne(k => k.KiraciKategori)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(k => k.BorcTipi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SozlesmeTarife>(entity =>
        {
            entity.Property(r => r.BirimDeger).HasPrecision(18, 4);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.KiraSozlesmesiId, r.BorcTipiId }).IsUnique();
            entity.HasOne(r => r.KiraSozlesmesi)
                  .WithMany(s => s.SozlesmeTarifeler)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.BorcTipi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BirimTarife>(entity =>
        {
            entity.Property(r => r.BirimDeger).HasPrecision(18, 4);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.BirimId, r.KiraciKategoriId, r.BorcTipiId }).IsUnique();
            entity.HasOne(r => r.Birim)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.KiraciKategori)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.BorcTipi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Tahakkuk>(entity =>
        {
            entity.Property(t => t.BeklenenTutar).HasPrecision(18, 2);
            entity.Property(t => t.Durum).HasComment(EC<TahakkukDurumu>());
            entity.Property(t => t.KaynakTipi).HasComment(EC<TahakkukKaynakTipi>());
            entity.Property(t => t.KdvTutari).HasPrecision(18, 2);
            entity.Property(t => t.ToplamTutar).HasPrecision(18, 2);
            entity.Property(t => t.OdenenTutar).HasPrecision(18, 2);
            entity.Property(t => t.IptalNotu).HasMaxLength(500);
            entity.HasOne(t => t.Kiraci)
                  .WithMany()
                  .HasForeignKey(t => t.KiraciId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.KiraSozlesmesi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => new { t.KiraSozlesmesiId, t.DonemBaslangic });
        });

        builder.Entity<TahakkukKalemi>(entity =>
        {
            entity.Property(k => k.Aciklama).HasMaxLength(200);
            entity.Property(k => k.BirimDeger).HasPrecision(18, 4);
            entity.Property(k => k.HesaplamaYontemi).HasComment(EC<HesaplamaYontemi>());
            entity.Property(k => k.KaynakTipi).HasComment(EC<KalemKaynakTipi>());
            entity.Property(k => k.Carpan).HasPrecision(18, 4);
            entity.Property(k => k.Tutar).HasPrecision(18, 2);
            entity.Property(k => k.KdvOrani).HasPrecision(5, 2);
            entity.Property(k => k.KdvTutari).HasPrecision(18, 2);
            entity.Property(k => k.ToplamTutar).HasPrecision(18, 2);
            entity.HasOne(k => k.Tahakkuk)
                  .WithMany(t => t.Kalemler)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(k => k.BorcTipi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<KiraOdeme>(entity =>
        {
            entity.Property(o => o.Tutar).HasPrecision(18, 2);
            entity.Property(o => o.RedNedeni).HasMaxLength(500);
            entity.Property(o => o.PosReferansNo).HasMaxLength(100);
            entity.Property(o => o.OdemeKanali).HasComment(EC<OdemeKanali>());
            entity.Property(o => o.OdemeKaynakTipi).HasComment(EC<OdemeKaynakTipi>());
            entity.Property(o => o.Durum).HasComment(EC<OdemeDurumu>());
            entity.HasOne(o => o.Tahakkuk)
                  .WithMany(t => t.Odemeler)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.KiraSozlesmesi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(o => o.GirenUser)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.OnaylayanUser)
                  .WithMany()
                  .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Dekont>(entity =>
        {
            entity.Property(d => d.OrijinalDosyaAdi).HasMaxLength(255);
            entity.Property(d => d.DiskDosyaAdi).HasMaxLength(255);
            entity.Property(d => d.DosyaYolu).HasMaxLength(500);
            entity.Property(d => d.DosyaTipi).HasMaxLength(100);
            entity.HasOne(d => d.KiraOdeme)
                  .WithMany(o => o.Dekontlar)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.YukleyenUser)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BankaHareketi>(entity =>
        {
            entity.Property(b => b.Tutar).HasPrecision(18, 2);
            entity.Property(b => b.Bakiye).HasPrecision(18, 2);
            entity.Property(b => b.Aciklama).HasMaxLength(500);
            entity.Property(b => b.EslesmeDurumu).HasComment(EC<BankaEslesmeDurumu>());
            entity.Property(b => b.KarsiHesap).HasMaxLength(50);
            entity.Property(b => b.KarsiUnvan).HasMaxLength(200);
            entity.Property(b => b.BankaKodu).HasMaxLength(20);
            entity.HasOne(b => b.ImportEdenUser)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(b => b.ImportBatchId);
        });

        builder.Entity<OdemeBankaEslesme>(entity =>
        {
            entity.Property(e => e.EslesmeTipi).HasComment(EC<EslesmeTipi>());
            entity.Property(e => e.EslesenTutar).HasPrecision(18, 2);
            entity.HasOne(e => e.KiraOdeme)
                  .WithMany(o => o.BankaEslesmeleri)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BankaHareketi)
                  .WithMany(b => b.OdemeEslesmeleri)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.EslestirenUser)
                  .WithMany()
                  .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<RezervasyonTarife>(entity =>
        {
            entity.Property(r => r.PeriyotUcreti).HasPrecision(18, 2);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.Property(r => r.Aciklama).HasMaxLength(300);
            entity.HasOne(r => r.Birim)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(r => r.BirimTuru)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => new { r.BirimTuruId, r.Yil })
                  .IsUnique()
                  .HasFilter("[BirimId] IS NULL");
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_RezervasyonTarife_BirimOrYilTuru",
                "[BirimId] IS NOT NULL OR ([BirimTuruId] IS NOT NULL AND [Yil] IS NOT NULL)"));
        });

        builder.Entity<Rezervasyon>(entity =>
        {
            entity.Property(r => r.BirimUcret).HasPrecision(18, 2);
            entity.Property(r => r.Durum).HasComment(EC<RezervasyonDurumu>());
            entity.Property(r => r.UcretTutar).HasPrecision(18, 2);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.Property(r => r.KdvTutari).HasPrecision(18, 2);
            entity.Property(r => r.ToplamTutar).HasPrecision(18, 2);
            entity.Property(r => r.Aciklama).HasMaxLength(500);
            entity.Property(r => r.OlusturanUserId).HasMaxLength(450);
            entity.HasOne(r => r.Birim)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Kiraci)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Tahakkuk)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(r => new { r.BirimId, r.BaslangicTarihi });
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne<Kiraci>()
                  .WithMany()
                  .HasForeignKey(u => u.KiraciId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Rol>(entity =>
        {
            entity.Property(r => r.Ad).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Aciklama).HasMaxLength(500);
            entity.Property(r => r.Scope).HasComment(EC<RolScope>());
            entity.HasIndex(r => new { r.Scope, r.KiraciId, r.Ad }).IsUnique();
            entity.HasOne<Kiraci>()
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
            entity.HasIndex(d => d.KiraciId);
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
            entity.HasIndex(o => new { o.KiraciId, o.Durum });
            entity.HasOne(o => o.Kiraci)
                  .WithMany()
                  .HasForeignKey(o => o.KiraciId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BelgeTuru>(entity =>
        {
            entity.Property(b => b.Kod).HasMaxLength(50).IsRequired();
            entity.HasIndex(b => b.Kod).IsUnique();
            entity.Property(b => b.Ad).HasMaxLength(200).IsRequired();
            entity.Property(b => b.Aciklama).HasMaxLength(500);
            entity.Property(b => b.IzinVerilenUzantilar).HasMaxLength(200);
            entity.Property(b => b.HedefEntite).HasComment(EC<BelgeOwnerTipi>());
            entity.HasOne(b => b.SablonBelge)
                  .WithMany()
                  .HasForeignKey(b => b.SablonBelgeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Belge>(entity =>
        {
            entity.Property(b => b.DosyaAdi).HasMaxLength(255).IsRequired();
            entity.Property(b => b.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Aciklama).HasMaxLength(500);
            entity.Property(b => b.OwnerType).HasComment(EC<BelgeOwnerTipi>());
            entity.HasOne(b => b.BelgeTuru)
                  .WithMany()
                  .HasForeignKey(b => b.BelgeTuruId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.DegistirenBelge)
                  .WithMany()
                  .HasForeignKey(b => b.DegistirenBelgeId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(b => new { b.OwnerType, b.OwnerId, b.Gecersiz, b.IsDeleted });
            entity.HasIndex(b => b.BelgeTuruId);
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
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
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
        builder.Entity<Kiraci>().HasQueryFilter(
            k => !k.IsDeleted && (!_currentUser.IsKiraciUser || k.Id == _currentUser.KiraciId));

        builder.Entity<KiraSozlesmesi>().HasQueryFilter(
            s => !s.IsDeleted && (!_currentUser.IsKiraciUser || s.KiraciId == _currentUser.KiraciId));

        builder.Entity<Tahakkuk>().HasQueryFilter(
            t => !t.IsDeleted && (!_currentUser.IsKiraciUser || t.KiraciId == _currentUser.KiraciId));

        builder.Entity<KiraOdeme>().HasQueryFilter(
            o => !o.IsDeleted && (!_currentUser.IsKiraciUser || o.Tahakkuk.KiraciId == _currentUser.KiraciId));

        builder.Entity<Dekont>().HasQueryFilter(
            d => !d.IsDeleted && (!_currentUser.IsKiraciUser || d.KiraOdeme.Tahakkuk.KiraciId == _currentUser.KiraciId));

        builder.Entity<Rezervasyon>().HasQueryFilter(
            r => !r.IsDeleted && (!_currentUser.IsKiraciUser || r.KiraciId == _currentUser.KiraciId));

        builder.Entity<SozlesmeIslemGecmisi>().HasQueryFilter(
            g => !g.IsDeleted && (!_currentUser.IsKiraciUser ||
                 g.KiraSozlesmesi!.KiraciId == _currentUser.KiraciId));

    }

    private static string EC<TEnum>() where TEnum : struct, Enum
        => string.Join(", ", Enum.GetValues<TEnum>().Select(v => $"{v}={(int)(object)v}"));
}
