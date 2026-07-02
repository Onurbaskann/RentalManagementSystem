using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class BirimInputViewModel
{
    [Display(Name = "Birim No")]
    public string BirimNo { get; set; } = string.Empty;
    public int? KatNo { get; set; }
    public string? Ad { get; set; }
    public decimal Yuzolcumu { get; set; }
    public string? Aciklama { get; set; }
    public int? UnitTypeId { get; set; }
}
