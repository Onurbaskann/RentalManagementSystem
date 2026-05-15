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

    public DbSet<TasinmazTipi> TasinmazTipleri { get; set; }
    public DbSet<TasinmazTipiKiralamaSekli> TasinmazTipiKiralamaSekilleri { get; set; }
    public DbSet<BirimTuru> BirimTurleri { get; set; }
    public DbSet<KiraciKategori> KiraciKategorileri { get; set; }
    public DbSet<Sektor> Sektorler { get; set; }

    public DbSet<TasinmazKiraciKategoriFiyat> TasinmazKiraciKategoriFiyatlari { get; set; }

    public DbSet<RezervasyonUcretKural> RezervasyonUcretKurallari { get; set; }
    public DbSet<ToplantiSalonuRezervasyon> ToplantiSalonuRezervasyonlari { get; set; }
    public DbSet<RezervasyonGenelTarife> RezervasyonGenelTarifeleri { get; set; }

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
            entity.HasOne(t => t.TasinmazTipi)
                  .WithMany()
                  .HasForeignKey(t => t.TasinmazTipiId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Birim>(entity =>
        {
            entity.Property(b => b.Ad).HasMaxLength(200);
            entity.Property(b => b.BirimNo).HasMaxLength(50);
            entity.Property(b => b.Yuzolcumu).HasPrecision(18, 2);
            entity.HasOne(b => b.Tasinmaz)
                  .WithMany(t => t.Birimler)
                  .HasForeignKey(b => b.TasinmazId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.BirimTuru)
                  .WithMany()
                  .HasForeignKey(b => b.BirimTuruId)
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
            entity.HasOne(k => k.KiraciKategori)
                  .WithMany()
                  .HasForeignKey(k => k.KiraciKategoriId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(k => k.Sektor)
                  .WithMany()
                  .HasForeignKey(k => k.SektorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<KiraSozlesmesi>(entity =>
        {
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

        builder.Entity<TasinmazTipi>(entity =>
        {
            entity.Property(t => t.Ad).HasMaxLength(100);
            entity.Property(t => t.Kod).HasMaxLength(20);
            entity.HasIndex(t => t.Kod).IsUnique();
        });

        builder.Entity<TasinmazTipiKiralamaSekli>(entity =>
        {
            entity.HasIndex(t => new { t.TasinmazTipiId, t.KiralamaSekli }).IsUnique();
            entity.HasOne(t => t.TasinmazTipi)
                  .WithMany(t => t.KiralamaSekilleri)
                  .HasForeignKey(t => t.TasinmazTipiId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BirimTuru>(entity =>
        {
            entity.Property(b => b.Ad).HasMaxLength(100);
            entity.Property(b => b.Kod).HasMaxLength(20);
            entity.HasIndex(b => b.Kod).IsUnique();

            entity.HasOne(b => b.BorcTipi)
                  .WithMany()
                  .HasForeignKey(b => b.BorcTipiId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<KiraciKategori>(entity =>
        {
            entity.Property(k => k.Ad).HasMaxLength(100);
            entity.Property(k => k.Kod).HasMaxLength(20);
            entity.HasIndex(k => k.Kod).IsUnique();
        });

        builder.Entity<Sektor>(entity =>
        {
            entity.Property(s => s.Ad).HasMaxLength(100);
            entity.Property(s => s.Kod).HasMaxLength(20);
            entity.HasIndex(s => s.Kod).IsUnique();
        });

        builder.Entity<TasinmazKiraciKategoriFiyat>(entity =>
        {
            entity.Property(f => f.BirimDeger).HasPrecision(18, 2);
            entity.Property(f => f.KdvOrani).HasPrecision(5, 2);
            entity.Property(f => f.Aciklama).HasMaxLength(300);
            entity.HasIndex(f => new { f.TasinmazId, f.KiraciKategoriId, f.BorcTipiId }).IsUnique();
            entity.HasOne(f => f.Tasinmaz)
                  .WithMany()
                  .HasForeignKey(f => f.TasinmazId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.KiraciKategori)
                  .WithMany()
                  .HasForeignKey(f => f.KiraciKategoriId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(f => f.BorcTipi)
                  .WithMany()
                  .HasForeignKey(f => f.BorcTipiId)
                  .OnDelete(DeleteBehavior.Restrict);
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
            entity.HasIndex(k => new { k.TarifeId, k.KiraciKategoriId, k.BorcTipiId }).IsUnique();
            entity.HasOne(k => k.Tarife)
                  .WithMany(t => t.Kalemler)
                  .HasForeignKey(k => k.TarifeId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(k => k.KiraciKategori)
                  .WithMany()
                  .HasForeignKey(k => k.KiraciKategoriId)
                  .OnDelete(DeleteBehavior.Restrict);
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
                  .WithMany(s => s.SozlesmeRateler)
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
            entity.HasIndex(r => new { r.BirimId, r.KiraciKategoriId, r.BorcTipiId }).IsUnique();
            entity.HasOne(r => r.Birim)
                  .WithMany()
                  .HasForeignKey(r => r.BirimId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.KiraciKategori)
                  .WithMany()
                  .HasForeignKey(r => r.KiraciKategoriId)
                  .OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(t => t.IptalNotu).HasMaxLength(500);
            entity.HasOne(t => t.KiraSozlesmesi)
                  .WithMany()
                  .HasForeignKey(t => t.KiraSozlesmesiId)
                  .OnDelete(DeleteBehavior.Restrict);
            // Unique index kaldırıldı: Manuel tahakkuklar aynı sözleşme + dönemde birden fazla olabilir.
            // Otomatik tahakkukların tekliği servis katmanında kod ile korunur.
            entity.HasIndex(t => new { t.KiraSozlesmesiId, t.DonemBaslangic });
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

        builder.Entity<RezervasyonUcretKural>(entity =>
        {
            entity.Property(r => r.PeriyotUcreti).HasPrecision(18, 2);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.Property(r => r.Aciklama).HasMaxLength(300);
            entity.HasOne(r => r.Birim)
                  .WithMany()
                  .HasForeignKey(r => r.BirimId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ToplantiSalonuRezervasyon>(entity =>
        {
            entity.Property(r => r.BirimUcret).HasPrecision(18, 2);
            entity.Property(r => r.UcretTutar).HasPrecision(18, 2);
            entity.Property(r => r.KdvOrani).HasPrecision(5, 2);
            entity.Property(r => r.KdvTutari).HasPrecision(18, 2);
            entity.Property(r => r.ToplamTutar).HasPrecision(18, 2);
            entity.Property(r => r.Aciklama).HasMaxLength(500);
            entity.Property(r => r.OlusturanUserId).HasMaxLength(450);
            entity.HasOne(r => r.Birim)
                  .WithMany()
                  .HasForeignKey(r => r.BirimId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Kiraci)
                  .WithMany()
                  .HasForeignKey(r => r.KiraciId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.KiraSozlesmesi)
                  .WithMany()
                  .HasForeignKey(r => r.KiraSozlesmesiId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(r => r.KiraTahakkuk)
                  .WithMany()
                  .HasForeignKey(r => r.KiraTahakkukId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(r => new { r.BirimId, r.BaslangicTarihi });
        });

        builder.Entity<RezervasyonGenelTarife>(e =>
        {
            e.HasIndex(r => new { r.TarifeId, r.BirimTuruId }).IsUnique();
            e.Property(r => r.PeriyotUcreti).HasPrecision(18, 2);
            e.Property(r => r.KdvOrani).HasPrecision(5, 2);
            e.HasOne(r => r.Tarife).WithMany().HasForeignKey(r => r.TarifeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.BirimTuru).WithMany().HasForeignKey(r => r.BirimTuruId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
