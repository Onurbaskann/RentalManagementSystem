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
    }
}
