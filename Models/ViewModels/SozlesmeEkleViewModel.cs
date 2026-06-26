using System;
using System.Collections.Generic;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class SozlesmeEkleViewModel
{
    public int? BirimId { get; set; }
    public int KiraciId { get; set; }
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;
    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddYears(1);
    public VadeKuraliTipi VadeKuraliTipi { get; set; } = VadeKuraliTipi.SabitAyGunu;
    public int VadeGunu { get; set; } = 1;
    public string? Aciklama { get; set; }
    public List<BirimLookupDto> MevcutBirimler { get; set; } = [];
    public List<KiraciListItemDto> Kiraciler { get; set; } = [];
    public List<SozlesmeKalemInputDto> SozlesmeKalemleri { get; set; } = [];
}
