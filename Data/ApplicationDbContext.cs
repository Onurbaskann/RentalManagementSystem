using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models;

namespace KiraTakip.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tasinmaz> Tasinmazlar { get; set; }
    public DbSet<Birim> Birimler { get; set; }
    public DbSet<Kiraci> Kiraciler { get; set; }
    public DbSet<KiraSozlesmesi> Sozlesmeler { get; set; }
    public DbSet<SozlesmeIslemGecmisi> SozlesmeIslemGecmisleri { get; set; }
    public DbSet<UserTasinmazYetki> UserTasinmazYetkileri { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    public DbSet<BorcTipi> BorcTipleri { get; set; }
    public DbSet<Tarife> Tarifeler { get; set; }
    public DbSet<TarifeKalemi> TarifeKalemleri { get; set; }
    public DbSet<BirimRate> BirimRateler { get; set; }
    public DbSet<SozlesmeRate> SozlesmeRateler { get; set; }

    public DbSet<KiraTahakkuk> KiraTahakkuklar { get; set; }
    public DbSet<TahakkukKalemi> TahakkukKalemleri { get; set; }
    public DbSet<KiraOdeme> KiraOdemeler { get; set; }
    public DbSet<Dekont> Dekontlar { get; set; }
    public DbSet<BankaHareketi> BankaHareketleri { get; set; }
    public DbSet<OdemeBankaEslesme> OdemeBankaEslesmeleri { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tasinmaz>(entity =>
        {
            entity.Property(t => t.Ad).HasMaxLength(200);
            entity.Property(t => t.Il).HasMaxLength(100);
            entity.Property(t => t.Ilce).HasMaxLength(100);
            entity.Property(t => t.Mahalle).HasMaxLength(200);
            entity.Property(t => t.AcikAdres).HasMaxLength(500);
            entity.Property(t => t.AcikYuzolcumu).HasPrecision(18, 2);
            entity.Property(t => t.KapaliYuzolcumu).HasPrecision(18, 2);
        });

        builder.Entity<Birim>(entity =>
        {
            entity.Property(b => b.Ad).HasMaxLength(200);
            entity.Property(b => b.OfisNo).HasMaxLength(50);
            entity.Property(b => b.Yuzolcumu).HasPrecision(18, 2);
            entity.HasOne(b => b.Tasinmaz)
                  .WithMany(t => t.Birimler)
                  .HasForeignKey(b => b.TasinmazId)
                  .OnDelete(DeleteBehavior.Cascade);
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
        });

        builder.Entity<KiraSozlesmesi>(entity =>
        {
            entity.Property(s => s.KiraBedeli).HasPrecision(18, 2);
            entity.Property(s => s.Depozito).HasPrecision(18, 2);
            entity.Property(s => s.KdvOrani).HasPrecision(5, 2);
            entity.HasOne(s => s.Birim)
                  .WithMany(b => b.Sozlesmeler)
                  .HasForeignKey(s => s.BirimId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.Kiraci)
                  .WithMany()
                  .HasForeignKey(s => s.KiraciId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SozlesmeIslemGecmisi>(entity =>
        {
            entity.Property(g => g.Aciklama).HasMaxLength(1000);
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
            entity.Property(p => p.Permission).HasMaxLength(100);
        });

        builder.Entity<BorcTipi>(entity =>
        {
            entity.Property(b => b.Ad).HasMaxLength(100);
            entity.Property(b => b.Kod).HasMaxLength(20);
            entity.HasIndex(b => b.Kod).IsUnique();
        });

        builder.Entity<Tarife>(entity =>
        {
            entity.Property(t => t.Aciklama).HasMaxLength(300);
            entity.HasIndex(t => t.Yil).IsUnique();
        });

        builder.Entity<TarifeKalemi>(entity =>
        {
            entity.Property(k => k.BirimDeger).HasPrecision(18, 4);
            entity.Property(k => k.KdvOrani).HasPrecision(5, 2);
            entity.HasIndex(k => new { k.TarifeId, k.BorcTipiId }).IsUnique();
            entity.HasOne(k => k.Tarife)
                  .WithMany(t => t.Kalemler)
                  .HasForeignKey(k => k.TarifeId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(k => k.BorcTipi)
                  .WithMany()
                  .HasForeignKey(k => k.BorcTipiId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SozlesmeRate>(entity =>
        {
            entity.Property(r => r.BirimDeger).HasPrecision(18, 4);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.SozlesmeId, r.BorcTipiId }).IsUnique();
            entity.HasOne(r => r.Sozlesme)
                  .WithMany()
                  .HasForeignKey(r => r.SozlesmeId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.BorcTipi)
                  .WithMany()
                  .HasForeignKey(r => r.BorcTipiId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BirimRate>(entity =>
        {
            entity.Property(r => r.BirimDeger).HasPrecision(18, 4);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.HasIndex(r => new { r.BirimId, r.BorcTipiId }).IsUnique();
            entity.HasOne(r => r.Birim)
                  .WithMany()
                  .HasForeignKey(r => r.BirimId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.BorcTipi)
                  .WithMany()
                  .HasForeignKey(r => r.BorcTipiId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<KiraTahakkuk>(entity =>
        {
            entity.Property(t => t.BeklenenTutar).HasPrecision(18, 2);
            entity.Property(t => t.KdvTutari).HasPrecision(18, 2);
            entity.Property(t => t.ToplamTutar).HasPrecision(18, 2);
            entity.Property(t => t.OdenenTutar).HasPrecision(18, 2);
            entity.HasOne(t => t.KiraSozlesmesi)
                  .WithMany()
                  .HasForeignKey(t => t.KiraSozlesmesiId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => new { t.KiraSozlesmesiId, t.DonemBaslangic }).IsUnique();
        });

        builder.Entity<TahakkukKalemi>(entity =>
        {
            entity.Property(k => k.Aciklama).HasMaxLength(200);
            entity.Property(k => k.BirimDeger).HasPrecision(18, 4);
            entity.Property(k => k.Carpan).HasPrecision(18, 4);
            entity.Property(k => k.Tutar).HasPrecision(18, 2);
            entity.Property(k => k.KdvOrani).HasPrecision(5, 2);
            entity.Property(k => k.KdvTutari).HasPrecision(18, 2);
            entity.Property(k => k.ToplamTutar).HasPrecision(18, 2);
            entity.HasOne(k => k.Tahakkuk)
                  .WithMany(t => t.Kalemler)
                  .HasForeignKey(k => k.TahakkukId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(k => k.BorcTipi)
                  .WithMany()
                  .HasForeignKey(k => k.BorcTipiId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<KiraOdeme>(entity =>
        {
            entity.Property(o => o.Tutar).HasPrecision(18, 2);
            entity.Property(o => o.RedNedeni).HasMaxLength(500);
            entity.HasOne(o => o.KiraTahakkuk)
                  .WithMany(t => t.Odemeler)
                  .HasForeignKey(o => o.KiraTahakkukId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.KiraSozlesmesi)
                  .WithMany()
                  .HasForeignKey(o => o.KiraSozlesmesiId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(o => o.GirenUser)
                  .WithMany()
                  .HasForeignKey(o => o.GirenUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.OnaylayanUser)
                  .WithMany()
                  .HasForeignKey(o => o.OnaylayanUserId)
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
                  .HasForeignKey(d => d.KiraOdemeId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.YukleyenUser)
                  .WithMany()
                  .HasForeignKey(d => d.YukleyenUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BankaHareketi>(entity =>
        {
            entity.Property(b => b.Tutar).HasPrecision(18, 2);
            entity.Property(b => b.Bakiye).HasPrecision(18, 2);
            entity.Property(b => b.Aciklama).HasMaxLength(500);
            entity.Property(b => b.KarsiHesap).HasMaxLength(50);
            entity.Property(b => b.KarsiUnvan).HasMaxLength(200);
            entity.Property(b => b.BankaKodu).HasMaxLength(20);
            entity.HasOne(b => b.ImportEdenUser)
                  .WithMany()
                  .HasForeignKey(b => b.ImportEdenUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(b => b.ImportBatchId);
        });

        builder.Entity<OdemeBankaEslesme>(entity =>
        {
            entity.HasOne(e => e.KiraOdeme)
                  .WithMany(o => o.BankaEslesmeleri)
                  .HasForeignKey(e => e.KiraOdemeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BankaHareketi)
                  .WithMany(b => b.OdemeEslesmeleri)
                  .HasForeignKey(e => e.BankaHareketiId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.EslestirenUser)
                  .WithMany()
                  .HasForeignKey(e => e.EslestirenUserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
