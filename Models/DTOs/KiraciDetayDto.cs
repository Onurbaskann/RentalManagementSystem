namespace KiraTakip.Models.Dtos;

public class KiraciDetayDto
{
    public int Id { get; set; }
    public int? KiraciKategoriId { get; set; }
    public string? KiraciKategoriAd { get; set; }
    public int? SektorId { get; set; }
    public string? SektorAd { get; set; }
    public string KiraciNo { get; set; } = string.Empty;
    public KiraciTuru KiraciTuru { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? Soyad { get; set; }
    public string? TcKimlikNo { get; set; }
    public string? PasaportNo { get; set; }
    public string? Unvan { get; set; }
    public string? AnneAdi { get; set; }
    public string? BabaAdi { get; set; }
    public DateTime? DogumTarihi { get; set; }
    public string? DogumYeri { get; set; }
    public string? TicaretSicilNo { get; set; }
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }
    public bool KvkkOnayi { get; set; }
    public DateTime KayitTarihi { get; set; }

    public string GosterimAdi =>
        KiraciTuru == KiraciTuru.Gercek
            ? $"{Ad} {Soyad}".Trim()
            : Ad;
}
