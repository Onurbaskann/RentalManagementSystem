using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models;

public class Kiraci
{
    public int Id { get; set; }

    public string KiraciNo { get; set; } = string.Empty;

    public KiraciTuru KiraciTuru { get; set; }

    // Gerçek kişi için ad, tüzel için firma/kurum adı
    public string Ad { get; set; } = string.Empty;

    // Gerçek kişi alanları
    public string? Soyad { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? PasaportNo { get; set; }
    public string? Unvan { get; set; }
    public string? AnneAdi { get; set; }
    public string? BabaAdi { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public string? DogumYeri { get; set; }

    // Tüzel kişi alanları
    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }

    // İletişim
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }

    public bool KvkkOnayi { get; set; }

    public int? KiraciKategoriId { get; set; }
    public Kategori? Kategori { get; set; }

    public int? SektorId { get; set; }
    public Kategori? SektorKategori { get; set; }

    public DateTime KayitTarihi { get; set; }

    public string GosterimAdi =>
        KiraciTuru == KiraciTuru.Gercek
            ? $"{Ad} {Soyad}".Trim()
            : Ad;
}
