using System.ComponentModel.DataAnnotations;
using KiraTakip.Models;
using Microsoft.AspNetCore.Http;

namespace KiraTakip.Models.ViewModels;

public class OdemeEkleViewModel
{
    public int KiraTahakkukId { get; set; }
    public int? KiraSozlesmesiId { get; set; }

    public DateTime OdemeTarihi { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    public decimal Tutar { get; set; }

    public OdemeKanali OdemeKanali { get; set; } = OdemeKanali.EFT;
    public string? Aciklama { get; set; }

    public KiraTahakkuk? Tahakkuk { get; set; }
}

public class OdemeRedViewModel
{
    public int OdemeId { get; set; }
    public string Neden { get; set; } = string.Empty;
}

public class DekontYukleViewModel
{
    public int OdemeId { get; set; }
    public IFormFile? Dosya { get; set; }
}

public class BankaImportViewModel
{
    public string BankaKodu { get; set; } = "AKBANK";
    public IFormFile? Dosya { get; set; }
}

public class EslesmeViewModel
{
    public int OdemeId { get; set; }
    public int BankaHareketiId { get; set; }
}

public class BankaEslesmeSecViewModel
{
    public BankaHareketi BankaHareketi { get; set; } = null!;
    public List<KiraOdeme> OdemeAdaylari { get; set; } = new();
}

public class OdemeHareketSecViewModel
{
    public KiraOdeme Odeme { get; set; } = null!;
    public List<BankaHareketi> HareketAdaylari { get; set; } = new();
}

public class AylikRaporViewModel
{
    public int Yil { get; set; }
    public List<AylikRaporSatir> Satirlar { get; set; } = new();
    public decimal ToplamBeklenen => Satirlar.Sum(s => s.Beklenen);
    public decimal ToplamTahsil   => Satirlar.Sum(s => s.TahsilEdilen);
    public int ToplamGecikmiş     => Satirlar.Sum(s => s.GecikmisTahakkukAdet);
    public decimal ToplamGecikmisTutar => Satirlar.Sum(s => s.GecikmisTutar);
    public double GenelTahsilOrani => ToplamBeklenen > 0 ? (double)(ToplamTahsil / ToplamBeklenen * 100) : 0;
}

public class AylikRaporSatir
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public int TahakkukSayisi { get; set; }
    public decimal Beklenen { get; set; }
    public decimal TahsilEdilen { get; set; }
    public int GecikmisTahakkukAdet { get; set; }
    public decimal GecikmisTutar { get; set; }
    public double TahsilOrani => Beklenen > 0 ? (double)(TahsilEdilen / Beklenen * 100) : 0;
}
