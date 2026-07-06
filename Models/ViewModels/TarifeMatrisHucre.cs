namespace KiraTakip.Models.ViewModels;

public class TarifeMatrisHucre
{
    public int KalemId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int ChargeTypeId { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
}
