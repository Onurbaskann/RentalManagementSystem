using System;
using System.Collections.Generic;

namespace KiraTakip.Models.ViewModels;

public class KiraciOdemePortalViewModel
{
    public int KiraciId { get; set; }
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string GosterimAdi => string.IsNullOrWhiteSpace(Soyad) ? Ad : $"{Ad} {Soyad}";
    public string Email { get; set; } = "";
    public List<BorcKart> Borclar { get; set; } = new();
    public int DefaultSelectedId { get; set; }
}

public class BorcKart
{
    public int TahakkukId { get; set; }
    public string TasinmazAdi { get; set; } = "";
    public string BirimAdi { get; set; } = "";
    public DateTime DonemBaslangic { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar => ToplamTutar - OdenenTutar;
}
