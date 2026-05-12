using System;
using KiraTakip.Models;

namespace KiraTakip.Models.Dtos;

public class SozlesmeKalemInputDto
{
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = string.Empty;
    public string BorcTipiKod { get; set; } = string.Empty;
    public BorcTipiDavranisi Davranis { get; set; }
    public decimal VarsayilanTutar { get; set; }
    public decimal Tutar { get; set; }
    public decimal KdvOrani { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public bool KullaniciDegistirdiMi { get; set; }
    public bool RateBulundu { get; set; }
    public string? KaynakTipi { get; set; }
}
