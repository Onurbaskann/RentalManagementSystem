using System;
using System.Collections.Generic;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class SozlesmeEkleViewModel
{
    public int? BirimId { get; set; }
    public int KiraciId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);
    public DueDateRuleType DueDateRuleType { get; set; } = DueDateRuleType.FixedDayOfMonth;
    public int VadeGunu { get; set; } = 1;
    public string? Aciklama { get; set; }
    public List<BirimLookupDto> MevcutBirimler { get; set; } = [];
    public List<KiraciListItemDto> Tenants { get; set; } = [];
    public List<SozlesmeKalemInputDto> SozlesmeKalemleri { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}
