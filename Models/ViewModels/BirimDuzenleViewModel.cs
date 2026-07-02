namespace KiraTakip.Models.ViewModels;

public class BirimDuzenleViewModel
{
    public int? Id { get; set; }
    public string BirimNo { get; set; } = string.Empty;
    public int? KatNo { get; set; }
    public string? Ad { get; set; }
    public decimal Yuzolcumu { get; set; }
    public string? Aciklama { get; set; }
    public int? UnitTypeId { get; set; }
    public bool AktifSozlesmesiVar { get; set; }
}
