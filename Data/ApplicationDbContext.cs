using KiraTakip.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace KiraTakip.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
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
    public DbSet<UserTasinmazYetki> UserTasinmazYetkileri { get; set; }
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

    public DbSet<KiraTahakkuk> KiraTahakkuklar { get; set; }
    public DbSet<TahakkukKalemi> TahakkukKalemleri { get; set; }
    public DbSet<KiraOdeme> KiraOdemeler { get; set; }
    public DbSet<Dekont> Dekontlar { get; set; }
    public DbSet<BankaHareketi> BankaHareketleri { get; set; }
    public DbSet<OdemeBankaEslesme> OdemeBankaEslesmeleri { get; set; }
    public DbSet<EnumDegeri> EnumDegerleri { get; set; }

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
            entity.Property(k => k.Soyad).HasMaxLength(200);
            entity.Property(k => k.Telefon).HasMaxLength(30);
            entity.Property(k => k.Email).HasMaxLength(200);
            entity.Property(k => k.TcKimlikNo).HasMaxLength(11);
            entity.Property(k => k.VergiNo).HasMaxLength(20);
            entity.Property(k => k.KiraciTuru).HasComment(EC<KiraciTuru>());
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
            entity.HasOne<KiraSozlesmesi>()
                  .WithMany(s => s.IslemGecmisi)
                  .HasForeignKey(g => g.KiraSozlesmesiId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserTasinmazYetki>(entity =>
        {
            entity.HasIndex(u => new { u.UserId, u.TasinmazId }).IsUnique();
            entity.HasOne<Tasinmaz>()
                  .WithMany()
                  .HasForeignKey(u => u.TasinmazId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserPermission>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.Permission }).IsUnique();
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.Permission).IsRequired().HasMaxLength(100);
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
            entity.Property(f => f.Aciklama).HasMaxLength(300);
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

        builder.Entity<KiraTahakkuk>(entity =>
        {
            entity.Property(t => t.BeklenenTutar).HasPrecision(18, 2);
            entity.Property(t => t.Durum).HasComment(EC<TahakkukDurumu>());
            entity.Property(t => t.KaynakTipi).HasComment(EC<TahakkukKaynakTipi>());
            entity.Property(t => t.KdvTutari).HasPrecision(18, 2);
            entity.Property(t => t.ToplamTutar).HasPrecision(18, 2);
            entity.Property(t => t.OdenenTutar).HasPrecision(18, 2);
            entity.Property(t => t.IptalNotu).HasMaxLength(500);
            entity.HasOne(t => t.KiraSozlesmesi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict);
            // Unique index kaldırıldı: Manuel tahakkuklar aynı sözleşme + dönemde birden fazla olabilir.
            // Otomatik tahakkukların tekliği servis katmanında kod ile korunur.
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
            entity.HasOne(o => o.KiraTahakkuk)
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
            entity.HasOne(r => r.KiraSozlesmesi)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(r => r.KiraTahakkuk)
                  .WithMany()
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(r => new { r.BirimId, r.BaslangicTarihi });
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

    }

    private static string EC<TEnum>() where TEnum : struct, Enum
        => string.Join(", ", Enum.GetValues<TEnum>().Select(v => $"{v}={(int)(object)v}"));
}
