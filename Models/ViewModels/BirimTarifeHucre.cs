namespace KiraTakip.Models.ViewModels;

public class BirimTarifeHucre
{
    public int RateId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int ChargeTypeId { get; set; }
    public bool OzelFiyatAktif { get; set; }
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }

    // Fallback/Varsayılan değer bilgileri
    public decimal VarsayilanBirimDeger { get; set; }
    public decimal VarsayilanKdvOrani { get; set; }
    public CalculationMethod VarsayilanCalculationMethod { get; set; } = CalculationMethod.Fixed;
    public string VarsayilanKaynak { get; set; } = string.Empty;
}
