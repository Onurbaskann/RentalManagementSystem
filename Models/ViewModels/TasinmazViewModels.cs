using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class TasinmazDetayViewModel
{
    public Tasinmaz Tasinmaz { get; set; } = null!;
    public List<BirimDetayViewModel> Birimler { get; set; } = new();
    public TasinmazFiyatMatrisiViewModel FiyatMatrisi { get; set; } = new();
    public List<ToplantiSalonuRezervasyon> Rezervasyonlar { get; set; } = new();
    public RezervasyonUcretKural? GlobalRezervasyonKural { get; set; }
    public List<RezervasyonUcretKural> BirimRezervasyonKurallari { get; set; } = new();
    public List<BirimOzelFiyatOzeti> BirimOzelFiyatlari { get; set; } = new();
}

public class BirimOzelFiyatOzeti
{
    public Birim Birim { get; set; } = null!;
    public List<BirimRate> Rateler { get; set; } = new();
}

public class BirimDetayViewModel
{
    public Birim Birim { get; set; } = null!;
    public KiraDurumu Durum { get; set; }
    public KiraSozlesmesi? AktifSozlesme { get; set; }
    public RezervasyonUcretKural? RezKural { get; set; }
}

public class TasinmazEkleViewModel
{
    [Required(ErrorMessage = "Taşınmaz adı zorunludur.")]
    public string Ad { get; set; } = string.Empty;

    public int? TasinmazTipiId { get; set; }
    public KiralamaSekli KiralamaSekli { get; set; } = KiralamaSekli.TekParca;

    [Required(ErrorMessage = "İl zorunludur.")]
    public string Il { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe zorunludur.")]
    public string Ilce { get; set; } = string.Empty;

    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;

    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }

    public int? KatSayisi { get; set; }
    public string? Aciklama { get; set; }

    public List<OfisBirimInputViewModel> Ofisler { get; set; } = new();
    public List<RezervasyonAlaniInputViewModel> RezervasyonAlanlari { get; set; } = new();

    public TasinmazFiyatMatrisiViewModel FiyatMatrisi { get; set; } = new();
}

public class OfisBirimInputViewModel
{
    public string OfisNo { get; set; } = string.Empty;
    public int? KatNo { get; set; }
    public string? Ad { get; set; }
    public decimal Yuzolcumu { get; set; }
    public string? Aciklama { get; set; }
    public int? BirimTuruId { get; set; }
}

public class RezervasyonAlaniInputViewModel
{
    public string? Ad { get; set; }
    public decimal Yuzolcumu { get; set; }
    public int? BirimTuruId { get; set; }
    public string? Aciklama { get; set; }
    
    public int UcretsizSureDakika { get; set; }
    public decimal SaatlikUcret { get; set; }
    public decimal KdvOrani { get; set; } = 20;
}
