namespace KiraTakip.Models.ViewModels;

public class BirimDuzenleViewModel
{
    public int? Id { get; set; }
    public string UnitNo { get; set; } = string.Empty;
    public int? FloorNo { get; set; }
    public string? Name { get; set; }
    public decimal Area { get; set; }
    public string? Description { get; set; }
    public int? UnitTypeId { get; set; }
    public bool AktifSozlesmesiVar { get; set; }
}
