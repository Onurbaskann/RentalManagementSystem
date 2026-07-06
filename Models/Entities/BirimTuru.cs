namespace KiraTakip.Models.Entities;

public class UnitType : BaseEntity
{
    public int? ChargeTypeId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public bool KiralanabilirMi { get; set; } = true;
    public bool RezervasyonYapilabilirMi { get; set; } = false;
    public int Sira { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    public ChargeType? ChargeType { get; set; }
}