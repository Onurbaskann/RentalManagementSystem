using KiraTakip.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class TasinmazDetayViewModel
{
    public Tasinmaz Tasinmaz { get; set; } = null!;
    public List<BirimDetayViewModel> Birimler { get; set; } = new();
    public TasinmazFiyatMatrisiViewModel FiyatMatrisi { get; set; } = new();
    public List<Rezervasyon> Rezervasyonlar { get; set; } = new();
    public List<RezervasyonTarife> BirimRezervasyonKurallari { get; set; } = new();
    public List<BirimOzelFiyatOzeti> BirimOzelFiyatlari { get; set; } = new();
    public Dictionary<int, decimal> SozlesmeAylikBedelleri { get; set; } = new();
}

public class BirimOzelFiyatOzeti
{
    public Birim Birim { get; set; } = null!;
    public List<BirimTarife> Rateler { get; set; } = new();
}

public class BirimDetayViewModel
{
    public Birim Birim { get; set; } = null!;
    public KiraDurumu Durum { get; set; }
    public KiraSozlesmesi? AktifSozlesme { get; set; }
    public decimal AylikBedel { get; set; }
    public RezervasyonTarife? RezKural { get; set; }
}

public class TasinmazEkleViewModel
{
    public string Ad { get; set; } = string.Empty;

    public ParentTarifeKartViewModel? ParentTarife { get; set; }
    public ParentRezervasyonTarifeKartViewModel? ParentRezervasyonTarife { get; set; }

    public int? TasinmazTipiId { get; set; }
    public KiralamaSekli KiralamaSekli { get; set; } = KiralamaSekli.TekParca;

    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;

    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }

    public int? KatSayisi { get; set; }
    public string? Aciklama { get; set; }

    public List<BirimInputViewModel> Birimler { get; set; } = new();
    public List<RezervasyonAlaniInputViewModel> RezervasyonAlanlari { get; set; } = new();

    public TasinmazFiyatMatrisiViewModel FiyatMatrisi { get; set; } = new();
}

public class BirimInputViewModel
{
    [Display(Name = "Birim No")]
    public string BirimNo { get; set; } = string.Empty;
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
